using Business.Interfaces.DTO;
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
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public RemoveProjectMemberCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
        {
            // Pobierz projekt do użycia w notyfikacji (walidacja już wykonana w validatorze)
            Project project = (await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId))!;

            // Pobierz członka projektu
            ProjectMember projectMember = (await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == request.ProjectId
                    && pm.TenantId == request.TenantId
                    && pm.UserId == request.UserId))!;

            // Usuń członka z projektu
            await projectMemberRepo.Delete(projectMember);

            // Wyślij notyfikację do usuniętego użytkownika
            NotificationDto notification = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = request.UserId,
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
