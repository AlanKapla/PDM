using Microsoft.EntityFrameworkCore;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Business.Interfaces.Services;

namespace Business.Implementation.Services
{
    
    public class CostEstimateCalculationService : ICostEstimateCalculationService
    {
        public void RecalculateCostEstimate(CostEstimate costEstimate)
        {
            if (costEstimate == null)
            {
                throw new ArgumentNullException(nameof(costEstimate));
            }

            decimal totalNet = 0m;
            decimal totalGross = 0m;
            decimal totalVat = 0m;

            var allGroups = costEstimate.AllGroups.Where(g => !g.IsDeleted).ToList();
            var rootGroups = allGroups.Where(g => g.ParentGroupId == null).ToList();
            
            foreach (var rootGroup in rootGroups)
            {
                var (groupNet, groupGross, groupVat) = RecalculateGroup(rootGroup, allGroups);
                
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

        public (decimal Net, decimal Gross, decimal Vat) RecalculateGroup(
            CostEstimateGroup group,
            List<CostEstimateGroup> allGroups)
        {
            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            decimal groupNet = 0m;
            decimal groupGross = 0m;
            decimal groupVat = 0m;

            // Calculate from items in this group
            foreach (var item in group.Items.Where(w => !w.IsDeleted))
            {
                var (itemNet, itemGross, itemVat) = CalculateItemValues(item);
                groupNet += itemNet ?? 0m;
                groupGross += itemGross ?? 0m;
                groupVat += itemVat ?? 0m;
            }

            // Recursively calculate from child groups
            var childGroups = allGroups.Where(g => g.ParentGroupId == group.Id && !g.IsDeleted).ToList();
            foreach (var childGroup in childGroups)
            {
                var (childNet, childGross, childVat) = RecalculateGroup(childGroup, allGroups);
                
                groupNet += childNet;
                groupGross += childGross;
                groupVat += childVat;
            }

            // Update group totals
            group.TotalNet = groupNet;
            group.TotalGross = groupGross;
            group.TotalVat = groupVat;
            group.LastCalculatedAt = DateTime.UtcNow;
            group.UpdatedAt = DateTime.UtcNow;

            return (groupNet, groupGross, groupVat);
        }

        public (decimal? Net, decimal? Gross, decimal? Vat) CalculateItemValues(CostEstimateItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // Zbierz pola obliczeniowe - rozpoznajemy po FieldDefinition.FieldScope == ItemCalculated
            var calculatedFields = item.FieldValues
                .Where(fv => fv.FieldDefinition != null && fv.FieldDefinition.FieldScope == FieldScope.ItemCalculated)
                .ToDictionary(
                    fv => fv.FieldDefinition.FieldType,
                    fv => ParseDecimalValue(fv.Value));

            var unitPriceNet = calculatedFields.GetValueOrDefault(FieldType.ItemCalculatedUnitPriceNet);
            var vatRate = calculatedFields.GetValueOrDefault(FieldType.ItemCalculatedVatRate);

            // Pobierz quantity z pola systemowego - rozpoznajemy po FieldScope == ItemSystem
            decimal? quantity = null;
            var systemQuantityField = item.FieldValues
                .FirstOrDefault(fv => fv.FieldDefinition != null &&
                                     fv.FieldDefinition.FieldScope == FieldScope.ItemSystem && 
                                     fv.FieldDefinition.FieldType == FieldType.ItemSystemQuantity);
            
            if (systemQuantityField != null)
            {
                quantity = ParseDecimalValue(systemQuantityField.Value);
            }

            decimal? valueNet = null;
            decimal? valueGross = null;
            decimal? totalVat = null;

            if (unitPriceNet.HasValue && quantity.HasValue)
            {
                valueNet = unitPriceNet.Value * quantity.Value;

                if (vatRate.HasValue)
                {
                    totalVat = valueNet.Value * (vatRate.Value / 100m);
                    valueGross = valueNet.Value + totalVat.Value;
                }
            }

            return (valueNet, valueGross, totalVat);
        }

        private decimal? ParseDecimalValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (decimal.TryParse(value, out var result))
            {
                return result;
            }

            return null;
        }
    }
}
