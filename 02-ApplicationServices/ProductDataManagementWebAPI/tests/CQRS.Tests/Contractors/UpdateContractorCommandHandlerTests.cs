using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Contractors;
using CQRS.Contractors.UpdateContractor;
using Entities.Models.Tenants;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Contractors;

public sealed class UpdateContractorCommandHandlerTests
{
    private readonly Mock<IRepository<Contractor>> _repoMock = new();
    private readonly UpdateContractorCommandHandler _handler;

    public UpdateContractorCommandHandlerTests()
    {
        _handler = new UpdateContractorCommandHandler(_repoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateContractorCommand ValidCommand(Guid tenantId, Guid contractorId) =>
        new UpdateContractorCommand
        {
            TenantId = tenantId,
            Id = contractorId,
            Name = "Updated Contractor",
            TaxId = "9876543210",
            Email = "updated@contractor.com",
            PhoneNumber = "987654321",
            Street = "New Street 5",
            City = "Krakow",
            PostalCode = "30-001",
            Country = "PL",
            Notes = "Updated notes",
        };

    private static Contractor BuildContractor(Guid id, Guid tenantId) => new Contractor
    {
        Id = id,
        TenantId = tenantId,
        Name = "Original Name",
        CreatedAt = DateTime.UtcNow,
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenContractorFound_UpdatesFieldsAndReturnsWeb()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid contractorId = Guid.NewGuid();
        Contractor existing = BuildContractor(contractorId, tenantId);
        UpdateContractorCommand command = ValidCommand(tenantId, contractorId);

        _repoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(existing);

        // Act
        ContractorWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.TaxId.Should().Be(command.TaxId);
        result.City.Should().Be(command.City);
        result.UpdatedAt.Should().NotBeNull();
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

        UpdateContractorCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

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
        Guid contractorId = Guid.NewGuid();
        Contractor existing = BuildContractor(contractorId, tenantId);

        _repoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(existing);

        UpdateContractorCommand command = ValidCommand(tenantId, contractorId) with { Name = "  Trimmed  " };

        // Act
        ContractorWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Trimmed");
    }
}
