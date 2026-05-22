using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Costs;
using Entities.Models.Notifications;
using Entities.Models.Users;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace Business.Implementation.Services
{
    public sealed class ProjectCostShareNotificationService : IProjectCostShareNotificationService
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ProjectCostShareNotificationService> logger;

        public ProjectCostShareNotificationService(
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ProjectCostShareNotificationService> logger)
        {
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task NotifyCostSharedAsync(
            ProjectCost cost,
            IReadOnlyCollection<Guid> targetUserIds,
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            if (targetUserIds.Count == 0)
            {
                return;
            }

            Dictionary<Guid, User> userDict = await LoadUsersAsync(targetUserIds, cancellationToken);
            string actorName = GetActorName();

            foreach (Guid userId in targetUserIds)
            {
                userDict.TryGetValue(userId, out User? targetUser);

                NotificationDto notification = BuildSharedNotification(
                    cost, userId, targetUser, actorUserId, actorName);

                NotificationPayloadDto payload = await BuildPayloadAsync(notification, cancellationToken);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }

            logger.LogInformation(
                "Cost {CostId} shared with {UserCount} users by {ActorUserId}",
                cost.Id, targetUserIds.Count, actorUserId);
        }

        public async Task NotifyShareUpdatedAsync(
            ProjectCost cost,
            IReadOnlyCollection<Guid> addedUserIds,
            IReadOnlyCollection<Guid> removedUserIds,
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            if (addedUserIds.Count == 0 && removedUserIds.Count == 0)
            {
                return;
            }

            List<Guid> allUserIds = addedUserIds.Concat(removedUserIds).Distinct().ToList();
            Dictionary<Guid, User> userDict = await LoadUsersAsync(allUserIds, cancellationToken);
            string actorName = GetActorName();

            foreach (Guid userId in removedUserIds)
            {
                userDict.TryGetValue(userId, out User? targetUser);

                NotificationDto notification = BuildUnsharedNotification(
                    cost, userId, targetUser, actorUserId, actorName);

                NotificationPayloadDto payload = await BuildPayloadAsync(notification, cancellationToken);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }

            foreach (Guid userId in addedUserIds)
            {
                userDict.TryGetValue(userId, out User? targetUser);

                NotificationDto notification = BuildSharedNotification(
                    cost, userId, targetUser, actorUserId, actorName);

                NotificationPayloadDto payload = await BuildPayloadAsync(notification, cancellationToken);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }

            logger.LogInformation(
                "Cost {CostId} share updated: +{Added} / -{Removed} by {ActorUserId}",
                cost.Id, addedUserIds.Count, removedUserIds.Count, actorUserId);
        }

        private async Task<Dictionary<Guid, User>> LoadUsersAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<Guid, User>();
            }

            IEnumerable<User> users = await userRepo.GetBySearch(
                u => userIds.Contains(u.Id));

            return users.ToDictionary(u => u.Id);
        }

        private string GetActorName()
        {
            string actorName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
            return string.IsNullOrEmpty(actorName) ? currentUser.FullName : actorName;
        }

        private async Task<NotificationPayloadDto> BuildPayloadAsync(
            NotificationDto notification,
            CancellationToken cancellationToken)
        {
            int unreadCount = await notificationRepo.CountAsync(
                n => n.UserId == notification.UserId && !n.IsRead,
                cancellationToken) + 1;

            return new NotificationPayloadDto(notification, unreadCount);
        }

        private static NotificationDto BuildSharedNotification(
            ProjectCost cost,
            Guid targetUserId,
            User? targetUser,
            Guid actorUserId,
            string actorName) => new NotificationDto
        {
            Id = Guid.NewGuid(),
            TenantId = cost.TenantId,
            ProjectId = cost.ProjectId,
            UserId = targetUserId,
            AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
            Type = NotificationType.Success,
            Title = "Udostępniono Ci koszt",
            Message = $"{actorName} udostępnił Ci koszt: {cost.Name}",
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            Metadata = new Dictionary<string, object?>
            {
                { "costId", cost.Id },
                { "costName", cost.Name },
                { "sharedByUserId", actorUserId },
                { "sharedByUserName", actorName },
                { "action", "shared" }
            }
        };

        private static NotificationDto BuildUnsharedNotification(
            ProjectCost cost,
            Guid targetUserId,
            User? targetUser,
            Guid actorUserId,
            string actorName) => new NotificationDto
        {
            Id = Guid.NewGuid(),
            TenantId = cost.TenantId,
            ProjectId = cost.ProjectId,
            UserId = targetUserId,
            AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
            Type = NotificationType.Info,
            Title = "Odebrano dostęp do kosztu",
            Message = $"{actorName} odebrał Ci dostęp do kosztu: {cost.Name}",
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            Metadata = new Dictionary<string, object?>
            {
                { "costId", cost.Id },
                { "costName", cost.Name },
                { "removedByUserId", actorUserId },
                { "removedByUserName", actorName },
                { "action", "unshared" }
            }
        };
    }
}
