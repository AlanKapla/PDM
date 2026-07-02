using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ProjectModelFallbackBuilderTests
{
    [Fact]
    public void Build_groupsRoomsByFloorLevel()
    {
        // Arrange
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "rzut_parteru",
                    FloorLevel = "Parter",
                    FloorOrder = 0,
                    Title = "Dom testowy"
                },
                Rooms = [new Room { Name = "Salon", AreaM2 = 20 }]
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "rzut_poddasza",
                    FloorLevel = "Poddasze",
                    FloorOrder = 99
                },
                Rooms = [new Room { Name = "Sypialnia", AreaM2 = 15 }]
            }
        ];

        // Act
        ProjectModel model = ProjectModelFallbackBuilder.Build(drawings);

        // Assert
        model.Project.Name.Should().Be("Dom testowy");
        model.Floors.Should().HaveCount(2);
        model.Floors[0].Level.Should().Be("Parter");
        model.Floors[0].Rooms.Should().ContainSingle(room => room.Name == "Salon");
        model.Floors[1].Level.Should().Be("Poddasze");
        model.Floors[1].Order.Should().Be(99);
    }
}
