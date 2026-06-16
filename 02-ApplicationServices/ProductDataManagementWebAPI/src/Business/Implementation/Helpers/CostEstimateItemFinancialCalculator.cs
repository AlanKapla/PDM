namespace Business.Implementation.Helpers
{
    /// <summary>
    /// Wspólna logika wyliczania i blokowania pól finansowych pozycji kosztorysu.
    /// </summary>
    public static class CostEstimateItemFinancialCalculator
    {
        public static decimal? CalculateValueNet(
            decimal? unitPriceNet,
            decimal? quantity,
            decimal? valueNetField)
        {
            if (unitPriceNet.HasValue && quantity.HasValue)
            {
                return unitPriceNet.Value * quantity.Value;
            }

            return valueNetField;
        }

        public static decimal? CalculateTotalVat(
            decimal? valueNet,
            decimal? vatRate,
            decimal? totalVatField)
        {
            if (valueNet.HasValue && vatRate.HasValue)
            {
                return valueNet.Value * vatRate.Value;
            }

            return totalVatField;
        }

        public static decimal? CalculateValueGross(
            decimal? valueNet,
            decimal? totalVat,
            decimal? vatRate,
            decimal? valueGrossField)
        {
            if (valueNet.HasValue && totalVat.HasValue)
            {
                return valueNet.Value + totalVat.Value;
            }

            if (valueNet.HasValue && vatRate.HasValue)
            {
                return valueNet.Value * (1m + vatRate.Value);
            }

            return valueGrossField;
        }

        public static decimal? CalculateUnitPriceGross(
            decimal? unitPriceNet,
            decimal? vatRate,
            decimal? valueGross,
            decimal? quantity,
            decimal? unitPriceGrossField)
        {
            if (unitPriceNet.HasValue && vatRate.HasValue)
            {
                return unitPriceNet.Value * (1m + vatRate.Value);
            }

            if (valueGross.HasValue && quantity.HasValue && quantity.Value != 0m)
            {
                return valueGross.Value / quantity.Value;
            }

            return unitPriceGrossField;
        }

        public static bool IsNetValueComputed(decimal? unitPriceNet, decimal? quantity)
        {
            return unitPriceNet.HasValue && quantity.HasValue;
        }

        public static bool IsVatValueComputed(decimal? valueNet, decimal? vatRate)
        {
            return valueNet.HasValue && vatRate.HasValue;
        }

        public static bool IsGrossValueComputed(
            decimal? valueNet,
            decimal? totalVat,
            decimal? vatRate)
        {
            return (valueNet.HasValue && totalVat.HasValue)
                || (valueNet.HasValue && vatRate.HasValue);
        }

        public static bool IsUnitPriceGrossComputed(
            decimal? unitPriceNet,
            decimal? vatRate,
            decimal? valueGross,
            decimal? quantity)
        {
            return (unitPriceNet.HasValue && vatRate.HasValue)
                || (valueGross.HasValue && quantity.HasValue && quantity.Value != 0m);
        }
    }
}
