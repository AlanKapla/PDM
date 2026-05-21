using Business.Interfaces.Exceptions;
using CQRS.Projects.SetProjectCurrency;
using Entities.Models.Projects;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class SetProjectCurrencyCommandHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectCurrency>> _currencyRepoMock = new();
    private readonly SetProjectCurrencyCommandHandler _handler;

    public SetProjectCurrencyCommandHandlerTests()
    {
        _handler = new SetProjectCurrencyCommandHandler(
            _projectRepoMock.Object,
            _currencyRepoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SetProjectCurrencyCommand ValidCommand(Guid tenantId, Guid projectId) =>
        new SetProjectCurrencyCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            Code = "EUR",
            Name = "Euro",
            Symbol = "€",
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCurrencyDoesNotExist_InsertsCurrencyAndReturnsUnit()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _projectRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _currencyRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectCurrency, bool>>>()))
            .ReturnsAsync((ProjectCurrency?)null);

        SetProjectCurrencyCommand command = ValidCommand(tenantId, projectId);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _currencyRepoMock.Verify(r => r.Insert(It.IsAny<ProjectCurrency>()), Times.Once);
        _currencyRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCurrencyExists_UpdatesCurrencyAndReturnsUnit()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _projectRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ProjectCurrency existing = new ProjectCurrency
        {
            ProjectId = projectId,
            Code = "PLN",
            Name = "Polski złoty",
            Symbol = "zł",
        };

        _currencyRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectCurrency, bool>>>()))
            .ReturnsAsync(existing);

        SetProjectCurrencyCommand command = ValidCommand(tenantId, projectId);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        existing.Code.Should().Be("EUR");
        existing.Name.Should().Be("Euro");
        existing.Symbol.Should().Be("€");
        _currencyRepoMock.Verify(r => r.Update(existing), Times.Once);
        _currencyRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _projectRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        SetProjectCurrencyCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenSymbolIsNull_InsertsWithNullSymbol()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _projectRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _currencyRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectCurrency, bool>>>()))
            .ReturnsAsync((ProjectCurrency?)null);

        SetProjectCurrencyCommand command = ValidCommand(tenantId, projectId) with { Symbol = null };

        ProjectCurrency? inserted = null;
        _currencyRepoMock
            .Setup(r => r.Insert(It.IsAny<ProjectCurrency>()))
            .Callback<ProjectCurrency>(c => inserted = c)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        inserted.Should().NotBeNull();
        inserted!.Symbol.Should().BeNull();
    }
}
