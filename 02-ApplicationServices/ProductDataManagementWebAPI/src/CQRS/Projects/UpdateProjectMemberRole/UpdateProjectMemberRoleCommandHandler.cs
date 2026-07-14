using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Enums;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    public sealed class UpdateProjectMemberRoleCommandHandler : IRequestHandler<UpdateProjectMemberRoleCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<ProjectMemberModulePermission> modulePermissionRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly IUserService userService;

        public UpdateProjectMemberRoleCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<ProjectMemberModulePermission> modulePermissionRepo,
            IReadRepository<Notification> notificationRepo,
            IPermissionsVersionService permissionsVersionService,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            IUserService userService)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.modulePermissionRepo = modulePermissionRepo;
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
                    && m.UserId == request.UserId
                    && m.IsActive)
                ?? throw new NotFoundApiException(nameof(ProjectMember), $"Project: {request.ProjectId}, User: {request.UserId}");

            ProjectMemberUserInfo? targetUser = await userService.GetProjectMemberAsync(
                request.TenantId, request.ProjectId, request.UserId, cancellationToken);

            bool wasAdmin = projectMember.IsAdmin;
            projectMember.IsAdmin = request.IsAdmin;

            await projectMemberRepo.Update(projectMember);

            // Replace module permissions (clear existing, insert new non-None entries)
            IEnumerable<ProjectMemberModulePermission> existingPermissions = await modulePermissionRepo.GetBySearch(
                mp => mp.TenantId == request.TenantId
                    && mp.ProjectId == request.ProjectId
                    && mp.UserId == request.UserId);

            foreach (ProjectMemberModulePermission existing in existingPermissions)
            {
                await modulePermissionRepo.Delete(existing);
            }

            IEnumerable<ProjectModule> effectiveModules = request.IsAdmin
                ? request.Modules
                : request.Modules.Where(m => m != ProjectModule.Settings);

            foreach (ProjectModule module in effectiveModules)
            {
                await modulePermissionRepo.Insert(new ProjectMemberModulePermission
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    UserId = request.UserId,
                    Module = module
                });
            }

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
                Title = "Zmieniono Twoje uprawnienia w projekcie",
                Message = $"Twoje uprawnienia w projekcie '{project.Name}' zostały zmienione.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "projectId", request.ProjectId },
                    { "projectName", project.Name },
                    { "wasAdmin", wasAdmin },
                    { "isAdmin", request.IsAdmin },
                    { "changedByUserId", currentUser.Id }
                }
            };

            NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            return Unit.Value;
        }
    }
}
