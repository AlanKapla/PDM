using Business.Interfaces.WebModels.Contractors;
using CQRS.Contractors.CreateContractor;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.Contractors;

public sealed class CreateContractorCommandHandlerTests
{
    private readonly Mock<IRepository<Contractor>> _repoMock = new();
    private readonly Mock<ILogger<CreateContractorCommandHandler>> _loggerMock = new();
    private readonly CreateContractorCommandHandler _handler;

    public CreateContractorCommandHandlerTests()
    {
        _handler = new CreateContractorCommandHandler(
            _repoMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateContractorCommand ValidCommand() => new CreateContractorCommand
    {
        TenantId = Guid.NewGuid(),
        Name = "Test Contractor",
        TaxId = "1234567890",
        Email = "test@contractor.com",
        PhoneNumber = "123456789",
        Street = "Main Street 1",
        City = "Warsaw",
        PostalCode = "00-001",
        Country = "PL",
        Notes = "Some notes",
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsContractorAndReturnsWeb()
    {
        // Arrange
        CreateContractorCommand command = ValidCommand();

        // Act
        ContractorWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(command.TenantId);
        result.Name.Should().Be(command.Name);
        result.TaxId.Should().Be(command.TaxId);
        result.Email.Should().Be(command.Email);
        result.PhoneNumber.Should().Be(command.PhoneNumber);
        result.City.Should().Be(command.City);
        _repoMock.Verify(r => r.Insert(It.IsAny<Contractor>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameHasLeadingAndTrailingWhitespace_TrimsName()
    {
        // Arrange
        CreateContractorCommand command = ValidCommand() with { Name = "  Trimmed Name  " };

        // Act
        ContractorWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Trimmed Name");
    }

    [Fact]
    public async Task Handle_WhenOptionalFieldsAreNull_InsertsContractorWithNullFields()
    {
        // Arrange
        CreateContractorCommand command = new CreateContractorCommand
        {
            TenantId = Guid.NewGuid(),
            Name = "Minimal Contractor",
        };

        // Act
        ContractorWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TaxId.Should().BeNull();
        result.Email.Should().BeNull();
        result.City.Should().BeNull();
        _repoMock.Verify(r => r.Insert(It.IsAny<Contractor>()), Times.Once);
    }
}
