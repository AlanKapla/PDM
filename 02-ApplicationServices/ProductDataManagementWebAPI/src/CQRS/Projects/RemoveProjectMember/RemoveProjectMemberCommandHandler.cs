using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
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

namespace CQRS.Projects.RemoveProjectMember
{
    public sealed class RemoveProjectMemberCommandHandler : IRequestHandler<RemoveProjectMemberCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly IUserService userService;

        public RemoveProjectMemberCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            IReadRepository<Notification> notificationRepo,
            IUserService userService)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.notificationRepo = notificationRepo;
            this.userService = userService;
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

            ProjectMemberUserInfo? targetUser = await userService.GetProjectMemberAsync(
                request.TenantId, request.ProjectId, request.UserId, cancellationToken);

            await projectMemberRepo.Delete(projectMember);
            await userService.InvalidateProjectMembersCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

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
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "projectId", request.ProjectId },
                    { "projectName", project.Name },
                    { "removedByUserId", currentUser.Id }
                }
            };

            NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            return Unit.Value;
        }
    }
}
