using Business.Implementation.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentAssertions;

namespace Business.Tests.Services;

public class CostEstimateCalculationServiceTests
{
    private readonly CostEstimateCalculationService _sut = new CostEstimateCalculationService();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CostEstimateTemplate TemplateWithNetAndGrossSum()
    {
        return new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Test Template",
            CalculatedFieldDefinitions = new List<CostEstimateTemplateItemCalculatedFieldDefinition>
            {
                new CostEstimateTemplateItemCalculatedFieldDefinition
                {
                    Id = Guid.NewGuid(),
                    FieldScope = FieldScope.ItemCalculated,
                    FieldType = FieldType.ItemCalculatedValueNet,
                    Label = "Value Net",
                    SumInGroup = true,
                    SumInTotal = true
                },
                new CostEstimateTemplateItemCalculatedFieldDefinition
                {
                    Id = Guid.NewGuid(),
                    FieldScope = FieldScope.ItemCalculated,
                    FieldType = FieldType.ItemCalculatedValueGross,
                    Label = "Value Gross",
                    SumInGroup = true,
                    SumInTotal = true
                },
                new CostEstimateTemplateItemCalculatedFieldDefinition
                {
                    Id = Guid.NewGuid(),
                    FieldScope = FieldScope.ItemCalculated,
                    FieldType = FieldType.ItemCalculatedTotalVat,
                    Label = "Total VAT",
                    SumInGroup = true,
                    SumInTotal = true
                }
            },
            SystemFieldDefinitions = new List<CostEstimateTemplateItemSystemFieldDefinition>()
        };
    }

    private static CostEstimateTemplate TemplateWithNoSumFields()
    {
        return new CostEstimateTemplate
        {
            Id = Guid.NewGuid(),
            Name = "No Sum Template",
            CalculatedFieldDefinitions = new List<CostEstimateTemplateItemCalculatedFieldDefinition>(),
            SystemFieldDefinitions = new List<CostEstimateTemplateItemSystemFieldDefinition>()
        };
    }

    private static CostEstimateItem BuildItemWithFieldValues(
        CostEstimateTemplateItemCalculatedFieldDefinition netFieldDef,
        CostEstimateTemplateItemCalculatedFieldDefinition grossFieldDef,
        CostEstimateTemplateItemCalculatedFieldDefinition vatFieldDef,
        decimal unitPriceNet,
        decimal quantity,
        decimal vatRate)
    {
        CostEstimateTemplateItemCalculatedFieldDefinition unitPriceDef = new CostEstimateTemplateItemCalculatedFieldDefinition
        {
            Id = Guid.NewGuid(),
            FieldScope = FieldScope.ItemCalculated,
            FieldType = FieldType.ItemCalculatedUnitPriceNet,
            Label = "Unit Price Net"
        };

        CostEstimateTemplateItemCalculatedFieldDefinition vatRateDef = new CostEstimateTemplateItemCalculatedFieldDefinition
        {
            Id = Guid.NewGuid(),
            FieldScope = FieldScope.ItemCalculated,
            FieldType = FieldType.ItemCalculatedVatRate,
            Label = "VAT Rate"
        };

        CostEstimateTemplateItemSystemFieldDefinition quantityDef = new CostEstimateTemplateItemSystemFieldDefinition
        {
            Id = Guid.NewGuid(),
            FieldScope = FieldScope.ItemSystem,
            FieldType = FieldType.ItemSystemQuantity,
            Label = "Quantity"
        };

        CostEstimateItem item = new CostEstimateItem
        {
            Id = Guid.NewGuid(),
            Name = "Test Item",
            RelationType = ItemRelationType.None,
            FieldValues = new List<CostEstimateItemFieldValue>
            {
                new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    FieldDefinition = unitPriceDef,
                    DecimalValue = unitPriceNet
                },
                new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    FieldDefinition = vatRateDef,
                    DecimalValue = vatRate
                },
                new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    FieldDefinition = quantityDef,
                    DecimalValue = quantity
                },
                new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    FieldDefinition = netFieldDef,
                    DecimalValue = null
                },
                new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    FieldDefinition = grossFieldDef,
                    DecimalValue = null
                },
                new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    FieldDefinition = vatFieldDef,
                    DecimalValue = null
                }
            }
        };

        return item;
    }

    // ─── RecalculateCostEstimate — null guard ─────────────────────────────────

    [Fact]
    public void RecalculateCostEstimate_NullInput_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _sut.RecalculateCostEstimate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // ─── RecalculateCostEstimate — no sum fields ──────────────────────────────

    [Fact]
    public void RecalculateCostEstimate_TemplateWithNoSumFields_TotalsRemainNull()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNoSumFields();
        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group 1",
            Items = new List<CostEstimateItem>()
        };
        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup> { group }
        };

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert
        estimate.TotalNet.Should().BeNull();
        estimate.TotalGross.Should().BeNull();
        estimate.TotalVat.Should().BeNull();
    }

    // ─── RecalculateCostEstimate — single item ────────────────────────────────

    [Fact]
    public void RecalculateCostEstimate_SingleItemWithUnitPriceAndQuantity_CalculatesCorrectTotals()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNetAndGrossSum();
        CostEstimateTemplateItemCalculatedFieldDefinition netDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueNet);
        CostEstimateTemplateItemCalculatedFieldDefinition grossDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueGross);
        CostEstimateTemplateItemCalculatedFieldDefinition vatDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedTotalVat);

        // unitPriceNet=100, quantity=3, vatRate=0.23 → net=300, vat=69, gross=369
        CostEstimateItem item = BuildItemWithFieldValues(netDef, grossDef, vatDef,
            unitPriceNet: 100m, quantity: 3m, vatRate: 0.23m);

        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group 1",
            Items = new List<CostEstimateItem> { item }
        };
        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup> { group }
        };

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert
        estimate.TotalNet.Should().Be(300m);
        estimate.TotalVat.Should().Be(69m);
        estimate.TotalGross.Should().Be(369m);
    }

    // ─── RecalculateCostEstimate — multiple items ─────────────────────────────

    [Fact]
    public void RecalculateCostEstimate_MultipleItems_SumsAllItems()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNetAndGrossSum();
        CostEstimateTemplateItemCalculatedFieldDefinition netDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueNet);
        CostEstimateTemplateItemCalculatedFieldDefinition grossDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueGross);
        CostEstimateTemplateItemCalculatedFieldDefinition vatDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedTotalVat);

        // item1: 100*2 = 200 net, vat=0.23→46, gross=246
        // item2: 50*4  = 200 net, vat=0.23→46, gross=246
        CostEstimateItem item1 = BuildItemWithFieldValues(netDef, grossDef, vatDef, 100m, 2m, 0.23m);
        CostEstimateItem item2 = BuildItemWithFieldValues(netDef, grossDef, vatDef, 50m, 4m, 0.23m);

        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group 1",
            Items = new List<CostEstimateItem> { item1, item2 }
        };
        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup> { group }
        };

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert
        estimate.TotalNet.Should().Be(400m);
        estimate.TotalVat.Should().Be(92m);
        estimate.TotalGross.Should().Be(492m);
    }

    // ─── RecalculateCostEstimate — deleted items/groups skipped ──────────────

    [Fact]
    public void RecalculateCostEstimate_DeletedItemsSkipped_NotIncludedInTotals()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNetAndGrossSum();
        CostEstimateTemplateItemCalculatedFieldDefinition netDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueNet);
        CostEstimateTemplateItemCalculatedFieldDefinition grossDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueGross);
        CostEstimateTemplateItemCalculatedFieldDefinition vatDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedTotalVat);

        CostEstimateItem activeItem = BuildItemWithFieldValues(netDef, grossDef, vatDef, 100m, 1m, 0m);
        CostEstimateItem deletedItem = BuildItemWithFieldValues(netDef, grossDef, vatDef, 500m, 1m, 0m);
        deletedItem.IsDeleted = true;

        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group 1",
            Items = new List<CostEstimateItem> { activeItem, deletedItem }
        };
        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup> { group }
        };

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert — only active item contributes
        estimate.TotalNet.Should().Be(100m);
    }

    [Fact]
    public void RecalculateCostEstimate_DeletedGroupsSkipped_NotIncludedInTotals()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNetAndGrossSum();
        CostEstimateTemplateItemCalculatedFieldDefinition netDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueNet);
        CostEstimateTemplateItemCalculatedFieldDefinition grossDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueGross);
        CostEstimateTemplateItemCalculatedFieldDefinition vatDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedTotalVat);

        CostEstimateItem item = BuildItemWithFieldValues(netDef, grossDef, vatDef, 100m, 1m, 0m);

        CostEstimateGroup deletedGroup = new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Group",
            Items = new List<CostEstimateItem> { item }
        };
        deletedGroup.IsDeleted = true;

        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup> { deletedGroup }
        };

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert — deleted group not counted
        estimate.TotalNet.Should().BeNull();
    }

    // ─── RecalculateCostEstimate — group totals ───────────────────────────────

    [Fact]
    public void RecalculateCostEstimate_SingleItem_GroupTotalsUpdated()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNetAndGrossSum();
        CostEstimateTemplateItemCalculatedFieldDefinition netDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueNet);
        CostEstimateTemplateItemCalculatedFieldDefinition grossDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueGross);
        CostEstimateTemplateItemCalculatedFieldDefinition vatDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedTotalVat);

        CostEstimateItem item = BuildItemWithFieldValues(netDef, grossDef, vatDef, 200m, 2m, 0.1m);

        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group",
            Items = new List<CostEstimateItem> { item }
        };
        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup> { group }
        };

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert — net=400, vat=40, gross=440
        group.TotalNet.Should().Be(400m);
        group.TotalVat.Should().Be(40m);
        group.TotalGross.Should().Be(440m);
    }

    // ─── RecalculateCostEstimate — item with components ───────────────────────

    [Fact]
    public void RecalculateCostEstimate_ItemWithComponents_SumsComponentValues()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNetAndGrossSum();
        CostEstimateTemplateItemCalculatedFieldDefinition netDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueNet);
        CostEstimateTemplateItemCalculatedFieldDefinition grossDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueGross);
        CostEstimateTemplateItemCalculatedFieldDefinition vatDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedTotalVat);

        CostEstimateItem component1 = BuildItemWithFieldValues(netDef, grossDef, vatDef, 100m, 1m, 0m);
        component1.RelationType = ItemRelationType.Component;

        CostEstimateItem component2 = BuildItemWithFieldValues(netDef, grossDef, vatDef, 200m, 1m, 0m);
        component2.RelationType = ItemRelationType.Component;

        CostEstimateItem parentItem = new CostEstimateItem
        {
            Id = Guid.NewGuid(),
            Name = "Parent Item",
            RelationType = ItemRelationType.None,
            FieldValues = new List<CostEstimateItemFieldValue>()
        };
        parentItem.SetChildItems(new List<CostEstimateItem> { component1, component2 });

        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group",
            Items = new List<CostEstimateItem> { parentItem }
        };
        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup> { group }
        };

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert — parent sums components: 100+200=300 net
        parentItem.NetValue.Should().Be(300m);
        estimate.TotalNet.Should().Be(300m);
    }

    // ─── RecalculateCostEstimate — timestamps updated ─────────────────────────

    [Fact]
    public void RecalculateCostEstimate_Always_SetsLastCalculatedAt()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNoSumFields();
        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup>()
        };

        DateTime before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert
        estimate.LastCalculatedAt.Should().NotBeNull();
        estimate.LastCalculatedAt.Should().BeAfter(before);
    }

    // ─── RecalculateCostEstimate — Selected field filter ──────────────────────

    [Fact]
    public void RecalculateCostEstimate_TemplateHasSelectedField_UnselectedItemsNotIncluded()
    {
        // Arrange
        CostEstimateTemplate template = TemplateWithNetAndGrossSum();

        CostEstimateTemplateItemSystemFieldDefinition selectedFieldDef = new CostEstimateTemplateItemSystemFieldDefinition
        {
            Id = Guid.NewGuid(),
            FieldScope = FieldScope.ItemSystem,
            FieldType = FieldType.ItemSystemSelected,
            Label = "Selected"
        };
        template.SystemFieldDefinitions = new List<CostEstimateTemplateItemSystemFieldDefinition> { selectedFieldDef };

        CostEstimateTemplateItemCalculatedFieldDefinition netDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueNet);
        CostEstimateTemplateItemCalculatedFieldDefinition grossDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedValueGross);
        CostEstimateTemplateItemCalculatedFieldDefinition vatDef = template.CalculatedFieldDefinitions.First(f => f.FieldType == FieldType.ItemCalculatedTotalVat);

        // Selected item
        CostEstimateItem selectedItem = BuildItemWithFieldValues(netDef, grossDef, vatDef, 100m, 1m, 0m);
        selectedItem.FieldValues.Add(new CostEstimateItemFieldValue
        {
            Id = Guid.NewGuid(),
            FieldDefinition = selectedFieldDef,
            BoolValue = true
        });

        // Unselected item
        CostEstimateItem unselectedItem = BuildItemWithFieldValues(netDef, grossDef, vatDef, 500m, 1m, 0m);
        unselectedItem.FieldValues.Add(new CostEstimateItemFieldValue
        {
            Id = Guid.NewGuid(),
            FieldDefinition = selectedFieldDef,
            BoolValue = false
        });

        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group",
            Items = new List<CostEstimateItem> { selectedItem, unselectedItem }
        };
        CostEstimate estimate = new CostEstimate
        {
            Id = Guid.NewGuid(),
            Template = template,
            AllGroups = new List<CostEstimateGroup> { group }
        };

        // Act
        _sut.RecalculateCostEstimate(estimate);

        // Assert — only selected item contributes
        estimate.TotalNet.Should().Be(100m);
    }
}
