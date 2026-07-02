using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationDrawingCatalogTests
{
    [Fact]
    public void BuildCatalogUserText_includesDescriptionsAndRelatedDrawings()
    {
        List<TechnicalDocumentationImageInput> images =
        [
            new TechnicalDocumentationImageInput([1], "plan.pdf", 1),
            new TechnicalDocumentationImageInput([2], "plan.pdf", 3)
        ];

        List<DrawingClassification> classifications =
        [
            new DrawingClassification
            {
                DrawingType = "rzut parteru",
                SheetNumber = "1",
                DescriptiveText = "Sciany z bloczka ceramicznego",
                DrawingTable = "Z1 - sciana zewnetrzna",
                RelatedDrawings =
                [
                    new RelatedDrawingRef
                    {
                        ReferenceLabel = "przekroj 1-1",
                        TargetSheetNumber = "3",
                        DetailType = "przekroj"
                    }
                ]
            },
            new DrawingClassification
            {
                DrawingType = "przekroj",
                SheetNumber = "3",
                Title = "Przekroj poprzeczny"
            }
        ];

        IReadOnlyList<DrawingCatalogEntry> catalog = TechnicalDocumentationDrawingCatalog.Build(images, classifications);
        TechnicalDocumentationExtractionContext context = TechnicalDocumentationDrawingCatalog.BuildExtractionContext(
            images[0],
            classifications[0],
            catalog);

        string userText = TechnicalDocumentationDrawingCatalog.BuildCatalogUserText(context);

        userText.Should().Contain("KATALOG RYSUNKÓW");
        userText.Should().Contain("plan.pdf s.1");
        userText.Should().Contain("opis: Sciany z bloczka ceramicznego");
        userText.Should().Contain("tabela: Z1 - sciana zewnetrzna");
        userText.Should().Contain("POWIĄZANIA TEGO RYSUNKU");
        userText.Should().Contain("przekroj 1-1");
    }
}
