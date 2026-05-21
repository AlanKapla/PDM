using Business.Implementation.Services;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class WorkScheduleAccessServiceTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IReadRepository<WorkSchedule>> _wsRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkAssignment>> _assignmentRepoMock = new();
    private readonly WorkScheduleAccessService _sut;

    public WorkScheduleAccessServiceTests()
    {
        _sut = new WorkScheduleAccessService(
            _currentUserMock.Object,
            _wsRepoMock.Object,
            _assignmentRepoMock.Object);
    }

    // ─── RequireAdminOrOwnerAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RequireAdminOrOwnerAsync_UserIsAdmin_DoesNotThrow()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid workScheduleId = Guid.NewGuid();

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.RequireAdminOrOwnerAsync(
            tenantId, projectId, workScheduleId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RequireAdminOrOwnerAsync_UserIsOwner_DoesNotThrow()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid workScheduleId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _wsRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.RequireAdminOrOwnerAsync(
            tenantId, projectId, workScheduleId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RequireAdminOrOwnerAsync_NotAdminNotOwner_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid workScheduleId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _wsRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.RequireAdminOrOwnerAsync(
            tenantId, projectId, workScheduleId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    // ─── RequireAdminOwnerOrAssignedAsync ─────────────────────────────────────

    [Fact]
    public async Task RequireAdminOwnerOrAssignedAsync_UserIsAdmin_DoesNotThrow()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid workScheduleId = Guid.NewGuid();
        Guid workId = Guid.NewGuid();

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.RequireAdminOwnerOrAssignedAsync(
            tenantId, projectId, workScheduleId, workId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RequireAdminOwnerOrAssignedAsync_UserIsOwner_DoesNotThrow()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid workScheduleId = Guid.NewGuid();
        Guid workId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _wsRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.RequireAdminOwnerOrAssignedAsync(
            tenantId, projectId, workScheduleId, workId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RequireAdminOwnerOrAssignedAsync_UserIsAssigned_DoesNotThrow()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid workScheduleId = Guid.NewGuid();
        Guid workId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _wsRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _assignmentRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWorkAssignment, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.RequireAdminOwnerOrAssignedAsync(
            tenantId, projectId, workScheduleId, workId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RequireAdminOwnerOrAssignedAsync_NeitherAdminOwnerNorAssigned_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid workScheduleId = Guid.NewGuid();
        Guid workId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _wsRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _assignmentRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWorkAssignment, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.RequireAdminOwnerOrAssignedAsync(
            tenantId, projectId, workScheduleId, workId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}
