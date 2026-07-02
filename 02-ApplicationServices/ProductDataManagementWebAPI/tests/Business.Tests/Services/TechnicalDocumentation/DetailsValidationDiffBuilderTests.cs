using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class DetailsValidationDiffBuilderTests
{
    [Fact]
    public void Compare_reportsMissingField()
    {
        string expected = """{"project":{"investor":"Kapłowie"}}""";
        string actual = """{"project":{}}""";

        List<DetailsValidationDifference> differences = DetailsValidationDiffBuilder.Compare(expected, actual);

        differences.Should().Contain(difference =>
            difference.Path == "project.investor"
            && difference.Issue == "Brakujące pole"
            && difference.Expected == "Kapłowie");
    }

    [Fact]
    public void Compare_reportsNumericDifferenceBeyondTolerance()
    {
        string expected = """{"totalAreaM2":100}""";
        string actual = """{"totalAreaM2":95}""";

        List<DetailsValidationDifference> differences = DetailsValidationDiffBuilder.Compare(expected, actual);

        differences.Should().Contain(difference =>
            difference.Path == "totalAreaM2"
            && difference.Issue == "Różna wartość liczbowa");
    }

    [Fact]
    public void Compare_ignoresNumericDifferenceWithinTolerance()
    {
        string expected = """{"totalAreaM2":100}""";
        string actual = """{"totalAreaM2":100.5}""";

        List<DetailsValidationDifference> differences = DetailsValidationDiffBuilder.Compare(expected, actual);

        differences.Should().NotContain(difference => difference.Path == "totalAreaM2");
    }

    [Fact]
    public void Compare_reportsArrayLengthMismatch()
    {
        string expected = """{"rooms":[{"number":1},{"number":2}]}""";
        string actual = """{"rooms":[{"number":1}]}""";

        List<DetailsValidationDifference> differences = DetailsValidationDiffBuilder.Compare(expected, actual);

        differences.Should().Contain(difference =>
            difference.Path == "rooms"
            && difference.Issue == "Różna liczba elementów tablicy");
    }

    [Fact]
    public void Compare_returnsEmptyWhenModelsMatch()
    {
        string json = """{"project":{"investor":"Kapłowie","totalAreaM2":142.5}}""";

        List<DetailsValidationDifference> differences = DetailsValidationDiffBuilder.Compare(json, json);

        differences.Should().BeEmpty();
    }
}
