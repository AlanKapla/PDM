using Business.Interfaces.WebModels.Contractors;
using CQRS.Contractors.GetContractors;
using Entities.Models.Tenants;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Contractors;

public sealed class GetContractorsQueryHandlerTests
{
    private readonly Mock<IReadRepository<Contractor>> _repoMock = new();
    private readonly GetContractorsQueryHandler _handler;

    public GetContractorsQueryHandlerTests()
    {
        _handler = new GetContractorsQueryHandler(_repoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetContractorsQuery ValidQuery(Guid tenantId, string? search = null) =>
        new GetContractorsQuery
        {
            TenantId = tenantId,
            Search = search,
        };

    private static Contractor BuildContractor(Guid tenantId, string name, string? taxId = null, string? city = null)
        => new Contractor
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            TaxId = taxId,
            City = city,
            CreatedAt = DateTime.UtcNow,
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoSearch_ReturnsAllContractorsOrderedByName()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        List<Contractor> contractors =
        [
            BuildContractor(tenantId, "Zebra Corp"),
            BuildContractor(tenantId, "Alpha Ltd"),
            BuildContractor(tenantId, "Middle Inc"),
        ];

        _repoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(contractors);

        GetContractorsQuery query = ValidQuery(tenantId);

        // Act
        IEnumerable<ContractorWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<ContractorWeb> list = result.ToList();
        list.Should().HaveCount(3);
        list[0].Name.Should().Be("Alpha Ltd");
        list[1].Name.Should().Be("Middle Inc");
        list[2].Name.Should().Be("Zebra Corp");
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesName_ReturnsFilteredResults()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        List<Contractor> contractors =
        [
            BuildContractor(tenantId, "Alpha Construction"),
            BuildContractor(tenantId, "Beta Services"),
        ];

        _repoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(contractors);

        GetContractorsQuery query = ValidQuery(tenantId, search: "alpha");

        // Act
        IEnumerable<ContractorWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<ContractorWeb> list = result.ToList();
        list.Should().HaveCount(1);
        list[0].Name.Should().Be("Alpha Construction");
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesTaxId_ReturnsFilteredResults()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        List<Contractor> contractors =
        [
            BuildContractor(tenantId, "Alpha Ltd", taxId: "PL1234567890"),
            BuildContractor(tenantId, "Beta Corp", taxId: "PL9999999999"),
        ];

        _repoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(contractors);

        GetContractorsQuery query = ValidQuery(tenantId, search: "PL1234");

        // Act
        IEnumerable<ContractorWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<ContractorWeb> list = result.ToList();
        list.Should().HaveCount(1);
        list[0].Name.Should().Be("Alpha Ltd");
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesCity_ReturnsFilteredResults()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        List<Contractor> contractors =
        [
            BuildContractor(tenantId, "Warsaw Firm", city: "Warsaw"),
            BuildContractor(tenantId, "Krakow Firm", city: "Krakow"),
        ];

        _repoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(contractors);

        GetContractorsQuery query = ValidQuery(tenantId, search: "krakow");

        // Act
        IEnumerable<ContractorWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<ContractorWeb> list = result.ToList();
        list.Should().HaveCount(1);
        list[0].Name.Should().Be("Krakow Firm");
    }

    [Fact]
    public async Task Handle_WhenSearchIsWhitespace_ReturnsAllContractors()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        List<Contractor> contractors =
        [
            BuildContractor(tenantId, "Alpha Ltd"),
            BuildContractor(tenantId, "Beta Corp"),
        ];

        _repoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(contractors);

        GetContractorsQuery query = ValidQuery(tenantId, search: "   ");

        // Act
        IEnumerable<ContractorWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenNoContractorsExist_ReturnsEmptyList()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<Contractor, bool>>>()))
            .ReturnsAsync(new List<Contractor>());

        GetContractorsQuery query = ValidQuery(tenantId);

        // Act
        IEnumerable<ContractorWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
