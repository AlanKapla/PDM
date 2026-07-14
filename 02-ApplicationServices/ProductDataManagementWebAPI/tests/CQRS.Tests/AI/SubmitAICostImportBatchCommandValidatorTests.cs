using Business.Interfaces.Configurations;
using CQRS.AI.ParseCostDocument;
using CQRS.AI.SubmitAICostImportBatch;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace CQRS.Tests.AI;

public sealed class SubmitAICostImportBatchCommandValidatorTests
{
    private readonly SubmitAICostImportBatchCommandValidator _validator;

    public SubmitAICostImportBatchCommandValidatorTests()
    {
        IOptions<AICostImportOptions> options = Options.Create(new AICostImportOptions
        {
            MaxBatchTotalBytes = 52_428_800
        });
        _validator = new SubmitAICostImportBatchCommandValidator(options);
    }

    [Fact]
    public void Validate_WhenOnlyOneFile_HasValidationError()
    {
        // Arrange
        SubmitAICostImportBatchCommand command = ValidCommand(
            new FormFileCollection { AICostImportTestHelpers.BuildFormFileMock().Object });

        // Act
        TestValidationResult<SubmitAICostImportBatchCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Files);
    }

    [Fact]
    public void Validate_WhenTotalSizeExceedsLimit_HasValidationError()
    {
        // Arrange
        Mock<IFormFile> large1 = AICostImportTestHelpers.BuildFormFileMock("a.jpg", 30_000_000);
        Mock<IFormFile> large2 = AICostImportTestHelpers.BuildFormFileMock("b.jpg", 30_000_000);

        SubmitAICostImportBatchCommand command = ValidCommand(
            new FormFileCollection { large1.Object, large2.Object });

        // Act
        TestValidationResult<SubmitAICostImportBatchCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Files);
    }

    [Fact]
    public void Validate_WhenInvalidExtension_HasValidationError()
    {
        // Arrange
        Mock<IFormFile> pdf1 = AICostImportTestHelpers.BuildFormFileMock("a.pdf");
        Mock<IFormFile> pdf2 = AICostImportTestHelpers.BuildFormFileMock("b.pdf");

        SubmitAICostImportBatchCommand command = ValidCommand(
            new FormFileCollection { pdf1.Object, pdf2.Object });

        // Act
        TestValidationResult<SubmitAICostImportBatchCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SubmitAICostImportBatchCommand command = ValidCommand();

        // Act
        TestValidationResult<SubmitAICostImportBatchCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static SubmitAICostImportBatchCommand ValidCommand(IFormFileCollection? files = null)
    {
        IFormFileCollection collection = files ?? new FormFileCollection
        {
            AICostImportTestHelpers.BuildFormFileMock("a.jpg").Object,
            AICostImportTestHelpers.BuildFormFileMock("b.jpg").Object
        };

        return new SubmitAICostImportBatchCommand
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId,
            Files = collection,
            CostDocumentType = CostDocumentType.ProjectCost
        };
    }
}
