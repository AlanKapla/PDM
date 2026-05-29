using Business.Implementation.Services;
using Business.Interfaces.Services;
using Entities.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class PermissionsVersionServiceTests
{
    private readonly Mock<IRepository<PermissionsVersionProfile>> _repoMock = new Mock<IRepository<PermissionsVersionProfile>>();
    private readonly Mock<IUserContextCache> _userContextCacheMock = new Mock<IUserContextCache>();
    private readonly Mock<ILogger<PermissionsVersionService>> _loggerMock = new Mock<ILogger<PermissionsVersionService>>();
    private readonly PermissionsVersionService _sut;

    public PermissionsVersionServiceTests()
    {
        _sut = new PermissionsVersionService(_repoMock.Object, _userContextCacheMock.Object, _loggerMock.Object);
    }

    // ─── BumpVersionAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task BumpVersionAsync_ProfileExists_IncrementsVersion()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        PermissionsVersionProfile existing = new PermissionsVersionProfile { UserId = userId, Version = 3 };
        _repoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, bool>>>()))
            .ReturnsAsync(existing);

        // Act
        await _sut.BumpVersionAsync(userId);

        // Assert
        existing.Version.Should().Be(4);
        _repoMock.Verify(r => r.Update(existing), Times.Once);
        _repoMock.Verify(r => r.Insert(It.IsAny<PermissionsVersionProfile>()), Times.Never);
        _userContextCacheMock.Verify(c => c.InvalidateUserPermissionsVersion(userId), Times.Once);
    }

    [Fact]
    public async Task BumpVersionAsync_ProfileNotFound_InsertsNewProfileWithVersionTwo()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, bool>>>()))
            .ReturnsAsync((PermissionsVersionProfile?)null);

        PermissionsVersionProfile? inserted = null;
        _repoMock
            .Setup(r => r.Insert(It.IsAny<PermissionsVersionProfile>()))
            .Callback<PermissionsVersionProfile>(p => inserted = p)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.BumpVersionAsync(userId);

        // Assert
        _repoMock.Verify(r => r.Insert(It.IsAny<PermissionsVersionProfile>()), Times.Once);
        _repoMock.Verify(r => r.Update(It.IsAny<PermissionsVersionProfile>()), Times.Never);
        inserted.Should().NotBeNull();
        inserted!.UserId.Should().Be(userId);
        inserted.Version.Should().Be(2);
        _userContextCacheMock.Verify(c => c.InvalidateUserPermissionsVersion(userId), Times.Once);
    }

    // ─── BumpVersionsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task BumpVersionsAsync_EmptyList_DoesNotCallRepository()
    {
        // Arrange
        List<Guid> emptyList = new List<Guid>();

        // Act
        await _sut.BumpVersionsAsync(emptyList);

        // Assert
        _repoMock.Verify(r => r.ExecuteUpdateAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, bool>>>(),
            It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<PermissionsVersionProfile>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BumpVersionsAsync_WithUsers_AllExist_DoesNotInsertNewProfiles()
    {
        // Arrange
        Guid userId1 = Guid.NewGuid();
        Guid userId2 = Guid.NewGuid();
        List<Guid> userIds = new List<Guid> { userId1, userId2 };

        _repoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<PermissionsVersionProfile>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _repoMock
            .Setup(r => r.SelectToHashSetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { userId1, userId2 }); // both already exist

        // Act
        await _sut.BumpVersionsAsync(userIds);

        // Assert
        _repoMock.Verify(r => r.InsertRange(It.IsAny<IEnumerable<PermissionsVersionProfile>>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _userContextCacheMock.Verify(c => c.InvalidateUserPermissionsVersion(userId1), Times.Once);
        _userContextCacheMock.Verify(c => c.InvalidateUserPermissionsVersion(userId2), Times.Once);
    }

    [Fact]
    public async Task BumpVersionsAsync_WithUsers_SomeMissing_InsertsNewProfiles()
    {
        // Arrange
        Guid existingUserId = Guid.NewGuid();
        Guid newUserId = Guid.NewGuid();
        List<Guid> userIds = new List<Guid> { existingUserId, newUserId };

        _repoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<PermissionsVersionProfile>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repoMock
            .Setup(r => r.SelectToHashSetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<PermissionsVersionProfile, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { existingUserId }); // only one exists

        List<PermissionsVersionProfile>? inserted = null;
        _repoMock
            .Setup(r => r.InsertRange(It.IsAny<IEnumerable<PermissionsVersionProfile>>()))
            .Callback<IEnumerable<PermissionsVersionProfile>>(p => inserted = p.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.BumpVersionsAsync(userIds);

        // Assert
        _repoMock.Verify(r => r.InsertRange(It.IsAny<IEnumerable<PermissionsVersionProfile>>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        inserted.Should().HaveCount(1);
        inserted![0].UserId.Should().Be(newUserId);
        inserted[0].Version.Should().Be(2);
        _userContextCacheMock.Verify(c => c.InvalidateUserPermissionsVersion(existingUserId), Times.Once);
        _userContextCacheMock.Verify(c => c.InvalidateUserPermissionsVersion(newUserId), Times.Once);
    }
}
