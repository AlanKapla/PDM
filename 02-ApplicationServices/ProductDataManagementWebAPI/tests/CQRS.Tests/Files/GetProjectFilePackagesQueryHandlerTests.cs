using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using CQRS.Files.GetProjectFilePackages;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.Files;

public sealed class GetProjectFilePackagesQueryHandlerTests
{
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetProjectFilePackagesQueryHandler _handler;

    public GetProjectFilePackagesQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _userServiceMock
            .Setup(s => s.GetProjectMembersByIdsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ProjectMemberUserInfo>());

        _handler = new GetProjectFilePackagesQueryHandler(
            _projectFilesServiceMock.Object,
            _userServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetProjectFilePackagesQuery ValidQuery() =>
        new GetProjectFilePackagesQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Scope = ResourceScope.All
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoPackages_ReturnsEmptyList()
    {
        // Arrange
        GetProjectFilePackagesQuery query = ValidQuery();
        _projectFilesServiceMock
            .Setup(s => s.GetAccessiblePackagesAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ProjectFilePackageDto>());

        // Act
        List<ProjectFilePackageWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenPackagesExist_ReturnsPackageList()
    {
        // Arrange
        GetProjectFilePackagesQuery query = ValidQuery();
        Guid packageId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();

        Dictionary<Guid, ProjectFilePackageDto> packages = new Dictionary<Guid, ProjectFilePackageDto>
        {
            [packageId] = new ProjectFilePackageDto
            {
                Id = packageId,
                TenantId = query.TenantId,
                ProjectId = query.ProjectId,
                OwnerId = ownerId,
                Name = "Package1",
                CreatedAt = DateTime.UtcNow
            }
        };

        _projectFilesServiceMock
            .Setup(s => s.GetAccessiblePackagesAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(packages);

        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFileCountsAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [packageId] = 3 });

        // Act
        List<ProjectFilePackageWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(packageId);
        result[0].Name.Should().Be("Package1");
        result[0].TotalFiles.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenPackagesExist_FetchesFileCounts()
    {
        // Arrange
        GetProjectFilePackagesQuery query = ValidQuery();
        Guid packageId = Guid.NewGuid();

        _projectFilesServiceMock
            .Setup(s => s.GetAccessiblePackagesAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ProjectFilePackageDto>
            {
                [packageId] = new ProjectFilePackageDto
                {
                    Id = packageId,
                    Name = "Pkg",
                    OwnerId = Guid.NewGuid(),
                    TenantId = query.TenantId,
                    ProjectId = query.ProjectId
                }
            });

        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFileCountsAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _projectFilesServiceMock.Verify(s => s.GetAccessibleFileCountsAsync(
            It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<HashSet<Guid>>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
