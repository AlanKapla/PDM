using Business.AIAgent.Services;
using Business.Implementation.Services.AI;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Business.Tests.Services.AI;

public sealed class DocumentParserServiceTests
{
    private readonly Mock<IAICompletionService> _completionMock = new();
    private readonly Mock<IPdfToImageConverter> _pdfConverterMock = new();
    private readonly Mock<ILogger<DocumentParserService>> _loggerMock = new();
    private readonly DocumentParserService _sut;

    private const string ValidAiJson =
        """
        {
          "name": "Cement",
          "description": "Cement portlandzki 25kg",
          "number": "FV/1/2026",
          "net": 100.50,
          "gross": 123.62,
          "date": "2026-07-01",
          "contractorName": "Bud-Hurt Sp. z o.o.",
          "contractorNip": "1234567890",
          "contractorAddress": "ul. Testowa 1, 00-001 Warszawa",
          "categoryName": "Materiały budowlane",
          "confidence": 0.95
        }
        """;

    public DocumentParserServiceTests()
    {
        _sut = new DocumentParserService(
            _completionMock.Object,
            _pdfConverterMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ParseAsync_WhenJpeg_UsesSingleImageCompletion()
    {
        // Arrange
        byte[] jpegBytes = [0xFF, 0xD8, 0xFF, 0x00];
        _completionMock
            .Setup(c => c.CompleteWithImageAsync(
                It.IsAny<string>(),
                jpegBytes,
                "image/jpeg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidAiJson);

        // Act
        ParsedCostDto result = await _sut.ParseAsync(jpegBytes, "image/jpeg", CancellationToken.None);

        // Assert
        result.Name.Should().Be("Cement");
        result.Confidence.Should().Be(0.95);
        result.Net.Should().Be(100.50m);
        _pdfConverterMock.Verify(
            c => c.ConvertAllPagesToJpegAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _completionMock.Verify(
            c => c.CompleteWithImagesAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<(byte[] ImageBytes, string MediaType)>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _completionMock.Verify(
            c => c.CompleteWithImageAsync(
                It.IsAny<string>(),
                jpegBytes,
                "image/jpeg",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ParseAsync_WhenPng_UsesSingleImageCompletion()
    {
        // Arrange
        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47];
        _completionMock
            .Setup(c => c.CompleteWithImageAsync(
                It.IsAny<string>(),
                pngBytes,
                "image/png",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidAiJson);

        // Act
        ParsedCostDto result = await _sut.ParseAsync(pngBytes, "image/png", CancellationToken.None);

        // Assert
        result.Name.Should().Be("Cement");
        _pdfConverterMock.Verify(
            c => c.ConvertAllPagesToJpegAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ParseAsync_WhenPdfMediaType_ConvertsPagesAndUsesMultiImage()
    {
        // Arrange
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-
        byte[] page1 = [0xFF, 0xD8, 0x01];
        byte[] page2 = [0xFF, 0xD8, 0x02];
        IReadOnlyList<byte[]> pages = [page1, page2];

        _pdfConverterMock
            .Setup(c => c.ConvertAllPagesToJpegAsync(pdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        _completionMock
            .Setup(c => c.CompleteWithImagesAsync(
                It.IsAny<string>(),
                It.Is<IReadOnlyList<(byte[] ImageBytes, string MediaType)>>(imgs =>
                    imgs.Count == 2
                    && imgs[0].ImageBytes == page1
                    && imgs[0].MediaType == "image/jpeg"
                    && imgs[1].ImageBytes == page2
                    && imgs[1].MediaType == "image/jpeg"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidAiJson);

        // Act
        ParsedCostDto result = await _sut.ParseAsync(pdfBytes, "application/pdf", CancellationToken.None);

        // Assert
        result.Name.Should().Be("Cement");
        result.Confidence.Should().Be(0.95);
        _pdfConverterMock.Verify(
            c => c.ConvertAllPagesToJpegAsync(pdfBytes, It.IsAny<CancellationToken>()),
            Times.Once);
        _completionMock.Verify(
            c => c.CompleteWithImageAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ParseAsync_WhenMultiPagePdfReturnsZeroConfidence_FallsBackToFirstPage()
    {
        // Arrange
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D];
        byte[] page1 = [0xFF, 0xD8, 0x01];
        byte[] page2 = [0xFF, 0xD8, 0x02];
        IReadOnlyList<byte[]> pages = [page1, page2];

        _pdfConverterMock
            .Setup(c => c.ConvertAllPagesToJpegAsync(pdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        _completionMock
            .Setup(c => c.CompleteWithImagesAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<(byte[] ImageBytes, string MediaType)>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                """
                {
                  "name": "Nieznany koszt",
                  "confidence": 0
                }
                """);

        _completionMock
            .Setup(c => c.CompleteWithImageAsync(
                It.IsAny<string>(),
                page1,
                "image/jpeg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidAiJson);

        // Act
        ParsedCostDto result = await _sut.ParseAsync(pdfBytes, "application/pdf", CancellationToken.None);

        // Assert
        result.Name.Should().Be("Cement");
        result.Confidence.Should().Be(0.95);
        _completionMock.Verify(
            c => c.CompleteWithImageAsync(
                It.IsAny<string>(),
                page1,
                "image/jpeg",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ParseAsync_WhenMagicBytesArePdf_ConvertsEvenIfMediaTypeIsWrong()
    {
        // Arrange
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46]; // %PDF
        byte[] page1 = [0xFF, 0xD8, 0x01];

        _pdfConverterMock
            .Setup(c => c.ConvertAllPagesToJpegAsync(pdfBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync([page1]);

        _completionMock
            .Setup(c => c.CompleteWithImagesAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<(byte[] ImageBytes, string MediaType)>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidAiJson);

        // Act
        ParsedCostDto result = await _sut.ParseAsync(pdfBytes, "application/octet-stream", CancellationToken.None);

        // Assert
        result.Name.Should().Be("Cement");
        _pdfConverterMock.Verify(
            c => c.ConvertAllPagesToJpegAsync(pdfBytes, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ParseAsync_WhenPdfConversionFails_PropagatesPdfConversionException()
    {
        // Arrange
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46];
        _pdfConverterMock
            .Setup(c => c.ConvertAllPagesToJpegAsync(pdfBytes, It.IsAny<CancellationToken>()))
            .ThrowsAsync(PdfConversionException.PasswordProtected());

        // Act
        Func<Task> act = async () => await _sut.ParseAsync(pdfBytes, "application/pdf", CancellationToken.None);

        // Assert
        PdfConversionException ex = (await act.Should().ThrowAsync<PdfConversionException>()).Which;
        ex.Reason.Should().Be(PdfConversionReason.PasswordProtected);
        ex.UserMessage.Should().Be("Plik PDF jest zabezpieczony hasłem i nie może zostać przetworzony");
    }

    [Fact]
    public async Task ParseAsync_WhenAiCompletionFailsForJpeg_ReturnsConfidenceZero()
    {
        // Arrange
        byte[] jpegBytes = [0xFF, 0xD8, 0xFF];
        _completionMock
            .Setup(c => c.CompleteWithImageAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI unavailable"));

        // Act
        ParsedCostDto result = await _sut.ParseAsync(jpegBytes, "image/jpeg", CancellationToken.None);

        // Assert
        result.Name.Should().Be("Nieznany koszt");
        result.Confidence.Should().Be(0);
    }
}
