using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Services.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ExtractionDiffEngineTests
{
    [Fact]
    public void Compare_numericDifferenceAboveOnePercent_isCritical()
    {
        string jsonA = """{"k02":{"total_mass_printed_kg":1170.30}}""";
        string jsonB = """{"k02":{"total_mass_printed_kg":1200.00}}""";

        ExtractionDiffResult result = ExtractionDiffEngine.Compare(jsonA, jsonB);

        result.HasCriticalDifferences.Should().BeTrue();
        result.Differences.Should().Contain(diff => diff.IsCritical);
    }

    [Fact]
    public void Compare_identicalJson_hasNoDifferences()
    {
        string json = """{"k02":{"total_mass_printed_kg":1170.30}}""";

        ExtractionDiffResult result = ExtractionDiffEngine.Compare(json, json);

        result.HasCriticalDifferences.Should().BeFalse();
        result.HasMinorDifferences.Should().BeFalse();
        result.Differences.Should().BeEmpty();
    }
}
