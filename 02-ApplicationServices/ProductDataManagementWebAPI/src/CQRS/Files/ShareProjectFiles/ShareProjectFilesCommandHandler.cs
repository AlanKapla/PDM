using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationTypeDto = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Files.ShareProjectFiles
{
    public class ShareProjectFilesCommandHandler : IRequestHandler<ShareProjectFilesCommand, Unit>
    {
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ShareProjectFilesCommandHandler> logger;

        public ShareProjectFilesCommandHandler(
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IRepository<ProjectFile> projectFileRepo,
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ShareProjectFilesCommandHandler> logger)
        {
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.projectFileRepo = projectFileRepo;
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(ShareProjectFilesCommand request, CancellationToken cancellationToken)
        {
            // 1. Get files and verify they exist and belong to the correct project/tenant
            var projectFiles = await projectFileRepo.GetBySearch(
                pf => request.ProjectFileIds.Contains(pf.Id) 
                    && pf.ProjectId == request.ProjectId
                    && pf.TenantId == request.TenantId
                    && !pf.IsDeleted);

            if (projectFiles.Count() != request.ProjectFileIds.Count())
            {
                throw new NotFoundApiException(nameof(ProjectFile), "One or more files not found");
            }

            // 2. Authorization check: tenant admin OR project admin OR owner of ALL files
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            
            if (!isAdmin)
            {
                var notOwnedFiles = projectFiles.Where(pf => pf.OwnerId != currentUser.Id).ToList();
                if (notOwnedFiles.Any())
                {
                    throw new NotFoundApiException(nameof(ProjectFile), "One or more files not found");
                }
            }

            // 3. Get all existing shares for all users and files in one query
            var existingShares = await sharedProjectFileRepo.GetBySearch(
                spf => request.SharedWithUserIds.Contains(spf.SharedWithUserId) &&
                       request.ProjectFileIds.Contains(spf.ProjectFileId));

            var existingSharesDict = existingShares
                .GroupBy(spf => spf.SharedWithUserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(spf => spf.ProjectFileId).ToHashSet()
                );

            var fileNamesDict = projectFiles.ToDictionary(pf => pf.Id, pf => pf.DisplayName);

            var allSharedFilesToInsert = new List<SharedProjectFile>();

            // 4. For each user, determine files to share
            foreach (var userId in request.SharedWithUserIds)
            {
                var existingFileIds = existingSharesDict.ContainsKey(userId) 
                    ? existingSharesDict[userId] 
                    : new HashSet<Guid>();

                var filesToShare = request.ProjectFileIds.Where(id => !existingFileIds.Contains(id)).ToList();

                if (!filesToShare.Any())
                {
                    continue;
                }

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

            // 5. Insert all shared files in one batch
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
            {
                return;
            }

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

            var payload = await NotificationPayloadHelper.CreatePayloadAsync(notificationDto, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            logger.LogInformation(
                "Notification sent to user {UserId} about {FileCount} shared files",
                sharedWithUserId, fileCount);
        }
    }
}
