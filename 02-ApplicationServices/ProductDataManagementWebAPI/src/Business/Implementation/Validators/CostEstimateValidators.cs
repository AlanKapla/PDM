using Business.Implementation.Helpers;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models.CostEstimates;
using Repositories.Repository.Interfaces;
using Entities.Models.CostEstimateTemplates;

namespace Business.Implementation.Validators
{
    public class CostEstimateGroupValidator
    {
        public ValidationResult ValidateGroupHierarchy(
            CostEstimateTemplateVersion version,
            List<CostEstimateGroup> allGroups,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (version == null)
            {
                errors.Add("Template version not found");
                return new ValidationResult(false, errors);
            }

            if (version.MaxGroupLevel.HasValue)
            {
                var maxLevel = allGroups.Max(g => g.Level);
                if (maxLevel > version.MaxGroupLevel.Value)
                {
                    errors.Add($"Group level {maxLevel} exceeds maximum allowed level {version.MaxGroupLevel.Value}");
                }
            }

            if (!version.CanBranchGroups)
            {
                var groupsWithChildren = allGroups.Where(g => allGroups.Any(child => child.ParentGroupId == g.Id)).ToList();
                if (groupsWithChildren.Any())
                {
                    errors.Add("Template does not allow branching groups (subgroups)");
                }
            }

            foreach (var group in allGroups)
            {
                if (group.ParentGroupId.HasValue)
                {
                    var parent = allGroups.FirstOrDefault(g => g.Id == group.ParentGroupId.Value);
                    if (parent == null)
                    {
                        errors.Add($"Group {group.Id}: Parent group {group.ParentGroupId.Value} not found");
                    }
                    else if (group.Level != parent.Level + 1)
                    {
                        errors.Add($"Group {group.Id}: Invalid level {group.Level}, expected {parent.Level + 1}");
                    }
                }
                else if (group.Level != 0)
                {
                    errors.Add($"Group {group.Id}: Root group must have level 0, got {group.Level}");
                }
            }

            return new ValidationResult(errors.Count == 0, errors);
        }

        public ValidationResult ValidateGroupFieldValues(
            Dictionary<Guid, CostEstimateTemplateGroupFieldDefinition> fieldDefinitionsById,
            List<CostEstimateGroupFieldValue> fieldValues,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            foreach (var fieldValue in fieldValues)
            {
                // Pobierz definicję pola ze słownika
                if (!fieldDefinitionsById.TryGetValue(fieldValue.FieldDefinitionId, out var fieldDef))
                {
                    errors.Add($"Field definition {fieldValue.FieldDefinitionId} not found in template version");
                    continue;
                }
                
                // TODO: Dodatkowa walidacja wartości według typu pola
            }

            return new ValidationResult(errors.Count == 0, errors);
        }
    }

    public class CostEstimateItemValidator
    {
        /// <summary>
        /// Waliduje hierarchię pozycji (opcji) - max 1 poziom zagnieżdżenia
        /// </summary>
        public ValidationResult ValidateItemOptionsHierarchy(
            List<CostEstimateItem> allItems,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            foreach (var item in allItems)
            {
                // Sprawdź czy opcja NIE MA kolejnych opcji (max 1 poziom)
                if (item.ParentItemId.HasValue)
                {
                    var parent = allItems.FirstOrDefault(i => i.Id == item.ParentItemId.Value);
                    if (parent != null && parent.ParentItemId.HasValue)
                    {
                        errors.Add($"Item {item.Id}: Option cannot have nested options (max 1 level allowed). Parent item {parent.Id} is already an option.");
                    }
                }

                // Sprawdź czy pozycja z Options ma pole ItemSystemOptions
                if (item.Options.Any())
                {
                    var hasOptionsField = item.FieldValues.Any(fv => 
                        fv.FieldDefinition != null && 
                        fv.FieldDefinition.FieldType == FieldType.ItemSystemOptions);
                    
                    if (!hasOptionsField)
                    {
                        errors.Add($"Item {item.Id}: Has {item.Options.Count} options but no ItemSystemOptions field in FieldValues");
                    }
                }
            }

            return new ValidationResult(errors.Count == 0, errors);
        }

        public ValidationResult ValidateItemFieldValues(
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> fieldDefinitionsById,
            List<CostEstimateItemFieldValue> fieldValues,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            foreach (var fieldValue in fieldValues)
            {
                // Pobierz definicję pola ze słownika
                if (!fieldDefinitionsById.TryGetValue(fieldValue.FieldDefinitionId, out var fieldDef))
                {
                    errors.Add($"Field value {fieldValue.Id}: Field definition {fieldValue.FieldDefinitionId} not found");
                    continue;
                }
                
                // Pomiń walidację dla pól kolekcji - walidujemy tylko ich child fields
                if (CostEstimateFieldTypeHelper.IsCollectionFieldType(fieldDef.FieldType))
                {
                    continue;
                }
                
                // Pobierz konfigurację typu pola
                var fieldTypeConfig = CostEstimateFieldTypeHelper.GetFieldTypeConfig(fieldDef.FieldType);
                if (fieldTypeConfig == null)
                {
                    errors.Add($"Field '{fieldDef.Label}': Unknown field type {fieldDef.FieldType}");
                    continue;
                }
                
                // Waliduj wartość według typu
                ValidateFieldValueByType(fieldValue, fieldDef, fieldTypeConfig, errors);
            }

            return new ValidationResult(errors.Count == 0, errors);
        }
        
        /// <summary>
        /// Waliduje wartość pola według konfiguracji typu
        /// </summary>
        private void ValidateFieldValueByType(
            CostEstimateItemFieldValue fieldValue,
            CostEstimateTemplateFieldDefinitionBase fieldDef,
            Business.Interfaces.WebModels.CostEstimateTemplates.CostEstimateFieldTypeConfigWeb fieldTypeConfig,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(fieldValue.Value))
            {
                return;
            }
            
            // Walidacja według typu wartości
            if (fieldTypeConfig.IsNumeric)
            {
                ValidateNumericValue(fieldValue, fieldDef, fieldTypeConfig, errors);
            }
            else if (fieldTypeConfig.IsBoolean)
            {
                ValidateBooleanValue(fieldValue, fieldDef, errors);
            }
            else if (fieldTypeConfig.IsDate)
            {
                ValidateDateValue(fieldValue, fieldDef, errors);
            }
            else if (fieldTypeConfig.IsText)
            {
                // String - zawsze valid, możliwe dodatkowe reguły w przyszłości
            }
        }
        
        private void ValidateNumericValue(
            CostEstimateItemFieldValue fieldValue,
            CostEstimateTemplateFieldDefinitionBase fieldDef,
            Business.Interfaces.WebModels.CostEstimateTemplates.CostEstimateFieldTypeConfigWeb fieldTypeConfig,
            List<string> errors)
        {
            bool isValid = fieldTypeConfig.ValueTypeName switch
            {
                "int" => int.TryParse(fieldValue.Value, out _),
                "decimal" => decimal.TryParse(fieldValue.Value, out var decimalValue) && ValidateDecimalRange(fieldDef.FieldType, decimalValue, errors, fieldDef.Label),
                _ => false
            };
            
            if (!isValid && !errors.Any(e => e.Contains(fieldDef.Label)))
            {
                errors.Add($"Field '{fieldDef.Label}': Invalid {fieldTypeConfig.ValueTypeName} value");
            }
        }
        
        private bool ValidateDecimalRange(FieldType fieldType, decimal value, List<string> errors, string label)
        {
            switch (fieldType)
            {
                case FieldType.ItemCalculatedVatRate:
                    if (value < 0 || value > 100)
                    {
                        errors.Add($"Field '{label}': VAT rate must be between 0 and 100");
                        return false;
                    }
                    break;
                    
                case FieldType.ItemCalculatedUnitPriceNet:
                case FieldType.ItemCalculatedUnitPriceGross:
                case FieldType.ItemCalculatedValueNet:
                case FieldType.ItemCalculatedValueGross:
                case FieldType.ItemCalculatedUnitVat:
                case FieldType.ItemCalculatedTotalVat:
                case FieldType.ItemSystemQuantity:
                    if (value < 0)
                    {
                        errors.Add($"Field '{label}': Value cannot be negative");
                        return false;
                    }
                    break;
            }
            
            return true;
        }
        
        private void ValidateBooleanValue(
            CostEstimateItemFieldValue fieldValue,
            CostEstimateTemplateFieldDefinitionBase fieldDef,
            List<string> errors)
        {
            if (!bool.TryParse(fieldValue.Value, out _))
            {
                errors.Add($"Field '{fieldDef.Label}': Invalid boolean value");
            }
        }
        
        private void ValidateDateValue(
            CostEstimateItemFieldValue fieldValue,
            CostEstimateTemplateFieldDefinitionBase fieldDef,
            List<string> errors)
        {
            if (!DateTime.TryParse(fieldValue.Value, out _))
            {
                errors.Add($"Field '{fieldDef.Label}': Invalid date/time value");
            }
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }

        public ValidationResult(bool isValid, List<string> errors)
        {
            IsValid = isValid;
            Errors = errors.AsReadOnly();
        }
    }
}
