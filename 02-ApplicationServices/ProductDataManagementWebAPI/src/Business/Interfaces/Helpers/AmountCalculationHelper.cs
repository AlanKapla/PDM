namespace Business.Interfaces.Helpers
{
    /// <summary>
    /// Helper class for amount calculations and validation
    /// </summary>
    public static class AmountCalculationHelper
    {
        /// <summary>
        /// Validates if valid amount combination is provided (NetAmount + VatRate OR GrossAmount)
        /// </summary>
        public static bool HasValidAmountCombination(decimal? netAmount, decimal? vatRate, decimal? grossAmount)
        {
            bool hasNetAndVat = netAmount.HasValue && vatRate.HasValue;
            bool hasGross = grossAmount.HasValue;
            
            // Must have exactly one option
            return hasNetAndVat || hasGross;
        }

        /// <summary>
        /// Calculates gross amount from net amount and VAT rate
        /// </summary>
        public static decimal CalculateGrossAmount(decimal netAmount, decimal vatRate)
        {
            decimal vatAmount = netAmount * (vatRate / 100);
            return netAmount + vatAmount;
        }

        /// <summary>
        /// Calculates amounts and returns the result
        /// </summary>
        /// <returns>Tuple with (GrossAmount, NetAmount, VatRate)</returns>
        public static (decimal grossAmount, decimal? netAmount, decimal? vatRate) CalculateAmounts(
            decimal? netAmount, 
            decimal? vatRate, 
            decimal? grossAmount)
        {
            if (netAmount.HasValue && vatRate.HasValue)
            {
                // Calculate gross from net + VAT
                decimal calculatedGross = CalculateGrossAmount(netAmount.Value, vatRate.Value);
                return (calculatedGross, netAmount.Value, vatRate.Value);
            }
            else if (grossAmount.HasValue)
            {
                // Use provided gross amount
                return (grossAmount.Value, null, null);
            }
            
            throw new ArgumentException("Must provide either NetAmount with VatRate or GrossAmount");
        }
    }
}
