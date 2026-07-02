using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class AuditResultJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    [Fact]
    public void Deserialize_unitErrorsObject_parsesFieldFoundExpected()
    {
        string json = """
            {
              "warnings": ["brak section w timberGroups[0]"],
              "unitErrors": [{"field": "timber[0].unit", "found": "mb", "expected": "m3"}],
              "missingData": [],
              "crossReferenceErrors": ["A-02 vs K-01 — różna grubość ściany"]
            }
            """;

        AuditResult? result = JsonSerializer.Deserialize<AuditResult>(json, JsonOptions);

        result.Should().NotBeNull();
        result!.Warnings.Should().ContainSingle();
        result.UnitErrors.Should().ContainSingle();
        result.UnitErrors[0].Field.Should().Be("timber[0].unit");
        result.UnitErrors[0].Found.Should().Be("mb");
        result.UnitErrors[0].Expected.Should().Be("m3");
        result.CrossReferenceErrors.Should().ContainSingle();
    }

    [Fact]
    public void Deserialize_unitErrorsLegacyStrings_mapsToFound()
    {
        string json = """{"unitErrors":["stal ma jednostkę m zamiast kg"]}""";

        AuditResult? result = JsonSerializer.Deserialize<AuditResult>(json, JsonOptions);

        result.Should().NotBeNull();
        result!.UnitErrors.Should().ContainSingle();
        result.UnitErrors[0].Found.Should().Contain("kg");
    }

    [Fact]
    public void Serialize_roundTrip_doesNotStackOverflow()
    {
        AuditResult original = new()
        {
            Warnings = ["brak section w timberGroups[0]"],
            UnitErrors =
            [
                new AuditUnitError
                {
                    Field = "timber[0].unit",
                    Found = "mb",
                    Expected = "m3"
                }
            ],
            CrossReferenceErrors = ["A-02 vs K-01"]
        };

        string json = JsonSerializer.Serialize(original, JsonOptions);
        AuditResult? roundTrip = JsonSerializer.Deserialize<AuditResult>(json, JsonOptions);

        roundTrip.Should().NotBeNull();
        roundTrip!.Warnings.Should().ContainSingle();
        roundTrip.UnitErrors.Should().ContainSingle();
        roundTrip.UnitErrors[0].Expected.Should().Be("m3");
        roundTrip.CrossReferenceErrors.Should().ContainSingle();
    }
}
