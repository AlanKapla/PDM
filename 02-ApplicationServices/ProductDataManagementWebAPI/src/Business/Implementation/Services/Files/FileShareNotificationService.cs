using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models.Notifications;
using Entities.Models.Users;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotifType = Business.Interfaces.DTO.NotificationType;

namespace Business.Implementation.Services.Files
{
    /// <summary>
    /// Sends file-share related notifications. Errors are logged and swallowed —
    /// the share state is already persisted in DB before this runs.
    /// </summary>
    public sealed class FileShareNotificationService : IFileShareNotificationService
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ILogger<FileShareNotificationService> logger;

        public FileShareNotificationService(
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ILogger<FileShareNotificationService> logger)
        {
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public Task NotifyShareGrantedAsync(FileShareNotificationContext context, CancellationToken cancellationToken) =>
            DispatchAsync(
                context,
                NotifType.Info,
                "Udostępniono Ci plik",
                user => $"{context.OwnerName} udostępnił Ci plik \"{context.FileDisplayName}\"",
                cancellationToken);

        public Task NotifyShareRevokedAsync(FileShareNotificationContext context, CancellationToken cancellationToken) =>
            DispatchAsync(
                context,
                NotifType.Warning,
                "Cofnięto dostęp do pliku",
                user => $"{context.OwnerName} cofnął Ci dostęp do pliku \"{context.FileDisplayName}\"",
                cancellationToken);

        private async Task DispatchAsync(
            FileShareNotificationContext context,
            NotifType type,
            string title,
            Func<User, string> messageBuilder,
            CancellationToken cancellationToken)
        {
            if (context.UserIds.Count == 0)
            {
                return;
            }

            try
            {
                List<Guid> userIds = context.UserIds.ToList();
                IEnumerable<User> users = await userRepo.GetBySearch(u => userIds.Contains(u.Id));
                Dictionary<Guid, User> userDict = users.ToDictionary(u => u.Id);

                foreach (Guid userId in userIds)
                {
                    if (!userDict.TryGetValue(userId, out User? user))
                    {
                        continue;
                    }

                    NotificationDto notification = BuildNotification(context, user, type, title, messageBuilder(user));
                    int unreadCount = await notificationRepo.CountAsync(
                        n => n.UserId == notification.UserId && !n.IsRead,
                        cancellationToken) + 1;
                    NotificationPayloadDto payload = new NotificationPayloadDto(notification, unreadCount);
                    await notificationSender.EnqueueAsync(payload, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to send file-share notification ({Title}) for file {FileId}",
                    title, context.FileId);
            }
        }

        private static NotificationDto BuildNotification(
            FileShareNotificationContext context,
            User user,
            NotifType type,
            string title,
            string message) =>
            new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                ProjectId = context.ProjectId,
                UserId = user.Id,
                AzureAdB2CObjectId = user.AzureAdB2CObjectId,
                Type = type,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object?>
                {
                    ["FileId"] = context.FileId,
                    ["EntityType"] = "ProjectFile",
                },
            };
    }
}
