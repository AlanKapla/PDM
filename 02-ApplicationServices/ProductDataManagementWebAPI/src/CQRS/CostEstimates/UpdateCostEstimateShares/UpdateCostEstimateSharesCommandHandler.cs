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

namespace CQRS.CostEstimates.UpdateCostEstimateShares
{
    public sealed class UpdateCostEstimateSharesCommandHandler : IRequestHandler<UpdateCostEstimateSharesCommand, Unit>
    {
        private readonly ICostEstimateCacheService cacheService;
        private readonly IRepository<SharedCostEstimate> sharedCeRepository;
        private readonly IUserService userService;
        private readonly IReadRepository<Notification> notificationRepository;
        private readonly ICostEstimateShareService ceShareService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateCostEstimateSharesCommandHandler> logger;

        public UpdateCostEstimateSharesCommandHandler(
            ICostEstimateCacheService cacheService,
            IRepository<SharedCostEstimate> sharedCeRepository,
            IUserService userService,
            IReadRepository<Notification> notificationRepository,
            ICostEstimateShareService ceShareService,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<UpdateCostEstimateSharesCommandHandler> logger)
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

        public async Task<Unit> Handle(UpdateCostEstimateSharesCommand request, CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            await ceShareService.ValidateOwnerOrAdminAsync(costEstimate, cancellationToken);

            // Load current shares as dict: userId → SharedCostEstimate
            List<SharedCostEstimate> existingShares = (await sharedCeRepository.GetBySearch(
                s => s.CostEstimateId == request.CostEstimateId)).ToList();

            HashSet<Guid> existingUserIds = existingShares.Select(s => s.SharedWithUserId).ToHashSet();
            HashSet<Guid> desiredUserIds = request.UserIds.ToHashSet();

            List<Guid> toAdd = desiredUserIds.Except(existingUserIds).ToList();
            List<Guid> toRemove = existingUserIds.Except(desiredUserIds).ToList();

            // Add new shares
            if (toAdd.Count > 0)
            {
                DateTime now = DateTime.UtcNow;
                List<SharedCostEstimate> newShares = toAdd.Select(userId => new SharedCostEstimate
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
                await ceShareService.InvalidateAccessCacheAsync(
                    request.CostEstimateId, request.ProjectId, request.TenantId, cancellationToken);
            }

            // Send notifications (fire-and-forget per user, never throw)
            string sharerName = currentUser.FullName;

            foreach (Guid userId in toAdd)
                await SendNotificationAsync(request, userId, sharerName, shared: true, cancellationToken);

            foreach (Guid userId in toRemove)
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
                ProjectMemberUserInfo? targetUser = await userService.GetProjectMemberAsync(
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

                Dictionary<string, object?> metadata = new Dictionary<string, object?>
                {
                    ["CostEstimateId"] = request.CostEstimateId,
                    ["ProjectId"] = request.ProjectId,
                    ["SharedByUserId"] = currentUser.Id,
                    ["SharedByUserName"] = sharerName,
                    ["Action"] = shared ? "Shared" : "Unshared"
                };

                NotificationDto notification = new NotificationDto
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
