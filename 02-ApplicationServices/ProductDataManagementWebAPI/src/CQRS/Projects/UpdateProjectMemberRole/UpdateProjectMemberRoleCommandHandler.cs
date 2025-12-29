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
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public UpdateProjectMemberRoleCommandHandler(
            IReadRepository<Project> projectRepo,
            IReadRepository<User> userRepo,
            IRepository<ProjectMember> projectMemberRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.userRepo = userRepo;
            this.projectMemberRepo = projectMemberRepo;
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

            ProjectRole oldRole = projectMember.Role;
            projectMember.Role = request.Role;

            await projectMemberRepo.Update(projectMember);

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
                Message = $"Twoja rola w projekcie '{project.Name}' została zmieniona z {oldRole} na {request.Role}.",
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "projectId", request.ProjectId },
                    { "projectName", project.Name },
                    { "oldRole", oldRole.ToString() },
                    { "newRole", request.Role.ToString() },
                    { "changedByUserId", currentUser.Id }
                }
            };

            await notificationSender.EnqueueAsync(notification, cancellationToken);

            return Unit.Value;
        }
    }
}
