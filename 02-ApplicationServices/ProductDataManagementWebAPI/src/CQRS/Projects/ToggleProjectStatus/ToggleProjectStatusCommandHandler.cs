using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.ToggleProjectStatus;

/// <summary>
/// Handler zmieniający status aktywności projektu
/// </summary>
public class ToggleProjectStatusCommandHandler : IRequestHandler<ToggleProjectStatusCommand, Unit>
{
    private readonly IReadRepository<Project> projectRepo;
    private readonly IRepository<ProjectMember> projectMemberRepo;
    private readonly INotificationSender notificationSender;
    private readonly ICurrentUser currentUser;

    public ToggleProjectStatusCommandHandler(
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

    public async Task<Unit> Handle(ToggleProjectStatusCommand request, CancellationToken cancellationToken)
    {
        // Pobierz projekt - może być aktywny lub nieaktywny
        Project? project = await projectRepo.GetFirstBySearch(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId)
            ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

        // Zmień status projektu
        project.IsActive = request.IsActive;
        await projectRepo.Update(project);

        // Pobierz wszystkich członków projektu do wysłania notyfikacji
        IEnumerable<ProjectMember> projectMembers = await projectMemberRepo.GetBySearch(
            pm => pm.ProjectId == request.ProjectId && pm.TenantId == request.TenantId);

        // Określ typ i treść notyfikacji w zależności od akcji
        string actionText = request.IsActive ? "aktywowany" : "zdezaktywowany";
        NotificationType notificationType = request.IsActive ? NotificationType.Info : NotificationType.Warning;

        // Wyślij notyfikację do wszystkich członków projektu oprócz użytkownika wykonującego akcję
        foreach (ProjectMember member in projectMembers.Where(pm => pm.UserId != currentUser.Id))
        {
            NotificationDto notification = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = member.UserId,
                Type = notificationType,
                Title = $"Projekt {actionText}",
                Message = $"Projekt \"{project.Name}\" został {actionText}",
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "projectId", request.ProjectId },
                    { "projectName", project.Name },
                    { "modifiedByUserId", currentUser.Id },
                    { "isActive", request.IsActive }
                }
            };

            await notificationSender.EnqueueAsync(notification, cancellationToken);
        }

        return Unit.Value;
    }
}
