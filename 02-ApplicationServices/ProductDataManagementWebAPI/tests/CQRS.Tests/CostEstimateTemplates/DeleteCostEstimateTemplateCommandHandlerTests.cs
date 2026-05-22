using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimateTemplates.DeleteCostEstimateTemplate;
using Entities.Models.CostEstimateTemplates;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class DeleteCostEstimateTemplateCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimateTemplate>> _templateRepoMock = new();
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly DeleteCostEstimateTemplateCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();

    public DeleteCostEstimateTemplateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);

        _handler = new DeleteCostEstimateTemplateCommandHandler(
            _templateRepoMock.Object,
            _templateServiceMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_DeletesTemplate()
    {
        // Arrange
        CostEstimateTemplate template = new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Name = "Test Template",
            IsDeleted = false
        };

        DeleteCostEstimateTemplateCommand command = new(template.Id);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(template);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _templateServiceMock.Verify(
            s => s.DeleteTemplateAsync(template, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        DeleteCostEstimateTemplateCommand command = new(Guid.NewGuid());

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync((CostEstimateTemplate?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_DoesNotCallDeleteTemplateOnWrongTemplate()
    {
        // Arrange
        CostEstimateTemplate template = new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Name = "Test Template",
            IsDeleted = false
        };

        DeleteCostEstimateTemplateCommand command = new(template.Id);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(template);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _templateServiceMock.Verify(
            s => s.DeleteTemplateAsync(It.IsAny<CostEstimateTemplate>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
