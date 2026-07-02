using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationCrossReferenceLinkerTests
{
    [Fact]
    public void LinkDrawings_resolvesTargetSheetAndBuildsDependencies()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Source = new DrawingSource { FileName = "plan.pdf", PageNumber = 1 },
                Classification = new DrawingClassification { SheetNumber = "1" },
                CrossReferences =
                [
                    new DrawingCrossReference
                    {
                        ReferenceLabel = "przekroj 1-1",
                        TargetSheetNumber = "3",
                        DetailType = "przekroj"
                    }
                ]
            },
            new FloorPlanDrawing
            {
                Source = new DrawingSource { FileName = "plan.pdf", PageNumber = 3 },
                Classification = new DrawingClassification { SheetNumber = "3", Title = "Przekroj" }
            }
        ];

        List<Business.Interfaces.WebModels.TechnicalDocumentation.DrawingDependencyLink> dependencies =
            TechnicalDocumentationCrossReferenceLinker.LinkDrawings(drawings);

        dependencies.Should().HaveCount(1);
        dependencies[0].TargetPageNumber.Should().Be(3);
        dependencies[0].TargetFileName.Should().Be("plan.pdf");
        drawings[0].CrossReferences[0].ResolvedPageNumber.Should().Be(3);
    }
}
