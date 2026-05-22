using Business.Interfaces.Services;
using Entities.Models.Files;

namespace Business.Implementation.Services.Files
{
    /// <summary>
    /// Pure computation of share-row diffs for <see cref="UpdateFileShareCommand"/>-style operations.
    /// Stateless, no I/O — safe to register as singleton.
    /// </summary>
    public sealed class FileShareDiffService : IFileShareDiffService
    {
        public FileShareDiffResult Compute(FileShareDiffInput input)
        {
            HashSet<Guid> targetUsers = input.TargetUserIds.ToHashSet();
            List<SharedProjectFile> sharesToInsert = new List<SharedProjectFile>();
            List<SharedProjectFile> sharesToDelete = new List<SharedProjectFile>();

            foreach (Guid userId in targetUsers)
            {
                PrepareGrantAccess(userId, input, sharesToInsert, sharesToDelete);
            }

            HashSet<Guid> usersWithAccess = GetUsersWithAccessToFile(input.FileId, input.ExistingPackageShares);
            List<Guid> usersToRevoke = usersWithAccess
                .Except(targetUsers)
                .Where(userId => userId != input.CurrentUserId)
                .ToList();

            foreach (Guid userId in usersToRevoke)
            {
                PrepareRevokeAccess(userId, input, sharesToInsert, sharesToDelete);
            }

            List<Guid> usersGrantedAccess = ComputeUsersGrantedAccess(targetUsers, input.FileId, sharesToInsert, sharesToDelete);
            List<Guid> usersRevokedAccess = ComputeUsersRevokedAccess(usersToRevoke, input.FileId, sharesToInsert, sharesToDelete);

            return new FileShareDiffResult
            {
                SharesToInsert = sharesToInsert,
                SharesToDelete = sharesToDelete,
                UsersGrantedAccess = usersGrantedAccess,
                UsersRevokedAccess = usersRevokedAccess,
            };
        }

        /// <summary>
        /// Returns user ids that effectively have access to the file:
        /// (Package shared AND no Deny) OR explicit Allow on the file.
        /// </summary>
        private static HashSet<Guid> GetUsersWithAccessToFile(
            Guid fileId,
            IEnumerable<SharedProjectFile> allPackageShares)
        {
            HashSet<Guid> usersWithAccess = new HashSet<Guid>();
            IEnumerable<IGrouping<Guid, SharedProjectFile>> sharesByUser = allPackageShares.GroupBy(s => s.SharedWithUserId);

            foreach (IGrouping<Guid, SharedProjectFile> userShares in sharesByUser)
            {
                SharedProjectFile? packageShare = userShares.FirstOrDefault(s => s.ProjectFileId == null);
                SharedProjectFile? fileShare = userShares.FirstOrDefault(s => s.ProjectFileId == fileId);

                bool hasAccess;
                if (fileShare?.Access == ProjectFileAccess.Deny)
                {
                    hasAccess = false;
                }
                else if (fileShare?.Access == ProjectFileAccess.Allow)
                {
                    hasAccess = true;
                }
                else
                {
                    hasAccess = packageShare is not null;
                }

                if (hasAccess)
                {
                    usersWithAccess.Add(userShares.Key);
                }
            }

            return usersWithAccess;
        }

        private static void PrepareGrantAccess(
            Guid userId,
            FileShareDiffInput input,
            List<SharedProjectFile> sharesToInsert,
            List<SharedProjectFile> sharesToDelete)
        {
            SharedProjectFile? packageShare = input.ExistingPackageShares.FirstOrDefault(
                s => s.ProjectFilePackageId == input.PackageId
                    && s.ProjectFileId == null
                    && s.SharedWithUserId == userId);

            SharedProjectFile? fileShare = input.ExistingPackageShares.FirstOrDefault(
                s => s.ProjectFileId == input.FileId
                    && s.SharedWithUserId == userId);

            if (packageShare is not null)
            {
                if (fileShare?.Access == ProjectFileAccess.Deny)
                {
                    sharesToDelete.Add(fileShare);
                }
                return;
            }

            if (fileShare is null)
            {
                sharesToInsert.Add(BuildShareRow(input, userId, ProjectFileAccess.Allow));
            }
            else if (fileShare.Access == ProjectFileAccess.Deny)
            {
                sharesToDelete.Add(fileShare);
                sharesToInsert.Add(BuildShareRow(input, userId, ProjectFileAccess.Allow));
            }
        }

        private static void PrepareRevokeAccess(
            Guid userId,
            FileShareDiffInput input,
            List<SharedProjectFile> sharesToInsert,
            List<SharedProjectFile> sharesToDelete)
        {
            SharedProjectFile? packageShare = input.ExistingPackageShares.FirstOrDefault(
                s => s.ProjectFilePackageId == input.PackageId
                    && s.ProjectFileId == null
                    && s.SharedWithUserId == userId);

            SharedProjectFile? fileShare = input.ExistingPackageShares.FirstOrDefault(
                s => s.ProjectFileId == input.FileId
                    && s.SharedWithUserId == userId);

            if (packageShare is not null)
            {
                if (fileShare?.Access == ProjectFileAccess.Deny)
                {
                    return;
                }

                if (fileShare?.Access == ProjectFileAccess.Allow)
                {
                    sharesToDelete.Add(fileShare);
                }

                sharesToInsert.Add(BuildShareRow(input, userId, ProjectFileAccess.Deny));
            }
            else if (fileShare?.Access == ProjectFileAccess.Allow)
            {
                sharesToDelete.Add(fileShare);
            }
        }

        private static SharedProjectFile BuildShareRow(FileShareDiffInput input, Guid userId, ProjectFileAccess access) =>
            new SharedProjectFile
            {
                TenantId = input.TenantId,
                ProjectId = input.ProjectId,
                ProjectFilePackageId = input.PackageId,
                ProjectFileId = input.FileId,
                Access = access,
                SharedByUserId = input.CurrentUserId,
                SharedWithUserId = userId,
                SharedAt = DateTime.UtcNow,
            };

        private static List<Guid> ComputeUsersGrantedAccess(
            IEnumerable<Guid> targetUsers,
            Guid fileId,
            List<SharedProjectFile> sharesToInsert,
            List<SharedProjectFile> sharesToDelete)
        {
            List<Guid> result = new List<Guid>();
            foreach (Guid userId in targetUsers)
            {
                bool wasGranted = sharesToInsert.Any(s =>
                    s.SharedWithUserId == userId
                    && s.ProjectFileId == fileId
                    && s.Access == ProjectFileAccess.Allow);

                bool denyWasRemoved = sharesToDelete.Any(s =>
                    s.SharedWithUserId == userId
                    && s.ProjectFileId == fileId
                    && s.Access == ProjectFileAccess.Deny);

                if (wasGranted || denyWasRemoved)
                {
                    result.Add(userId);
                }
            }
            return result;
        }

        private static List<Guid> ComputeUsersRevokedAccess(
            IEnumerable<Guid> usersToRevoke,
            Guid fileId,
            List<SharedProjectFile> sharesToInsert,
            List<SharedProjectFile> sharesToDelete)
        {
            List<Guid> result = new List<Guid>();
            foreach (Guid userId in usersToRevoke)
            {
                bool wasRevoked = sharesToInsert.Any(s =>
                    s.SharedWithUserId == userId
                    && s.ProjectFileId == fileId
                    && s.Access == ProjectFileAccess.Deny);

                bool allowWasRemoved = sharesToDelete.Any(s =>
                    s.SharedWithUserId == userId
                    && s.ProjectFileId == fileId
                    && s.Access == ProjectFileAccess.Allow);

                if (wasRevoked || allowWasRemoved)
                {
                    result.Add(userId);
                }
            }
            return result;
        }
    }
}
