using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Contractors;
using CQRS.Contractors.GetContractor;
using Entities.Models.Tenants;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Contractors;

public sealed class GetContractorQueryHandlerTests
{
    private readonly Mock<IReadRepository<Contractor>> _repoMock = new();
    private readonly GetContractorQueryHandler _handler;

    public GetContractorQueryHandlerTests()
    {
        _handler = new GetContractorQueryHandler(_repoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetContractorQuery ValidQuery(Guid tenantId, Guid contractorId) =>
        new GetContractorQuery
        {
            TenantId = tenantId,
            ContractorId = contractorId,
        };

    private static Contractor BuildContractor(Guid id, Guid tenantId) => new Contractor
    {
        Id = id,
        TenantId = tenantId,
        Name = "Contractor Name",
        TaxId = "1234567890",
        Email = "info@contractor.com",
        City = "Warsaw",
        CreatedAt = DateTime.UtcNow,
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenContractorFound_ReturnsMappedWeb()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid contractorId = Guid.NewGuid();
        Contractor contractor = BuildContractor(contractorId, tenantId);
        GetContractorQuery query = ValidQuery(tenantId, contractorId);

        _repoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Contractor, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(contractor);

        // Act
        ContractorWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(contractorId);
        result.TenantId.Should().Be(tenantId);
        result.Name.Should().Be(contractor.Name);
        result.TaxId.Should().Be(contractor.TaxId);
        result.Email.Should().Be(contractor.Email);
        result.City.Should().Be(contractor.City);
    }

    [Fact]
    public async Task Handle_WhenContractorNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Contractor, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contractor?)null);

        GetContractorQuery query = ValidQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
