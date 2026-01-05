using Business.Interfaces.Constants;
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

namespace CQRS.Projects.AddProjectMember
{
    public class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public AddProjectMemberCommandHandler(
            IReadRepository<Project> projectRepo,
            IReadRepository<User> userRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IReadRepository<Role> roleRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.userRepo = userRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.roleRepo = roleRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
        {
            Project project = await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            // Get PROJECT.VIEWER role as default
            var viewerRole = await roleRepo.GetFirstBySearch(
                r => r.Scope == RoleScope.Project && r.Code == RoleCodes.ProjectViewer && r.IsActive,
                cancellationToken)
                ?? throw new InvalidOperationException($"{RoleCodes.ProjectViewer} role not found");

            ProjectMember newMember = new ProjectMember
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = request.UserId,
                RoleId = viewerRole.Id,
                JoinedAt = DateTime.UtcNow
            };

            await projectMemberRepo.Insert(newMember);

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId, cancellationToken);

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

            await notificationSender.EnqueueAsync(notification, cancellationToken);

            return Unit.Value;
        }
    }
}
