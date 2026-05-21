using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplates;
using Entities.Models.CostEstimateTemplates;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class GetCostEstimateTemplatesQueryHandlerTests
{
    private readonly Mock<IReadRepository<CostEstimateTemplate>> _templateRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetCostEstimateTemplatesQueryHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();

    public GetCostEstimateTemplatesQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);

        _handler = new GetCostEstimateTemplatesQueryHandler(
            _templateRepoMock.Object,
            _currentUserMock.Object);
    }

    private static CostEstimateTemplate BuildTemplate(string name = "Template")
    {
        return new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Name = name,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            Owner = new User { FirstName = "Jan", LastName = "Kowalski" }
        };
    }

    [Fact]
    public async Task Handle_WhenUserHasTemplates_ReturnsOrderedList()
    {
        // Arrange
        List<CostEstimateTemplate> templates = new List<CostEstimateTemplate>
        {
            BuildTemplate("Template A"),
            BuildTemplate("Template B")
        };

        GetCostEstimateTemplatesQuery query = new();

        _templateRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(templates);

        // Act
        List<CostEstimateTemplateListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoTemplates_ReturnsEmptyList()
    {
        // Arrange
        GetCostEstimateTemplatesQuery query = new();

        _templateRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(new List<CostEstimateTemplate>());

        // Act
        List<CostEstimateTemplateListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCalled_MapsOwnerName()
    {
        // Arrange
        CostEstimateTemplate template = BuildTemplate();
        GetCostEstimateTemplatesQuery query = new();

        _templateRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(new List<CostEstimateTemplate> { template });

        // Act
        List<CostEstimateTemplateListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result[0].OwnerName.Should().Be("Jan Kowalski");
    }
}
