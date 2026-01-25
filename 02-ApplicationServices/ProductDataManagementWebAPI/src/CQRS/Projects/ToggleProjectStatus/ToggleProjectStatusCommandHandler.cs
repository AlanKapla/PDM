using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.ToggleProjectStatus;

/// <summary>
/// Handler zmieniający status aktywności projektu
/// </summary>
public class ToggleProjectStatusCommandHandler : IRequestHandler<ToggleProjectStatusCommand, Unit>
{
    private readonly IReadRepository<Project> projectRepo;
    private readonly IReadRepository<User> userRepo;
    private readonly IRepository<ProjectMember> projectMemberRepo;
    private readonly IReadRepository<Notification> notificationRepo;
    private readonly INotificationSender notificationSender;
    private readonly ICurrentUser currentUser;

    public ToggleProjectStatusCommandHandler(
        IReadRepository<Project> projectRepo,
        IReadRepository<User> userRepo,
        IRepository<ProjectMember> projectMemberRepo,
        IReadRepository<Notification> notificationRepo,
        INotificationSender notificationSender,
        ICurrentUser currentUser)
    {
        this.projectRepo = projectRepo;
        this.userRepo = userRepo;
        this.projectMemberRepo = projectMemberRepo;
        this.notificationRepo = notificationRepo;
        this.notificationSender = notificationSender;
        this.currentUser = currentUser;
    }

    public async Task<Unit> Handle(ToggleProjectStatusCommand request, CancellationToken cancellationToken)
    {
        // Pobierz projekt - może być aktywny lub nieaktywny
        Project project = await projectRepo.GetFirstBySearch(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId)
            ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

        // Zmień status projektu
        project.IsActive = request.IsActive;
        await projectRepo.Update(project);

        IEnumerable<ProjectMember> projectMembers = await projectMemberRepo.GetBySearch(
            pm => pm.ProjectId == request.ProjectId && pm.TenantId == request.TenantId);

        string actionText = request.IsActive ? "aktywowany" : "zdezaktywowany";
        NotificationType notificationType = request.IsActive ? NotificationType.Info : NotificationType.Warning;

        var memberUserIds = projectMembers
            .Where(pm => pm.UserId != currentUser.Id)
            .Select(pm => pm.UserId)
            .ToList();

        var users = await userRepo.GetBySearch(u => memberUserIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id);

        foreach (ProjectMember member in projectMembers.Where(pm => pm.UserId != currentUser.Id))
        {
            userDict.TryGetValue(member.UserId, out User? targetUser);

            NotificationDto notification = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = member.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
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

            var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);
        }

        return Unit.Value;
    }
}
