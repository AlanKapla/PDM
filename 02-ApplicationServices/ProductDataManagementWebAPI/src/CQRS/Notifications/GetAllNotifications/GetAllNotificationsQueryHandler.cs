using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Notifications;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using System.Text.Json;
using EntityNotificationType = Entities.Models.Notifications.NotificationType;
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
            logger.LogInformation("📥 Fetching all notifications for user {UserId}, take={Take}, skip={Skip}", currentUser.Id, request.Take, request.Skip);
            
            IEnumerable<Notification> notifications = await notificationRepo.GetBySearch(
                n => n.UserId == currentUser.Id,
                include => include.Include(n => n.Tenant)
                                  .Include(n => n.Project));

            List<NotificationWeb> items = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Skip(request.Skip)
                .Take(request.Take)
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
                    n.IsRead,
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
