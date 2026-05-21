using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotificationTypeDto = Business.Interfaces.DTO.NotificationType;

namespace CQRS.CostEstimates.ShareCostEstimate
{
    public sealed class ShareCostEstimateCommandHandler : IRequestHandler<ShareCostEstimateCommand, Unit>
    {
        private readonly ICostEstimateCacheService cacheService;
        private readonly IRepository<SharedCostEstimate> sharedCeRepository;
        private readonly IUserService userService;
        private readonly IReadRepository<Notification> notificationRepository;
        private readonly ICostEstimateShareService ceShareService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ShareCostEstimateCommandHandler> logger;

        public ShareCostEstimateCommandHandler(
            ICostEstimateCacheService cacheService,
            IRepository<SharedCostEstimate> sharedCeRepository,
            IUserService userService,
            IReadRepository<Notification> notificationRepository,
            ICostEstimateShareService ceShareService,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ShareCostEstimateCommandHandler> logger)
        {
            this.cacheService = cacheService;
            this.sharedCeRepository = sharedCeRepository;
            this.userService = userService;
            this.notificationRepository = notificationRepository;
            this.ceShareService = ceShareService;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(ShareCostEstimateCommand request, CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            await ceShareService.ValidateOwnerOrAdminAsync(costEstimate, cancellationToken);

            HashSet<Guid> existingUserIds = await sharedCeRepository.SelectToHashSetAsync(
                s => s.CostEstimateId == request.CostEstimateId,
                s => s.SharedWithUserId,
                cancellationToken);

            DateTime now = DateTime.UtcNow;
            List<SharedCostEstimate> newShares = request.ShareWithUserIds
                .Where(userId => !existingUserIds.Contains(userId))
                .Select(userId => new SharedCostEstimate
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    CostEstimateId = request.CostEstimateId,
                    SharedByUserId = currentUser.Id,
                    SharedWithUserId = userId,
                    SharedAt = now
                }).ToList();

            if (newShares.Count > 0)
                await sharedCeRepository.InsertRange(newShares);

            await ceShareService.InvalidateAccessCacheAsync(
                request.CostEstimateId, request.ProjectId, request.TenantId, cancellationToken);

            // Send notifications only to users who were newly added
            if (newShares.Count > 0)
            {
                string sharerName = currentUser.FullName;

                foreach (SharedCostEstimate share in newShares)
                    await SendNotificationAsync(request, share.SharedWithUserId, sharerName, cancellationToken);
            }

            logger.LogInformation(
                "Cost estimate {CostEstimateId} shared with {Count} new users by {UserId}",
                request.CostEstimateId, newShares.Count, currentUser.Id);

            return Unit.Value;
        }

        private async Task SendNotificationAsync(
            ShareCostEstimateCommand request,
            Guid targetUserId,
            string sharerName,
            CancellationToken cancellationToken)
        {
            try
            {
                ProjectMemberUserInfo? targetUser = await userService.GetProjectMemberAsync(
                    request.TenantId, request.ProjectId, targetUserId, cancellationToken);

                if (targetUser == null)
                {
                    return;
                }

                Dictionary<string, object?> metadata = new Dictionary<string, object?>
                {
                    ["CostEstimateId"] = request.CostEstimateId,
                    ["ProjectId"] = request.ProjectId,
                    ["SharedByUserId"] = currentUser.Id,
                    ["SharedByUserName"] = sharerName
                };

                NotificationDto notification = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    UserId = targetUserId,
                    AzureAdB2CObjectId = targetUser.AzureAdB2CObjectId,
                    Type = NotificationTypeDto.Info,
                    Title = "Udostępniono Ci kosztorys",
                    Message = $"{sharerName} udostępnił Ci kosztorys",
                    Metadata = metadata,
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
                    "Failed to send share notification to user {UserId} for cost estimate {CostEstimateId}",
                    targetUserId, request.CostEstimateId);
            }
        }
    }
}
