using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using MediatR;
using Repositories.Repository.Interfaces;
using System.Text.Json;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplate
{
    /// <summary>
    /// Handler dla tworzenia szablonu kosztorysu
    /// Tworzy tylko minimalny szablon (nazwa, opis) i pierwszą wersję Draft
    /// Cała struktura (pola, waluty, jednostki, konfiguracje) jest dodawana przez UpdateCostEstimateTemplate
    /// </summary>
    public class CreateCostEstimateTemplateCommandHandler : IRequestHandler<CreateCostEstimateTemplateCommand, Guid>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateVersion> versionRepository;
        private readonly ICurrentUser currentUser;

        public CreateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateVersion> versionRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.versionRepository = versionRepository;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            // Create minimal template entity
            var template = new CostEstimateTemplate
            {
                Id = Guid.NewGuid(),
                OwnerId = currentUser.Id,
                Name = request.Name,
                Description = request.Description,
                Category = null,
                CreatedAt = now,
                IsDeleted = false
            };

            await templateRepository.Insert(template);
            await templateRepository.SaveChangesAsync(cancellationToken);

            // Create initial empty version (Draft) with default configuration
            var version = new CostEstimateTemplateVersion
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                VersionNumber = 1,
                VersionName = "Initial version",
                Status = TemplateVersionStatus.Draft,
                CanAddGroups = true,
                CanBranchGroups = true,
                MaxGroupLevel = null,
                AutoNumberGroups = false,
                GroupNumberFormat = null,
                CreatedAt = now,
                IsDeleted = false
            };

            await versionRepository.Insert(version);
            await versionRepository.SaveChangesAsync(cancellationToken);

            return template.Id;
        }
    }
}
