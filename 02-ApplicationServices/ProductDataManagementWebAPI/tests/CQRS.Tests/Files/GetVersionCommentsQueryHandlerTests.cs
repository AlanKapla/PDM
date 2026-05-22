using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using CQRS.Files.GetVersionComments;
using Entities.Models.Files;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.Files;

public sealed class GetVersionCommentsQueryHandlerTests
{
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetVersionCommentsQueryHandler _handler;

    private static readonly Guid CurrentUserId = Guid.NewGuid();

    public GetVersionCommentsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(CurrentUserId);

        _userServiceMock
            .Setup(s => s.GetProjectMembersByIdsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ProjectMemberUserInfo>());

        _handler = new GetVersionCommentsQueryHandler(
            _projectFilesServiceMock.Object,
            _userServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetVersionCommentsQuery ValidQuery() =>
        new GetVersionCommentsQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Scope = ResourceScope.All
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFileNotAccessible_ThrowsNotFoundApiException()
    {
        // Arrange
        GetVersionCommentsQuery query = ValidQuery();
        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFileByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectFileCacheDto?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenVersionNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        GetVersionCommentsQuery query = ValidQuery();
        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFileByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectFileCacheDto { Id = query.FileId });

        _projectFilesServiceMock
            .Setup(s => s.GetFileVersionByIdAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectFileVersionDto?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNoComments_ReturnsEmptyList()
    {
        // Arrange
        GetVersionCommentsQuery query = ValidQuery();
        SetupAccessibleFile(query);
        SetupAccessibleVersion(query);

        _projectFilesServiceMock
            .Setup(s => s.GetVersionCommentsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectFileVersionCommentDto>());

        // Act
        List<ProjectFileVersionCommentWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCommentsExist_ReturnsOrderedComments()
    {
        // Arrange
        GetVersionCommentsQuery query = ValidQuery();
        Guid authorId = Guid.NewGuid();
        SetupAccessibleFile(query);
        SetupAccessibleVersion(query);

        List<ProjectFileVersionCommentDto> commentDtos = new List<ProjectFileVersionCommentDto>
        {
            new ProjectFileVersionCommentDto
            {
                Id = Guid.NewGuid(),
                ProjectFileVersionId = query.VersionId,
                UserId = authorId,
                Content = "First comment",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new ProjectFileVersionCommentDto
            {
                Id = Guid.NewGuid(),
                ProjectFileVersionId = query.VersionId,
                UserId = CurrentUserId,
                Content = "Second comment",
                CreatedAt = DateTime.UtcNow
            }
        };

        _projectFilesServiceMock
            .Setup(s => s.GetVersionCommentsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commentDtos);

        // Act
        List<ProjectFileVersionCommentWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        // Second comment belongs to current user — CanEdit and CanDelete should be true
        ProjectFileVersionCommentWeb ownComment = result.First(c => c.UserId == CurrentUserId);
        ownComment.CanEdit.Should().BeTrue();
        ownComment.CanDelete.Should().BeTrue();
        // Other user comment — CanEdit and CanDelete should be false
        ProjectFileVersionCommentWeb otherComment = result.First(c => c.UserId == authorId);
        otherComment.CanEdit.Should().BeFalse();
        otherComment.CanDelete.Should().BeFalse();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetupAccessibleFile(GetVersionCommentsQuery query)
    {
        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFileByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectFileCacheDto { Id = query.FileId });
    }

    private void SetupAccessibleVersion(GetVersionCommentsQuery query)
    {
        _projectFilesServiceMock
            .Setup(s => s.GetFileVersionByIdAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectFileVersionDto
            {
                Id = query.VersionId,
                ProjectFileId = query.FileId,
                VersionNumber = 1,
                BlobFileName = "v1.pdf",
                BlobPath = "path/v1.pdf",
                ContentType = "application/pdf"
            });
    }
}
