using Business.Implementation.Utilities;
using System.Text.Json;
using Business.Interfaces.WebModels.Notifications;
using Entities.Models.Notifications;
using DtoNotificationType = Business.Interfaces.DTO.NotificationType;
using EntityNotificationType = Entities.Models.Notifications.NotificationType;

namespace CQRS.Notifications
{
    internal static class NotificationWebMapper
    {
        public static NotificationWeb ToWeb(Notification notification)
        {
            return new NotificationWeb
            {
                Id = notification.Id,
                TenantId = notification.TenantId,
                ProjectId = notification.ProjectId,
                TenantName = notification.Tenant.Name,
                ProjectName = notification.Project?.Name,
                UserId = notification.UserId,
                Type = MapType(notification.Type),
                Title = notification.Title,
                Message = notification.Message,
                CreatedAt = UtcDateTimeHelper.ToUtcOffset(notification.CreatedAt),
                IsRead = notification.IsRead,
                Metadata = DeserializeMetadata(notification.MetadataJson)
            };
        }

        public static DtoNotificationType MapType(EntityNotificationType type) => type switch
        {
            EntityNotificationType.Info => DtoNotificationType.Info,
            EntityNotificationType.Success => DtoNotificationType.Success,
            EntityNotificationType.Warning => DtoNotificationType.Warning,
            EntityNotificationType.Error => DtoNotificationType.Error,
            _ => DtoNotificationType.Info
        };

        public static IReadOnlyDictionary<string, object?>? DeserializeMetadata(string? metadataJson)
        {
            if (string.IsNullOrWhiteSpace(metadataJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson);
            }
            catch
            {
                return null;
            }
        }
    }
}
