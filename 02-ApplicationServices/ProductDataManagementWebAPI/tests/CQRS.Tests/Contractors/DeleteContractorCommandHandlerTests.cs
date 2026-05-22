using Business.Interfaces.Exceptions;
using CQRS.Contractors.DeleteContractor;
using Entities.Models.Tenants;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Contractors;

public sealed class DeleteContractorCommandHandlerTests
{
    private readonly Mock<IRepository<Contractor>> _repoMock = new();
    private readonly Mock<ILogger<DeleteContractorCommandHandler>> _loggerMock = new();
    private readonly DeleteContractorCommandHandler _handler;

    public DeleteContractorCommandHandlerTests()
    {
        _handler = new DeleteContractorCommandHandler(
            _repoMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static DeleteContractorCommand ValidCommand(Guid tenantId, Guid contractorId) =>
        new DeleteContractorCommand
        {
            TenantId = tenantId,
            Id = contractorId,
        };

    private static Contractor BuildContractor(Guid id, Guid tenantId) => new Contractor
    {
        Id = id,
        TenantId = tenantId,
        Name = "To Be Deleted",
        CreatedAt = DateTime.UtcNow,
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenContractorFound_SetsIsDeletedAndSaves()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid contractorId = Guid.NewGuid();
        Contractor existing = BuildContractor(contractorId, tenantId);
        DeleteContractorCommand command = ValidCommand(tenantId, contractorId);

        _repoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(existing);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        existing.IsDeleted.Should().BeTrue();
        existing.DeletedAt.Should().NotBeNull();
        _repoMock.Verify(r => r.Update(existing), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenContractorNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync((Contractor?)null);

        DeleteContractorCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
