using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.UpdateTenant;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class UpdateTenantCommandHandlerTests
{
    private readonly Mock<IRepository<Tenant>> _tenantRepoMock = new();
    private readonly UpdateTenantCommandHandler _handler;

    public UpdateTenantCommandHandlerTests()
    {
        _handler = new UpdateTenantCommandHandler(_tenantRepoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateTenantCommand ValidCommand(Guid tenantId) => new UpdateTenantCommand
    {
        TenantId = tenantId,
        Name = "  Updated Tenant Name  "
    };

    private static Tenant BuildTenant(Guid id) => new Tenant
    {
        Id = id,
        Name = "Old Name",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTenantNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<Func<IQueryable<Tenant>, IIncludableQueryable<Tenant, object>>[]>()))
            .ReturnsAsync((Tenant?)null);

        UpdateTenantCommand command = ValidCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenTenantExists_UpdatesNameAndReturnsTenantDetails()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<Func<IQueryable<Tenant>, IIncludableQueryable<Tenant, object>>[]>()))
            .ReturnsAsync(tenant);

        UpdateTenantCommand command = ValidCommand(tenantId);

        // Act
        TenantDetailsWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(tenantId);
        result.Name.Should().Be("Updated Tenant Name");
        result.IsActive.Should().BeTrue();
        result.IsAdmin.Should().BeTrue();

        tenant.Name.Should().Be("Updated Tenant Name");
        _tenantRepoMock.Verify(r => r.Update(tenant), Times.Once);
    }
}
