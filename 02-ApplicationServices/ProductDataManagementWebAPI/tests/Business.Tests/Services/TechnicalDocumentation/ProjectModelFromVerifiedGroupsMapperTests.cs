using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ProjectModelFromVerifiedGroupsMapperTests
{
    [Fact]
    public void Map_k02SteelMass_mapsToCeilingSteelBottom()
    {
        VerifiedGroupExtractionResult verified = new()
        {
            GroupName = "reinforcement",
            VerifiedJson = """{"k02":{"total_mass_printed_kg":1170.30}}""",
        };

        ProjectModel model = ProjectModelFromVerifiedGroupsMapper.Map([verified]);

        model.Ceilings.Should().ContainSingle();
        model.Ceilings[0].SteelBottomKg.Should().Be(1170.30);
    }

    [Fact]
    public void MergePreferNonEmpty_preservesLlmMetadataAndAddsStructuralData()
    {
        ProjectModel llm = new()
        {
            Project = new ProjectModelMetadata { Name = "Dom z LLM" },
        };

        ProjectModel mapped = new()
        {
            Ceilings =
            [
                new ProjectModelCeiling { SteelBottomKg = 1170.30 }
            ],
        };

        ProjectModel merged = ProjectModelFromVerifiedGroupsMapper.MergePreferNonEmpty(llm, mapped);

        merged.Project.Name.Should().Be("Dom z LLM");
        merged.Ceilings.Should().ContainSingle();
        merged.Ceilings[0].SteelBottomKg.Should().Be(1170.30);
    }
}
