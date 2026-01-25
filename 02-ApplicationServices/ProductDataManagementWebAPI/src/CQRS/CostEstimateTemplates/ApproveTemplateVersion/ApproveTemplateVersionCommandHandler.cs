using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.ApproveTemplateVersion
{
    /// <summary>
    /// Handler dla zatwierdzenia wersji szablonu kosztorysu
    /// </summary>
    public class ApproveTemplateVersionCommandHandler : IRequestHandler<ApproveTemplateVersionCommand, Unit>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateVersion> versionRepository;
        private readonly ICurrentUser currentUser;

        public ApproveTemplateVersionCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateVersion> versionRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.versionRepository = versionRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(ApproveTemplateVersionCommand request, CancellationToken cancellationToken)
        {
            // Get template to verify ownership
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted);

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Get version with template to verify it belongs to this template
            var version = await versionRepository.GetFirstBySearch(
                v => v.Id == request.VersionId && v.TemplateId == request.TemplateId);

            if (version == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplateVersion), request.VersionId.ToString());
            }

            // Check if version is already approved
            if (version.Status == TemplateVersionStatus.Approved)
            {
                throw new ValidationApiException("This version is already approved.");
            }

            // Approve the version
            version.Status = TemplateVersionStatus.Approved;
            version.ApprovedAt = DateTime.UtcNow;
            version.ApprovedById = currentUser.Id;

            await versionRepository.Update(version);
            await versionRepository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
