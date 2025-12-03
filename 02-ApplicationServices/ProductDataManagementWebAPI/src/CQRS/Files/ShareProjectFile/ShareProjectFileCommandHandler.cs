using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationTypeDto = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Files.ShareProjectFile
{
    public class ShareProjectFileCommandHandler : IRequestHandler<ShareProjectFileCommand, ShareProjectFileResult>
    {
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly IReadRepository<ProjectFile> projectFileRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<ShareProjectFileCommandHandler> logger;

        public ShareProjectFileCommandHandler(
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IReadRepository<ProjectFile> projectFileRepo,
            IReadRepository<User> userRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<ShareProjectFileCommandHandler> logger)
        {
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.projectFileRepo = projectFileRepo;
            this.userRepo = userRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<ShareProjectFileResult> Handle(ShareProjectFileCommand request, CancellationToken cancellationToken)
        {
            var sharedFileIds = new List<Guid>();
            var errors = new List<string>();
            int successCount = 0;
            int failedCount = 0;

            // Pobierz wszystkie istniejące udostępnienia dla tego użytkownika w jednym zapytaniu
            var existingShares = await sharedProjectFileRepo.GetBySearch(
                spf => spf.SharedWithUserId == request.SharedWithUserId &&
                       request.ProjectFileIds.Contains(spf.ProjectFileId));

            // Zbuduj słownik dla szybkiego sprawdzania (O(1) zamiast O(n))
            var existingSharesDict = existingShares
                .ToDictionary(spf => spf.ProjectFileId, spf => spf.Id);

            // Pobierz nazwy plików dla powiadomienia
            var projectFiles = await projectFileRepo.GetBySearch(
                pf => request.ProjectFileIds.Contains(pf.Id));
            var fileNamesDict = projectFiles.ToDictionary(pf => pf.Id, pf => pf.DisplayName);

            foreach (var fileId in request.ProjectFileIds)
            {
                try
                {
                    // Sprawdź w słowniku czy plik już jest udostępniony
                    if (existingSharesDict.ContainsKey(fileId))
                    {
                        logger.LogWarning(
                            "File {FileId} already shared with user {SharedWithUserId}, skipping",
                            fileId, request.SharedWithUserId);
                        
                        errors.Add($"Plik {fileId} został już udostępniony temu użytkownikowi");
                        failedCount++;
                        continue;
                    }

                    var sharedFile = new SharedProjectFile
                    {
                        Id = Guid.NewGuid(),
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectFileId = fileId,
                        SharedByUserId = currentUser.Id,
                        SharedWithUserId = request.SharedWithUserId,
                        SharedAt = DateTime.UtcNow
                    };

                    await sharedProjectFileRepo.Insert(sharedFile);
                    sharedFileIds.Add(sharedFile.Id);
                    successCount++;

                    logger.LogInformation(
                        "User {UserId} shared file {FileId} with user {SharedWithUserId} in project {ProjectId}",
                        currentUser.Id, fileId, request.SharedWithUserId, request.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error sharing file {FileId} with user {SharedWithUserId}",
                        fileId, request.SharedWithUserId);
                    
                    errors.Add($"Błąd podczas udostępniania pliku {fileId}: {ex.Message}");
                    failedCount++;
                }
            }

            // Wyślij powiadomienie jeśli udostępniono przynajmniej jeden plik
            if (successCount > 0)
            {
                await SendNotificationAsync(request, successCount, fileNamesDict, sharedFileIds, cancellationToken);
            }

            logger.LogInformation(
                "Share operation completed: {SuccessCount} succeeded, {FailedCount} failed",
                successCount, failedCount);

            return new ShareProjectFileResult
            {
                SharedFileIds = sharedFileIds,
                SuccessCount = successCount,
                FailedCount = failedCount,
                Errors = errors
            };
        }

        private async Task SendNotificationAsync(
            ShareProjectFileCommand request, 
            int fileCount, 
            Dictionary<Guid, string> fileNamesDict,
            List<Guid> sharedFileIds,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserDetails = await userRepo.GetById(currentUser.Id);
                if (currentUserDetails == null)
                {
                    logger.LogWarning("Cannot create notification - current user not found");
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
                    ["SharedFileIds"] = sharedFileIds,
                    ["FileNames"] = fileNamesDict.Where(kvp => request.ProjectFileIds.Contains(kvp.Key))
                                                 .Select(kvp => kvp.Value)
                                                 .Take(5)
                                                 .ToList()
                };

                var notificationDto = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    UserId = request.SharedWithUserId,
                    Type = NotificationTypeDto.Info,
                    Title = title,
                    Message = message,
                    Metadata = metadata,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Readed = false
                };

                await notificationSender.EnqueueAsync(notificationDto, cancellationToken);

                logger.LogInformation(
                    "Notification enqueued for user {UserId} about {FileCount} shared files",
                    request.SharedWithUserId, fileCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Failed to enqueue notification for shared files");
                // Nie rzucamy wyjątku - powiadomienie jest opcjonalne
            }
        }
    }
}
