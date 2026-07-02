using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Business.Interfaces.WebModels.TechnicalDocumentation.Validation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ProjectModelSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    [Fact]
    public void Serialize_roundTrip_newFormat_preservesProjectModelSection81()
    {
        // Arrange
        DateTimeOffset processedAt = DateTimeOffset.Parse("2024-06-26T12:00:00Z");
        ProjectTechnicalDocumentationDetails details = new()
        {
            ProjectModel = new ProjectModel
            {
                Project = new ProjectModelMetadata { Name = "Dom jednorodzinny" },
                Floors =
                [
                    new ProjectModelFloor
                    {
                        Level = "Parter",
                        Order = 0,
                        Rooms = [new ProjectModelRoom { Name = "Salon", AreaM2 = 24.5 }]
                    }
                ],
                Ceilings =
                [
                    new ProjectModelCeiling
                    {
                        ThicknessCm = 18,
                        Concrete = "C25/30",
                        SteelBottomKg = 1170.30,
                        SteelTopKg = 604.73
                    }
                ],
                Elevations =
                [
                    new ProjectModelElevation
                    {
                        Orientation = "Południe",
                        SourceDrawing = "A-07",
                        Finishes =
                        [
                            new ProjectModelElevationFinish
                            {
                                Zone = "E",
                                Material = "Tynk elewacyjny"
                            }
                        ]
                    }
                ],
                Warnings =
                [
                    new ProjectModelWarning
                    {
                        Code = "missing_data",
                        Message = "Brak zestawienia stolarki",
                        Severity = "warning"
                    }
                ],
                ExtractionMetadata = new ProjectModelExtractionMetadata
                {
                    PipelineVersion = ProjectModelSection81Enricher.PipelineVersion,
                    ThematicGroups = ["floor_plans", "elevations"],
                    TokenUsage = 12_500,
                    ProcessedAt = processedAt
                }
            },
            MaterialSchedule = new DetailsMaterialSchedule
            {
                CalculatedAt = DateTime.UtcNow
            },
            AuditResult = new AuditResult
            {
                Warnings = ["Test audit warning"]
            },
            TokenUsage = 12_500,
            ProcessedAt = processedAt
        };

        // Act
        string json = TechnicalDocumentationDetailsSerializer.Serialize(details);
        ProjectTechnicalDocumentationDetails? roundTrip = TechnicalDocumentationDetailsSerializer.Deserialize(json);

        // Assert
        json.Should().Contain("\"projectModel\"");
        json.Should().Contain("\"materialSchedule\"");
        json.Should().Contain("\"auditResult\"");
        TechnicalDocumentationDetailsSerializer.IsNewFormat(json).Should().BeTrue();

        roundTrip.Should().NotBeNull();
        roundTrip!.ProjectModel.Should().NotBeNull();
        roundTrip.ProjectModel!.Project.Name.Should().Be("Dom jednorodzinny");
        roundTrip.ProjectModel.Slab.Should().NotBeNull();
        roundTrip.ProjectModel.Slab!.ThicknessCm.Should().Be(18);
        roundTrip.ProjectModel.Slab.SteelBottomKg.Should().Be(1170.30);
        roundTrip.ProjectModel.Elevations.Should().ContainSingle();
        roundTrip.ProjectModel.Elevations[0].Orientation.Should().Be("Południe");
        roundTrip.ProjectModel.Warnings.Should().ContainSingle();
        roundTrip.ProjectModel.ExtractionMetadata.PipelineVersion.Should().Be(ProjectModelSection81Enricher.PipelineVersion);
        roundTrip.MaterialSchedule.Should().NotBeNull();
        roundTrip.AuditResult!.Warnings.Should().Contain("Test audit warning");
        roundTrip.TokenUsage.Should().Be(12_500);
        roundTrip.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    public void Deserialize_legacyFormat_doesNotCrash()
    {
        // Arrange
        string legacyJson = """
            {
              "project": { "name": "Legacy dom" },
              "totalAreaM2": 120.5,
              "rooms": [
                {
                  "floor": "Parter",
                  "floorOrder": 0,
                  "items": [{ "name": "Salon", "areaM2": 24.5 }]
                }
              ],
              "installations": {},
              "tokenUsage": 500
            }
            """;

        // Act
        ProjectTechnicalDocumentationDetails? details = TechnicalDocumentationDetailsSerializer.Deserialize(legacyJson);

        // Assert
        details.Should().NotBeNull();
        details!.Project.Name.Should().Be("Legacy dom");
        details.Rooms.Should().ContainSingle();
        details.TotalAreaM2.Should().Be(120.5);
        details.TokenUsage.Should().Be(500);
        TechnicalDocumentationDetailsSerializer.IsNewFormat(legacyJson).Should().BeFalse();
    }

    [Fact]
    public void Deserialize_projectModel_standalone_doesNotStackOverflow()
    {
        // Arrange
        string json = """
            {
              "project": {"name": "Test"},
              "floors": [{"level": "Parter", "order": 0, "rooms": []}],
              "slab": {"thicknessCm": 18, "concrete": "C25/30"},
              "elevations": [{"orientation": "Północ", "finishes": [], "openings": []}],
              "warnings": [{"message": "test", "severity": "warning"}],
              "extractionMetadata": {"pipelineVersion": "group-pipeline-v1"},
              "conflicts": []
            }
            """;

        // Act
        ProjectModel model = JsonSerializer.Deserialize<ProjectModel>(json, JsonOptions)
            ?? new ProjectModel();

        // Assert
        model.Project.Name.Should().Be("Test");
        model.Floors.Should().HaveCount(1);
        model.Slab!.ThicknessCm.Should().Be(18);
        model.Elevations.Should().ContainSingle();
        model.Warnings.Should().ContainSingle();
        model.ExtractionMetadata.PipelineVersion.Should().Be("group-pipeline-v1");
    }

    [Fact]
    public void Serialize_mapsCeilingsToSlab_whenSlabMissing()
    {
        // Arrange
        ProjectTechnicalDocumentationDetails details = new()
        {
            ProjectModel = new ProjectModel
            {
                Ceilings =
                [
                    new ProjectModelCeiling
                    {
                        ThicknessCm = 20,
                        Concrete = "C30/37",
                        SteelBottomKg = 800
                    }
                ]
            }
        };

        // Act
        string json = TechnicalDocumentationDetailsSerializer.Serialize(details);
        ProjectTechnicalDocumentationDetails? roundTrip = TechnicalDocumentationDetailsSerializer.Deserialize(json);

        // Assert
        roundTrip!.ProjectModel!.Slab.Should().NotBeNull();
        roundTrip.ProjectModel.Slab!.ThicknessCm.Should().Be(20);
        roundTrip.ProjectModel.Slab.Concrete.Should().Be("C30/37");
        roundTrip.ProjectModel.Slab.SteelBottomKg.Should().Be(800);
    }

    [Fact]
    public void Serialize_mapsConflictsAndMissingDataToWarnings_whenWarningsEmpty()
    {
        // Arrange
        ProjectTechnicalDocumentationDetails details = new()
        {
            ProjectModel = new ProjectModel
            {
                Conflicts =
                [
                    new ProjectModelConflict
                    {
                        FieldPath = "floors[0].totalAreaM2",
                        ValueA = "100",
                        ValueB = "99"
                    }
                ],
                MissingData = ["Brak opisu instalacji"]
            }
        };

        // Act
        string json = TechnicalDocumentationDetailsSerializer.Serialize(details);
        ProjectTechnicalDocumentationDetails? roundTrip = TechnicalDocumentationDetailsSerializer.Deserialize(json);

        // Assert
        roundTrip!.ProjectModel!.Warnings.Should().HaveCount(2);
        roundTrip.ProjectModel.Warnings.Should().Contain(w => w.Code == "conflict");
        roundTrip.ProjectModel.Warnings.Should().Contain(w => w.Code == "missing_data");
    }

    [Fact]
    public void Serialize_validationReport_roundTrip_doesNotStackOverflow()
    {
        // Arrange
        ValidationReport original = new()
        {
            TotalFields = 10,
            HighConfidence = 7,
            MediumConfidence = 2,
            LowConfidence = 1,
            Disagreements =
            [
                new FieldDisagreement
                {
                    FieldPath = "rooms[0].areaM2",
                    ValueA = "21.3",
                    ValueB = "21.0",
                    Resolved = "21.3"
                }
            ]
        };

        // Act
        string json = JsonSerializer.Serialize(original, JsonOptions);
        ValidationReport? roundTrip = JsonSerializer.Deserialize<ValidationReport>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Disagreements.Should().ContainSingle();
        roundTrip.Disagreements[0].FieldPath.Should().Be("rooms[0].areaM2");
    }
}
