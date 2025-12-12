using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationTypeDto = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Files.ShareProjectFiles
{
    public class ShareProjectFilesCommandHandler : IRequestHandler<ShareProjectFilesCommand, Unit>
    {
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ShareProjectFilesCommandHandler> logger;

        public ShareProjectFilesCommandHandler(
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IRepository<ProjectFile> projectFileRepo,
            IReadRepository<User> userRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ShareProjectFilesCommandHandler> logger)
        {
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.projectFileRepo = projectFileRepo;
            this.userRepo = userRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(ShareProjectFilesCommand request, CancellationToken cancellationToken)
        {
            // Get all existing shares for all users and files in one query
            var existingShares = await sharedProjectFileRepo.GetBySearch(
                spf => request.SharedWithUserIds.Contains(spf.SharedWithUserId) &&
                       request.ProjectFileIds.Contains(spf.ProjectFileId));

            var existingSharesDict = existingShares
                .GroupBy(spf => spf.SharedWithUserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(spf => spf.ProjectFileId).ToHashSet()
                );

            // Get file details for notification once
            var projectFiles = await projectFileRepo.GetBySearch(
                pf => request.ProjectFileIds.Contains(pf.Id) && !pf.IsDeleted);

            var fileNamesDict = projectFiles.ToDictionary(pf => pf.Id, pf => pf.DisplayName);

            var allSharedFilesToInsert = new List<SharedProjectFile>();

            // For each user, determine files to share
            foreach (var userId in request.SharedWithUserIds)
            {
                var existingFileIds = existingSharesDict.ContainsKey(userId) 
                    ? existingSharesDict[userId] 
                    : new HashSet<Guid>();

                var filesToShare = request.ProjectFileIds.Where(id => !existingFileIds.Contains(id)).ToList();

                if (!filesToShare.Any())
                    continue;

                // Prepare shares for this user
                foreach (var fileId in filesToShare)
                {
                    var sharedFile = new SharedProjectFile
                    {
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectFileId = fileId,
                        SharedByUserId = currentUser.Id,
                        SharedWithUserId = userId,
                        SharedAt = DateTime.UtcNow
                    };

                    allSharedFilesToInsert.Add(sharedFile);
                }

                logger.LogInformation(
                    "User {UserId} will share {FileCount} files with user {SharedWithUserId} in project {ProjectId}",
                    currentUser.Id, filesToShare.Count, userId, request.ProjectId);

                // Send notification to this user
                await SendNotificationAsync(request, userId, filesToShare.Count, fileNamesDict, cancellationToken);
            }

            // Insert all shared files in one batch
            if (allSharedFilesToInsert.Any())
            {
                await sharedProjectFileRepo.InsertRange(allSharedFilesToInsert);
            }

            return Unit.Value;
        }

        private async Task SendNotificationAsync(
            ShareProjectFilesCommand request,
            Guid sharedWithUserId,
            int fileCount,
            Dictionary<Guid, string> fileNamesDict,
            CancellationToken cancellationToken)
        {
            var currentUserDetails = await userRepo.GetById(currentUser.Id);
            if (currentUserDetails == null)
                return;

            string sharerName = $"{currentUserDetails.FirstName} {currentUserDetails.LastName}".Trim();

            string title;
            string message;

            if (fileCount == 1)
            {
                var fileName = fileNamesDict.Values.FirstOrDefault() ?? "plik";
                title = "Udostępniono Ci plik";
                message = $"{sharerName} udostępnił Ci plik: {fileName}";
            }
            else
            {
                title = "Udostępniono Ci pliki";
                message = $"{sharerName} udostępnił Ci {fileCount} plików";
            }

            var metadata = new Dictionary<string, object?>
            {
                ["ProjectId"] = request.ProjectId,
                ["SharedByUserId"] = currentUser.Id,
                ["SharedByUserName"] = sharerName,
                ["FileCount"] = fileCount,
                ["FileNames"] = fileNamesDict.Values.Take(5).ToList()
            };

            var notificationDto = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                UserId = sharedWithUserId,
                Type = NotificationTypeDto.Info,
                Title = title,
                Message = message,
                Metadata = metadata,
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false
            };

            await notificationSender.EnqueueAsync(notificationDto, cancellationToken);

            logger.LogInformation(
                "Notification sent to user {UserId} about {FileCount} shared files",
                sharedWithUserId, fileCount);
        }
    }
}
