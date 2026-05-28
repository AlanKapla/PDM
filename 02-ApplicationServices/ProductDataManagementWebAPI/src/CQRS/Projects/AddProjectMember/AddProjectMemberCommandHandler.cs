using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Enums;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.AddProjectMember
{
    public sealed class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<ProjectMemberModulePermission> modulePermissionRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly IUserService userService;

        public AddProjectMemberCommandHandler(
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

        public async Task<Unit> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
        {
            Project project = await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            ProjectMemberUserInfo? targetUser = await userService.GetTenantMemberInfoAsync(
                request.TenantId, request.UserId, cancellationToken);

            ProjectMember newMember = new ProjectMember
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = request.UserId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            };

            await projectMemberRepo.Insert(newMember);

            // Save module permissions — Settings is admin-only, strip it for new (non-admin) members
            IEnumerable<ProjectModule> effectiveModules = request.Modules.Where(m => m != ProjectModule.Settings);

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
            await permissionsVersionService.BumpVersionAsync(request.UserId, cancellationToken);

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

            NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            return Unit.Value;
        }
    }
}
