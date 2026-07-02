using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class BuildClassificationContextTests
{
    [Fact]
    public void BuildClassificationContext_includesAllSixTextSources()
    {
        // Arrange
        DrawingClassification classification = new()
        {
            DrawingType = "rzut parteru",
            Scale = 50,
            SheetNumber = "A-02",
            Title = "Rzut parteru",
            Revision = "B",
            FloorLevel = "parter",
            FloorOrder = 0,
            HasMaterialTable = true,
            TableContent = "Salon 21,3 m2",
            DescriptiveText = "Ściany z bloczka ceramicznego",
            TechnicalParameters = "Beton C20/25",
            ElementAnnotations = "N-1: nadproże",
            Legend = "Z1 - ściana zewnętrzna",
            Notes = "UWAGA: izolacja wg K-01"
        };

        // Act
        string context = FloorPlanDrawingJsonParser.BuildClassificationContext(classification);

        // Assert
        context.Should().Contain("typ:rzut parteru");
        context.Should().Contain("skala:1:50");
        context.Should().Contain("arkusz:A-02");
        context.Should().Contain("tabele:Salon 21,3 m2");
        context.Should().Contain("opis:Ściany z bloczka ceramicznego");
        context.Should().Contain("parametry:Beton C20/25");
        context.Should().Contain("etykiety:N-1: nadproże");
        context.Should().Contain("legenda:Z1 - ściana zewnętrzna");
        context.Should().Contain("uwagi:UWAGA: izolacja wg K-01");
        context.Should().Contain("hasMaterialTable:true");
        context.Should().Contain("floorOrder:0");
    }

    [Fact]
    public void BuildExtractionUserText_includesSixSourceBlock_whenExplicitlyRequested()
    {
        // Arrange
        DrawingClassification classification = new()
        {
            DrawingType = "przekrój",
            DescriptiveText = "Warstwy ściany zewnętrznej",
            Legend = "Symbol Z1"
        };

        // Act
        string userText = FloorPlanDrawingJsonParser.BuildExtractionUserText(
            classification,
            extractionContext: null,
            focusPrompt: "Priorytet: warstwy ścian",
            includeFullTextSources: true);

        // Assert
        userText.Should().Contain("6 ŹRÓDEŁ TEKSTU");
        userText.Should().Contain("[BLOKI OPISOWE]");
        userText.Should().Contain("Warstwy ściany zewnętrznej");
        userText.Should().Contain("[LEGENDA]");
        userText.Should().Contain("FOCUS EKSTRAKCJI");
        userText.Should().Contain("Priorytet: warstwy ścian");
    }

    [Fact]
    public void BuildExtractionUserText_byDefault_omitsSixSourceBlock()
    {
        // Arrange
        DrawingClassification classification = new()
        {
            DrawingType = "przekrój",
            DescriptiveText = "Warstwy ściany zewnętrznej",
            Legend = "Symbol Z1"
        };

        // Act
        string userText = FloorPlanDrawingJsonParser.BuildExtractionUserText(classification);

        // Assert
        userText.Should().NotContain("6 ŹRÓDEŁ TEKSTU");
        userText.Should().NotContain("[BLOKI OPISOWE]");
        userText.Should().Contain("typ:przekrój");
    }

    [Fact]
    public void BuildExtractionUserText_withMaterialTable_includesTableSectionOnly()
    {
        // Arrange
        DrawingClassification classification = new()
        {
            DrawingType = "rzut parteru",
            HasMaterialTable = true,
            TableContent = "Z1 - ściana zewnętrzna 38mb",
            DescriptiveText = "Opis który nie powinien trafić do payloadu"
        };

        // Act
        string userText = FloorPlanDrawingJsonParser.BuildExtractionUserText(classification);

        // Assert
        userText.Should().Contain("[TABELE MATERIAŁOWE]");
        userText.Should().Contain("Z1 - ściana zewnętrzna 38mb");
        userText.Should().NotContain("6 ŹRÓDEŁ TEKSTU");
        userText.Should().NotContain("[BLOKI OPISOWE]");
    }

    [Fact]
    public void BuildClassificationContext_fallsBackToDrawingTable()
    {
        // Arrange
        DrawingClassification classification = new()
        {
            DrawingType = "rzut parteru",
            DrawingTable = "O1 - okno 120x140"
        };

        // Act
        string context = FloorPlanDrawingJsonParser.BuildClassificationContext(classification);

        // Assert
        context.Should().Contain("tabele:O1 - okno 120x140");
    }
}
