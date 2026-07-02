using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class MaterialScheduleMergerTests
{
    [Fact]
    public void Overlay_replacesMatchingItemsAndKeepsOthers()
    {
        MaterialSchedule baseSchedule = new()
        {
            Foundations =
            {
                Concrete =
                [
                    new MaterialItem
                    {
                        Element = "Beton ław",
                        NetQuantity = 10,
                        GrossQuantity = 10.5,
                        Unit = "m3",
                        SourceType = "estimated"
                    }
                ]
            },
            Ceilings =
            {
                Steel =
                [
                    new MaterialItem
                    {
                        Element = "Stal dolna",
                        NetQuantity = 500,
                        GrossQuantity = 550,
                        Unit = "kg",
                        SourceType = "estimated"
                    }
                ]
            }
        };

        MaterialSchedule overlay = new()
        {
            Foundations =
            {
                Concrete =
                [
                    new MaterialItem
                    {
                        Element = "Beton ław",
                        NetQuantity = 12,
                        GrossQuantity = 12.6,
                        Unit = "m3",
                        SourceType = "read"
                    }
                ]
            },
            Ceilings =
            {
                Steel =
                [
                    new MaterialItem
                    {
                        Element = "Stal górna",
                        NetQuantity = 600,
                        GrossQuantity = 660,
                        Unit = "kg",
                        SourceType = "read"
                    }
                ]
            }
        };

        MaterialSchedule merged = MaterialScheduleMerger.Overlay(baseSchedule, overlay);

        merged.Foundations.Concrete.Should().ContainSingle(item => item.SourceType == "read" && item.NetQuantity == 12);
        merged.Ceilings.Steel.Should().HaveCount(2);
        merged.Ceilings.Steel.Should().Contain(item => item.Element == "Stal dolna");
        merged.Ceilings.Steel.Should().Contain(item => item.Element == "Stal górna");
    }

    [Fact]
    public void Merge_deduplicatesInsulationByElementName()
    {
        MaterialSchedule foundations = new()
        {
            Insulation =
            [
                new MaterialItem
                {
                    Element = "Styropian EPS 100 — ściany zewnętrzne",
                    NetQuantity = 150,
                    GrossQuantity = 165,
                    Unit = "m2"
                }
            ]
        };

        MaterialSchedule walls = new()
        {
            Insulation =
            [
                new MaterialItem
                {
                    Element = "Styropian EPS 100 — ściany zewnętrzne",
                    NetQuantity = 148,
                    GrossQuantity = 162.8,
                    Unit = "m2"
                }
            ]
        };

        MaterialSchedule merged = MaterialScheduleMerger.Merge([foundations, walls]);

        merged.Insulation.Should().ContainSingle();
        merged.Insulation[0].GrossQuantity.Should().Be(165);
    }
}
