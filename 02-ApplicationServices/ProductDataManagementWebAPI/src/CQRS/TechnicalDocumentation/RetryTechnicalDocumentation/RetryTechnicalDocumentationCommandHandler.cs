using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.TechnicalDocumentation;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.TechnicalDocumentation.RetryTechnicalDocumentation;

public sealed class RetryTechnicalDocumentationCommandHandler
    : IRequestHandler<RetryTechnicalDocumentationCommand, Unit>
{
    private readonly IRepository<ProjectTechnicalDocumentation> documentationRepository;
    private readonly IQueuedTechnicalDocumentationSender queueSender;
    private readonly ICurrentUser currentUser;

    public RetryTechnicalDocumentationCommandHandler(
        IRepository<ProjectTechnicalDocumentation> documentationRepository,
        IQueuedTechnicalDocumentationSender queueSender,
        ICurrentUser currentUser)
    {
        this.documentationRepository = documentationRepository;
        this.queueSender = queueSender;
        this.currentUser = currentUser;
    }

    public async Task<Unit> Handle(
        RetryTechnicalDocumentationCommand request,
        CancellationToken cancellationToken)
    {
        ProjectTechnicalDocumentation documentation = await GetDocumentationAsync(request, cancellationToken);

        if (documentation.Status != TechnicalDocumentationStatus.Failed)
        {
            throw new ConflictApiException(
                nameof(ProjectTechnicalDocumentation),
                request.DocumentationId.ToString(),
                "Retry is only allowed when documentation status is Failed.");
        }

        documentation.Status = TechnicalDocumentationStatus.Pending;
        documentation.ErrorMessage = null;
        documentation.CompletedAt = null;
        documentation.DetailsJson = null;

        await documentationRepository.Update(documentation);
        await documentationRepository.SaveChangesAsync(cancellationToken);

        await queueSender.EnqueueAsync(
            documentation.Id,
            request.TenantId,
            request.ProjectId,
            currentUser.Id,
            isManualRetry: true,
            cancellationToken);

        return Unit.Value;
    }

    private async Task<ProjectTechnicalDocumentation> GetDocumentationAsync(
        RetryTechnicalDocumentationCommand request,
        CancellationToken cancellationToken)
    {
        ProjectTechnicalDocumentation? documentation = await documentationRepository.GetFirstBySearch(
            d => d.TenantId == request.TenantId
                && d.ProjectId == request.ProjectId
                && d.Id == request.DocumentationId);

        if (documentation is null)
        {
            throw new NotFoundApiException(
                nameof(ProjectTechnicalDocumentation),
                request.DocumentationId.ToString());
        }

        return documentation;
    }
}
