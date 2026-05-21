using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplateDetails;
using Entities.Models.CostEstimateTemplates;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class GetCostEstimateTemplateDetailsQueryHandlerTests
{
    private readonly Mock<IReadRepository<CostEstimateTemplate>> _templateRepoMock = new();
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetCostEstimateTemplateDetailsQueryHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();

    public GetCostEstimateTemplateDetailsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);

        _handler = new GetCostEstimateTemplateDetailsQueryHandler(
            _templateRepoMock.Object,
            _templateServiceMock.Object,
            _currentUserMock.Object);
    }

    private static CostEstimateTemplate BuildTemplate(Guid ownerId)
    {
        return new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Test Template",
            Description = "desc",
            IsDeleted = false,
            Owner = new User { FirstName = "Jan", LastName = "Kowalski" }
        };
    }

    [Fact]
    public async Task Handle_WhenTemplateExistsAndUserIsOwner_ReturnsDetails()
    {
        // Arrange
        CostEstimateTemplate template = BuildTemplate(UserId);
        CostEstimateTemplateStructureWeb structure = new CostEstimateTemplateStructureWeb(
            template.Id,
            null,
            new List<UnitWeb>(),
            new List<CategoryWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            null);

        GetCostEstimateTemplateDetailsQuery query = new(template.Id);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(template);

        _templateServiceMock
            .Setup(s => s.GetTemplateStructureCachedAsync(template, It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        // Act
        CostEstimateTemplateDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(template.Id);
        result.Name.Should().Be(template.Name);
        result.OwnerId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        GetCostEstimateTemplateDetailsQuery query = new(Guid.NewGuid());

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync((CostEstimateTemplate?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenTemplateOwnedByOtherUserAndNotSuperAdmin_ThrowsNotFoundApiException()
    {
        // Arrange
        CostEstimateTemplate template = BuildTemplate(Guid.NewGuid()); // different owner

        GetCostEstimateTemplateDetailsQuery query = new(template.Id);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(template);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenSuperAdminAccessesOtherUsersTemplate_ReturnsDetails()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);
        CostEstimateTemplate template = BuildTemplate(Guid.NewGuid()); // different owner
        CostEstimateTemplateStructureWeb structure = new CostEstimateTemplateStructureWeb(
            template.Id,
            null,
            new List<UnitWeb>(),
            new List<CategoryWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            null);

        GetCostEstimateTemplateDetailsQuery query = new(template.Id);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(template);

        _templateServiceMock
            .Setup(s => s.GetTemplateStructureCachedAsync(template, It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        // Act
        CostEstimateTemplateDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(template.Id);
    }
}
