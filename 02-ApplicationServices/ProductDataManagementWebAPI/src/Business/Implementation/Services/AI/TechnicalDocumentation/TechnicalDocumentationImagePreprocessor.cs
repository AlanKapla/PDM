using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class TechnicalDocumentationImagePreprocessor : ITechnicalDocumentationImagePreprocessor
{
    private const int MaxDimension = 2048;
    private const int JpegQuality = 85;

    private readonly TechnicalDocumentationOptions options;

    public TechnicalDocumentationImagePreprocessor(IOptions<TechnicalDocumentationOptions> options)
    {
        this.options = options.Value;
    }

    public Task<(byte[] OptimizedBytes, string OptimizedMediaType)> PrepareForVisionAsync(
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        long recompressThresholdBytes = options.CompressionThresholdBytes;

        using SKBitmap? original = SKBitmap.Decode(imageBytes);
        if (original is null)
        {
            return Task.FromResult((imageBytes, mediaType));
        }

        bool isJpeg = mediaType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase);
        bool withinDimension = original.Width <= MaxDimension && original.Height <= MaxDimension;
        bool withinSize = imageBytes.Length <= recompressThresholdBytes;

        if (withinDimension && isJpeg && withinSize)
        {
            return Task.FromResult((imageBytes, mediaType));
        }

        SKBitmap bitmapToEncode = original;
        SKBitmap? resizedBitmap = null;

        if (!withinDimension)
        {
            double scale = Math.Min(
                (double)MaxDimension / original.Width,
                (double)MaxDimension / original.Height);
            int newWidth = Math.Max(1, (int)Math.Round(original.Width * scale));
            int newHeight = Math.Max(1, (int)Math.Round(original.Height * scale));
            SKImageInfo resizeInfo = new(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            resizedBitmap = original.Resize(resizeInfo, SKFilterQuality.High);
            bitmapToEncode = resizedBitmap ?? original;
        }

        try
        {
            using SKImage image = SKImage.FromBitmap(bitmapToEncode);
            using SKData encoded = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            byte[] optimizedBytes = encoded.ToArray();
            return Task.FromResult((optimizedBytes, "image/jpeg"));
        }
        finally
        {
            resizedBitmap?.Dispose();
        }
    }
}
