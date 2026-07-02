using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class MaterialDrawingGroupResolverTests
{
    [Fact]
    public void Resolve_groupsFoundationsWithDetailDrawing_byDrawingTypeAndDependency()
    {
        FloorPlanDrawing foundation = CreateDrawing("BR-01", "rzut_fundamentow", "Rzut fundamentów");
        FloorPlanDrawing detail = CreateDrawing("BR-06", "detale_konstrukcyjne", "Detale zbrojenia słupów");
        List<DrawingDependencyLink> dependencies =
        [
            new DrawingDependencyLink
            {
                SourceFileName = foundation.Source.FileName,
                SourcePageNumber = foundation.Source.PageNumber,
                TargetFileName = detail.Source.FileName,
                TargetPageNumber = detail.Source.PageNumber,
                DetailType = "zbrojenie słupów"
            }
        ];

        IReadOnlyList<MaterialDrawingGroup> groups = MaterialDrawingGroupResolver.Resolve(
            [foundation, detail],
            dependencies);

        MaterialDrawingGroup foundationGroup = groups.Single(group => group.Kind == MaterialDrawingGroupKind.Foundations);
        foundationGroup.Drawings.Should().HaveCount(2);
        foundationGroup.Drawings.Select(drawing => drawing.Classification.DrawingType)
            .Should()
            .BeEquivalentTo("rzut_fundamentow", "detale_konstrukcyjne");
    }

    [Fact]
    public void Resolve_groupsCeilingsWithSectionDrawing_byDrawingType()
    {
        FloorPlanDrawing lowerSteel = CreateDrawing("STR-02", "zbrojenie_stropu_dolne", "Zbrojenie dolne stropu");
        FloorPlanDrawing upperSteel = CreateDrawing("STR-03", "zbrojenie_stropu_gorne", "Zbrojenie górne stropu");
        FloorPlanDrawing section = CreateDrawing("ARCH-05", "przekroj", "Przekrój poprzeczny");

        IReadOnlyList<MaterialDrawingGroup> groups = MaterialDrawingGroupResolver.Resolve(
            [lowerSteel, upperSteel, section],
            []);

        MaterialDrawingGroup ceilingGroup = groups.Single(group => group.Kind == MaterialDrawingGroupKind.Ceilings);
        ceilingGroup.Drawings.Should().HaveCount(3);
        ceilingGroup.Drawings.Select(drawing => drawing.Classification.DrawingType)
            .Should()
            .BeEquivalentTo("zbrojenie_stropu_dolne", "zbrojenie_stropu_gorne", "przekroj");
    }

    [Fact]
    public void Resolve_groupsRoofDrawingsTogether_byDrawingType()
    {
        FloorPlanDrawing roofPlan = CreateDrawing("ARCH-14", "rzut_dachu", "Rzut dachu");
        FloorPlanDrawing timber = CreateDrawing("STR-14", "rzut_wiezby_dachowej", "Rzut więźby dachowej");

        IReadOnlyList<MaterialDrawingGroup> groups = MaterialDrawingGroupResolver.Resolve(
            [roofPlan, timber],
            []);

        MaterialDrawingGroup roofGroup = groups.Single(group => group.Kind == MaterialDrawingGroupKind.Roof);
        roofGroup.Drawings.Should().HaveCount(2);
    }

    [Fact]
    public void Resolve_groupsWallsFromFloorPlansAndElevations()
    {
        FloorPlanDrawing groundFloor = CreateDrawing("ARCH-02", "rzut_parteru", "Rzut parteru");
        FloorPlanDrawing attic = CreateDrawing("ARCH-03", "rzut_poddasza", "Rzut poddasza");
        FloorPlanDrawing elevation = CreateDrawing("ARCH-08", "elewacja", "Elewacja frontowa");

        IReadOnlyList<MaterialDrawingGroup> groups = MaterialDrawingGroupResolver.Resolve(
            [groundFloor, attic, elevation],
            []);

        MaterialDrawingGroup wallGroup = groups.Single(group => group.Kind == MaterialDrawingGroupKind.Walls);
        wallGroup.Drawings.Should().HaveCount(3);
    }

    [Fact]
    public void Resolve_doesNotIncludeEmptyGroupsInMemberKeys()
    {
        FloorPlanDrawing roofPlan = CreateDrawing("ARCH-14", "rzut_dachu", "Rzut dachu");

        IReadOnlyList<MaterialDrawingGroup> groups = MaterialDrawingGroupResolver.Resolve([roofPlan], []);

        MaterialDrawingGroup foundationsGroup = groups.Single(group => group.Kind == MaterialDrawingGroupKind.Foundations);
        foundationsGroup.Drawings.Should().BeEmpty();
    }

    private static FloorPlanDrawing CreateDrawing(string sheetNumber, string drawingType, string title)
    {
        return new FloorPlanDrawing
        {
            Source = new DrawingSource
            {
                FileName = $"{sheetNumber}.pdf",
                PageNumber = 1
            },
            Classification = new DrawingClassification
            {
                SheetNumber = sheetNumber,
                DrawingType = drawingType,
                Title = title
            }
        };
    }
}
