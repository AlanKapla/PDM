using Business.Implementation.Helpers;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;

namespace Business.Implementation.Services
{
    public class CostEstimateCalculationService : ICostEstimateCalculationService
    {
        public void RecalculateCostEstimate(CostEstimate costEstimate)
        {
            ArgumentNullException.ThrowIfNull(costEstimate);

            decimal totalNet = 0m;
            decimal totalGross = 0m;
            decimal totalVat = 0m;

            List<CostEstimateGroup> allGroups = costEstimate.AllGroups.Where(g => !g.IsDeleted).ToList();
            List<CostEstimateGroup> rootGroups = allGroups.Where(g => g.ParentGroupId == null).ToList();

            foreach (CostEstimateGroup rootGroup in rootGroups)
            {
                (decimal groupNet, decimal groupGross, decimal groupVat) = RecalculateGroup(rootGroup, allGroups);

                totalNet += groupNet;
                totalGross += groupGross;
                totalVat += groupVat;
            }

            costEstimate.TotalNet = totalNet;
            costEstimate.TotalGross = totalGross;
            costEstimate.TotalVat = totalVat;
            costEstimate.LastCalculatedAt = DateTime.UtcNow;
            costEstimate.UpdatedAt = DateTime.UtcNow;
        }

        private static (decimal Net, decimal Gross, decimal Vat) RecalculateGroup(
            CostEstimateGroup group,
            List<CostEstimateGroup> allGroups)
        {
            ArgumentNullException.ThrowIfNull(group);

            decimal groupNet = 0m;
            decimal groupGross = 0m;
            decimal groupVat = 0m;

            // Sumuj tylko pozycje główne (RelationType.None) z IsSelected == true
            List<CostEstimateItem> mainItems = group.Items
                .Where(i => !i.IsDeleted && i.RelationType == ItemRelationType.None && i.IsSelected)
                .ToList();

            foreach (CostEstimateItem item in mainItems)
            {
                CalculateItemValues(item);

                if (item.NetValue.HasValue)
                {
                    groupNet += item.NetValue.Value;
                }

                if (item.GrossValue.HasValue)
                {
                    groupGross += item.GrossValue.Value;
                }

                if (item.VatValue.HasValue)
                {
                    groupVat += item.VatValue.Value;
                }
            }

            // Rekurencyjnie podgrupy
            List<CostEstimateGroup> childGroups = allGroups
                .Where(g => g.ParentGroupId == group.Id && !g.IsDeleted)
                .ToList();

            foreach (CostEstimateGroup childGroup in childGroups)
            {
                (decimal childNet, decimal childGross, decimal childVat) = RecalculateGroup(childGroup, allGroups);
                groupNet += childNet;
                groupGross += childGross;
                groupVat += childVat;
            }

            group.TotalNet = groupNet;
            group.TotalGross = groupGross;
            group.TotalVat = groupVat;
            group.LastCalculatedAt = DateTime.UtcNow;
            group.UpdatedAt = DateTime.UtcNow;

            return (groupNet, groupGross, groupVat);
        }

        /// <summary>
        /// Oblicza wartości dla pozycji i zapisuje w NetValue, GrossValue, VatValue.
        /// Kolejność: Opcje (tylko zaznaczona) → Komponenty → Własne wartości.
        /// Jeśli pozycja ma opcje ale żadna nie jest zaznaczona — spada do komponentów/własnych wartości.
        /// </summary>
        private static void CalculateItemValues(CostEstimateItem item)
        {
            if (item.IsDeleted)
            {
                return;
            }

            // === OPCJE (tylko jeśli któraś jest zaznaczona) ===
            List<CostEstimateItem> options = item.Options.Where(o => !o.IsDeleted).ToList();
            CostEstimateItem? selectedOption = options.FirstOrDefault(o => o.IsSelected);

            if (selectedOption is not null)
            {
                // Najpierw oblicz wartości samej opcji
                CalculateItemValues(selectedOption);

                // Kopiuj wartości z zaznaczonej opcji do pozycji nadrzędnej
                item.NetValue = selectedOption.NetValue;
                item.GrossValue = selectedOption.GrossValue;
                item.VatValue = selectedOption.VatValue;
                return;
            }

            // === KOMPONENTY ===
            List<CostEstimateItem> components = item.Components.Where(c => !c.IsDeleted).ToList();
            if (components.Count > 0)
            {
                CalculateItemValuesFromComponents(item, components);
                return;
            }

            // === WŁASNE WARTOŚCI ===
            CalculateItemValuesFromDirectProperties(item);
        }

        private static void CalculateItemValuesFromComponents(
            CostEstimateItem item,
            List<CostEstimateItem> components)
        {
            decimal? totalNet = null;
            decimal? totalGross = null;
            decimal? totalVat = null;

            foreach (CostEstimateItem component in components.Where(c => c.IsSelected))
            {
                // Rekurencyjnie — komponent może mieć własne wartości
                CalculateItemValues(component);

                if (component.NetValue.HasValue)
                {
                    totalNet = (totalNet ?? 0m) + component.NetValue.Value;
                }

                if (component.GrossValue.HasValue)
                {
                    totalGross = (totalGross ?? 0m) + component.GrossValue.Value;
                }

                if (component.VatValue.HasValue)
                {
                    totalVat = (totalVat ?? 0m) + component.VatValue.Value;
                }
            }

            item.NetValue = totalNet;
            item.GrossValue = totalGross;
            item.VatValue = totalVat;
        }

        private static void CalculateItemValuesFromDirectProperties(CostEstimateItem item)
        {
            decimal? valueNet = CostEstimateItemFinancialCalculator.CalculateValueNet(
                item.UnitPriceNet,
                item.Quantity,
                item.NetValue);
            decimal? totalVat = CostEstimateItemFinancialCalculator.CalculateTotalVat(
                valueNet,
                item.VatRate,
                item.VatValue);
            decimal? valueGross = CostEstimateItemFinancialCalculator.CalculateValueGross(
                valueNet,
                totalVat,
                item.VatRate,
                item.GrossValue);
            decimal? unitPriceGross = CostEstimateItemFinancialCalculator.CalculateUnitPriceGross(
                item.UnitPriceNet,
                item.VatRate,
                valueGross,
                item.Quantity,
                item.UnitPriceGross);

            item.NetValue = valueNet;
            item.GrossValue = valueGross;
            item.VatValue = totalVat;
            item.UnitPriceGross = unitPriceGross;
        }
    }
}
