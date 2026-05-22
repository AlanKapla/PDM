using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimateTemplates.DuplicateCostEstimateTemplate;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class DuplicateCostEstimateTemplateCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimateTemplate>> _templateRepoMock = new();
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly DuplicateCostEstimateTemplateCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();

    public DuplicateCostEstimateTemplateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);

        _handler = new DuplicateCostEstimateTemplateCommandHandler(
            _templateRepoMock.Object,
            _templateServiceMock.Object,
            _currentUserMock.Object);
    }

    private static CostEstimateTemplate BuildTemplateWithRequiredFields()
    {
        return new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Name = "Source Template",
            IsDeleted = false,
            GroupFieldDefinitions = new List<CostEstimateTemplateGroupFieldDefinition>
            {
                new CostEstimateTemplateGroupFieldDefinition { FieldType = FieldType.GroupName }
            },
            SystemFieldDefinitions = new List<CostEstimateTemplateItemSystemFieldDefinition>
            {
                new CostEstimateTemplateItemSystemFieldDefinition { FieldType = FieldType.ItemSystemName }
            },
            CalculatedFieldDefinitions = new List<CostEstimateTemplateItemCalculatedFieldDefinition>
            {
                new CostEstimateTemplateItemCalculatedFieldDefinition { FieldType = FieldType.ItemCalculatedValueNet },
                new CostEstimateTemplateItemCalculatedFieldDefinition { FieldType = FieldType.ItemCalculatedValueGross }
            }
        };
    }

    [Fact]
    public async Task Handle_WhenSourceTemplateExistsWithRequiredFields_DuplicatesTemplate()
    {
        // Arrange
        CostEstimateTemplate sourceTemplate = BuildTemplateWithRequiredFields();
        Guid expectedId = Guid.NewGuid();

        DuplicateCostEstimateTemplateCommand command = new(sourceTemplate.Id, "Duplicate", "Duplicate desc");

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(sourceTemplate);

        _templateServiceMock
            .Setup(s => s.DuplicateTemplateAsync(
                It.IsAny<CostEstimateTemplate>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedId);
        _templateServiceMock.Verify(
            s => s.DuplicateTemplateAsync(
                sourceTemplate,
                UserId,
                command.Name,
                command.Description,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSourceTemplateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        DuplicateCostEstimateTemplateCommand command = new(Guid.NewGuid(), "Duplicate", null);

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
    public async Task Handle_WhenRequiredFieldsMissing_ThrowsValidationApiException()
    {
        // Arrange
        CostEstimateTemplate sourceTemplate = new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            OwnerId = UserId,
            Name = "Source Template",
            IsDeleted = false,
            GroupFieldDefinitions = new List<CostEstimateTemplateGroupFieldDefinition>(),
            SystemFieldDefinitions = new List<CostEstimateTemplateItemSystemFieldDefinition>(),
            CalculatedFieldDefinitions = new List<CostEstimateTemplateItemCalculatedFieldDefinition>()
        };

        DuplicateCostEstimateTemplateCommand command = new(sourceTemplate.Id, "Duplicate", null);

        _templateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimateTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateTemplate>, IIncludableQueryable<CostEstimateTemplate, object>>[]>()))
            .ReturnsAsync(sourceTemplate);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }
}
