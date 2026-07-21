using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.ParseCostDocument;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CQRS.Tests.AI;

public sealed class ParseCostDocumentQueryHandlerTests
{
    private readonly Mock<IDocumentParserService> _parserMock = new();
    private readonly Mock<IAICostDocumentEnrichmentService> _enrichmentMock = new();
    private readonly ParseCostDocumentQueryHandler _handler;

    public ParseCostDocumentQueryHandlerTests()
    {
        _enrichmentMock
            .Setup(e => e.EnrichWithContractorAsync(
                It.IsAny<ParsedCostDto>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParsedCostDto dto, Guid _, CancellationToken _) => dto);

        _enrichmentMock
            .Setup(e => e.EnrichWithCategoryAsync(
                It.IsAny<ParsedCostDto>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParsedCostDto dto, Guid _, CancellationToken _) => dto);

        _handler = new ParseCostDocumentQueryHandler(
            _parserMock.Object,
            _enrichmentMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPdfConversionFails_ThrowsValidationApiExceptionWithPolishMessage()
    {
        // Arrange
        PdfConversionException conversionEx = PdfConversionException.PasswordProtected();
        _parserMock
            .Setup(p => p.ParseAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(conversionEx);

        ParseCostDocumentQuery query = BuildQuery("invoice.pdf");

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        ValidationApiException ex = (await act.Should().ThrowAsync<ValidationApiException>()).Which;
        ex.Message.Should().Be(conversionEx.UserMessage);
        ex.Message.Should().Contain("hasłem");
    }

    [Fact]
    public async Task Handle_WhenParseSucceeds_PassesBytesAndContentTypeToParser()
    {
        // Arrange
        ParsedCostDto parsed = AICostImportTestHelpers.ValidParsedCost();
        byte[]? capturedBytes = null;
        string? capturedMediaType = null;

        _parserMock
            .Setup(p => p.ParseAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<byte[], string, CancellationToken>((bytes, mediaType, _) =>
            {
                capturedBytes = bytes;
                capturedMediaType = mediaType;
            })
            .ReturnsAsync(parsed);

        ParseCostDocumentQuery query = BuildQuery("invoice.pdf");

        // Act
        ParsedCostDto result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(parsed);
        capturedMediaType.Should().Be("application/pdf");
        capturedBytes.Should().NotBeNull();
        capturedBytes![0].Should().Be(0x25); // %
        capturedBytes[1].Should().Be(0x50); // P
        capturedBytes[2].Should().Be(0x44); // D
        capturedBytes[3].Should().Be(0x46); // F
    }

    private static ParseCostDocumentQuery BuildQuery(string fileName)
    {
        byte[] content = AICostImportTestHelpers.BuildFileContent(fileName, 256);
        FormFile file = new FormFile(
            new MemoryStream(content),
            0,
            content.Length,
            "file",
            fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = AICostImportTestHelpers.GetContentType(fileName)
        };

        return new ParseCostDocumentQuery
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId,
            File = file
        };
    }
}
