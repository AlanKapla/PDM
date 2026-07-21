using CQRS.AI.ParseCostDocument;
using CQRS.Tests.AI;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;

namespace CQRS.Tests.AI;

public sealed class ParseCostDocumentQueryValidatorTests
{
    private readonly ParseCostDocumentQueryValidator _validator = new();

    [Theory]
    [InlineData("invoice.jpg", "image/jpeg")]
    [InlineData("invoice.png", "image/png")]
    [InlineData("invoice.pdf", "application/pdf")]
    public void Validate_WhenFileTypeIsAllowed_HasNoValidationErrors(string fileName, string contentType)
    {
        // Arrange
        ParseCostDocumentQuery query = ValidQuery(fileName, contentType);

        // Act
        TestValidationResult<ParseCostDocumentQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenExtensionIsPdf_HasNoValidationErrors()
    {
        // Arrange
        ParseCostDocumentQuery query = ValidQuery("invoice.pdf", "application/pdf");

        // Act
        TestValidationResult<ParseCostDocumentQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenExtensionIsInvalid_HasValidationError()
    {
        // Arrange
        ParseCostDocumentQuery query = ValidQuery("invoice.txt", "text/plain");

        // Act
        TestValidationResult<ParseCostDocumentQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.File);
    }

    private static ParseCostDocumentQuery ValidQuery(string fileName, string contentType)
    {
        FormFile file = new FormFile(
            new MemoryStream(AICostImportTestHelpers.GetMagicBytes(fileName)),
            0,
            1024,
            "file",
            fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

        return new ParseCostDocumentQuery
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId,
            File = file,
            CostType = CostDocumentType.ProjectCost
        };
    }
}
