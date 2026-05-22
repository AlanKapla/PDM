using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Enums;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    public sealed class UpdateProjectMemberRoleCommandHandler : IRequestHandler<UpdateProjectMemberRoleCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly IUserService userService;

        public UpdateProjectMemberRoleCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IReadRepository<Role> roleRepo,
            IReadRepository<Notification> notificationRepo,
            IPermissionsVersionService permissionsVersionService,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            IUserService userService)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.roleRepo = roleRepo;
            this.notificationRepo = notificationRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.userService = userService;
        }

        public async Task<Unit> Handle(UpdateProjectMemberRoleCommand request, CancellationToken cancellationToken)
        {
            // NOTE: self-edit guard is enforced by UpdateProjectMemberRoleCommandValidator (NotCurrentUser).

            Project project = await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            ProjectMember projectMember = await projectMemberRepo.GetFirstBySearch(
                m => m.ProjectId == request.ProjectId
                    && m.TenantId == request.TenantId
                    && m.UserId == request.UserId)
                ?? throw new NotFoundApiException(nameof(ProjectMember), $"Project: {request.ProjectId}, User: {request.UserId}");

            ProjectMemberUserInfo? targetUser = await userService.GetProjectMemberAsync(
                request.TenantId, request.ProjectId, request.UserId, cancellationToken);

            Role newRole = await roleRepo.GetFirstBySearch(
                r => r.Id == request.RoleId && r.Scope == RoleScope.Project && r.IsActive,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(Role), request.RoleId.ToString());

            Guid? oldRoleId = projectMember.RoleId;
            projectMember.RoleId = newRole.Id;

            await projectMemberRepo.Update(projectMember);
            await userService.InvalidateProjectMembersCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

            // Bump permissions version for the user whose role changed
            await permissionsVersionService.BumpVersionAsync(request.UserId, cancellationToken);

            NotificationDto notification = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = request.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = NotificationType.Info,
                Title = "Zmieniono Twoją rolę w projekcie",
                Message = $"Twoja rola w projekcie '{project.Name}' została zmieniona na {newRole.Name}.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "projectId", request.ProjectId },
                    { "projectName", project.Name },
                    { "oldRoleId", oldRoleId },
                    { "newRoleId", newRole.Id },
                    { "newRoleCode", newRole.Code },
                    { "newRoleName", newRole.Name },
                    { "changedByUserId", currentUser.Id }
                }
            };

            NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            return Unit.Value;
        }
    }
}
