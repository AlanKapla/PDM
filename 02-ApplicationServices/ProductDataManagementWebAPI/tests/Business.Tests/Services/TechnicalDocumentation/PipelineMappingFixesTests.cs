using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class DrawingSheetNumberInferrerTests
{
    [Theory]
    [InlineData("A-01_zagospodarowanie.pdf", "A-01")]
    [InlineData("K-04_lista_drewna.pdf", "K-04")]
    [InlineData("rybienko_k03_gorne.png", "K-03")]
    public void InferFromFileName_extractsSheetNumber(string fileName, string expected)
    {
        string? sheetNumber = DrawingSheetNumberInferrer.InferFromFileName(fileName);

        sheetNumber.Should().Be(expected);
    }
}

public sealed class ValidatedDrawingCatalogBuilderTests
{
    [Fact]
    public void Build_includesFailedAndMissingImages()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Source = new DrawingSource { FileName = "A-02.pdf", PageNumber = 1 },
                Classification = new DrawingClassification
                {
                    SheetNumber = "A-02",
                    DrawingType = "rzut_parteru"
                }
            }
        ];

        List<TechnicalDocumentationImageInput> images =
        [
            new TechnicalDocumentationImageInput([], "A-01.pdf", 1),
            new TechnicalDocumentationImageInput([], "A-02.pdf", 1),
            new TechnicalDocumentationImageInput([], "K-04.pdf", 1)
        ];

        List<string> failedPages = ["K-04.pdf (str. 1)"];

        List<ValidatedDrawingEntry> entries = ValidatedDrawingCatalogBuilder.Build(
            drawings,
            images,
            failedPages);

        entries.Should().HaveCount(3);
        entries.Should().Contain(entry => entry.SheetNumber == "A-01" && entry.Validated == false);
        entries.Should().Contain(entry => entry.SheetNumber == "A-02" && entry.Validated == true);
        entries.Should().Contain(entry => entry.SheetNumber == "K-04" && entry.Validated == false);
    }
}

public sealed class DrawingExtractionNormalizerRoomNumberTests
{
    [Fact]
    public void Normalize_correctsSequentialRoomNumbersFromClassificationTable()
    {
        FloorPlanDrawing drawing = new()
        {
            Rooms =
            [
                new Room { Number = "01", Symbol = "01", Name = "Salon", AreaM2 = 24.5 },
                new Room { Number = "02", Symbol = "02", Name = "Kuchnia", AreaM2 = 12.1 }
            ]
        };

        DrawingClassification classification = new()
        {
            DrawingTable = "11-Salon-24,5 m2;12-Kuchnia-12,1 m2"
        };

        DrawingExtractionNormalizer.Normalize(drawing, classification);

        drawing.Rooms[0].Number.Should().Be("11");
        drawing.Rooms[1].Number.Should().Be("12");
    }
}
