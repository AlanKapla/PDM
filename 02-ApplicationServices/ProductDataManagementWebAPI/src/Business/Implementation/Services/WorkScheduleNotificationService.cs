using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace Business.Implementation.Services
{
    public class WorkScheduleNotificationService : IWorkScheduleNotificationService
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public WorkScheduleNotificationService(
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task SendAssignmentCreatedNotificationsAsync(
            IEnumerable<Guid> userIds,
            Guid workScheduleId,
            string workScheduleName,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            List<Guid> userIdList = userIds.ToList();
            if (userIdList.Count == 0)
                return;

            IEnumerable<User> users = await userRepo.GetBySearch(u => userIdList.Contains(u.Id));
            string actorName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            HashSet<Guid> notifyUserIds = users.Select(u => u.Id).ToHashSet();
            Dictionary<Guid, int> unreadCounts = await GetBulkUnreadCountsAsync(notifyUserIds, cancellationToken);

            foreach (User targetUser in users)
            {
                NotificationDto notification = BuildNotification(
                    targetUser.Id, targetUser,
                    "Przypisano do harmonogramu prac",
                    $"Zostałeś przypisany do prac w harmonogramie: {workScheduleName}",
                    workScheduleId, workScheduleName, tenantId, projectId,
                    "createdByUserId", "createdByUserName", actorName);

                int unreadCount = unreadCounts.GetValueOrDefault(targetUser.Id, 0) + 1;
                NotificationPayloadDto payload = new NotificationPayloadDto(notification, unreadCount);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }
        }

        public async Task SendAssignmentChangedNotificationsAsync(
            HashSet<Guid> removedUserIds,
            HashSet<Guid> addedUserIds,
            Guid workScheduleId,
            string workScheduleName,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            if (removedUserIds.Count == 0 && addedUserIds.Count == 0)
                return;

            removedUserIds.ExceptWith(addedUserIds);

            List<Guid> allNotificationUserIds = removedUserIds.Union(addedUserIds).ToList();
            if (allNotificationUserIds.Count == 0)
                return;

            IEnumerable<User> notificationUsers = await userRepo.GetBySearch(u => allNotificationUserIds.Contains(u.Id));
            Dictionary<Guid, User> notificationUserDict = notificationUsers.ToDictionary(u => u.Id);
            string actorName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            HashSet<Guid> notifyUserIds = allNotificationUserIds.ToHashSet();
            Dictionary<Guid, int> unreadCounts = await GetBulkUnreadCountsAsync(notifyUserIds, cancellationToken);

            foreach (Guid userId in removedUserIds)
            {
                notificationUserDict.TryGetValue(userId, out User? targetUser);
                NotificationDto notification = BuildNotification(
                    userId, targetUser,
                    "Usunięto z harmonogramu prac",
                    $"Zostałeś usunięty z prac w harmonogramie: {workScheduleName}",
                    workScheduleId, workScheduleName, tenantId, projectId,
                    "updatedByUserId", "updatedByUserName", actorName);

                int unreadCount = unreadCounts.GetValueOrDefault(userId, 0) + 1;
                NotificationPayloadDto payload = new NotificationPayloadDto(notification, unreadCount);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }

            foreach (Guid userId in addedUserIds)
            {
                notificationUserDict.TryGetValue(userId, out User? targetUser);
                NotificationDto notification = BuildNotification(
                    userId, targetUser,
                    "Przypisano do harmonogramu prac",
                    $"Zostałeś przypisany do prac w harmonogramie: {workScheduleName}",
                    workScheduleId, workScheduleName, tenantId, projectId,
                    "updatedByUserId", "updatedByUserName", actorName);

                int unreadCount = unreadCounts.GetValueOrDefault(userId, 0) + 1;
                NotificationPayloadDto payload = new NotificationPayloadDto(notification, unreadCount);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }
        }

        private async Task<Dictionary<Guid, int>> GetBulkUnreadCountsAsync(
            HashSet<Guid> userIds,
            CancellationToken cancellationToken)
        {
            if (userIds.Count == 0)
                return new Dictionary<Guid, int>();

            return await notificationRepo.CountGroupedByAsync(
                n => userIds.Contains(n.UserId) && !n.Readed,
                n => n.UserId,
                cancellationToken);
        }

        private NotificationDto BuildNotification(
            Guid userId,
            User? targetUser,
            string title,
            string message,
            Guid workScheduleId,
            string workScheduleName,
            Guid tenantId,
            Guid projectId,
            string actorUserIdKey,
            string actorUserNameKey,
            string actorName) => new NotificationDto
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            UserId = userId,
            AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
            Type = NotificationType.Info,
            Title = title,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow,
            Readed = false,
            Metadata = new Dictionary<string, object?>
            {
                { "workScheduleId", workScheduleId },
                { "workScheduleName", workScheduleName },
                { "projectId", projectId },
                { actorUserIdKey, currentUser.Id },
                { actorUserNameKey, actorName }
            }
        };
    }
}
