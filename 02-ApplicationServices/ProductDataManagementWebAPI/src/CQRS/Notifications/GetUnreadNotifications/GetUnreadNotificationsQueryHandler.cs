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

namespace CQRS.Notifications.GetUnreadNotifications
{
    public class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, IEnumerable<NotificationWeb>>
    {
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;

        public GetUnreadNotificationsQueryHandler(IReadRepository<Notification> notificationRepo, ICurrentUser currentUser)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<NotificationWeb>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<Notification> notifications = await notificationRepo.GetBySearch(
                n => n.UserId == currentUser.Id && !n.IsRead,
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
