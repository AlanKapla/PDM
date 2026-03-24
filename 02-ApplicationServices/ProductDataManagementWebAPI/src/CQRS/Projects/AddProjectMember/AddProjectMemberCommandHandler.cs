using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.AddProjectMember
{
    public class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly IUserService userService;

        public AddProjectMemberCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IReadRepository<Role> roleRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            IUserService userService)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.roleRepo = roleRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.userService = userService;
        }

        public async Task<Unit> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
        {
            Project project = await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            var viewerRole = await roleRepo.GetFirstBySearch(
                r => r.Scope == RoleScope.Project && r.Code == RoleCodes.ProjectViewer && r.IsActive,
                cancellationToken)
                ?? throw new InvalidOperationException($"{RoleCodes.ProjectViewer} role not found");

            var targetUser = await userService.GetTenantMemberInfoAsync(
                request.TenantId, request.UserId, cancellationToken);

            ProjectMember newMember = new ProjectMember
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = request.UserId,
                RoleId = viewerRole.Id,
                JoinedAt = DateTime.UtcNow
            };

            await projectMemberRepo.Insert(newMember);
            await userService.InvalidateProjectMembersCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

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
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "projectId", request.ProjectId },
                    { "projectName", project.Name },
                    { "addedByUserId", currentUser.Id }
                }
            };

            var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            return Unit.Value;
        }
    }
}
