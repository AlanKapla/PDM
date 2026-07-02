using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class MaterialDrawingGroupClassifierTests
{
    [Theory]
    [InlineData("rzut_fundamentow", MaterialDrawingGroupKind.Foundations)]
    [InlineData("zbrojenie_stropu_dolne", MaterialDrawingGroupKind.Ceilings)]
    [InlineData("zbrojenie_stropu_gorne", MaterialDrawingGroupKind.Ceilings)]
    [InlineData("rzut_dachu", MaterialDrawingGroupKind.Roof)]
    [InlineData("rzut_wiezby_dachowej", MaterialDrawingGroupKind.Roof)]
    [InlineData("rzut_parteru", MaterialDrawingGroupKind.Walls)]
    [InlineData("elewacja", MaterialDrawingGroupKind.Walls)]
    public void QualifiesForGroup_usesDrawingTypeNotSheetNumber(string drawingType, MaterialDrawingGroupKind kind)
    {
        FloorPlanDrawing drawing = new()
        {
            Classification = new DrawingClassification
            {
                DrawingType = drawingType,
                SheetNumber = "X-99",
                Title = "Dowolny arkusz"
            }
        };

        MaterialDrawingGroupClassifier.QualifiesForGroup(drawing, kind).Should().BeTrue();
    }

    [Fact]
    public void QualifiesAsSectionContext_detectsCrossSections()
    {
        FloorPlanDrawing drawing = new()
        {
            Classification = new DrawingClassification
            {
                DrawingType = "przekroj",
                SheetNumber = "ANY-05",
                Title = "Przekrój"
            }
        };

        MaterialDrawingGroupClassifier.QualifiesAsSectionContext(drawing).Should().BeTrue();
    }

    [Fact]
    public void QualifiesForGroup_usesExtractedFoundationData()
    {
        FloorPlanDrawing drawing = new()
        {
            Classification = new DrawingClassification
            {
                DrawingType = "nieznany",
                Title = "Arkusz bez tytułu"
            },
            Foundations = new FoundationSection
            {
                Footings = [new FootingDetail { Symbol = "L-1" }]
            }
        };

        MaterialDrawingGroupClassifier.QualifiesForGroup(drawing, MaterialDrawingGroupKind.Foundations)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void QualifiesForGroup_structuralDetailWithFundamentTitle_mapsToFoundations()
    {
        FloorPlanDrawing drawing = new()
        {
            Classification = new DrawingClassification
            {
                DrawingType = "detale_konstrukcyjne",
                Title = "Detale fundamentów — zbrojenie ław"
            }
        };

        MaterialDrawingGroupClassifier.QualifiesForGroup(drawing, MaterialDrawingGroupKind.Foundations)
            .Should()
            .BeTrue();
    }
}
