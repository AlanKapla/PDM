using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class GroupExtractionProjectModelMapperTests
{
    private const string SampleExtractionJson = """
        {
          "projectModel": {
            "floorPlans": [
              {
                "drawingNumber": "A-02",
                "name": "Rzut parteru",
                "type": "rzut_parteru",
                "tables": [
                  {
                    "name": "Pomieszczenia",
                    "rows": [
                      {"Nr": "01", "Nazwa": "Wiatrołap", "Pow. [m2]": 4.52},
                      {"Nr": "05", "Nazwa": "Salon", "Pow. [m2]": 42.13}
                    ],
                    "totalArea": 130.45
                  }
                ]
              },
              {
                "drawingNumber": "A-03",
                "name": "Rzut poddasza",
                "type": "rzut_poddasza",
                "tables": [
                  {
                    "name": "Pomieszczenia",
                    "rows": [
                      {"Nr": "02", "Nazwa": "Pokój 1", "Pow. [m2]": 15.42}
                    ],
                    "totalArea": 69.40
                  }
                ]
              }
            ],
            "elevations": [
              {
                "drawingNumber": "A-07",
                "title": "Elewacja frontowa (NE)",
                "materials": [
                  {"type": "Tynk elewacji", "color": "Biały"}
                ]
              }
            ],
            "reinforcement": {
              "drawings": [
                {
                  "id": "K-02",
                  "name": "Zbrojenie dolne stropu",
                  "type": "zbrojenie_stropu_dolne",
                  "tables": [{ "totalMass": 1170.30 }]
                },
                {
                  "id": "K-03",
                  "name": "Zbrojenie górne stropu",
                  "type": "zbrojenie_stropu_gorne",
                  "tables": [{ "totalMass": 604.73 }]
                }
              ]
            },
            "roofStructure": {
              "drawings": [
                {
                  "id": "A-04",
                  "details": { "roofArea": "328 m2", "roofSlope": "35°" }
                },
                {
                  "id": "K-04",
                  "details": {
                    "woodList": [
                      {"type": "Krokwie", "dimensions": "7 x 14", "quantity": 38, "volume": "1.091 m3"}
                    ],
                    "totalVolume": "2.000 m3"
                  }
                }
              ]
            }
          }
        }
        """;

    [Fact]
    public void ApplyGroupJson_mapsGroupExtractionFormat_toProjectModelSection81()
    {
        ProjectModel model = new();

        GroupExtractionProjectModelMapper.ApplyGroupJson(model, SampleExtractionJson, "floor_plans");

        model.Floors.Should().HaveCount(2);
        model.Floors[0].Level.Should().Be("Parter");
        model.Floors[0].Rooms.Should().HaveCount(2);
        model.Floors[0].TotalAreaM2.Should().Be(130.45);
        model.Elevations.Should().ContainSingle();
        model.Ceilings.Should().HaveCount(2);
        model.Ceilings[0].SteelBottomKg.Should().Be(1170.30);
        model.Ceilings[1].SteelTopKg.Should().Be(604.73);
        model.Slab!.SteelBottomKg.Should().Be(1170.30);
        model.Roof.AreaM2.Should().Be(328);
        model.Roof.PitchDegrees.Should().Be(35);
        model.Roof.TimberGroups.Should().ContainSingle();
    }

    [Fact]
    public void Map_mergesMultipleVerifiedGroups_withoutOverwritingEntireProjectModel()
    {
        VerifiedGroupExtractionResult floorPlans = new()
        {
            GroupName = "floor_plans",
            VerifiedJson = """
                {"projectModel":{"floorPlans":[{"drawingNumber":"A-02","name":"Rzut parteru","type":"rzut_parteru","tables":[{"rows":[{"Nr":"01","Nazwa":"Salon","Pow. [m2]":28.4}],"totalArea":28.4}]}]}}
                """,
        };

        VerifiedGroupExtractionResult reinforcement = new()
        {
            GroupName = "reinforcement",
            VerifiedJson = """
                {"projectModel":{"reinforcement":{"drawings":[{"id":"K-02","name":"Zbrojenie dolne","type":"zbrojenie_stropu_dolne","tables":[{"totalMass":1170.30}]}]}}}
                """,
        };

        ProjectModel model = ProjectModelFromVerifiedGroupsMapper.Map([floorPlans, reinforcement]);

        model.Floors.Should().ContainSingle();
        model.Floors[0].Rooms.Should().ContainSingle();
        model.Ceilings.Should().ContainSingle();
        model.Ceilings[0].SteelBottomKg.Should().Be(1170.30);
    }
}
