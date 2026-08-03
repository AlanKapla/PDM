using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Exceptions;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Business.Implementation.Services.AI
{
    public sealed class PdfToImageConverter : IPdfToImageConverter
    {
        private const int Dpi = 175;
        private const int MaxPages = 20;
        private const int JpegQuality = 85;
        private const uint PdfiumPasswordError = 4;
        private static readonly double ScalingFactor = Dpi / 72.0;

        public Task<IReadOnlyList<byte[]>> ConvertAllPagesToJpegAsync(
            byte[] pdfBytes,
            CancellationToken cancellationToken)
        {
            if (pdfBytes is null || pdfBytes.Length == 0)
            {
                throw PdfConversionException.Corrupt();
            }

            return Task.Run(
                () => ConvertAllPagesToJpeg(pdfBytes, cancellationToken),
                cancellationToken);
        }

        private static IReadOnlyList<byte[]> ConvertAllPagesToJpeg(
            byte[] pdfBytes,
            CancellationToken cancellationToken)
        {
            try
            {
                return RenderPages(pdfBytes, cancellationToken);
            }
            catch (PdfConversionException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DocnetLoadDocumentException ex) when (ex.ErrorCode == PdfiumPasswordError)
            {
                throw PdfConversionException.PasswordProtected(ex);
            }
            catch (DocnetException ex)
            {
                throw PdfConversionException.Corrupt(ex);
            }
            catch (Exception ex)
            {
                throw PdfConversionException.Corrupt(ex);
            }
        }

        private static IReadOnlyList<byte[]> RenderPages(
            byte[] pdfBytes,
            CancellationToken cancellationToken)
        {
            PageDimensions dimensions = new PageDimensions(ScalingFactor);
            using IDocReader docReader = DocLib.Instance.GetDocReader(pdfBytes, dimensions);

            int pageCount = docReader.GetPageCount();
            if (pageCount > MaxPages)
            {
                throw PdfConversionException.TooManyPages(pageCount, MaxPages);
            }

            List<byte[]> pages = new List<byte[]>(pageCount);
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pages.Add(ConvertPageToJpeg(docReader, pageIndex));
            }

            return pages;
        }

        private static byte[] ConvertPageToJpeg(IDocReader docReader, int pageIndex)
        {
            using IPageReader pageReader = docReader.GetPageReader(pageIndex);
            int width = pageReader.GetPageWidth();
            int height = pageReader.GetPageHeight();
            byte[] rawBytes = pageReader.GetImage(new NaiveTransparencyRemover());

            using Image<Bgra32> image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
            using MemoryStream output = new MemoryStream();
            JpegEncoder encoder = new JpegEncoder { Quality = JpegQuality };
            image.SaveAsJpeg(output, encoder);
            return output.ToArray();
        }
    }
}
