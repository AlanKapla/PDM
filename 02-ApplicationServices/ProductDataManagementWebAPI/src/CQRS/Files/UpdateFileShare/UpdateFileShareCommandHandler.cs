using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using MediatR;
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
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly IProjectFilesService projectFilesService;
        private readonly ICurrentUser currentUser;
        private readonly INotificationSender notificationSender;
        private readonly ILogger<UpdateFileShareCommandHandler> logger;

        public UpdateFileShareCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            IProjectFilesService projectFilesService,
            ICurrentUser currentUser,
            INotificationSender notificationSender,
            ILogger<UpdateFileShareCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.projectFilesService = projectFilesService;
            this.currentUser = currentUser;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateFileShareCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify file exists and belongs to the correct project/tenant
            var file = await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId
                    && pf.ProjectId == request.ProjectId
                    && pf.TenantId == request.TenantId
                    && !pf.IsDeleted)
                ?? throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR file owner
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isFileOwner = file.OwnerId == currentUser.Id;
            
            if (!isAdmin && !isFileOwner)
            {
                throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
            }

            // 3. Get all shares for this package (we need to check if package is shared)
            var packageId = file.ProjectFilePackageId;
            var allPackageShares = await sharedProjectFileRepo.GetBySearch(
                spf => spf.ProjectFilePackageId == packageId);

            // 4. Get owner info for notifications
            var owner = await userRepo.GetFirstBySearch(u => u.Id == file.OwnerId);
            string ownerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unknown";

            // 5. Get user details for notifications
            var affectedUserIds = request.SharedWithUserIds
                .Concat(GetUsersWithAccessToFile(request.FileId, allPackageShares))
                .Distinct()
                .ToList();
            
            var users = await userRepo.GetBySearch(u => affectedUserIds.Contains(u.Id));
            var userDict = users.ToDictionary(u => u.Id);

            // 6. Zbierz wszystkie operacje do wykonania (batch)
            var sharesToInsert = new List<SharedProjectFile>();
            var sharesToDelete = new List<SharedProjectFile>();

            // 7. Process each user - zbierz operacje
            foreach (var userId in request.SharedWithUserIds)
            {
                PrepareGrantAccessOperations(
                    userId, 
                    file, 
                    packageId, 
                    allPackageShares,
                    request,
                    sharesToInsert,
                    sharesToDelete);
            }

            // 8. Revoke access for users NOT in the list - zbierz operacje
            // WAŻNE: NIE cofamy dostępu current userowi (może być adminem z wcześniejszym udostępnieniem)
            var usersWithAccess = GetUsersWithAccessToFile(request.FileId, allPackageShares);
            var usersToRevoke = usersWithAccess
                .Except(request.SharedWithUserIds)
                .Where(userId => userId != currentUser.Id)  // Nie cofamy dostępu sobie samemu
                .ToList();

            foreach (var userId in usersToRevoke)
            {
                PrepareRevokeAccessOperations(
                    userId, 
                    file, 
                    packageId, 
                    allPackageShares,
                    request,
                    sharesToInsert,
                    sharesToDelete);
            }

            // 9. Wykonaj wszystkie operacje w batch (2 queries zamiast N)
            if (sharesToDelete.Any())
            {
                await sharedProjectFileRepo.DeleteRange(sharesToDelete);
            }

            if (sharesToInsert.Any())
            {
                foreach (var share in sharesToInsert)
                {
                    await sharedProjectFileRepo.Insert(share);
                }
            }

            await sharedProjectFileRepo.SaveChangesAsync(cancellationToken);

            // Invalidate file access cache after sharing changes
            await projectFilesService.InvalidateFileAccessCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

            // 10. Wyślij notyfikacje po zapisaniu zmian
            // Notyfikacje dla userów którzy DOSTALI dostęp
            foreach (var userId in request.SharedWithUserIds)
            {
                // Sprawdź czy faktycznie dodaliśmy/zmieniliśmy dostęp
                var wasGranted = sharesToInsert.Any(s => 
                    s.SharedWithUserId == userId && 
                    s.ProjectFileId == request.FileId &&
                    s.Access == ProjectFileAccess.Allow);
                
                var denyWasRemoved = sharesToDelete.Any(s =>
                    s.SharedWithUserId == userId &&
                    s.ProjectFileId == request.FileId &&
                    s.Access == ProjectFileAccess.Deny);

                if (wasGranted || denyWasRemoved)
                {
                    await SendAccessGrantedNotificationAsync(userId, file, ownerName, userDict, request, cancellationToken);
                }
            }

            // Notyfikacje dla userów którzy STRACILI dostęp
            foreach (var userId in usersToRevoke)
            {
                // Sprawdź czy faktycznie cofnęliśmy dostęp
                var wasRevoked = sharesToInsert.Any(s => 
                    s.SharedWithUserId == userId && 
                    s.ProjectFileId == request.FileId &&
                    s.Access == ProjectFileAccess.Deny);
                
                var allowWasRemoved = sharesToDelete.Any(s =>
                    s.SharedWithUserId == userId &&
                    s.ProjectFileId == request.FileId &&
                    s.Access == ProjectFileAccess.Allow);

                if (wasRevoked || allowWasRemoved)
                {
                    await SendAccessRevokedNotificationAsync(userId, file, ownerName, userDict, request, cancellationToken);
                }
            }

            logger.LogInformation(
                "File sharing updated for {FileId}: {GrantedCount} users granted, {RevokedCount} revoked, " +
                "{InsertCount} inserts, {DeleteCount} deletes",
                request.FileId, request.SharedWithUserIds.Count, usersToRevoke.Count,
                sharesToInsert.Count, sharesToDelete.Count);

            return Unit.Value;
        }

        /// <summary>
        /// Zwraca IDs wszystkich userów, którzy mają dostęp do pliku
        /// Uwzględnia: Allow dla pliku OR (Paczka shared AND NIE ma Deny)
        /// </summary>
        private HashSet<Guid> GetUsersWithAccessToFile(
            Guid fileId,
            IEnumerable<SharedProjectFile> allPackageShares)
        {
            var usersWithAccess = new HashSet<Guid>();

            // Grupuj shares po userId
            var sharesByUser = allPackageShares.GroupBy(s => s.SharedWithUserId);

            foreach (var userShares in sharesByUser)
            {
                var userId = userShares.Key;
                
                var packageShare = userShares.FirstOrDefault(s => s.ProjectFileId == null);
                var fileShare = userShares.FirstOrDefault(s => s.ProjectFileId == fileId);

                bool hasAccess = false;

                // Logika dostępu: (Package shared AND NIE Deny) OR Allow
                if (fileShare?.Access == ProjectFileAccess.Deny)
                {
                    hasAccess = false;  // Deny ma priorytet
                }
                else if (fileShare?.Access == ProjectFileAccess.Allow)
                {
                    hasAccess = true;  // Jawny Allow
                }
                else if (packageShare != null)
                {
                    hasAccess = true;  // Dostęp przez paczkę (i brak Deny)
                }

                if (hasAccess)
                {
                    usersWithAccess.Add(userId);
                }
            }

            return usersWithAccess;
        }

        /// <summary>
        /// Przygotowuje operacje do udzielenia dostępu (bez wykonywania queries)
        /// ✅ Delete + Insert zamiast Update (unika problemów z EF tracking)
        /// </summary>
        private void PrepareGrantAccessOperations(
            Guid userId,
            ProjectFile file,
            Guid packageId,
            IEnumerable<SharedProjectFile> allPackageShares,
            UpdateFileShareCommand request,
            List<SharedProjectFile> sharesToInsert,
            List<SharedProjectFile> sharesToDelete)
        {
            var packageShare = allPackageShares.FirstOrDefault(
                s => s.ProjectFilePackageId == packageId 
                    && s.ProjectFileId == null 
                    && s.SharedWithUserId == userId);

            var fileShare = allPackageShares.FirstOrDefault(
                s => s.ProjectFileId == file.Id 
                    && s.SharedWithUserId == userId);

            if (packageShare != null)
            {
                // Paczka udostępniona
                if (fileShare?.Access == ProjectFileAccess.Deny)
                {
                    // Usuń Deny - user dostanie dostęp przez paczkę
                    sharesToDelete.Add(fileShare);
                }
                // Jeśli brak Deny - user już ma dostęp, nic nie rób
            }
            else
            {
                // Paczka NIE udostępniona
                if (fileShare == null)
                {
                    // Dodaj Allow
                    sharesToInsert.Add(new SharedProjectFile
                    {
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectFilePackageId = packageId,
                        ProjectFileId = file.Id,
                        Access = ProjectFileAccess.Allow,
                        SharedByUserId = currentUser.Id,
                        SharedWithUserId = userId,
                        SharedAt = DateTime.UtcNow
                    });
                }
                else if (fileShare.Access == ProjectFileAccess.Deny)
                {
                    // ✅ Zmień Deny na Allow: Delete + Insert (zamiast Update)
                    sharesToDelete.Add(fileShare);
                    sharesToInsert.Add(new SharedProjectFile
                    {
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectFilePackageId = packageId,
                        ProjectFileId = file.Id,
                        Access = ProjectFileAccess.Allow,
                        SharedByUserId = currentUser.Id,
                        SharedWithUserId = userId,
                        SharedAt = DateTime.UtcNow
                    });
                }
                // Jeśli ma Allow - nic nie rób
            }
        }

        /// <summary>
        /// Przygotowuje operacje do cofnięcia dostępu (bez wykonywania queries)
        /// </summary>
        private void PrepareRevokeAccessOperations(
            Guid userId,
            ProjectFile file,
            Guid packageId,
            IEnumerable<SharedProjectFile> allPackageShares,
            UpdateFileShareCommand request,
            List<SharedProjectFile> sharesToInsert,
            List<SharedProjectFile> sharesToDelete)
        {
            var packageShare = allPackageShares.FirstOrDefault(
                s => s.ProjectFilePackageId == packageId 
                    && s.ProjectFileId == null 
                    && s.SharedWithUserId == userId);

            var fileShare = allPackageShares.FirstOrDefault(
                s => s.ProjectFileId == file.Id 
                    && s.SharedWithUserId == userId);

            if (packageShare != null)
            {
                if (fileShare?.Access == ProjectFileAccess.Deny)
                {
                    return;
                }

                if (fileShare?.Access == ProjectFileAccess.Allow)
                {
                    sharesToDelete.Add(fileShare);
                }

                sharesToInsert.Add(new SharedProjectFile
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    ProjectFilePackageId = packageId,
                    ProjectFileId = file.Id,
                    Access = ProjectFileAccess.Deny,
                    SharedByUserId = currentUser.Id,
                    SharedWithUserId = userId,
                    SharedAt = DateTime.UtcNow
                });
            }
            else
            {
                if (fileShare?.Access == ProjectFileAccess.Allow)
                {
                    sharesToDelete.Add(fileShare);
                }
            }
        }

        private async Task SendAccessGrantedNotificationAsync(
            Guid userId,
            ProjectFile file,
            string ownerName,
            Dictionary<Guid, User> userDict,
            UpdateFileShareCommand request,
            CancellationToken cancellationToken)
        {
            if (userDict.TryGetValue(userId, out var user))
            {
                var notification = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    UserId = userId,
                    AzureAdB2CObjectId = user.AzureAdB2CObjectId,
                    Type = NotifType.Info,
                    Title = "Udostępniono Ci plik",
                    Message = $"{ownerName} udostępnił Ci plik \"{file.DisplayName}\"",
                    Readed = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["FileId"] = request.FileId,
                        ["EntityType"] = "ProjectFile"
                    }
                };

                var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }
        }

        private async Task SendAccessRevokedNotificationAsync(
            Guid userId,
            ProjectFile file,
            string ownerName,
            Dictionary<Guid, User> userDict,
            UpdateFileShareCommand request,
            CancellationToken cancellationToken)
        {
            if (userDict.TryGetValue(userId, out var user))
            {
                var notification = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    UserId = userId,
                    AzureAdB2CObjectId = user.AzureAdB2CObjectId,
                    Type = NotifType.Warning,
                    Title = "Cofnięto dostęp do pliku",
                    Message = $"{ownerName} cofnął Ci dostęp do pliku \"{file.DisplayName}\"",
                    Readed = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["FileId"] = request.FileId,
                        ["EntityType"] = "ProjectFile"
                    }
                };

                var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }
        }
    }
}
