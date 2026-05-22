using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using CQRS.Projects.UpdateProjectBudget;
using Entities.Models.Projects;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class UpdateProjectBudgetCommandHandlerTests
{
    private readonly Mock<IRepository<Project>> _projectRepositoryMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<UpdateProjectBudgetCommandHandler>> _loggerMock = new();
    private readonly UpdateProjectBudgetCommandHandler _handler;

    public UpdateProjectBudgetCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new UpdateProjectBudgetCommandHandler(
            _projectRepositoryMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateProjectBudgetCommand ValidCommand(Guid tenantId, Guid projectId) =>
        new UpdateProjectBudgetCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            BudgetNet = 10000m,
            BudgetGross = 12300m,
        };

    private static Project BuildProject(Guid id, Guid tenantId) => new Project
    {
        Id = id,
        TenantId = tenantId,
        Name = "Budget Project",
        IsActive = true,
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProjectExists_UpdatesBudgetAndReturnsUnit()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Project project = BuildProject(projectId, tenantId);

        _projectRepositoryMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        UpdateProjectBudgetCommand command = ValidCommand(tenantId, projectId);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        project.BudgetNet.Should().Be(10000m);
        project.BudgetGross.Should().Be(12300m);
        _projectRepositoryMock.Verify(r => r.Update(project), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _projectRepositoryMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync((Project?)null);

        UpdateProjectBudgetCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenBudgetFieldsAreNull_SetsNullBudget()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Project project = BuildProject(projectId, tenantId);
        project.BudgetNet = 5000m;
        project.BudgetGross = 6150m;

        _projectRepositoryMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        UpdateProjectBudgetCommand command = ValidCommand(tenantId, projectId) with
        {
            BudgetNet = null,
            BudgetGross = null,
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        project.BudgetNet.Should().BeNull();
        project.BudgetGross.Should().BeNull();
    }
}
