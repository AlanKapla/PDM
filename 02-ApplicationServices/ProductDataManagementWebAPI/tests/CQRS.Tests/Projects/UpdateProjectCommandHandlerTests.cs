using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.UpdateProject;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class UpdateProjectCommandHandlerTests
{
    private readonly Mock<IRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateProjectCommandHandler _handler;

    public UpdateProjectCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _currentUserMock
            .Setup(u => u.GetProjectSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCtxSnapshot?)null);

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>>()))
            .ReturnsAsync((ProjectMember?)null);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>>()))
            .ReturnsAsync((TenantMember?)null);

        _projectMemberRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _handler = new UpdateProjectCommandHandler(
            _projectRepoMock.Object,
            _projectMemberRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateProjectCommand ValidCommand(Guid tenantId, Guid projectId) =>
        new UpdateProjectCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            Name = "Updated Project Name",
        };

    private static Project BuildProject(Guid id, Guid tenantId) => new Project
    {
        Id = id,
        TenantId = tenantId,
        Name = "Original Name",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        CreatedByUserId = Guid.NewGuid(),
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProjectExists_UpdatesProjectAndReturnsDetails()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Project project = BuildProject(projectId, tenantId);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        UpdateProjectCommand command = ValidCommand(tenantId, projectId);

        // Act
        ProjectDetailsWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(projectId);
        result.TenantId.Should().Be(tenantId);
        result.Name.Should().Be(command.Name);

        _projectRepoMock.Verify(r => r.Update(It.IsAny<Project>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync((Project?)null);

        UpdateProjectCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNameHasLeadingAndTrailingWhitespace_TrimsName()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Project project = BuildProject(projectId, tenantId);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        UpdateProjectCommand command = ValidCommand(tenantId, projectId) with { Name = "  Trimmed Name  " };

        // Act
        ProjectDetailsWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Trimmed Name");
    }
}
