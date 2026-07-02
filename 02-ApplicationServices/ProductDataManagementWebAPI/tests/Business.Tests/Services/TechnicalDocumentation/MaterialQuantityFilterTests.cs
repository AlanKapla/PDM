using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class MaterialQuantityFilterTests
{
    [Fact]
    public void PruneZeroQuantities_removesSummaryAndSectionItemsWithZeroQuantity()
    {
        MaterialSchedule schedule = new()
        {
            Summary =
            [
                new MaterialSummaryItem { Category = "sciany", MaterialType = "tynk", GrossQuantity = 0, Unit = "m2" },
                new MaterialSummaryItem { Category = "sciany", MaterialType = "bloczek", GrossQuantity = 12.5, Unit = "m2" }
            ],
            Roof = new RoofMaterials
            {
                Timber =
                [
                    new MaterialItem { Element = "murlaty", GrossQuantity = 0, Unit = "mb" },
                    new MaterialItem { Element = "krokwie", GrossQuantity = 24, Unit = "szt" }
                ]
            }
        };

        MaterialSchedule pruned = MaterialQuantityFilter.PruneZeroQuantities(schedule);

        pruned.Summary.Should().HaveCount(1);
        pruned.Summary[0].MaterialType.Should().Be("bloczek");
        pruned.Roof.Timber.Should().HaveCount(1);
        pruned.Roof.Timber[0].Element.Should().Be("krokwie");
    }

    [Fact]
    public void Filter_removesMaterialQuantitiesWithZeroOrNegativeQuantity()
    {
        List<Business.Interfaces.WebModels.TechnicalDocumentation.MaterialQuantity> items =
        [
            new() { MaterialType = "lata sosna", Quantity = 0, Unit = "mb" },
            new() { MaterialType = "beton komorkowy", Quantity = 45, Unit = "m3" }
        ];

        List<Business.Interfaces.WebModels.TechnicalDocumentation.MaterialQuantity> filtered =
            MaterialQuantityFilter.Filter(items);

        filtered.Should().HaveCount(1);
        filtered[0].MaterialType.Should().Be("beton komorkowy");
    }
}
