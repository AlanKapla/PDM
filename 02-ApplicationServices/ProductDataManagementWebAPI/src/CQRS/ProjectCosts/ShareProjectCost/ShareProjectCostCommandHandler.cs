using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.ProjectCosts.ShareProjectCost
{
    public class ShareProjectCostCommandHandler : IRequestHandler<ShareProjectCostCommand, Unit>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IRepository<Project> projectRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ShareProjectCostCommandHandler> logger;

        public ShareProjectCostCommandHandler(
            IRepository<ProjectCost> projectCostRepo,
            IRepository<SharedProjectCost> sharedProjectCostRepo,
            IRepository<Project> projectRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ShareProjectCostCommandHandler> logger)
        {
            this.projectCostRepo = projectCostRepo;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.projectRepo = projectRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(ShareProjectCostCommand request, CancellationToken cancellationToken)
        {
            // ProjectMemberHandler already validated tenant isolation and project membership
            // Validator already checked cost ownership

            // Get existing cost with shared relationships (without Project to avoid unnecessary data)
            var cost = await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == request.CostId 
                    && pc.TenantId == request.TenantId 
                    && pc.ProjectId == request.ProjectId 
                    && !pc.IsDeleted,
                query => query.Include(pc => pc.SharedWith));

            if (cost == null)
            {
                throw new NotFoundApiException("ProjectCost", request.CostId.ToString());
            }

            // Verify ownership
            if (cost.UserId != currentUser.Id)
            {
                throw new ForbiddenApiException("Only the cost owner can share it");
            }

            // Get project name separately only if needed for notifications
            var project = await projectRepo.GetFirstBySearch(p => p.Id == request.ProjectId && p.IsActive);

            string projectName = project?.Name ?? string.Empty;

            var existingUserIds = cost.SharedWith.Select(s => s.SharedWithUserId).ToHashSet();
            var requestedUserIds = request.SharedWithUserIds.ToHashSet();

            // Find users to add (in request but not in existing)
            var usersToAdd = requestedUserIds.Except(existingUserIds).ToList();

            // Find users to remove (in existing but not in request)
            var usersToRemove = existingUserIds.Except(requestedUserIds).ToList();

            // Remove shares that are no longer in the list
            if (usersToRemove.Any())
            {
                var sharesToRemove = cost.SharedWith
                    .Where(s => usersToRemove.Contains(s.SharedWithUserId))
                    .ToList();

                await sharedProjectCostRepo.DeleteRange(sharesToRemove);

                // Send notifications to users who lost access
                foreach (var userId in usersToRemove)
                {
                    var notification = new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectName = projectName,
                        UserId = userId,
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

                    await notificationSender.EnqueueAsync(notification, cancellationToken);
                }

                logger.LogInformation(
                    "Cost {CostId} unshared from {UserCount} users",
                    request.CostId, usersToRemove.Count);
            }

            // Add new shares
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
                    var notification = new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectName = projectName,
                        UserId = userId,
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

                    await notificationSender.EnqueueAsync(notification, cancellationToken);
                }

                logger.LogInformation(
                    "Cost {CostId} shared with {UserCount} new users",
                    request.CostId, usersToAdd.Count);
            }

            // Save all changes (deletes and inserts)
            await sharedProjectCostRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Cost {CostId} now shared with {TotalCount} users by {UserId}",
                request.CostId, request.SharedWithUserIds.Count, currentUser.Id);

            return Unit.Value;
        }
    }
}
