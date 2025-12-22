using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using NotifType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Files.UpdateFileShare
{
    public class UpdateFileShareCommandHandler : IRequestHandler<UpdateFileShareCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly IRepository<User> userRepo;
        private readonly ICurrentUser currentUser;
        private readonly INotificationSender notificationSender;
        private readonly ILogger<UpdateFileShareCommandHandler> logger;

        public UpdateFileShareCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IRepository<User> userRepo,
            ICurrentUser currentUser,
            INotificationSender notificationSender,
            ILogger<UpdateFileShareCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.userRepo = userRepo;
            this.currentUser = currentUser;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateFileShareCommand request, CancellationToken cancellationToken)
        {
            // Get file with current shares
            var file = (await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId
                    && pf.ProjectId == request.ProjectId
                    && pf.TenantId == request.TenantId
                    && !pf.IsDeleted,
                include => include.Include(pf => pf.SharedWith)
                                  .Include(pf => pf.Owner)))!;

            var currentShares = file.SharedWith.ToList();
            var currentUserIds = currentShares.Select(s => s.SharedWithUserId).ToHashSet();
            var newUserIds = request.SharedWithUserIds.ToHashSet();

            // Users to add (in new list but not in current)
            var usersToAdd = newUserIds.Except(currentUserIds).ToList();
            
            // Users to remove (in current but not in new list)
            var usersToRemove = currentUserIds.Except(newUserIds).ToList();

            // Get user details for notifications
            var affectedUserIds = usersToAdd.Concat(usersToRemove).ToList();
            var users = (await userRepo.GetBySearch(
                u => affectedUserIds.Contains(u.Id)))
                .ToList();
            var userDict = users.ToDictionary(u => u.Id);

            // Add new shares
            foreach (var userId in usersToAdd)
            {
                var sharedFile = new SharedProjectFile
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    ProjectFileId = request.FileId,
                    SharedByUserId = currentUser.Id,
                    SharedWithUserId = userId,
                    SharedAt = DateTime.UtcNow
                };

                await sharedProjectFileRepo.Insert(sharedFile);

                // Send notification to user who received access
                if (userDict.TryGetValue(userId, out var user))
                {
                    await notificationSender.EnqueueAsync(new NotificationDto
                    {
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        UserId = userId,
                        AzureAdB2CObjectId = user.AzureAdB2CObjectId,
                        Type = NotifType.Info,
                        Title = "Udostępniono Ci plik",
                        Message = $"{file.Owner.FirstName} {file.Owner.LastName} udostępnił Ci plik \"{file.DisplayName}\"",
                        Readed = false,
                        CreatedAt = DateTimeOffset.UtcNow,
                        Metadata = new Dictionary<string, object?>
                        {
                            ["FileId"] = request.FileId,
                            ["EntityType"] = "ProjectFile"
                        }
                    }, cancellationToken);
                }

                logger.LogInformation(
                    "File {FileId} shared with user {UserId} by {CurrentUserId} in project {ProjectId}",
                    request.FileId, userId, currentUser.Id, request.ProjectId);
            }

            // Remove shares
            foreach (var userId in usersToRemove)
            {
                var shareToRemove = currentShares.FirstOrDefault(s => s.SharedWithUserId == userId);
                if (shareToRemove != null)
                {
                    await sharedProjectFileRepo.Delete(shareToRemove);

                    // Send notification to user who lost access
                    if (userDict.TryGetValue(userId, out var user))
                    {
                        await notificationSender.EnqueueAsync(new NotificationDto
                        {
                            TenantId = request.TenantId,
                            ProjectId = request.ProjectId,
                            UserId = userId,
                            AzureAdB2CObjectId = user.AzureAdB2CObjectId,
                            Type = NotifType.Warning,
                            Title = "Cofnięto dostęp do pliku",
                            Message = $"{file.Owner.FirstName} {file.Owner.LastName} cofnął Ci dostęp do pliku \"{file.DisplayName}\"",
                            Readed = false,
                            CreatedAt = DateTimeOffset.UtcNow,
                            Metadata = new Dictionary<string, object?>
                            {
                                ["FileId"] = request.FileId,
                                ["EntityType"] = "ProjectFile"
                            }
                        }, cancellationToken);
                    }

                    logger.LogInformation(
                        "File {FileId} unshared from user {UserId} by {CurrentUserId} in project {ProjectId}",
                        request.FileId, userId, currentUser.Id, request.ProjectId);
                }
            }

            logger.LogInformation(
                "File sharing updated for {FileId}: {AddedCount} users added, {RemovedCount} users removed",
                request.FileId, usersToAdd.Count, usersToRemove.Count);

            return Unit.Value;
        }
    }
}
