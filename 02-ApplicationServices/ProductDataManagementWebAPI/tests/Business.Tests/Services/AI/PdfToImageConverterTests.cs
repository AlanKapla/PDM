using Business.Implementation.Services.AI;
using Business.Interfaces.Exceptions;
using FluentAssertions;

namespace Business.Tests.Services.AI
{
    public sealed class PdfToImageConverterTests
    {
        private readonly PdfToImageConverter _sut = new();

        [Fact]
        public async Task ConvertAllPagesToJpegAsync_WhenBytesAreNull_ThrowsCorrupt()
        {
            // Act
            Func<Task> act = async () => await _sut.ConvertAllPagesToJpegAsync(null!, CancellationToken.None);

            // Assert
            PdfConversionException ex = (await act.Should().ThrowAsync<PdfConversionException>()).Which;
            ex.Reason.Should().Be(PdfConversionReason.Corrupt);
            ex.UserMessage.Should().Be("Nie udało się odczytać pliku PDF – plik może być uszkodzony");
        }

        [Fact]
        public async Task ConvertAllPagesToJpegAsync_WhenBytesAreEmpty_ThrowsCorrupt()
        {
            // Act
            Func<Task> act = async () => await _sut.ConvertAllPagesToJpegAsync([], CancellationToken.None);

            // Assert
            PdfConversionException ex = (await act.Should().ThrowAsync<PdfConversionException>()).Which;
            ex.Reason.Should().Be(PdfConversionReason.Corrupt);
        }

        [Fact]
        public async Task ConvertAllPagesToJpegAsync_WhenBytesAreNotPdf_ThrowsCorrupt()
        {
            // Arrange
            byte[] garbage = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05];

            // Act
            Func<Task> act = async () => await _sut.ConvertAllPagesToJpegAsync(garbage, CancellationToken.None);

            // Assert
            PdfConversionException ex = (await act.Should().ThrowAsync<PdfConversionException>()).Which;
            ex.Reason.Should().Be(PdfConversionReason.Corrupt);
            ex.Message.Should().Contain("corrupt");
        }

        [Fact]
        public async Task ConvertAllPagesToJpegAsync_WhenValidSinglePagePdf_ReturnsJpegBytes()
        {
            // Arrange — minimal valid 1-page PDF
            byte[] pdfBytes = CreateMinimalPdf();

            // Act
            IReadOnlyList<byte[]> pages = await _sut.ConvertAllPagesToJpegAsync(pdfBytes, CancellationToken.None);

            // Assert
            pages.Should().HaveCount(1);
            pages[0].Length.Should().BeGreaterThan(0);
            // JPEG SOI marker
            pages[0][0].Should().Be(0xFF);
            pages[0][1].Should().Be(0xD8);
        }

        private static byte[] CreateMinimalPdf()
        {
            // Minimal single-page PDF (blank page) accepted by PDFium
            const string pdf =
                """
                %PDF-1.1
                1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj
                2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj
                3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R >>endobj
                4 0 obj<< /Length 0 >>stream
                endstream
                endobj
                xref
                0 5
                0000000000 65535 f 
                0000000009 00000 n 
                0000000058 00000 n 
                0000000115 00000 n 
                0000000206 00000 n 
                trailer<< /Size 5 /Root 1 0 R >>
                startxref
                253
                %%EOF
                """;
            return System.Text.Encoding.ASCII.GetBytes(pdf.Replace("\r\n", "\n").Replace("\n", "\r\n"));
        }
    }
}
