using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Notifications;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using System.Text.Json;
using EntityNotificationType = Entities.Models.NotificationType;
using DtoNotificationType = Business.Interfaces.DTO.NotificationType;
using Microsoft.Extensions.Logging;

namespace CQRS.Notifications.GetAllNotifications
{
    public class GetAllNotificationsQueryHandler : IRequestHandler<GetAllNotificationsQuery, IEnumerable<NotificationWeb>>
    {
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<GetAllNotificationsQueryHandler> logger;

        public GetAllNotificationsQueryHandler(
            IReadRepository<Notification> notificationRepo,
            ICurrentUser currentUser,
            ILogger<GetAllNotificationsQueryHandler> logger)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<IEnumerable<NotificationWeb>> Handle(GetAllNotificationsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("📥 Fetching all notifications for user {UserId}, limit={Limit}", currentUser.Id, request.Limit);
            
            // Pobierz ostatnie N powiadomień użytkownika (niezależnie od statusu przeczytane/nieprzeczytane)
            IEnumerable<Notification> notifications = await notificationRepo.GetBySearch(
                n => n.UserId == currentUser.Id,
                include => include.Include(n => n.Tenant)
                                  .Include(n => n.Project));

            List<NotificationWeb> items = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(request.Limit)
                .Select(n => new NotificationWeb(
                    n.Id,
                    n.TenantId,
                    n.ProjectId,
                    n.Tenant.Name,
                    n.Project?.Name,
                    n.UserId,
                    MapType(n.Type),
                    n.Title,
                    n.Message,
                    n.CreatedAt,
                    n.Readed,
                    DeserializeMetadata(n.MetadataJson)
                ))
                .ToList();

            logger.LogInformation("✅ Returning {Count} notifications for user {UserId}", items.Count, currentUser.Id);
            return items;
        }

        private static Dictionary<string, object?>? DeserializeMetadata(string? metadataJson)
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

        private static DtoNotificationType MapType(EntityNotificationType type) => type switch
        {
            EntityNotificationType.Info => DtoNotificationType.Info,
            EntityNotificationType.Success => DtoNotificationType.Success,
            EntityNotificationType.Warning => DtoNotificationType.Warning,
            EntityNotificationType.Error => DtoNotificationType.Error,
            _ => DtoNotificationType.Info
        };
    }
}
