using System.Linq.Expressions;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using CQRS.CostEstimates.CreateCostEstimate;
using Entities.Models.CostEstimates;
// using Entities.Models.CostEstimateTemplates; // Removed
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class CreateCostEstimateCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IRepository<CostEstimateTemplate>> _templateRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CreateCostEstimateCommandHandler _handler;

    public CreateCostEstimateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new CreateCostEstimateCommandHandler(
            _costEstimateRepoMock.Object,
            _templateRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CostEstimateTemplate BuildTemplate(Guid? ownerId = null) =>
        new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId ?? Guid.NewGuid(),
            Name = "Test Template",
            IsDeleted = false
        };

    private static CreateCostEstimateCommand ValidCommand(Guid templateId) =>
        new CreateCostEstimateCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            TemplateId = templateId,
            Name = "Test Cost Estimate",
            Description = "Some description"
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTemplateExists_InsertsAndReturnsGuid()
    {
        // Arrange
        CostEstimateTemplate template = BuildTemplate();
        CreateCostEstimateCommand command = ValidCommand(template.Id);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>()))
            .ReturnsAsync(template);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _costEstimateRepoMock.Verify(r => r.Insert(It.IsAny<CostEstimate>()), Times.Once);
        _costEstimateRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>()))
            .ReturnsAsync((CostEstimateTemplate?)null);

        CreateCostEstimateCommand command = ValidCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenDescriptionIsNull_InsertsWithStatusDraft()
    {
        // Arrange
        CostEstimateTemplate template = BuildTemplate();

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>()))
            .ReturnsAsync(template);

        CreateCostEstimateCommand command = new CreateCostEstimateCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            TemplateId = template.Id,
            Name = "No Description Estimate",
            Description = null
        };

        CostEstimate? inserted = null;
        _costEstimateRepoMock
            .Setup(r => r.Insert(It.IsAny<CostEstimate>()))
            .Callback<CostEstimate>(ce => inserted = ce)
            .Returns(Task.CompletedTask);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        inserted.Should().NotBeNull();
        inserted!.Status.Should().Be(CostEstimateStatus.Draft);
        inserted.Description.Should().BeNull();
    }
}

