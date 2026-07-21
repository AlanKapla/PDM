using Business.Interfaces.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Business.Tests.Helpers;

public sealed class FileContentValidatorTests
{
    [Theory]
    [InlineData("invoice.jpg", "image/jpeg")]
    [InlineData("invoice.jpeg", "image/jpeg")]
    [InlineData("invoice.png", "image/png")]
    [InlineData("invoice.pdf", "application/pdf")]
    public void Validate_WhenFileIsValid_ReturnsSuccess(string fileName, string contentType)
    {
        // Arrange
        IFormFile file = BuildFile(fileName, contentType, AICostImportTestFileBytes.ForFileName(fileName));

        // Act
        FileContentValidator.FileValidationResult result = FileContentValidator.Validate(file);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenExtensionIsInvalid_ReturnsPolishFailureReason()
    {
        // Arrange
        IFormFile file = BuildFile("document.txt", "text/plain", [0x74, 0x65, 0x78, 0x74]);

        // Act
        FileContentValidator.FileValidationResult result = FileContentValidator.Validate(file);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Niedozwolony format pliku");
    }

    [Fact]
    public void Validate_WhenMagicBytesDoNotMatchExtension_ReturnsFailure()
    {
        // Arrange
        IFormFile file = BuildFile("invoice.jpg", "image/jpeg", [0x25, 0x50, 0x44, 0x46]);

        // Act
        FileContentValidator.FileValidationResult result = FileContentValidator.Validate(file);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Zawartość pliku nie odpowiada");
    }

    [Fact]
    public void ValidateBytes_WhenPdfHeaderIsValid_ReturnsSuccess()
    {
        // Act
        FileContentValidator.FileValidationResult result = FileContentValidator.ValidateBytes(
            AICostImportTestFileBytes.Pdf,
            "invoice.pdf",
            "application/pdf");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    private static IFormFile BuildFile(string fileName, string contentType, byte[] content)
    {
        Mock<IFormFile> mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(content.Length);
        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        return mock.Object;
    }
}

internal static class AICostImportTestFileBytes
{
    internal static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    internal static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    internal static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];

    internal static byte[] ForFileName(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => Jpeg,
            ".png" => Png,
            ".pdf" => Pdf,
            _ => [0x00, 0x01, 0x02, 0x03]
        };
    }
}
