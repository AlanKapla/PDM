using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate;
using Entities.Models.CostEstimateTemplates;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class UpdateCostEstimateTemplateCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimateTemplate>> _templateRepoMock = new();
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateCostEstimateTemplateCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();

    public UpdateCostEstimateTemplateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);

        _handler = new UpdateCostEstimateTemplateCommandHandler(
            _templateRepoMock.Object,
            _templateServiceMock.Object,
            _currentUserMock.Object);
    }

    private static UpdateCostEstimateTemplateCommand BuildCommand(Guid templateId, bool updateStructure = false)
    {
        return new UpdateCostEstimateTemplateCommand(
            TemplateId: templateId,
            CurrentVersionId: Guid.NewGuid(),
            Name: "Updated Name",
            Description: "Updated Description",
            Category: null,
            CanAddGroups: true,
            CanBranchGroups: false,
            MaxGroupLevel: null,
            AutoNumberGroups: false,
            GroupNumberFormat: null,
            UpdateStructure: updateStructure,
            Units: null,
            Categories: null,
            GroupHeaderFields: null,
            SystemFields: null,
            CalculatedFields: null,
            GenericFields: null,
            UiConfiguration: null);
    }

    [Fact]
    public async Task Handle_WhenTemplateExistsAndUpdateStructureFalse_UpdatesTemplate()
    {
        // Arrange
        CostEstimateTemplate template = new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Name = "Old Name",
            IsDeleted = false
        };

        UpdateCostEstimateTemplateCommand command = BuildCommand(template.Id, updateStructure: false);

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
            s => s.UpdateTemplateAsync(
                template,
                command.Name,
                command.Description,
                command.Category,
                command.CanAddGroups,
                command.CanBranchGroups,
                command.MaxGroupLevel,
                command.AutoNumberGroups,
                command.GroupNumberFormat,
                command.UpdateStructure,
                command.Units,
                command.Categories,
                command.GroupHeaderFields,
                command.SystemFields,
                command.CalculatedFields,
                command.GenericFields,
                command.UiConfiguration,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        UpdateCostEstimateTemplateCommand command = BuildCommand(Guid.NewGuid());

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
    public async Task Handle_WhenUpdateStructureTrueWithRequiredFields_UpdatesTemplate()
    {
        // Arrange
        CostEstimateTemplate template = new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Name = "Old Name",
            IsDeleted = false
        };

        // Build a command with required field types
        UpdateCostEstimateTemplateCommand command = new UpdateCostEstimateTemplateCommand(
            TemplateId: template.Id,
            CurrentVersionId: Guid.NewGuid(),
            Name: "Updated Name",
            Description: null,
            Category: null,
            CanAddGroups: true,
            CanBranchGroups: false,
            MaxGroupLevel: null,
            AutoNumberGroups: false,
            GroupNumberFormat: null,
            UpdateStructure: true,
            Units: null,
            Categories: null,
            GroupHeaderFields: new List<FieldDefinitionDto>
            {
                new FieldDefinitionDto(Guid.NewGuid(), (int)Entities.Models.CostEstimates.FieldType.GroupName, "Group Name", false, false)
            },
            SystemFields: new List<FieldDefinitionDto>
            {
                new FieldDefinitionDto(Guid.NewGuid(), (int)Entities.Models.CostEstimates.FieldType.ItemSystemName, "Item Name", false, false)
            },
            CalculatedFields: new List<FieldDefinitionDto>
            {
                new FieldDefinitionDto(Guid.NewGuid(), (int)Entities.Models.CostEstimates.FieldType.ItemCalculatedValueNet, "Net", false, false),
                new FieldDefinitionDto(Guid.NewGuid(), (int)Entities.Models.CostEstimates.FieldType.ItemCalculatedValueGross, "Gross", false, false)
            },
            GenericFields: null,
            UiConfiguration: null);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(template);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _templateServiceMock.Verify(s => s.UpdateTemplateAsync(
            It.IsAny<CostEstimateTemplate>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<List<UnitDto>?>(),
            It.IsAny<List<CategoryDto>?>(),
            It.IsAny<List<FieldDefinitionDto>?>(),
            It.IsAny<List<FieldDefinitionDto>?>(),
            It.IsAny<List<FieldDefinitionDto>?>(),
            It.IsAny<List<FieldDefinitionDto>?>(),
            It.IsAny<UiConfigurationDto?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUpdateStructureTrueWithMissingRequiredFields_ThrowsValidationApiException()
    {
        // Arrange
        CostEstimateTemplate template = new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Name = "Old Name",
            IsDeleted = false
        };

        UpdateCostEstimateTemplateCommand command = new UpdateCostEstimateTemplateCommand(
            TemplateId: template.Id,
            CurrentVersionId: Guid.NewGuid(),
            Name: "Updated Name",
            Description: null,
            Category: null,
            CanAddGroups: true,
            CanBranchGroups: false,
            MaxGroupLevel: null,
            AutoNumberGroups: false,
            GroupNumberFormat: null,
            UpdateStructure: true,
            Units: null,
            Categories: null,
            GroupHeaderFields: new List<FieldDefinitionDto>(),
            SystemFields: new List<FieldDefinitionDto>(),
            CalculatedFields: new List<FieldDefinitionDto>(),
            GenericFields: null,
            UiConfiguration: null);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(template);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }
}
