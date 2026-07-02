using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationImagePreprocessorTests
{
    [Fact]
    public async Task PrepareForVisionAsync_largePng_resizesAndConvertsToJpeg()
    {
        // Arrange
        TechnicalDocumentationImagePreprocessor preprocessor = CreatePreprocessor();
        byte[] largePng = CreatePng(4000, 3000);

        // Act
        (byte[] optimizedBytes, string optimizedMediaType) =
            await preprocessor.PrepareForVisionAsync(largePng, "image/png", CancellationToken.None);

        // Assert
        optimizedMediaType.Should().Be("image/jpeg");
        optimizedBytes.Should().NotBeEmpty();

        using SKBitmap decoded = SKBitmap.Decode(optimizedBytes);
        decoded.Should().NotBeNull();
        decoded!.Width.Should().BeLessOrEqualTo(2048);
        decoded.Height.Should().BeLessOrEqualTo(2048);
    }

    [Fact]
    public async Task PrepareForVisionAsync_smallJpegUnderDefaultThreshold_returnsUnchanged()
    {
        // Arrange
        TechnicalDocumentationImagePreprocessor preprocessor = CreatePreprocessor();
        byte[] smallJpeg = CreateJpeg(800, 600, quality: 85);

        // Act
        (byte[] optimizedBytes, string optimizedMediaType) =
            await preprocessor.PrepareForVisionAsync(smallJpeg, "image/jpeg", CancellationToken.None);

        // Assert
        optimizedMediaType.Should().Be("image/jpeg");
        optimizedBytes.Should().BeEquivalentTo(smallJpeg);
    }

    [Fact]
    public async Task PrepareForVisionAsync_jpegOverConfiguredThreshold_recompresses()
    {
        // Arrange
        byte[] jpeg = CreateJpeg(4000, 3000, quality: 95);
        jpeg.Length.Should().BeGreaterThan(50_000);

        TechnicalDocumentationImagePreprocessor preprocessor = CreatePreprocessor(compressionThresholdBytes: 50_000);

        // Act
        (byte[] optimizedBytes, string optimizedMediaType) =
            await preprocessor.PrepareForVisionAsync(jpeg, "image/jpeg", CancellationToken.None);

        // Assert
        optimizedMediaType.Should().Be("image/jpeg");
        optimizedBytes.Should().NotBeEquivalentTo(jpeg);
        optimizedBytes.Length.Should().BeLessOrEqualTo(jpeg.Length);
    }

    private static TechnicalDocumentationImagePreprocessor CreatePreprocessor(
        long compressionThresholdBytes = 3_145_728)
    {
        return new TechnicalDocumentationImagePreprocessor(Options.Create(new TechnicalDocumentationOptions
        {
            CompressionThresholdBytes = compressionThresholdBytes,
        }));
    }

    private static byte[] CreatePng(int width, int height)
    {
        using SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.White);

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private static byte[] CreateJpeg(int width, int height, int quality)
    {
        using SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.White);

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return encoded.ToArray();
    }
}
