using Business.Implementation.Services.AI.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class MaterialUnitNormalizerTests
{
    [Theory]
    [InlineData("bloczek fundamentowy Fb", MaterialUnitSection.FoundationBlocks, "szt")]
    [InlineData("beton C25/30", MaterialUnitSection.FoundationConcrete, "m3")]
    [InlineData("stal zbrojeniowa Ø12", MaterialUnitSection.FoundationSteel, "kg")]
    [InlineData("styropian XPS 10cm", MaterialUnitSection.FoundationInsulation, "m2")]
    [InlineData("tynk mozaikowy", MaterialUnitSection.WallMortar, "m2")]
    [InlineData("beton komórkowy Ytong", MaterialUnitSection.WallLayer, "m3")]
    [InlineData("krokwie C24", MaterialUnitSection.TimberElement, "m3")]
    public void ResolveUnit_returnsExpectedUnitForMaterialType(
        string materialType,
        MaterialUnitSection section,
        string expectedUnit)
    {
        MaterialUnitNormalizer.ResolveUnit(materialType, section).Should().Be(expectedUnit);
    }

    [Fact]
    public void ResolveWallLayerQuantity_calculatesVolumeForM3Layers()
    {
        Business.Interfaces.WebModels.TechnicalDocumentation.Drawings.WallLayer layer = new()
        {
            Material = "beton komórkowy",
            ThicknessCm = 24
        };

        double quantity = MaterialUnitNormalizer.ResolveWallLayerQuantity(50, layer, "m3");

        quantity.Should().Be(12);
    }

    [Fact]
    public void ResolveTimberVolumeM3_calculatesFromSectionLengthAndCount()
    {
        Business.Interfaces.WebModels.TechnicalDocumentation.Drawings.TimberElement timber = new()
        {
            Element = "krokwie",
            Section = "8x16",
            LengthM = 4.5,
            Count = 16
        };

        MaterialUnitNormalizer.ResolveTimberVolumeM3(timber).Should().Be(0.922);
    }

    [Fact]
    public void ResolveTimberVolumeM3_acceptsSlashSectionNotation()
    {
        Business.Interfaces.WebModels.TechnicalDocumentation.Drawings.TimberElement timber = new()
        {
            Element = "krokwie",
            Section = "20/5",
            LengthM = 4.0,
            Count = 10
        };

        MaterialUnitNormalizer.ResolveTimberVolumeM3(timber).Should().Be(0.4);
    }

    [Fact]
    public void FormatTimberMaterialLabel_includesSectionBetweenElementAndWoodType()
    {
        Business.Interfaces.WebModels.TechnicalDocumentation.Drawings.TimberElement timber = new()
        {
            Element = "murlaty",
            Section = "10x20",
            WoodType = "C24"
        };

        MaterialUnitNormalizer.FormatTimberMaterialLabel(timber).Should().Be("murlaty 10x20 C24");
    }

    [Fact]
    public void NormalizeSchedule_fixesIncorrectUnits()
    {
        Business.Interfaces.WebModels.TechnicalDocumentation.Materials.MaterialSchedule schedule = new()
        {
            Summary =
            [
                new()
                {
                    Category = "sciany",
                    MaterialType = "bloczek ceramiczny",
                    GrossQuantity = 1200,
                    Unit = "m2"
                }
            ],
            Foundations = new()
            {
                Steel =
                [
                    new()
                    {
                        Element = "stal",
                        GrossQuantity = 420,
                        Unit = "m2"
                    }
                ]
            }
        };

        MaterialUnitNormalizer.NormalizeSchedule(schedule);

        schedule.Summary[0].Unit.Should().Be("szt");
        schedule.Foundations.Steel[0].Unit.Should().Be("kg");
    }
}
