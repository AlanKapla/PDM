using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.ProjectCosts.UpdateCostShare
{
    public class UpdateCostShareCommandHandler : IRequestHandler<UpdateCostShareCommand, Unit>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IRepository<Project> projectRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateCostShareCommandHandler> logger;

        public UpdateCostShareCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IRepository<SharedProjectCost> sharedProjectCostRepo,
            IRepository<Project> projectRepo,
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<UpdateCostShareCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.projectRepo = projectRepo;
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateCostShareCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify cost exists and belongs to the correct project/tenant
            var cost = await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId 
                    && pc.TenantId == request.TenantId 
                    && pc.ProjectId == request.ProjectId 
                    && !pc.IsDeleted,
                query => query.Include(pc => pc.SharedWith))
                ?? throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR cost owner
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isCostOwner = cost.UserId == currentUser.Id;
            
            if (!isAdmin && !isCostOwner)
            {
                throw new NotFoundApiException(nameof(ProjectCost), request.CostId.ToString());
            }

            var existingUserIds = cost.SharedWith.Select(s => s.SharedWithUserId).ToHashSet();
            var requestedUserIds = request.SharedWithUserIds.ToHashSet();

            var usersToAdd = requestedUserIds.Except(existingUserIds).ToList();
            var usersToRemove = existingUserIds.Except(requestedUserIds).ToList();

            var allAffectedUserIds = usersToAdd.Union(usersToRemove).ToList();
            var affectedUsers = await userRepo.GetBySearch(u => allAffectedUserIds.Contains(u.Id));
            var userDict = affectedUsers.ToDictionary(u => u.Id);

            // 4. Remove shares that are no longer in the list
            if (usersToRemove.Any())
            {
                var sharesToRemove = cost.SharedWith
                    .Where(s => usersToRemove.Contains(s.SharedWithUserId))
                    .ToList();

                await sharedProjectCostRepo.DeleteRange(sharesToRemove);

                // Send notifications to users who lost access
                foreach (var userId in usersToRemove)
                {
                    userDict.TryGetValue(userId, out User? targetUser);

                    var notification = new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        UserId = userId,
                        AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                        Type = NotificationType.Info,
                        Title = "Odebrano dostęp do kosztu",
                        Message = $"{currentUser.FirstName} {currentUser.LastName} odebrał Ci dostęp do kosztu: {cost.Name}",
                        CreatedAt = DateTimeOffset.UtcNow,
                        Readed = false,
                        Metadata = new Dictionary<string, object?>
                        {
                            { "costId", request.CostId },
                            { "costName", cost.Name },
                            { "removedByUserId", currentUser.Id },
                            { "action", "unshared" }
                        }
                    };

                    var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                    await notificationSender.EnqueueAsync(payload, cancellationToken);
                }

                logger.LogInformation(
                    "Cost {CostId} unshared from {UserCount} users",
                    request.CostId, usersToRemove.Count);
            }

            // 5. Add new shares
            if (usersToAdd.Any())
            {
                var newShares = usersToAdd.Select(userId => new SharedProjectCost
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    ProjectCostId = request.CostId,
                    SharedWithUserId = userId,
                    SharedByUserId = currentUser.Id,
                    SharedAt = DateTime.UtcNow
                }).ToList();

                await sharedProjectCostRepo.InsertRange(newShares);

                // Send notifications to users who gained access
                foreach (var userId in usersToAdd)
                {
                    userDict.TryGetValue(userId, out User? targetUser);

                    var notification = new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        UserId = userId,
                        AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                        Type = NotificationType.Success,
                        Title = "Udostępniono Ci koszt",
                        Message = $"{currentUser.FirstName} {currentUser.LastName} udostępnił Ci koszt: {cost.Name}",
                        CreatedAt = DateTimeOffset.UtcNow,
                        Readed = false,
                        Metadata = new Dictionary<string, object?>
                        {
                            { "costId", request.CostId },
                            { "costName", cost.Name },
                            { "sharedByUserId", currentUser.Id },
                            { "action", "shared" }
                        }
                    };

                    var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                    await notificationSender.EnqueueAsync(payload, cancellationToken);
                }

                logger.LogInformation(
                    "Cost {CostId} shared with {UserCount} new users",
                    request.CostId, usersToAdd.Count);
            }

            // 6. Save all changes
            await sharedProjectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} now shared with {TotalCount} users by {UserId}",
                request.CostId, request.SharedWithUserIds.Count, currentUser.Id);

            return Unit.Value;
        }
    }
}
