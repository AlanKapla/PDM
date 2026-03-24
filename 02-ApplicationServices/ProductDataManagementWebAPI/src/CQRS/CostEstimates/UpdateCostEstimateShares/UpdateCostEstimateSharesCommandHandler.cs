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

namespace CQRS.CostEstimates.UpdateCostEstimateShares
{
    public sealed class UpdateCostEstimateSharesCommandHandler : IRequestHandler<UpdateCostEstimateSharesCommand, Unit>
    {
        private readonly ICostEstimateCacheService cacheService;
        private readonly IRepository<SharedCostEstimate> sharedCeRepository;
        private readonly IUserService userService;
        private readonly IReadRepository<Notification> notificationRepository;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateCostEstimateSharesCommandHandler> logger;

        public UpdateCostEstimateSharesCommandHandler(
            ICostEstimateCacheService cacheService,
            IRepository<SharedCostEstimate> sharedCeRepository,
            IUserService userService,
            IReadRepository<Notification> notificationRepository,
            ICostEstimateAccessService ceAccessService,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<UpdateCostEstimateSharesCommandHandler> logger)
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

        public async Task<Unit> Handle(UpdateCostEstimateSharesCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            if (costEstimate.OwnerId != currentUser.Id && !isAdmin)
                throw new ForbiddenApiException("Only the owner or an admin can update shares for this cost estimate.");

            // Load current shares as dict: userId → SharedCostEstimate
            var existingShares = (await sharedCeRepository.GetBySearch(
                s => s.CostEstimateId == request.CostEstimateId)).ToList();

            var existingUserIds = existingShares.Select(s => s.SharedWithUserId).ToHashSet();
            var desiredUserIds = request.UserIds.ToHashSet();

            var toAdd = desiredUserIds.Except(existingUserIds).ToList();
            var toRemove = existingUserIds.Except(desiredUserIds).ToList();

            // Add new shares
            if (toAdd.Count > 0)
            {
                var now = DateTime.UtcNow;
                var newShares = toAdd.Select(userId => new SharedCostEstimate
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    CostEstimateId = request.CostEstimateId,
                    SharedByUserId = currentUser.Id,
                    SharedWithUserId = userId,
                    SharedAt = now
                }).ToList();

                await sharedCeRepository.InsertRange(newShares);
            }

            // Remove revoked shares
            if (toRemove.Count > 0)
            {
                await sharedCeRepository.ExecuteDeleteAsync(
                    s => s.CostEstimateId == request.CostEstimateId &&
                         toRemove.Contains(s.SharedWithUserId),
                    cancellationToken);
            }

            if (toAdd.Count > 0 || toRemove.Count > 0)
            {
                await ceAccessService.InvalidateCostEstimateAccessCacheAsync(
                    request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

                await ceAccessService.InvalidateAccessCacheAsync(
                    request.TenantId, request.ProjectId, cancellationToken);
            }

            // Send notifications (fire-and-forget per user, never throw)
            string sharerName = currentUser.FullName;

            foreach (var userId in toAdd)
                await SendNotificationAsync(request, userId, sharerName, shared: true, cancellationToken);

            foreach (var userId in toRemove)
                await SendNotificationAsync(request, userId, sharerName, shared: false, cancellationToken);

            logger.LogInformation(
                "Cost estimate {CostEstimateId} shares updated by {UserId}: +{Added} -{Removed}",
                request.CostEstimateId, currentUser.Id, toAdd.Count, toRemove.Count);

            return Unit.Value;
        }

        private async Task SendNotificationAsync(
            UpdateCostEstimateSharesCommand request,
            Guid targetUserId,
            string sharerName,
            bool shared,
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

                string title = shared
                    ? "Udostępniono Ci kosztorys"
                    : "Cofnięto dostęp do kosztorysu";

                string message = shared
                    ? $"{sharerName} udostępnił Ci kosztorys"
                    : $"{sharerName} cofnął Twój dostęp do kosztorysu";

                var metadata = new Dictionary<string, object?>
                {
                    ["CostEstimateId"] = request.CostEstimateId,
                    ["ProjectId"] = request.ProjectId,
                    ["SharedByUserId"] = currentUser.Id,
                    ["SharedByUserName"] = sharerName,
                    ["Action"] = shared ? "Shared" : "Unshared"
                };

                var notification = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    UserId = targetUserId,
                    AzureAdB2CObjectId = targetUser.AzureAdB2CObjectId,
                    Type = NotificationTypeDto.Info,
                    Title = title,
                    Message = message,
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
