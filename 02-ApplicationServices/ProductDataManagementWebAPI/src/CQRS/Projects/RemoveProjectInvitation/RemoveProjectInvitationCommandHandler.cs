using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.RemoveProjectInvitation;

public sealed class RemoveProjectInvitationCommandHandler : IRequestHandler<RemoveProjectInvitationCommand, Unit>
{
    private readonly IRepository<TenantInvitation> invitationRepo;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<RemoveProjectInvitationCommandHandler> logger;

    public RemoveProjectInvitationCommandHandler(
        IRepository<TenantInvitation> invitationRepo,
        ICurrentUser currentUser,
        ILogger<RemoveProjectInvitationCommandHandler> logger)
    {
        this.invitationRepo = invitationRepo;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(RemoveProjectInvitationCommand request, CancellationToken cancellationToken)
    {
        TenantInvitation invitation = await invitationRepo.GetFirstBySearch(
            i => i.Id == request.InvitationId
                && i.TenantId == request.TenantId
                && i.ProjectId == request.ProjectId)
            ?? throw new NotFoundApiException(nameof(TenantInvitation), request.InvitationId.ToString());

        invitation.IsActive = false;
        invitation.Status = InvitationStatus.Revoked;
        await invitationRepo.Update(invitation);

        logger.LogInformation(
            "Project invitation {InvitationId} for project {ProjectId} revoked by user {UserId}",
            request.InvitationId,
            request.ProjectId,
            currentUser.Id);

        return Unit.Value;
    }
}
