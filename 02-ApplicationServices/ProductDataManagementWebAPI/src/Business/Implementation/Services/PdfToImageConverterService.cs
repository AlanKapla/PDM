using System.Runtime.InteropServices;
using Business.Interfaces.Services;
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Business.Implementation.Services;

/// <summary>
/// Konwersja PDF → PNG przez Docnet.Core (PDFium) i SkiaSharp.
/// Wymaga natywnych bibliotek PDFium — zweryfikuj build linux-x64 w Dockerfile.
/// </summary>
public sealed class PdfToImageConverterService : IPdfToImageConverterService
{
    private const double TargetDpi = 150;
    private const double PixelsPerPoint = TargetDpi / 72.0;

    private readonly ILogger<PdfToImageConverterService> logger;

    public PdfToImageConverterService(ILogger<PdfToImageConverterService> logger)
    {
        this.logger = logger;
    }

    public Task<IReadOnlyList<byte[]>> ConvertAllPagesToJpegAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            List<byte[]> pages = new();

            using (IDocLib docLib = DocLib.Instance)
            using (IDocReader reader = docLib.GetDocReader(pdfBytes, new PageDimensions(PixelsPerPoint)))
            {
                int pageCount = reader.GetPageCount();

                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using IPageReader pageReader = reader.GetPageReader(pageIndex);
                    byte[] rawBytes = pageReader.GetImage();
                    int width = pageReader.GetPageWidth();
                    int height = pageReader.GetPageHeight();

                    byte[] pngBytes = ConvertBgraToPng(rawBytes, width, height);
                    pages.Add(pngBytes);
                }
            }

            return Task.FromResult<IReadOnlyList<byte[]>>(pages);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to convert PDF to PNG images");
            throw new InvalidOperationException("Invalid or corrupted PDF file.", ex);
        }
    }

    private static byte[] ConvertBgraToPng(byte[] rawBytes, int width, int height)
    {
        SKImageInfo sourceInfo = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKBitmap sourceBitmap = new(sourceInfo);
        Marshal.Copy(rawBytes, 0, sourceBitmap.GetPixels(), rawBytes.Length);

        SKImageInfo surfaceInfo = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(surfaceInfo);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(sourceBitmap, 0, 0);

        using SKImage snapshot = surface.Snapshot();
        using SKData encoded = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
