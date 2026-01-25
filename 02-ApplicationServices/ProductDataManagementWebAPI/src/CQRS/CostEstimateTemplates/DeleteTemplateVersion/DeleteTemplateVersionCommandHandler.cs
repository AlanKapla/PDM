using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.DeleteTemplateVersion
{
    /// <summary>
    /// Handler dla usunięcia wersji szablonu kosztorysu (soft delete)
    /// Po usunięciu wersji, pozostałe wersje są renumerowane według daty utworzenia
    /// </summary>
    public class DeleteTemplateVersionCommandHandler : IRequestHandler<DeleteTemplateVersionCommand, Unit>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateVersion> versionRepository;
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly ICurrentUser currentUser;

        public DeleteTemplateVersionCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateVersion> versionRepository,
            IRepository<CostEstimate> costEstimateRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.versionRepository = versionRepository;
            this.costEstimateRepository = costEstimateRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteTemplateVersionCommand request, CancellationToken cancellationToken)
        {
            // Get template to verify ownership
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted,
                q => q.Include(t => t.Versions.Where(v => !v.IsDeleted)));

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Get version to delete (only Draft versions can be deleted)
            var versionToDelete = template.Versions
                .FirstOrDefault(v => v.Id == request.VersionId && v.Status == TemplateVersionStatus.Draft);

            if (versionToDelete == null)
            {
                throw new ValidationApiException("Only Draft versions can be deleted.");
            }

            // Soft delete the version
            versionToDelete.IsDeleted = true;
            versionToDelete.DeletedAt = DateTime.UtcNow;
            await versionRepository.Update(versionToDelete);

            return Unit.Value;
        }
    }
}
