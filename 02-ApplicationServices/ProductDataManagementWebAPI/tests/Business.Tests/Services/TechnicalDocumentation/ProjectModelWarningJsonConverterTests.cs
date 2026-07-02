using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ProjectModelWarningJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    [Fact]
    public void Deserialize_warningsAsStrings_mapsToProjectModelWarningMessage()
    {
        string json = """
            {
              "warnings": [
                "Brak rzutów — konsolidacja materiałów z rysunków ogólnych.",
                "Druga uwaga"
              ]
            }
            """;

        ProjectModel model = JsonSerializer.Deserialize<ProjectModel>(json, JsonOptions)!;

        model.Warnings.Should().HaveCount(2);
        model.Warnings[0].Message.Should().Contain("Brak rzutów");
        model.Warnings[1].Message.Should().Be("Druga uwaga");
    }

    [Fact]
    public void Deserialize_warningsAsObjects_preservesStructuredFields()
    {
        string json = """
            {
              "warnings": [
                {
                  "code": "missing_data",
                  "message": "Brak zestawienia stolarki",
                  "severity": "warning",
                  "sourceGroup": "floor_plans"
                }
              ]
            }
            """;

        ProjectModel model = JsonSerializer.Deserialize<ProjectModel>(json, JsonOptions)!;

        model.Warnings.Should().ContainSingle();
        model.Warnings[0].Code.Should().Be("missing_data");
        model.Warnings[0].Message.Should().Be("Brak zestawienia stolarki");
        model.Warnings[0].Severity.Should().Be("warning");
        model.Warnings[0].SourceGroup.Should().Be("floor_plans");
    }
}
