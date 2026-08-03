using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.GetAdditionalFields;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.ExportCostEstimate
{
    public sealed class ExportCostEstimateQueryHandler
        : IRequestHandler<ExportCostEstimateQuery, CostEstimateExportFile>
    {
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICostEstimateExportService exportService;
        private readonly IReadRepository<ProjectCurrency> projectCurrencyRepository;
        private readonly IReadRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly ICurrentUser currentUser;

        public ExportCostEstimateQueryHandler(
            ICostEstimateCacheService ceCacheService,
            ICostEstimateAccessService ceAccessService,
            ICostEstimateExportService exportService,
            IReadRepository<ProjectCurrency> projectCurrencyRepository,
            IReadRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            ICurrentUser currentUser)
        {
            this.ceCacheService = ceCacheService;
            this.ceAccessService = ceAccessService;
            this.exportService = exportService;
            this.projectCurrencyRepository = projectCurrencyRepository;
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.currentUser = currentUser;
        }

        public async Task<CostEstimateExportFile> Handle(
            ExportCostEstimateQuery request,
            CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await GetCostEstimateOrThrowAsync(request, cancellationToken);
            await EnsureAccessAllowedAsync(request, cancellationToken);

            Dictionary<Guid, CostEstimateGroup> groupsDict = await ceCacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            Dictionary<Guid, CostEstimateItem> itemsDict = await ceCacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            ProjectCurrency? projectCurrency = await projectCurrencyRepository.GetFirstBySearch(
                c => c.ProjectId == costEstimate.ProjectId,
                cancellationToken);

            List<CostEstimateFieldSchemaWeb> fieldSchemas = await LoadFieldSchemasAsync(request.CostEstimateId);

            return exportService.Export(
                costEstimate,
                groupsDict.Values.ToList(),
                itemsDict.Values.ToList(),
                fieldSchemas,
                projectCurrency?.Code,
                projectCurrency?.Symbol,
                request.Format);
        }

        private async Task<CostEstimate> GetCostEstimateOrThrowAsync(
            ExportCostEstimateQuery request,
            CancellationToken cancellationToken)
        {
            CostEstimate? costEstimate = await ceCacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (costEstimate is null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            return costEstimate;
        }

        private async Task EnsureAccessAllowedAsync(
            ExportCostEstimateQuery request,
            CancellationToken cancellationToken)
        {
            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
            {
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");
            }
        }

        private async Task<List<CostEstimateFieldSchemaWeb>> LoadFieldSchemasAsync(Guid costEstimateId)
        {
            IEnumerable<CostEstimateFieldSchema> fieldSchemas = await fieldSchemaRepository.GetBySearch(
                f => f.CostEstimateId == costEstimateId);

            return fieldSchemas
                .OrderBy(f => f.Order)
                .Select(GetAdditionalFieldsQueryHandler.MapToFieldSchemaWeb)
                .ToList();
        }
    }
}
