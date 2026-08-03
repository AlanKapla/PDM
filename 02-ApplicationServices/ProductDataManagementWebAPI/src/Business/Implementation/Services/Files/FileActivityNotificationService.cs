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
    /// Sends file-activity notifications to the owner and users with effective share access.
    /// Errors are logged and swallowed — the activity is already persisted in DB before this runs.
    /// </summary>
    public sealed class FileActivityNotificationService : IFileActivityNotificationService
    {
        private readonly IProjectFilesService projectFilesService;
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ILogger<FileActivityNotificationService> logger;

        public FileActivityNotificationService(
            IProjectFilesService projectFilesService,
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ILogger<FileActivityNotificationService> logger)
        {
            this.projectFilesService = projectFilesService;
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public Task NotifyCommentAddedAsync(
            FileActivityNotificationContext context,
            CancellationToken cancellationToken) =>
            DispatchAsync(
                context,
                "Nowy komentarz do pliku",
                $"{context.ActorName} dodał komentarz do pliku \"{context.FileDisplayName}\"",
                cancellationToken);

        public Task NotifyVersionUploadedAsync(
            FileActivityNotificationContext context,
            CancellationToken cancellationToken) =>
            DispatchAsync(
                context,
                "Nowa wersja pliku",
                $"{context.ActorName} dodał nową wersję pliku \"{context.FileDisplayName}\"",
                cancellationToken);

        private async Task DispatchAsync(
            FileActivityNotificationContext context,
            string title,
            string message,
            CancellationToken cancellationToken)
        {
            try
            {
                List<Guid> recipientIds = await ResolveRecipientIdsAsync(context, cancellationToken);
                if (recipientIds.Count == 0)
                {
                    logger.LogInformation(
                        "Skipping file-activity notification ({Title}) for file {FileId}: no recipients (actor={ActorUserId}, owner={OwnerId})",
                        title,
                        context.FileId,
                        context.ActorUserId,
                        context.OwnerId);
                    return;
                }

                logger.LogInformation(
                    "Sending file-activity notification ({Title}) for file {FileId} to {RecipientCount} recipient(s)",
                    title,
                    context.FileId,
                    recipientIds.Count);

                IEnumerable<User> users = await userRepo.GetBySearch(u => recipientIds.Contains(u.Id));
                Dictionary<Guid, User> userDict = users.ToDictionary(u => u.Id);

                foreach (Guid userId in recipientIds)
                {
                    if (!userDict.TryGetValue(userId, out User? user))
                    {
                        continue;
                    }

                    NotificationDto notification = BuildNotification(context, user, title, message);
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
                    "Failed to send file-activity notification ({Title}) for file {FileId}",
                    title,
                    context.FileId);
            }
        }

        private async Task<List<Guid>> ResolveRecipientIdsAsync(
            FileActivityNotificationContext context,
            CancellationToken cancellationToken)
        {
            HashSet<Guid> recipients = new HashSet<Guid> { context.OwnerId };

            Dictionary<Guid, List<Guid>> sharedByFile = await projectFilesService.GetSharedWithUsersAsync(
                context.TenantId,
                context.ProjectId,
                context.PackageId,
                new HashSet<Guid> { context.FileId },
                cancellationToken);

            if (sharedByFile.TryGetValue(context.FileId, out List<Guid>? sharedUserIds))
            {
                foreach (Guid sharedUserId in sharedUserIds)
                {
                    recipients.Add(sharedUserId);
                }
            }

            recipients.Remove(context.ActorUserId);
            return recipients.ToList();
        }

        private static NotificationDto BuildNotification(
            FileActivityNotificationContext context,
            User user,
            string title,
            string message)
        {
            Dictionary<string, object?> metadata = new Dictionary<string, object?>
            {
                ["FileId"] = context.FileId.ToString(),
                ["PackageId"] = context.PackageId.ToString(),
                ["EntityType"] = "ProjectFile",
                ["route"] = BuildDeepLinkRoute(context),
            };

            if (context.VersionId.HasValue)
            {
                metadata["VersionId"] = context.VersionId.Value.ToString();
            }

            if (context.CommentId.HasValue)
            {
                metadata["CommentId"] = context.CommentId.Value.ToString();
            }

            return new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                ProjectId = context.ProjectId,
                UserId = user.Id,
                AzureAdB2CObjectId = user.AzureAdB2CObjectId,
                Type = NotifType.Info,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Metadata = metadata,
            };
        }

        private static string BuildDeepLinkRoute(FileActivityNotificationContext context)
        {
            string route =
                $"/projects/{context.ProjectId}/files?fileId={context.FileId}&packageId={context.PackageId}";

            if (context.VersionId.HasValue)
            {
                route += $"&versionId={context.VersionId.Value}";
            }

            if (context.CommentId.HasValue)
            {
                route += $"&commentId={context.CommentId.Value}";
            }

            return route;
        }
    }
}
