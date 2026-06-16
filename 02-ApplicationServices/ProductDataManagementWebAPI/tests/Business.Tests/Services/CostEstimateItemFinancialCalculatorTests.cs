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
    public void CalculateValueGross_PrefersNetPlusVat_WhenBothPresent()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateValueGross(100m, 23m, 0.23m, 999m);

        result.Should().Be(123m);
    }

    [Fact]
    public void CalculateValueGross_UsesNetAndRate_WhenVatValueMissing()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateValueGross(100m, null, 0.23m, 999m);

        result.Should().Be(123m);
    }

    [Fact]
    public void CalculateUnitPriceGross_UsesGrossDividedByQuantity_WhenSourcesMissing()
    {
        decimal? result = CostEstimateItemFinancialCalculator.CalculateUnitPriceGross(
            null,
            null,
            300m,
            3m,
            999m);

        result.Should().Be(100m);
    }

    [Fact]
    public void IsNetValueComputed_ReturnsTrue_WhenUnitPriceAndQuantityExist()
    {
        bool result = CostEstimateItemFinancialCalculator.IsNetValueComputed(10m, 2m);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsGrossValueComputed_ReturnsTrue_WhenNetAndVatRateExist()
    {
        bool result = CostEstimateItemFinancialCalculator.IsGrossValueComputed(100m, null, 0.23m);

        result.Should().BeTrue();
    }
}
