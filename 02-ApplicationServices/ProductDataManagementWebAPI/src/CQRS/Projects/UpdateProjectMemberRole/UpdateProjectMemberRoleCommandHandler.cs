using Business.Implementation.Services;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    public class UpdateProjectMemberRoleCommandHandler : IRequestHandler<UpdateProjectMemberRoleCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly PermissionsVersionService permissionsVersionService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public UpdateProjectMemberRoleCommandHandler(
            IReadRepository<Project> projectRepo,
            IReadRepository<User> userRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IReadRepository<Role> roleRepo,
            PermissionsVersionService permissionsVersionService,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.userRepo = userRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.roleRepo = roleRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateProjectMemberRoleCommand request, CancellationToken cancellationToken)
        {
            Project project = (await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId && p.IsActive))
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            ProjectMember projectMember = (await projectMemberRepo.GetFirstBySearch(
                m => m.ProjectId == request.ProjectId 
                    && m.UserId == request.UserId))!;

            // Verify role exists and is a Project scope role
            var newRole = await roleRepo.GetFirstBySearch(
                r => r.Id == request.RoleId && r.Scope == RoleScope.Project,
                cancellationToken);

            if (newRole == null)
                throw new NotFoundApiException("Role", request.RoleId.ToString());

            var oldRoleId = projectMember.RoleId;
            projectMember.RoleId = newRole.Id;

            await projectMemberRepo.Update(projectMember);

            // Bump permissions version for the user whose role changed
            await permissionsVersionService.BumpVersionAsync(request.UserId, cancellationToken);

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId);

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
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
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

            await notificationSender.EnqueueAsync(notification, cancellationToken);

            return Unit.Value;
        }
    }
}
