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
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotificationTypeDto = Business.Interfaces.DTO.NotificationType;

namespace CQRS.ProjectCosts.ShareProjectCosts
{
    public class ShareProjectCostsCommandHandler : IRequestHandler<ShareProjectCostsCommand, Unit>
    {
        private readonly IRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IUserService userService;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ShareProjectCostsCommandHandler> logger;

        public ShareProjectCostsCommandHandler(
            IRepository<SharedProjectCost> sharedProjectCostRepo,
            IRepository<ProjectCost> projectCostRepo,
            IUserService userService,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ShareProjectCostsCommandHandler> logger)
        {
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.projectCostRepo = projectCostRepo;
            this.userService = userService;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(ShareProjectCostsCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify costs exist and belong to the correct project/tenant
            IEnumerable<ProjectCost> projectCostsEnumerable = await projectCostRepo.GetBySearch(
                pc => request.ProjectCostIds.Contains(pc.Id)
                    && pc.ProjectId == request.ProjectId
                    && pc.TenantId == request.TenantId);

            List<ProjectCost> projectCosts = projectCostsEnumerable.ToList();

            if (projectCosts.Count != request.ProjectCostIds.Count())
            {
                throw new NotFoundApiException(nameof(ProjectCost), "One or more costs not found");
            }

            // 2. Authorization check: tenant admin OR project admin OR owner of ALL costs
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);

            if (!isAdmin)
            {
                List<ProjectCost> notOwnedCosts = projectCosts.Where(pc => pc.UserId != currentUser.Id).ToList();
                if (notOwnedCosts.Count > 0)
                {
                    throw new ForbiddenApiException("You do not have permission to share one or more of the selected costs.");
                }
            }

            // 3. Get all existing shares for all users and costs in one query
            IEnumerable<SharedProjectCost> existingShares = await sharedProjectCostRepo.GetBySearch(
                spc => request.SharedWithUserIds.Contains(spc.SharedWithUserId) &&
                       request.ProjectCostIds.Contains(spc.ProjectCostId));

            Dictionary<Guid, HashSet<Guid>> existingSharesDict = existingShares
                .GroupBy(spc => spc.SharedWithUserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(spc => spc.ProjectCostId).ToHashSet()
                );

            Dictionary<Guid, string> costNamesDict = projectCosts.ToDictionary(pc => pc.Id, pc => pc.Name);

            List<SharedProjectCost> allSharedCostsToInsert = new List<SharedProjectCost>();
            List<(Guid UserId, int CostCount)> pendingNotifications = new List<(Guid, int)>();

            // 4. For each user, determine costs to share
            foreach (Guid userId in request.SharedWithUserIds)
            {
                HashSet<Guid> existingCostIds = existingSharesDict.ContainsKey(userId)
                    ? existingSharesDict[userId]
                    : new HashSet<Guid>();

                List<Guid> costsToShare = request.ProjectCostIds.Where(id => !existingCostIds.Contains(id)).ToList();

                if (costsToShare.Count == 0)
                {
                    continue;
                }

                // Prepare shares for this user
                foreach (Guid costId in costsToShare)
                {
                    SharedProjectCost sharedCost = new SharedProjectCost
                    {
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectCostId = costId,
                        SharedByUserId = currentUser.Id,
                        SharedWithUserId = userId,
                        SharedAt = DateTime.UtcNow
                    };

                    allSharedCostsToInsert.Add(sharedCost);
                }

                logger.LogInformation(
                    "User {UserId} will share {CostCount} costs with user {SharedWithUserId} in project {ProjectId}",
                    currentUser.Id, costsToShare.Count, userId, request.ProjectId);

                pendingNotifications.Add((userId, costsToShare.Count));
            }

            // 5. Insert all shared costs and commit BEFORE sending notifications
            if (allSharedCostsToInsert.Count > 0)
            {
                await sharedProjectCostRepo.InsertRange(allSharedCostsToInsert);
                await sharedProjectCostRepo.SaveChangesAsync(cancellationToken);
            }

            // 6. Send notifications only after shares are persisted
            foreach ((Guid userId, int costCount) in pendingNotifications)
            {
                await SendNotificationAsync(request, userId, costCount, costNamesDict, cancellationToken);
            }

            return Unit.Value;
        }

        private async Task SendNotificationAsync(
            ShareProjectCostsCommand request,
            Guid sharedWithUserId,
            int costCount,
            Dictionary<Guid, string> costNamesDict,
            CancellationToken cancellationToken)
        {
            var currentUserDetails = await userService.GetProjectMemberAsync(
                request.TenantId, request.ProjectId, currentUser.Id, cancellationToken);

            string sharerName = currentUserDetails?.FullName ?? currentUser.FullName;

            string title;
            string message;

            if (costCount == 1)
            {
                var costName = costNamesDict.Values.FirstOrDefault() ?? "koszt";
                title = "Udostępniono Ci koszt";
                message = $"{sharerName} udostępnił Ci koszt: {costName}";
            }
            else
            {
                title = "Udostępniono Ci koszty";
                message = $"{sharerName} udostępnił Ci {costCount} kosztów";
            }

            var metadata = new Dictionary<string, object?>
            {
                ["ProjectId"] = request.ProjectId,
                ["SharedByUserId"] = currentUser.Id,
                ["SharedByUserName"] = sharerName,
                ["CostCount"] = costCount,
                ["CostNames"] = costNamesDict.Values.Take(5).ToList()
            };

            var targetMember = await userService.GetProjectMemberAsync(
                request.TenantId, request.ProjectId, sharedWithUserId, cancellationToken);

            var notificationDto = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = sharedWithUserId,
                AzureAdB2CObjectId = targetMember?.AzureAdB2CObjectId,
                Type = NotificationTypeDto.Info,
                Title = title,
                Message = message,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            var payload = await NotificationPayloadHelper.CreatePayloadAsync(notificationDto, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            logger.LogInformation(
                "Notification sent to user {UserId} about {CostCount} shared costs",
                sharedWithUserId, costCount);
        }
    }
}
