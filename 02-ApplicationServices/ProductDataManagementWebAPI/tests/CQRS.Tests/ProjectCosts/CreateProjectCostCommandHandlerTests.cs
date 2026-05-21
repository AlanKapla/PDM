using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.CreateProjectCost;
using Entities.Models.Costs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.ProjectCosts;

public sealed class CreateProjectCostCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<IRepository<BaseCostAttachment>> _attachmentRepoMock = new();
    private readonly Mock<IContractorService> _contractorServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<CreateProjectCostCommandHandler>> _loggerMock = new();
    private readonly Mock<ILogger<CQRS.ProjectCosts.Shared.ProjectCostHandlerBase>> _baseLoggerMock = new();
    private readonly CreateProjectCostCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public CreateProjectCostCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);
        _currentUserMock.Setup(u => u.FullName).Returns("Test User");

        _handler = new CreateProjectCostCommandHandler(
            _projectCostRepoMock.Object,
            _blobStorageServiceMock.Object,
            _attachmentRepoMock.Object,
            _contractorServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object,
            _baseLoggerMock.Object);
    }

    private CreateProjectCostCommand BuildCommand(Guid? contractorId = null)
    {
        return new CreateProjectCostCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            Name = "Test Cost",
            ContractorId = contractorId,
            Net = 1000m,
            IsAccepted = false
        };
    }

    [Fact]
    public async Task Handle_WhenCostWithoutDocument_InsertsAndReturnsWebModel()
    {
        // Arrange
        CreateProjectCostCommand command = BuildCommand();

        _contractorServiceMock
            .Setup(s => s.GetNamesByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        // Act
        ProjectCostListItemWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.UserId.Should().Be(UserId);
        _projectCostRepoMock.Verify(r => r.Insert(It.IsAny<ProjectCost>()), Times.Once);
        _projectCostRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostHasContractor_LooksUpContractorName()
    {
        // Arrange
        Guid contractorId = Guid.NewGuid();
        CreateProjectCostCommand command = BuildCommand(contractorId);
        string contractorName = "Contractor ACME";

        _contractorServiceMock
            .Setup(s => s.GetNamesByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                TenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { { contractorId, contractorName } });

        // Act
        ProjectCostListItemWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ContractorName.Should().Be(contractorName);
    }

    [Fact]
    public async Task Handle_WhenCostWithoutContractor_ContractorNameIsNull()
    {
        // Arrange
        CreateProjectCostCommand command = BuildCommand();

        // Act
        ProjectCostListItemWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ContractorName.Should().BeNull();
        _contractorServiceMock.Verify(
            s => s.GetNamesByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
