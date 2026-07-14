using CQRS.CostEstimates.UploadCostEstimateFieldFiles;
using Entities.Models.CostEstimates;
// using Entities.Models.CostEstimateTemplates; // Removed
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimates;

public sealed class UploadCostEstimateFieldFilesCommandValidatorTests
{
    private readonly Mock<IReadRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IReadRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<IRepository<CostEstimateTemplateItemSystemFieldDefinition>> _fieldDefRepoMock = new();
    private readonly UploadCostEstimateFieldFilesCommandValidator _validator;

    public UploadCostEstimateFieldFilesCommandValidatorTests()
    {
        // Default: cost estimate exists
        _costEstimateRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Default: item exists
        _itemRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Default: field definition exists and is of type ItemSystemFiles
        _fieldDefRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimateTemplateItemSystemFieldDefinition, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _validator = new UploadCostEstimateFieldFilesCommandValidator(
            _costEstimateRepoMock.Object,
            _itemRepoMock.Object,
            _fieldDefRepoMock.Object);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand();

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public async Task Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public async Task Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand();

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand();

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === ItemId ===

    [Fact]
    public async Task Validate_WhenItemIdIsEmpty_HasValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand() with { ItemId = Guid.Empty };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }

    [Fact]
    public async Task Validate_WhenItemIdIsValid_HasNoValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand();

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ItemId);
    }

    // === FieldDefinitionId ===

    [Fact]
    public async Task Validate_WhenFieldDefinitionIdIsEmpty_HasValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand() with { FieldDefinitionId = Guid.Empty };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FieldDefinitionId);
    }

    [Fact]
    public async Task Validate_WhenFieldDefinitionIdIsValid_HasNoValidationError()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand();

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FieldDefinitionId);
    }

    // === Files — count limit ===

    [Fact]
    public async Task Validate_WhenFilesCountExceedsLimit_HasValidationError()
    {
        // Arrange
        List<IFormFile> files = Enumerable.Range(0, 11)
            .Select(_ => CreateValidFileMock())
            .ToList();

        UploadCostEstimateFieldFilesCommand command = ValidCommand() with { Files = files };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Files);
    }

    // === Files child rules — length ===

    [Fact]
    public async Task Validate_WhenFileIsEmpty_HasValidationError()
    {
        // Arrange
        Mock<IFormFile> emptyFile = new Mock<IFormFile>();
        emptyFile.Setup(f => f.Length).Returns(0);
        emptyFile.Setup(f => f.FileName).Returns("file.pdf");
        emptyFile.Setup(f => f.ContentType).Returns("application/pdf");

        UploadCostEstimateFieldFilesCommand command = ValidCommand() with
        {
            Files = [emptyFile.Object]
        };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Files[0].Length");
    }

    [Fact]
    public async Task Validate_WhenFileExceedsMaxSize_HasValidationError()
    {
        // Arrange — 51 MB
        Mock<IFormFile> largeFile = new Mock<IFormFile>();
        largeFile.Setup(f => f.Length).Returns(51L * 1024 * 1024);
        largeFile.Setup(f => f.FileName).Returns("file.pdf");
        largeFile.Setup(f => f.ContentType).Returns("application/pdf");

        UploadCostEstimateFieldFilesCommand command = ValidCommand() with
        {
            Files = [largeFile.Object]
        };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Files[0].Length");
    }

    // === Files child rules — extension ===

    [Fact]
    public async Task Validate_WhenFileHasInvalidExtension_HasValidationError()
    {
        // Arrange
        Mock<IFormFile> invalidFile = new Mock<IFormFile>();
        invalidFile.Setup(f => f.Length).Returns(1024);
        invalidFile.Setup(f => f.FileName).Returns("file.exe");
        invalidFile.Setup(f => f.ContentType).Returns("application/pdf");

        UploadCostEstimateFieldFilesCommand command = ValidCommand() with
        {
            Files = [invalidFile.Object]
        };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Files[0].FileName");
    }

    // === Files child rules — content type ===

    [Fact]
    public async Task Validate_WhenFileHasInvalidContentType_HasValidationError()
    {
        // Arrange
        Mock<IFormFile> invalidFile = new Mock<IFormFile>();
        invalidFile.Setup(f => f.Length).Returns(1024);
        invalidFile.Setup(f => f.FileName).Returns("file.pdf");
        invalidFile.Setup(f => f.ContentType).Returns("text/plain");

        UploadCostEstimateFieldFilesCommand command = ValidCommand() with
        {
            Files = [invalidFile.Object]
        };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Files[0].ContentType");
    }

    // === Async — Cost estimate does not exist ===

    [Fact]
    public async Task Validate_WhenCostEstimateDoesNotExist_HasValidationError()
    {
        // Arrange
        _costEstimateRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UploadCostEstimateFieldFilesCommand command = ValidCommand();

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    // === Async — Item does not exist ===

    [Fact]
    public async Task Validate_WhenItemDoesNotExist_HasValidationError()
    {
        // Arrange
        _itemRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UploadCostEstimateFieldFilesCommand command = ValidCommand();

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    // === Async — Field definition does not exist ===

    [Fact]
    public async Task Validate_WhenFieldDefinitionDoesNotExistOrWrongType_HasValidationError()
    {
        // Arrange
        _fieldDefRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimateTemplateItemSystemFieldDefinition, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UploadCostEstimateFieldFilesCommand command = ValidCommand();

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    // === Happy path — empty files list is valid ===

    [Fact]
    public async Task Validate_WhenFilesListIsEmpty_HasNoValidationErrors()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand() with { Files = [] };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Happy path — valid file ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UploadCostEstimateFieldFilesCommand command = ValidCommand() with
        {
            Files = [CreateValidFileMock()]
        };

        // Act
        TestValidationResult<UploadCostEstimateFieldFilesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helpers ===

    private static IFormFile CreateValidFileMock()
    {
        Mock<IFormFile> fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.FileName).Returns("document.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        return fileMock.Object;
    }

    private static UploadCostEstimateFieldFilesCommand ValidCommand() => new UploadCostEstimateFieldFilesCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        ItemId = Guid.NewGuid(),
        FieldDefinitionId = Guid.NewGuid(),
        Files = []
    };
}

