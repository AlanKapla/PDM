using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Handler dla pobrania szczegółów kosztorysu
    /// Returns cost estimate with full hierarchy of groups and work scope items + template structure
    /// </summary>
    public class GetCostEstimateDetailsQueryHandler : IRequestHandler<GetCostEstimateDetailsQuery, CostEstimateDetailsWeb>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly ITemplateStructureService templateStructureService;
        private readonly ICurrentUser currentUser;

        public GetCostEstimateDetailsQueryHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            ITemplateStructureService templateStructureService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.templateStructureService = templateStructureService;
            this.currentUser = currentUser;
        }

        public async Task<CostEstimateDetailsWeb> Handle(GetCostEstimateDetailsQuery request, CancellationToken cancellationToken)
        {
            // Get cost estimate with all related data
            var costEstimates = await costEstimateRepository.GetBySearch(
                c => c.Id == request.CostEstimateId && 
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     !c.IsDeleted,
                q => q.Include(c => c.Template)
                      .Include(c => c.TemplateVersion)
                      .Include(c => c.Owner)
                      .Include(c => c.SelectedCurrency)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.FieldValues)
                              .ThenInclude(fv => fv.FieldDefinition)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.Items.Where(w => !w.IsDeleted))
                              .ThenInclude(w => w.FieldValues)
                                  .ThenInclude(fv => fv.FieldDefinition)
                      .Include(c => c.AllGroups.Where(g => !g.IsDeleted))
                          .ThenInclude(g => g.Items.Where(w => !w.IsDeleted))
                              .ThenInclude(w => w.Options.Where(o => !o.IsDeleted))
                                  .ThenInclude(o => o.FieldValues)
                                      .ThenInclude(fv => fv.FieldDefinition)
                );
                
            var costEstimate = costEstimates.FirstOrDefault();

            if (costEstimate == null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            // Pobierz strukturę szablonu przez wspólny serwis
            var templateStructure = await templateStructureService.BuildCostEstimateTemplateStructureAsync(
                costEstimate.Template, 
                costEstimate.TemplateVersion, 
                cancellationToken);

            // Build hierarchical structure of root groups
            var rootGroups = BuildGroupHierarchy(costEstimate.AllGroups.Where(g => g.ParentGroupId == null).ToList(), costEstimate.AllGroups.ToList());

            return new CostEstimateDetailsWeb(
                Id: costEstimate.Id,
                TenantId: costEstimate.TenantId,
                ProjectId: costEstimate.ProjectId,
                TemplateId: costEstimate.TemplateId,
                TemplateName: costEstimate.Template.Name,
                TemplateVersionId: costEstimate.TemplateVersionId,
                TemplateVersionNumber: costEstimate.TemplateVersion.VersionNumber,
                SelectedCurrencyId: costEstimate.SelectedCurrencyId,
                SelectedCurrencyCode: costEstimate.SelectedCurrency.Code,
                SelectedCurrencySymbol: costEstimate.SelectedCurrency.Symbol,
                Name: costEstimate.Name,
                Description: costEstimate.Description,
                Status: costEstimate.Status,
                RootGroups: rootGroups,
                TotalNet: costEstimate.TotalNet,
                TotalGross: costEstimate.TotalGross,
                TotalVat: costEstimate.TotalVat,
                CreatedAt: costEstimate.CreatedAt,
                UpdatedAt: costEstimate.UpdatedAt,
                LastCalculatedAt: costEstimate.LastCalculatedAt,
                OwnerId: costEstimate.OwnerId,
                OwnerName: $"{costEstimate.Owner.FirstName} {costEstimate.Owner.LastName}",
                TemplateStructure: templateStructure
            );
        }

        private List<CostEstimateGroupWeb> BuildGroupHierarchy(List<CostEstimateGroup> currentLevelGroups, List<CostEstimateGroup> allGroups)
        {
            return currentLevelGroups
                .OrderBy(g => g.Order)
                .Select(group => new CostEstimateGroupWeb(
                    Id: group.Id,
                    ParentGroupId: group.ParentGroupId,
                    Level: group.Level,
                    Order: group.Order,
                    FieldValues: group.FieldValues.Select(fv => new CostEstimateGroupFieldValueWeb(
                        Id: fv.Id,
                        FieldDefinitionId: fv.FieldDefinitionId,
                        FieldType: (int)fv.FieldDefinition.FieldType,
                        FieldScope: (int)fv.FieldDefinition.FieldScope,
                        FieldLabel: fv.FieldDefinition.Label,
                        Value: fv.Value
                    )).ToList(),
                    TotalNet: group.TotalNet,
                    TotalGross: group.TotalGross,
                    TotalVat: group.TotalVat,
                    LastCalculatedAt: group.LastCalculatedAt,
                    ChildGroups: BuildGroupHierarchy(
                        allGroups.Where(g => g.ParentGroupId == group.Id).ToList(),
                        allGroups
                    ),
                    Items: group.Items
                        .Where(w => w.ParentItemId == null)  // Tylko główne pozycje (nie opcje)
                        .OrderBy(w => w.Order)
                        .Select(item => BuildItemWeb(item))
                        .ToList(),
                    CreatedAt: group.CreatedAt,
                    UpdatedAt: group.UpdatedAt
                ))
                .ToList();
        }

        private CostEstimateItemWeb BuildItemWeb(CostEstimateItem item)
        {
            return new CostEstimateItemWeb(
                Id: item.Id,
                GroupId: item.GroupId,
                ParentItemId: item.ParentItemId,
                Order: item.Order,
                FieldValues: item.FieldValues.Select(fv => new CostEstimateItemFieldValueWeb(
                    Id: fv.Id,
                    FieldDefinitionId: fv.FieldDefinitionId,
                    FieldType: (int)fv.FieldDefinition.FieldType,
                    FieldScope: (int)fv.FieldDefinition.FieldScope,
                    FieldName: fv.FieldDefinition.FieldName,
                    FieldLabel: fv.FieldDefinition.Label,
                    Value: fv.Value
                )).ToList(),
                Options: item.Options
                    .Where(o => !o.IsDeleted)
                    .OrderBy(o => o.Order)
                    .Select(option => BuildItemWeb(option))
                    .ToList(),
                CreatedAt: item.CreatedAt,
                UpdatedAt: item.UpdatedAt
            );
        }
    }
}
