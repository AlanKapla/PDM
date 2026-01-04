using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.RemoveProjectMember
{
    public class RemoveProjectMemberCommandHandler : IRequestHandler<RemoveProjectMemberCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public RemoveProjectMemberCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            IReadRepository<User> userRepo)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.userRepo = userRepo;
        }

        public async Task<Unit> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
        {
            Project project = await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            ProjectMember projectMember = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == request.ProjectId
                    && pm.TenantId == request.TenantId
                    && pm.UserId == request.UserId)
                ?? throw new NotFoundApiException(nameof(ProjectMember), $"Project: {request.ProjectId}, User: {request.UserId}");

            await projectMemberRepo.Delete(projectMember);

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId, cancellationToken);

            NotificationDto notification = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = request.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = NotificationType.Warning,
                Title = "Usunięto z projektu",
                Message = $"Zostałeś usunięty z projektu: {project.Name}",
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "projectId", request.ProjectId },
                    { "projectName", project.Name },
                    { "removedByUserId", currentUser.Id }
                }
            };

            await notificationSender.EnqueueAsync(notification, cancellationToken);

            return Unit.Value;
        }
    }
}
