using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Files;
using Entities.Models.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UpdateFileShare
{
    public sealed class UpdateFileShareCommandHandler : IRequestHandler<UpdateFileShareCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IProjectFilesService projectFilesService;
        private readonly IFileAccessGuard fileAccessGuard;
        private readonly IFileShareDiffService shareDiffService;
        private readonly IFileShareNotificationService notifications;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateFileShareCommandHandler> logger;

        public UpdateFileShareCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IReadRepository<User> userRepo,
            IProjectFilesService projectFilesService,
            IFileAccessGuard fileAccessGuard,
            IFileShareDiffService shareDiffService,
            IFileShareNotificationService notifications,
            ICurrentUser currentUser,
            ILogger<UpdateFileShareCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.userRepo = userRepo;
            this.projectFilesService = projectFilesService;
            this.fileAccessGuard = fileAccessGuard;
            this.shareDiffService = shareDiffService;
            this.notifications = notifications;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateFileShareCommand request, CancellationToken cancellationToken)
        {
            await fileAccessGuard.EnsureCanAccessFileAsync(
                request.TenantId, request.ProjectId, request.FileId, FileAccessKind.Share, cancellationToken);

            ProjectFile file = await GetAndValidateFileAsync(request, cancellationToken);
            IReadOnlyCollection<SharedProjectFile> existing = await LoadPackageSharesAsync(request, file.ProjectFilePackageId);

            FileShareDiffResult diff = shareDiffService.Compute(new FileShareDiffInput
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                PackageId = file.ProjectFilePackageId,
                FileId = file.Id,
                CurrentUserId = currentUser.Id,
                ExistingPackageShares = existing,
                TargetUserIds = request.SharedWithUserIds,
            });

            await ApplyDiffAsync(diff, cancellationToken);
            await projectFilesService.InvalidateFileAccessCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

            string ownerName = await ResolveOwnerNameAsync(file.OwnerId);
            await notifications.NotifyShareGrantedAsync(
                BuildNotificationContext(request, file, ownerName, diff.UsersGrantedAccess), cancellationToken);
            await notifications.NotifyShareRevokedAsync(
                BuildNotificationContext(request, file, ownerName, diff.UsersRevokedAccess), cancellationToken);

            logger.LogInformation(
                "File sharing updated for {FileId}: {GrantedCount} granted, {RevokedCount} revoked, {InsertCount} inserts, {DeleteCount} deletes",
                request.FileId, diff.UsersGrantedAccess.Count, diff.UsersRevokedAccess.Count,
                diff.SharesToInsert.Count, diff.SharesToDelete.Count);

            return Unit.Value;
        }

        private async Task<ProjectFile> GetAndValidateFileAsync(
            UpdateFileShareCommand request, CancellationToken cancellationToken)
        {
            ProjectFile? file = await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId
                    && pf.ProjectId == request.ProjectId
                    && pf.TenantId == request.TenantId);

            if (file is null)
            {
                throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
            }

            return file;
        }

        private async Task<IReadOnlyCollection<SharedProjectFile>> LoadPackageSharesAsync(
            UpdateFileShareCommand request, Guid packageId)
        {
            IEnumerable<SharedProjectFile> shares = await sharedProjectFileRepo.GetBySearch(
                spf => spf.ProjectFilePackageId == packageId
                    && spf.TenantId == request.TenantId
                    && spf.ProjectId == request.ProjectId);

            return shares.ToList();
        }

        private async Task ApplyDiffAsync(FileShareDiffResult diff, CancellationToken cancellationToken)
        {
            if (diff.SharesToDelete.Count > 0)
            {
                await sharedProjectFileRepo.DeleteRange(diff.SharesToDelete);
            }

            foreach (SharedProjectFile share in diff.SharesToInsert)
            {
                await sharedProjectFileRepo.Insert(share);
            }

            await sharedProjectFileRepo.SaveChangesAsync(cancellationToken);
        }

        private async Task<string> ResolveOwnerNameAsync(Guid ownerId)
        {
            User? owner = await userRepo.GetFirstBySearch(u => u.Id == ownerId);
            return owner is not null ? $"{owner.FirstName} {owner.LastName}" : "Unknown";
        }

        private static FileShareNotificationContext BuildNotificationContext(
            UpdateFileShareCommand request,
            ProjectFile file,
            string ownerName,
            IReadOnlyCollection<Guid> userIds) =>
            new FileShareNotificationContext
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                FileId = file.Id,
                FileDisplayName = file.DisplayName,
                OwnerName = ownerName,
                UserIds = userIds,
            };
    }
}
