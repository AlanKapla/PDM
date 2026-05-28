using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotificationTypeDto = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Helpers
{
    /// <summary>
    /// Helper for sending cost estimate field update notifications to the cost estimate owner.
    /// </summary>
    internal static class CostEstimateFieldUpdateNotificationHelper
    {
        /// <summary>
        /// Sends a field-update notification to the cost estimate owner.
        /// Swallows all exceptions and logs a warning to avoid interrupting the main flow.
        /// </summary>
        public static async Task SendOwnerNotificationAsync(
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            Guid ownerId,
            ICurrentUser currentUser,
            IUserService userService,
            IReadRepository<Notification> notificationRepository,
            INotificationSender notificationSender,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                string updaterName = currentUser.FullName;

                var owner = await userService.GetProjectMemberAsync(
                    tenantId, projectId, ownerId, cancellationToken);

                if (owner == null)
                {
                    return;
                }

                NotificationDto notification = new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProjectId = projectId,
                    UserId = ownerId,
                    AzureAdB2CObjectId = owner.AzureAdB2CObjectId,
                    Type = NotificationTypeDto.Info,
                    Title = "Zaktualizowano pole kosztorysu",
                    Message = $"{updaterName} zaktualizował pole w kosztorysie",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["CostEstimateId"] = costEstimateId,
                        ["ProjectId"] = projectId,
                        ["UpdatedByUserId"] = currentUser.Id,
                        ["UpdatedByUserName"] = updaterName
                    },
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(
                    notification, notificationRepository, cancellationToken);

                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to send field update notification to owner {OwnerId} for cost estimate {CostEstimateId}",
                    ownerId, costEstimateId);
            }
        }
    }
}
