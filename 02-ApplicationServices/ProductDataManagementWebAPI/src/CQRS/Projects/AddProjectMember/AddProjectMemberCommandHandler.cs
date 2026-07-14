using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.AddProjectMember;

public sealed class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, Unit>
{
    private readonly IReadRepository<Project> projectRepo;
    private readonly IReadRepository<Notification> notificationRepo;
    private readonly IProjectMembershipProvisioner membershipProvisioner;
    private readonly INotificationSender notificationSender;
    private readonly ICurrentUser currentUser;
    private readonly IUserService userService;

    public AddProjectMemberCommandHandler(
        IReadRepository<Project> projectRepo,
        IReadRepository<Notification> notificationRepo,
        INotificationSender notificationSender,
        ICurrentUser currentUser,
        IUserService userService,
        IProjectMembershipProvisioner membershipProvisioner)
    {
        this.projectRepo = projectRepo;
        this.notificationRepo = notificationRepo;
        this.notificationSender = notificationSender;
        this.currentUser = currentUser;
        this.userService = userService;
        this.membershipProvisioner = membershipProvisioner;
    }

    public async Task<Unit> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        Project project = await projectRepo.GetFirstBySearch(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)
            ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

        ProjectMemberUserInfo? targetUser = await userService.GetTenantMemberInfoAsync(
            request.TenantId, request.UserId, cancellationToken);

        await membershipProvisioner.ProvisionProjectMemberAsync(
            request.TenantId,
            request.ProjectId,
            request.UserId,
            isAdmin: false,
            request.Modules,
            cancellationToken);

        NotificationDto notification = new NotificationDto
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ProjectId = request.ProjectId,
            UserId = request.UserId,
            AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
            Type = NotificationType.Info,
            Title = "Dodano do projektu",
            Message = $"Zostałeś dodany do projektu: {project.Name}",
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            Metadata = new Dictionary<string, object?>
            {
                { "projectId", request.ProjectId },
                { "projectName", project.Name },
                { "addedByUserId", currentUser.Id }
            }
        };

        NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(
            notification,
            notificationRepo,
            cancellationToken);
        await notificationSender.EnqueueAsync(payload, cancellationToken);

        return Unit.Value;
    }
}
