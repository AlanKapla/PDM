using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationTypeDto = Business.Interfaces.DTO.NotificationType;

namespace CQRS.ProjectCosts.ShareProjectCosts
{
    public class ShareProjectCostsCommandHandler : IRequestHandler<ShareProjectCostsCommand, Unit>
    {
        private readonly IRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ShareProjectCostsCommandHandler> logger;

        public ShareProjectCostsCommandHandler(
            IRepository<SharedProjectCost> sharedProjectCostRepo,
            IRepository<ProjectCost> projectCostRepo,
            IReadRepository<User> userRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ShareProjectCostsCommandHandler> logger)
        {
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.projectCostRepo = projectCostRepo;
            this.userRepo = userRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(ShareProjectCostsCommand request, CancellationToken cancellationToken)
        {
            // 1. Pobierz koszty i sprawdź ownership
            var projectCosts = await projectCostRepo.GetBySearch(
                pc => request.ProjectCostIds.Contains(pc.Id) && !pc.IsDeleted);

            if (projectCosts.Count() != request.ProjectCostIds.Count())
            {
                throw new NotFoundApiException(nameof(ProjectCost), "One or more costs not found");
            }

            // 2. Sprawdź czy user jest właścicielem WSZYSTKICH kosztów
            var notOwnedCosts = projectCosts.Where(pc => pc.UserId != currentUser.Id).ToList();
            if (notOwnedCosts.Any())
            {
                throw new ForbiddenApiException($"You can only share costs you own. {notOwnedCosts.Count} costs are not owned by you.");
            }

            // 3. Get all existing shares for all users and costs in one query
            var existingShares = await sharedProjectCostRepo.GetBySearch(
                spc => request.SharedWithUserIds.Contains(spc.SharedWithUserId) &&
                       request.ProjectCostIds.Contains(spc.ProjectCostId));

            var existingSharesDict = existingShares
                .GroupBy(spc => spc.SharedWithUserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(spc => spc.ProjectCostId).ToHashSet()
                );

            var costNamesDict = projectCosts.ToDictionary(pc => pc.Id, pc => pc.Name);

            var allSharedCostsToInsert = new List<SharedProjectCost>();

            // For each user, determine costs to share
            foreach (var userId in request.SharedWithUserIds)
            {
                var existingCostIds = existingSharesDict.ContainsKey(userId) 
                    ? existingSharesDict[userId] 
                    : new HashSet<Guid>();

                var costsToShare = request.ProjectCostIds.Where(id => !existingCostIds.Contains(id)).ToList();

                if (!costsToShare.Any())
                    continue;

                // Prepare shares for this user
                foreach (var costId in costsToShare)
                {
                    var sharedCost = new SharedProjectCost
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

                // Send notification to this user
                await SendNotificationAsync(request, userId, costsToShare.Count, costNamesDict, cancellationToken);
            }

            // Insert all shared costs in one batch
            if (allSharedCostsToInsert.Any())
            {
                await sharedProjectCostRepo.InsertRange(allSharedCostsToInsert);
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
            var currentUserDetails = await userRepo.GetById(currentUser.Id);
            if (currentUserDetails == null)
                return;

            string sharerName = $"{currentUserDetails.FirstName} {currentUserDetails.LastName}".Trim();

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

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == sharedWithUserId, cancellationToken);

            var notificationDto = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = sharedWithUserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = NotificationTypeDto.Info,
                Title = title,
                Message = message,
                Metadata = metadata,
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false
            };

            await notificationSender.EnqueueAsync(notificationDto, cancellationToken);

            logger.LogInformation(
                "Notification sent to user {UserId} about {CostCount} shared costs",
                sharedWithUserId, costCount);
        }
    }
}
