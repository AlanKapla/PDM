using Business.Interfaces.DTO;
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
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public AddProjectMemberCommandHandler(
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

        public async Task<Unit> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
        {
            // Pobierz projekt do użycia w notyfikacji (walidacja już wykonana w validatorze)
            Project project = (await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken))!;

            // Utwórz nowego członka projektu z rolą Member
            ProjectMember newMember = new ProjectMember
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = request.UserId
            };

            await projectMemberRepo.Insert(newMember);

            // Wyślij notyfikację do użytkownika
            NotificationDto notification = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = request.UserId,
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
