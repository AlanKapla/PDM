using Business.Implementation.Services.Files;
using Business.Interfaces.Services;
using Entities.Models.Files;
using FluentAssertions;

namespace Business.Tests.Services.Files;

public class FileShareDiffServiceTests
{
    private readonly FileShareDiffService _sut = new FileShareDiffService();

    private static readonly Guid TenantId    = Guid.NewGuid();
    private static readonly Guid ProjectId   = Guid.NewGuid();
    private static readonly Guid PackageId   = Guid.NewGuid();
    private static readonly Guid FileId      = Guid.NewGuid();
    private static readonly Guid CurrentUser = Guid.NewGuid();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SharedProjectFile PackageShare(Guid userId, Guid? packageId = null) =>
        new SharedProjectFile
        {
            TenantId = TenantId, ProjectId = ProjectId,
            ProjectFilePackageId = packageId ?? PackageId,
            ProjectFileId = null,
            Access = ProjectFileAccess.Allow,
            SharedByUserId = CurrentUser,
            SharedWithUserId = userId,
            SharedAt = DateTime.UtcNow
        };

    private static SharedProjectFile FileShare(Guid userId, ProjectFileAccess access, Guid? fileId = null) =>
        new SharedProjectFile
        {
            TenantId = TenantId, ProjectId = ProjectId,
            ProjectFilePackageId = PackageId,
            ProjectFileId = fileId ?? FileId,
            Access = access,
            SharedByUserId = CurrentUser,
            SharedWithUserId = userId,
            SharedAt = DateTime.UtcNow
        };

    private FileShareDiffInput BuildInput(
        IReadOnlyCollection<Guid> targetUserIds,
        IReadOnlyCollection<SharedProjectFile> existingShares)
        => new FileShareDiffInput
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            PackageId = PackageId,
            FileId = FileId,
            CurrentUserId = CurrentUser,
            ExistingPackageShares = existingShares,
            TargetUserIds = targetUserIds
        };

    // ─── Grant Access scenarios ───────────────────────────────────────────────

    [Fact]
    public void Compute_UserInTargetNoExistingShare_InsertsAllowFileShare()
    {
        Guid userId = Guid.NewGuid();
        FileShareDiffInput input = BuildInput(
            targetUserIds: new[] { userId },
            existingShares: Array.Empty<SharedProjectFile>());

        FileShareDiffResult result = _sut.Compute(input);

        result.SharesToInsert.Should().ContainSingle(s =>
            s.SharedWithUserId == userId &&
            s.ProjectFileId == FileId &&
            s.Access == ProjectFileAccess.Allow);
        result.UsersGrantedAccess.Should().Contain(userId);
    }

    [Fact]
    public void Compute_UserInTargetWithPackageShareNoDenyFile_NoInsert()
    {
        Guid userId = Guid.NewGuid();
        // User already has package-level access — no file-level share needed
        FileShareDiffInput input = BuildInput(
            targetUserIds: new[] { userId },
            existingShares: new[] { PackageShare(userId) });

        FileShareDiffResult result = _sut.Compute(input);

        result.SharesToInsert.Should().BeEmpty();
        result.SharesToDelete.Should().BeEmpty();
        result.UsersGrantedAccess.Should().BeEmpty();
    }

    [Fact]
    public void Compute_UserInTargetWithPackageShareAndDenyFile_DeletesDeny()
    {
        Guid userId = Guid.NewGuid();
        SharedProjectFile deny = FileShare(userId, ProjectFileAccess.Deny);
        FileShareDiffInput input = BuildInput(
            targetUserIds: new[] { userId },
            existingShares: new[] { PackageShare(userId), deny });

        FileShareDiffResult result = _sut.Compute(input);

        result.SharesToDelete.Should().Contain(deny);
        result.SharesToInsert.Should().BeEmpty();
        // deny removed → user regains access
        result.UsersGrantedAccess.Should().Contain(userId);
    }

    [Fact]
    public void Compute_UserInTargetWithExistingAllowFileShare_NoChange()
    {
        Guid userId = Guid.NewGuid();
        SharedProjectFile allow = FileShare(userId, ProjectFileAccess.Allow);
        FileShareDiffInput input = BuildInput(
            targetUserIds: new[] { userId },
            existingShares: new[] { allow });

        FileShareDiffResult result = _sut.Compute(input);

        // Already has access through explicit Allow — nothing to do
        result.SharesToInsert.Should().BeEmpty();
        result.SharesToDelete.Should().BeEmpty();
        result.UsersGrantedAccess.Should().BeEmpty();
    }

    [Fact]
    public void Compute_UserInTargetWithDenyAndNoPackageShare_DeletesDenyInsertsAllow()
    {
        Guid userId = Guid.NewGuid();
        SharedProjectFile deny = FileShare(userId, ProjectFileAccess.Deny);
        FileShareDiffInput input = BuildInput(
            targetUserIds: new[] { userId },
            existingShares: new[] { deny });

        FileShareDiffResult result = _sut.Compute(input);

        result.SharesToDelete.Should().Contain(deny);
        result.SharesToInsert.Should().ContainSingle(s =>
            s.SharedWithUserId == userId && s.Access == ProjectFileAccess.Allow);
        result.UsersGrantedAccess.Should().Contain(userId);
    }

    // ─── Revoke Access scenarios ──────────────────────────────────────────────

    [Fact]
    public void Compute_UserWithPackageShareNotInTarget_InsertsFileShareDeny()
    {
        Guid userId = Guid.NewGuid();
        FileShareDiffInput input = BuildInput(
            targetUserIds: Array.Empty<Guid>(),
            existingShares: new[] { PackageShare(userId) });

        FileShareDiffResult result = _sut.Compute(input);

        result.SharesToInsert.Should().ContainSingle(s =>
            s.SharedWithUserId == userId &&
            s.ProjectFileId == FileId &&
            s.Access == ProjectFileAccess.Deny);
        result.UsersRevokedAccess.Should().Contain(userId);
    }

    [Fact]
    public void Compute_UserWithAllowFileShareNotInTarget_DeletesFileShare()
    {
        Guid userId = Guid.NewGuid();
        SharedProjectFile allow = FileShare(userId, ProjectFileAccess.Allow);
        FileShareDiffInput input = BuildInput(
            targetUserIds: Array.Empty<Guid>(),
            existingShares: new[] { allow });

        FileShareDiffResult result = _sut.Compute(input);

        result.SharesToDelete.Should().Contain(allow);
        result.UsersRevokedAccess.Should().Contain(userId);
    }

    [Fact]
    public void Compute_UserWithDenyAlreadyNotInTarget_NoChange()
    {
        // User already has no access (deny) and not in target — nothing to do
        Guid userId = Guid.NewGuid();
        SharedProjectFile deny = FileShare(userId, ProjectFileAccess.Deny);
        FileShareDiffInput input = BuildInput(
            targetUserIds: Array.Empty<Guid>(),
            existingShares: new[] { PackageShare(userId), deny });

        FileShareDiffResult result = _sut.Compute(input);

        // User had package share + deny → no access. Not in target → no revoke needed.
        result.SharesToInsert.Should().BeEmpty();
        result.SharesToDelete.Should().BeEmpty();
    }

    // ─── Current user protection ──────────────────────────────────────────────

    [Fact]
    public void Compute_CurrentUserNotInTarget_NotRevoked()
    {
        // Current user has package access but is not in TargetUserIds
        // They should NOT be revoked
        FileShareDiffInput input = BuildInput(
            targetUserIds: Array.Empty<Guid>(),
            existingShares: new[] { PackageShare(CurrentUser) });

        FileShareDiffResult result = _sut.Compute(input);

        result.UsersRevokedAccess.Should().NotContain(CurrentUser);
    }

    // ─── Empty / no-op scenarios ──────────────────────────────────────────────

    [Fact]
    public void Compute_EmptyTargetEmptyExisting_ReturnsEmptyResult()
    {
        FileShareDiffInput input = BuildInput(
            targetUserIds: Array.Empty<Guid>(),
            existingShares: Array.Empty<SharedProjectFile>());

        FileShareDiffResult result = _sut.Compute(input);

        result.SharesToInsert.Should().BeEmpty();
        result.SharesToDelete.Should().BeEmpty();
        result.UsersGrantedAccess.Should().BeEmpty();
        result.UsersRevokedAccess.Should().BeEmpty();
    }
}
