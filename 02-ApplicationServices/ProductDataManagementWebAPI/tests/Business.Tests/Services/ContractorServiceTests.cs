using Business.Implementation.Services;
using Entities.Models.Tenants;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace Business.Tests.Services;

public sealed class ContractorServiceTests
{
    private readonly Mock<IReadRepository<Contractor>> _repoMock = new();
    private readonly ContractorService _sut;

    public ContractorServiceTests()
    {
        _sut = new ContractorService(_repoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Contractor BuildContractor(Guid id, Guid tenantId, string name)
        => new Contractor
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            CreatedAt = DateTime.UtcNow,
        };

    // ─── GetNamesByIdsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetNamesByIdsAsync_WhenIdsIsEmpty_ReturnsEmptyDictionaryWithoutCallingRepo()
    {
        // Arrange
        IReadOnlyCollection<Guid> ids = Array.Empty<Guid>();
        Guid tenantId = Guid.NewGuid();

        // Act
        Dictionary<Guid, string> result = await _sut.GetNamesByIdsAsync(ids, tenantId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetDictionaryBySearchAsync(
            It.IsAny<Expression<Func<Contractor, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNamesByIdsAsync_WhenContractorsFound_ReturnsDictionaryOfIdToName()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        IReadOnlyCollection<Guid> ids = new[] { id1, id2 };

        Dictionary<Guid, Contractor> repoResult = new()
        {
            { id1, BuildContractor(id1, tenantId, "Firm Alpha") },
            { id2, BuildContractor(id2, tenantId, "Firm Beta") },
        };

        _repoMock
            .Setup(r => r.GetDictionaryBySearchAsync(
                It.IsAny<Expression<Func<Contractor, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResult);

        // Act
        Dictionary<Guid, string> result = await _sut.GetNamesByIdsAsync(ids, tenantId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[id1].Should().Be("Firm Alpha");
        result[id2].Should().Be("Firm Beta");
    }

    [Fact]
    public async Task GetNamesByIdsAsync_WhenNoContractorsFound_ReturnsEmptyDictionary()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        IReadOnlyCollection<Guid> ids = new[] { Guid.NewGuid() };

        _repoMock
            .Setup(r => r.GetDictionaryBySearchAsync(
                It.IsAny<Expression<Func<Contractor, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Contractor>());

        // Act
        Dictionary<Guid, string> result = await _sut.GetNamesByIdsAsync(ids, tenantId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNamesByIdsAsync_WhenSomeContractorsNotFound_ReturnsOnlyFoundIds()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid foundId = Guid.NewGuid();
        Guid missingId = Guid.NewGuid();
        IReadOnlyCollection<Guid> ids = new[] { foundId, missingId };

        Dictionary<Guid, Contractor> repoResult = new()
        {
            { foundId, BuildContractor(foundId, tenantId, "Found Firm") },
        };

        _repoMock
            .Setup(r => r.GetDictionaryBySearchAsync(
                It.IsAny<Expression<Func<Contractor, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResult);

        // Act
        Dictionary<Guid, string> result = await _sut.GetNamesByIdsAsync(ids, tenantId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[foundId].Should().Be("Found Firm");
        result.Should().NotContainKey(missingId);
    }
}
