using Business.Implementation.Services.AI.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class GroupExtractionJsonMergerTests
{
    [Fact]
    public void Merge_overlappingKeys_lastBatchWins()
    {
        string batchA = """{"k02":{"total_mass_printed_kg":1170.30},"k03":{"title":"górne"}}""";
        string batchB = """{"k04":{"title":"więźba"}}""";

        string merged = GroupExtractionJsonMerger.Merge([batchA, batchB]);

        merged.Should().Contain("k02");
        merged.Should().Contain("k03");
        merged.Should().Contain("k04");
    }
}
