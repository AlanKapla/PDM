using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimates.AddCostEstimateGroup;
using Entities.Models.CostEstimates;
// using Entities.Models.CostEstimateTemplates; // Removed
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class AddCostEstimateGroupCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimateGroup>> _groupRepoMock = new();
    private readonly Mock<ICostEstimateCacheService> _cacheServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AddCostEstimateGroupCommandHandler _handler;

    public AddCostEstimateGroupCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new AddCostEstimateGroupCommandHandler(
            _groupRepoMock.Object,
            _cacheServiceMock.Object,
            _ceAccessServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CostEstimate BuildCostEstimate() =>
        new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            Name = "Test CE",
            Status = CostEstimateStatus.Draft,
            IsDeleted = false
        };

    private static CostEstimateTemplate BuildTemplate(bool canAddGroups = true) =>
        new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Template",
            CanAddGroups = canAddGroups,
            CanBranchGroups = true,
            IsDeleted = false
        };

    private static AddCostEstimateGroupCommand ValidCommand(CostEstimate costEstimate) =>
        new AddCostEstimateGroupCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            ParentGroupId = null,
            Order = 0
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidRequest_InsertsGroupAndReturnsGuid()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        CostEstimateTemplate template = BuildTemplate(canAddGroups: true);
        AddCostEstimateGroupCommand command = ValidCommand(costEstimate);

        _cacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Full);

        _cacheServiceMock
            .Setup(s => s.GetTemplateAsync(costEstimate.TemplateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _groupRepoMock.Verify(r => r.Insert(It.IsAny<CostEstimateGroup>()), Times.Once);
        _cacheServiceMock.Verify(s => s.InvalidateGroupsAsync(
            costEstimate.Id,
            costEstimate.TenantId,
            costEstimate.ProjectId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _cacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostEstimate?)null);

        AddCostEstimateGroupCommand command = new AddCostEstimateGroupCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid(),
            Order = 0
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenTemplateDoesNotAllowAddGroups_ThrowsValidationApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        CostEstimateTemplate template = BuildTemplate(canAddGroups: false);
        AddCostEstimateGroupCommand command = ValidCommand(costEstimate);

        _cacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Full);

        _cacheServiceMock
            .Setup(s => s.GetTemplateAsync(costEstimate.TemplateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessLevelIsNone_ThrowsForbiddenApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        AddCostEstimateGroupCommand command = ValidCommand(costEstimate);

        _cacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.None);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}

