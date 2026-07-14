using Business.Implementation.Helpers;
using FluentAssertions;

namespace Business.Tests.Services;

public class CostEstimateItemFinancialCalculatorTests
{
    [Fact]
    public void CalculateValueNet_UsesUnitPriceAndQuantity_WhenBothPresent()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateValueNet(10m, 3m, 99m);

        result.Should().Be(30m);
    }

    [Fact]
    public void CalculateValueNet_UsesManualValue_WhenSourcesMissing()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateValueNet(null, 3m, 99m);

        result.Should().Be(99m);
    }

    [Fact]
    public void CalculateTotalVat_UsesNetAndRate_WhenBothPresent()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateTotalVat(100m, 0.23m, 50m);

        result.Should().Be(23m);
    }

    [Fact]
    public void CalculateValueGross_PrefersUnitPriceGrossTimesQuantity_WhenBothPresent()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateValueGross(
            100m,
            23m,
            0.23m,
            50m,
            3m,
            999m);

        result.Should().Be(150m);
    }

    [Fact]
    public void CalculateValueGross_PrefersNetPlusVat_WhenUnitPriceGrossMissing()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateValueGross(
            100m,
            23m,
            0.23m,
            null,
            3m,
            999m);

        result.Should().Be(123m);
    }

    [Fact]
    public void CalculateValueGross_UsesNetAndRate_WhenVatValueMissing()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateValueGross(
            100m,
            null,
            0.23m,
            null,
            3m,
            999m);

        result.Should().Be(123m);
    }

    [Fact]
    public void CalculateGrossValueFromUnitPriceGross_MultipliesUnitPriceByQuantity()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateGrossValueFromUnitPriceGross(12.5m, 4m);

        result.Should().Be(50m);
    }

    [Fact]
    public void CalculateUnitPriceGross_UsesGrossDividedByQuantity_WhenManualFieldMissing()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateUnitPriceGross(
            null,
            null,
            300m,
            3m,
            null);

        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateUnitPriceGross_PrefersManualField_WhenVatMissing()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateUnitPriceGross(
            null,
            null,
            1m,
            1m,
            12m);

        result.Should().Be(12m);
    }

    [Fact]
    public void CalculateUnitPriceGross_UsesNetAndVatRate_WhenBothPresent()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateUnitPriceGross(
            100m,
            0.23m,
            300m,
            3m,
            999m);

        result.Should().Be(123m);
    }

    [Fact]
    public void IsNetValueComputed_ReturnsTrue_WhenUnitPriceAndQuantityExist()
    {
        bool result = CostEstimateItemFinancialCalculator.IsNetValueComputed(10m, 2m);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsGrossValueComputed_ReturnsTrue_WhenUnitPriceGrossAndQuantityExist()
    {
        bool result = CostEstimateItemFinancialCalculator.IsGrossValueComputed(
            null,
            null,
            null,
            50m,
            3m);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsGrossValueComputed_ReturnsTrue_WhenNetAndVatRateExist()
    {
        bool result = CostEstimateItemFinancialCalculator.IsGrossValueComputed(100m, null, 0.23m, null, 3m);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsUnitPriceGrossComputed_ReturnsTrue_WhenUnitNetAndVatRateExist()
    {
        bool result = CostEstimateItemFinancialCalculator.IsUnitPriceGrossComputed(10m, 0.23m);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsUnitPriceGrossComputed_ReturnsFalse_WhenOnlyGrossAndQuantityExist()
    {
        bool result = CostEstimateItemFinancialCalculator.IsUnitPriceGrossComputed(null, null);

        result.Should().BeFalse();
    }
}
