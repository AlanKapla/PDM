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
            ArgumentNullException.ThrowIfNull(costEstimate);

            // Sprawdź które pola kalkulowane powinny być sumowane w totalach
            bool shouldSumValueNetInTotal = ShouldSumFieldInTotal(costEstimate.Template, FieldType.ItemCalculatedValueNet);
            bool shouldSumValueGrossInTotal = ShouldSumFieldInTotal(costEstimate.Template, FieldType.ItemCalculatedValueGross);
            bool shouldSumTotalVatInTotal = ShouldSumFieldInTotal(costEstimate.Template, FieldType.ItemCalculatedTotalVat);

            // Sprawdź które pola powinny być sumowane w grupach
            bool shouldSumValueNetInGroup = ShouldSumFieldInGroup(costEstimate.Template, FieldType.ItemCalculatedValueNet);
            bool shouldSumValueGrossInGroup = ShouldSumFieldInGroup(costEstimate.Template, FieldType.ItemCalculatedValueGross);
            bool shouldSumTotalVatInGroup = ShouldSumFieldInGroup(costEstimate.Template, FieldType.ItemCalculatedTotalVat);

            // Sprawdź czy szablon definiuje pole ItemSystemSelected
            bool hasSelectedField = HasSelectedFieldDefined(costEstimate.Template);

            decimal? totalNet = null;
            decimal? totalGross = null;
            decimal? totalVat = null;

            var allGroups = costEstimate.AllGroups.Where(g => !g.IsDeleted).ToList();
            var rootGroups = allGroups.Where(g => g.ParentGroupId == null).ToList();

            foreach (var rootGroup in rootGroups)
            {
                var (groupNet, groupGross, groupVat) = RecalculateGroup(
                    rootGroup, 
                    allGroups, 
                    shouldSumValueNetInGroup, 
                    shouldSumValueGrossInGroup, 
                    shouldSumTotalVatInGroup,
                    hasSelectedField);

                // ✅ Sumuj w total jeśli szablon ma SumInTotal = true
                if (shouldSumValueNetInTotal)
                {
                    totalNet = (totalNet ?? 0m) + groupNet;
                }

                if (shouldSumValueGrossInTotal)
                {
                    totalGross = (totalGross ?? 0m) + groupGross;
                }

                if (shouldSumTotalVatInTotal)
                {
                    totalVat = (totalVat ?? 0m) + groupVat;
                }
            }

            // Zapisz totale - tylko dla pól z SumInTotal = true
            costEstimate.TotalNet = shouldSumValueNetInTotal ? totalNet : null;
            costEstimate.TotalGross = shouldSumValueGrossInTotal ? totalGross : null;
            costEstimate.TotalVat = shouldSumTotalVatInTotal ? totalVat : null;
            costEstimate.LastCalculatedAt = DateTime.UtcNow;
            costEstimate.UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Sprawdza czy pole powinno być sumowane w totalach kosztorysu
        /// Pole musi istnieć w szablonie jako calculated field (nie jako child field) i mieć SumInTotal = true
        /// </summary>
        private static bool ShouldSumFieldInTotal(CostEstimateTemplate template, FieldType fieldType)
        {
            var field = template.CalculatedFieldDefinitions?
                .FirstOrDefault(f => f.FieldType == fieldType && !f.ParentFieldId.HasValue && f.SumInTotal);

            return field != null;
        }
        
        /// <summary>
        /// Sprawdza czy pole powinno być sumowane w grupach
        /// Pole musi istnieć w szablonie jako calculated field (nie jako child field) i mieć SumInGroup = true
        /// </summary>
        private static bool ShouldSumFieldInGroup(CostEstimateTemplate template, FieldType fieldType)
        {
            var field = template.CalculatedFieldDefinitions?
                .FirstOrDefault(f => f.FieldType == fieldType && !f.ParentFieldId.HasValue && f.SumInGroup);

            return field != null;
        }

        /// <summary>
        /// Sprawdza czy szablon definiuje pole ItemSystemSelected
        /// </summary>
        private static bool HasSelectedFieldDefined(CostEstimateTemplate template)
        {
            return template.SystemFieldDefinitions?
                .Any(f => f.FieldType == FieldType.ItemSystemSelected) == true;
        }

        /// <summary>
        /// Sprawdza czy pozycja jest zaznaczona do sumowania.
        /// Jeśli szablon nie definiuje pola ItemSystemSelected, pozycja zawsze bierze udział w sumowaniu.
        /// Jeśli definiuje - pozycja bierze udział tylko gdy BoolValue == true.
        /// </summary>
        private static bool IsItemSelectedForSumming(CostEstimateItem item, bool hasSelectedField)
        {
            if (!hasSelectedField)
            {
                return true;
            }

            var selectedFieldValue = item.FieldValues
                .FirstOrDefault(fv => fv.FieldDefinition != null
                    && fv.FieldDefinition.FieldScope == FieldScope.ItemSystem
                    && fv.FieldDefinition.FieldType == FieldType.ItemSystemSelected);

            return selectedFieldValue?.BoolValue == true;
        }

        private static (decimal Net, decimal Gross, decimal Vat) RecalculateGroup(
            CostEstimateGroup group,
            List<CostEstimateGroup> allGroups,
            bool shouldSumValueNetInGroup,
            bool shouldSumValueGrossInGroup,
            bool shouldSumTotalVatInGroup,
            bool hasSelectedField)
        {
            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            decimal? groupNet = null;
            decimal? groupGross = null;
            decimal? groupVat = null;

            // Calculate from items in this group (tylko główne pozycje - RelationType.None)
            var mainItems = group.Items.Where(w => !w.IsDeleted && w.RelationType == ItemRelationType.None).ToList();
            
            foreach (var item in mainItems)
            {
                CalculateItemValues(item);

                // Pozycja bierze udział w sumowaniu tylko gdy jest zaznaczona (lub szablon nie definiuje pola Selected)
                if (!IsItemSelectedForSumming(item, hasSelectedField))
                {
                    continue;
                }

                // Sumuj tylko jeśli szablon ma zdefiniowane pole z SumInGroup = true i wartość istnieje
                if (shouldSumValueNetInGroup && item.NetValue.HasValue)
                {
                    groupNet = (groupNet ?? 0m) + item.NetValue.Value;
                }

                if (shouldSumValueGrossInGroup && item.GrossValue.HasValue)
                {
                    groupGross = (groupGross ?? 0m) + item.GrossValue.Value;
                }

                if (shouldSumTotalVatInGroup && item.VatValue.HasValue)
                {
                    groupVat = (groupVat ?? 0m) + item.VatValue.Value;
                }
            }

            // Recursively calculate from child groups
            var childGroups = allGroups.Where(g => g.ParentGroupId == group.Id && !g.IsDeleted).ToList();
            foreach (var childGroup in childGroups)
            {
                var (childNet, childGross, childVat) = RecalculateGroup(
                    childGroup, 
                    allGroups,
                    shouldSumValueNetInGroup,
                    shouldSumValueGrossInGroup,
                    shouldSumTotalVatInGroup,
                    hasSelectedField);
                
                // Sumuj tylko jeśli szablon ma zdefiniowane pole z SumInGroup = true
                if (shouldSumValueNetInGroup)
                {
                    groupNet = (groupNet ?? 0m) + childNet;
                }
                
                if (shouldSumValueGrossInGroup)
                {
                    groupGross = (groupGross ?? 0m) + childGross;
                }
                
                if (shouldSumTotalVatInGroup)
                {
                    groupVat = (groupVat ?? 0m) + childVat;
                }
            }

            // Update group totals - zapisz tylko gdy szablon ma zdefiniowane pole z SumInGroup = true
            group.TotalNet = shouldSumValueNetInGroup ? groupNet : null;
            group.TotalGross = shouldSumValueGrossInGroup ? groupGross : null;
            group.TotalVat = shouldSumTotalVatInGroup ? groupVat : null;
            group.LastCalculatedAt = DateTime.UtcNow;
            group.UpdatedAt = DateTime.UtcNow;

            // ✅ ZAWSZE zwróć obliczone wartości (nie zależnie od shouldSumInGroup!)
            // Nawet gdy grupa nie sumuje (SumInGroup=false), total kosztorysu może wymagać sumy (SumInTotal=true)
            return (
                groupNet ?? 0m,
                groupGross ?? 0m,
                groupVat ?? 0m
            );
        }

        /// <summary>
        /// Oblicza wartości dla pozycji i zapisuje w NetValue, GrossValue, VatValue
        /// Jeśli pozycja ma Components - sumuje z komponentów, jeśli nie - oblicza z FieldValues
        /// </summary>
        private static void CalculateItemValues(CostEstimateItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // Sprawdź czy pozycja ma komponenty
            var components = item.Components?.Where(c => !c.IsDeleted).ToList() ?? new List<CostEstimateItem>();
            
            if (components.Any())
            {
                // Pozycja ma komponenty - sumuj wartości z komponentów
                decimal? totalNet = null;
                decimal? totalGross = null;
                decimal? totalVat = null;
                
                foreach (var component in components)
                {
                    // Rekurencyjnie oblicz wartości komponentu
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
            else
            {
                // Pozycja NIE ma komponentów - oblicz z FieldValues (jak dotychczas)
                Dictionary<FieldType, CostEstimateItemFieldValue> calculatedFieldValues = item.FieldValues
                    .Where(fv => fv.FieldDefinition != null && fv.FieldDefinition.FieldScope == FieldScope.ItemCalculated)
                    .ToDictionary(
                        fv => fv.FieldDefinition.FieldType,
                        fv => fv);

                decimal? unitPriceNet = calculatedFieldValues.GetValueOrDefault(FieldType.ItemCalculatedUnitPriceNet)?.DecimalValue;
                decimal? vatRate = calculatedFieldValues.GetValueOrDefault(FieldType.ItemCalculatedVatRate)?.DecimalValue;
                decimal? unitPriceGross = calculatedFieldValues.GetValueOrDefault(FieldType.ItemCalculatedUnitPriceGross)?.DecimalValue;
                decimal? valueNetField = calculatedFieldValues.GetValueOrDefault(FieldType.ItemCalculatedValueNet)?.DecimalValue;
                decimal? valueGrossField = calculatedFieldValues.GetValueOrDefault(FieldType.ItemCalculatedValueGross)?.DecimalValue;
                decimal? totalVatField = calculatedFieldValues.GetValueOrDefault(FieldType.ItemCalculatedTotalVat)?.DecimalValue;
                decimal? unitVatField = calculatedFieldValues.GetValueOrDefault(FieldType.ItemCalculatedUnitVat)?.DecimalValue;

                decimal? quantity = GetQuantityFromSystemField(item);

                decimal? valueNet = CalculateValueNet(unitPriceNet, quantity, valueNetField, calculatedFieldValues);
                decimal? totalVat = CalculateTotalVat(valueNet, vatRate, totalVatField, calculatedFieldValues);
                decimal? valueGross = CalculateValueGross(valueNet, totalVat, vatRate, valueGrossField, calculatedFieldValues);

                CalculateUnitPriceGross(unitPriceNet, vatRate, totalVat, quantity, valueGross, unitPriceGross, calculatedFieldValues);
                CalculateUnitVat(unitPriceNet, vatRate, unitVatField, calculatedFieldValues);

                // Zapisz obliczone wartości w pozycji
                item.NetValue = valueNet;
                item.GrossValue = valueGross;
                item.VatValue = totalVat;
            }
        }

        private static decimal? GetQuantityFromSystemField(CostEstimateItem item)
        {
            var systemQuantityField = item.FieldValues
                .FirstOrDefault(fv => fv.FieldDefinition != null &&
                                     fv.FieldDefinition.FieldScope == FieldScope.ItemSystem &&
                                     fv.FieldDefinition.FieldType == FieldType.ItemSystemQuantity);

            return systemQuantityField?.DecimalValue;
        }

        private static decimal? CalculateUnitPriceGross(
            decimal? unitPriceNet,
            decimal? vatRate,
            decimal? totalVat,
            decimal? quantity,
            decimal? valueGross,
            decimal? unitPriceGross,
            Dictionary<FieldType, CostEstimateItemFieldValue> calculatedFieldValues)
        {
            decimal? calculated = null;

            if (unitPriceNet.HasValue && vatRate.HasValue)
            {
                calculated = unitPriceNet.Value * (1m + vatRate.Value);
            }
            else if (unitPriceNet.HasValue && totalVat.HasValue && quantity.HasValue && quantity.Value != 0m)
            {
                calculated = unitPriceNet.Value + (totalVat.Value / quantity.Value);
            }
            else if (valueGross.HasValue && quantity.HasValue && quantity.Value != 0m)
            {
                calculated = valueGross.Value / quantity.Value;
            }

            if (calculated.HasValue)
            {
                if (calculatedFieldValues.TryGetValue(FieldType.ItemCalculatedUnitPriceGross, out CostEstimateItemFieldValue fieldValue))
                {
                    fieldValue.DecimalValue = calculated.Value;
                }

                return calculated;
            }

            return unitPriceGross;
        }

        private static decimal? CalculateUnitVat(
            decimal? unitPriceNet,
            decimal? vatRate,
            decimal? unitVatField,
            Dictionary<FieldType, CostEstimateItemFieldValue> calculatedFieldValues)
        {
            if (unitPriceNet.HasValue && vatRate.HasValue)
            {
                decimal calculated = unitPriceNet.Value * vatRate.Value;

                if (calculatedFieldValues.TryGetValue(FieldType.ItemCalculatedUnitVat, out CostEstimateItemFieldValue fieldValue))
                {
                    fieldValue.DecimalValue = calculated;
                }

                return calculated;
            }

            return unitVatField;
        }

        private static decimal? CalculateValueNet(
            decimal? unitPriceNet,
            decimal? quantity,
            decimal? valueNetField,
            Dictionary<FieldType, CostEstimateItemFieldValue> calculatedFieldValues)
        {
            if (unitPriceNet.HasValue && quantity.HasValue)
            {
                decimal calculated = unitPriceNet.Value * quantity.Value;

                if (calculatedFieldValues.TryGetValue(FieldType.ItemCalculatedValueNet, out CostEstimateItemFieldValue fieldValue))
                {
                    fieldValue.DecimalValue = calculated;
                }

                return calculated;
            }

            return valueNetField;
        }

        private static decimal? CalculateTotalVat(
            decimal? valueNet,
            decimal? vatRate,
            decimal? totalVatField,
            Dictionary<FieldType, CostEstimateItemFieldValue> calculatedFieldValues)
        {
            if (valueNet.HasValue && vatRate.HasValue)
            {
                decimal calculated = valueNet.Value * vatRate.Value;

                if (calculatedFieldValues.TryGetValue(FieldType.ItemCalculatedTotalVat, out CostEstimateItemFieldValue fieldValue))
                {
                    fieldValue.DecimalValue = calculated;
                }

                return calculated;
            }

            return totalVatField;
        }

        private static decimal? CalculateValueGross(
            decimal? valueNet,
            decimal? totalVat,
            decimal? vatRate,
            decimal? valueGrossField,
            Dictionary<FieldType, CostEstimateItemFieldValue> calculatedFieldValues)
        {
            if (valueNet.HasValue && totalVat.HasValue)
            {
                decimal calculated = valueNet.Value + totalVat.Value;

                if (calculatedFieldValues.TryGetValue(FieldType.ItemCalculatedValueGross, out CostEstimateItemFieldValue fieldValue))
                {
                    fieldValue.DecimalValue = calculated;
                }

                return calculated;
            }

            if (valueNet.HasValue && vatRate.HasValue)
            {
                decimal calculated = valueNet.Value * (1m + vatRate.Value);

                if (calculatedFieldValues.TryGetValue(FieldType.ItemCalculatedValueGross, out CostEstimateItemFieldValue fieldValue))
                {
                    fieldValue.DecimalValue = calculated;
                }

                return calculated;
            }

            return valueGrossField;
        }
    }
}
