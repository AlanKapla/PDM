using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
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
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ShareCostEstimateCommandHandler> logger;

        public ShareCostEstimateCommandHandler(
            ICostEstimateCacheService cacheService,
            IRepository<SharedCostEstimate> sharedCeRepository,
            IUserService userService,
            IReadRepository<Notification> notificationRepository,
            ICostEstimateAccessService ceAccessService,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ShareCostEstimateCommandHandler> logger)
        {
            this.cacheService = cacheService;
            this.sharedCeRepository = sharedCeRepository;
            this.userService = userService;
            this.notificationRepository = notificationRepository;
            this.ceAccessService = ceAccessService;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(ShareCostEstimateCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            if (costEstimate.OwnerId != currentUser.Id && !isAdmin)
                throw new ForbiddenApiException("Only the owner or an admin can share this cost estimate.");

            var existingUserIds = await sharedCeRepository.SelectToHashSetAsync(
                s => s.CostEstimateId == request.CostEstimateId,
                s => s.SharedWithUserId,
                cancellationToken);

            var now = DateTime.UtcNow;
            var newShares = request.ShareWithUserIds
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

            await ceAccessService.InvalidateCostEstimateAccessCacheAsync(
                request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            await ceAccessService.InvalidateAccessCacheAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            // Send notifications only to users who were newly added
            if (newShares.Count > 0)
            {
                string sharerName = currentUser.FullName;

                foreach (var share in newShares)
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
                var targetUser = await userService.GetProjectMemberAsync(
                    request.TenantId, request.ProjectId, targetUserId, cancellationToken);

                if (targetUser == null)
                {
                    return;
                }

                var metadata = new Dictionary<string, object?>
                {
                    ["CostEstimateId"] = request.CostEstimateId,
                    ["ProjectId"] = request.ProjectId,
                    ["SharedByUserId"] = currentUser.Id,
                    ["SharedByUserName"] = sharerName
                };

                var notification = new NotificationDto
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
                    CreatedAt = DateTimeOffset.UtcNow,
                    Readed = false
                };

                var payload = await NotificationPayloadHelper.CreatePayloadAsync(
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
