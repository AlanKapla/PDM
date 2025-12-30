using Entities.Models.CostEstimateData;
using Entities.Models.CostEstimateTemplateDefinitions;

namespace Business.Implementation.Validators
{
    /// <summary>
    /// Walidator sprawdzający zgodność wypełnionych danych kosztorysu z szablonem
    /// </summary>
    public class CostEstimateSchemaValidator
    {
        private readonly CostEstimateTemplateStructure templateStructure;
        private readonly List<string> errors = new();
        
        public CostEstimateSchemaValidator(CostEstimateTemplateStructure templateStructure)
        {
            this.templateStructure = templateStructure;
        }
        
        /// <summary>
        /// Waliduje wypełnione dane względem szablonu
        /// </summary>
        public ValidationResult Validate(CostEstimateDataModel data)
        {
            errors.Clear();
            
            if (data == null)
            {
                errors.Add("Cost estimate data cannot be null");
                return new ValidationResult(false, errors);
            }
            
            if (data.Groups == null || data.Groups.Count == 0)
            {
                errors.Add("Cost estimate must have at least one group");
                return new ValidationResult(false, errors);
            }
            
            // Validate groups
            ValidateGroups(data.Groups, null, 0);
            
            return new ValidationResult(errors.Count == 0, errors);
        }
        
        private void ValidateGroups(List<CostEstimateGroup> groups, string? parentId, int currentLevel)
        {
            // Check max group level
            if (templateStructure.MaxGroupLevel.HasValue && currentLevel > templateStructure.MaxGroupLevel.Value)
            {
                errors.Add($"Group level {currentLevel} exceeds maximum allowed level {templateStructure.MaxGroupLevel.Value}");
                return;
            }
            
            foreach (var group in groups)
            {
                ValidateGroup(group, parentId, currentLevel);
                
                // Validate subgroups recursively
                if (group.SubGroups != null && group.SubGroups.Count > 0)
                {
                    if (!templateStructure.CanBranchGroups)
                    {
                        errors.Add($"Group '{group.Id}': Template does not allow branching groups");
                    }
                    else
                    {
                        ValidateGroups(group.SubGroups, group.Id, currentLevel + 1);
                    }
                }
            }
        }
        
        private void ValidateGroup(CostEstimateGroup group, string? expectedParentId, int expectedLevel)
        {
            // Validate basic structure
            if (string.IsNullOrWhiteSpace(group.Id))
            {
                errors.Add("Group ID cannot be null or empty");
                return;
            }
            
            if (group.ParentId != expectedParentId)
            {
                errors.Add($"Group '{group.Id}': ParentId mismatch. Expected '{expectedParentId}', got '{group.ParentId}'");
            }
            
            if (group.Level != expectedLevel)
            {
                errors.Add($"Group '{group.Id}': Level mismatch. Expected {expectedLevel}, got {group.Level}");
            }
            
            // Validate header fields
            ValidateGroupHeaderFields(group);
            
            // Validate work scopes
            if (group.WorkScopes != null)
            {
                foreach (var workScope in group.WorkScopes)
                {
                    ValidateWorkScope(workScope, group.Id);
                }
            }
        }
        
        private void ValidateGroupHeaderFields(CostEstimateGroup group)
        {
            var headerFieldDef = templateStructure.GroupDefinition.HeaderFields;
            
            foreach (var fieldDef in headerFieldDef)
            {
                var fieldKey = fieldDef.Type.ToString();
                
                // Check required fields
                if (fieldDef.Required && !group.HeaderValues.ContainsKey(fieldKey))
                {
                    errors.Add($"Group '{group.Id}': Required header field '{fieldDef.Type}' is missing");
                    continue;
                }
                
                // Validate field value if present
                if (group.HeaderValues.TryGetValue(fieldKey, out var value))
                {
                    if (fieldDef.Required && value == null)
                    {
                        errors.Add($"Group '{group.Id}': Required header field '{fieldDef.Type}' cannot be null");
                    }
                    
                    // Validate allowed values
                    if (fieldDef.AllowedValues != null && fieldDef.AllowedValues.Count > 0 && value != null)
                    {
                        var valueStr = value.ToString();
                        if (valueStr != null && !fieldDef.AllowedValues.Contains(valueStr))
                        {
                            errors.Add($"Group '{group.Id}': Header field '{fieldDef.Type}' value '{valueStr}' is not in allowed values");
                        }
                    }
                }
            }
        }
        
        private void ValidateWorkScope(CostEstimateWorkScope workScope, string groupId)
        {
            if (string.IsNullOrWhiteSpace(workScope.Id))
            {
                errors.Add($"Group '{groupId}': Work scope ID cannot be null or empty");
                return;
            }
            
            // Validate calculated fields
            var calculatedFields = templateStructure.WorkScopeFieldsDefinition.CalculatedFields;
            foreach (var fieldDef in calculatedFields)
            {
                ValidateFieldValue(workScope.CalculatedFieldValues, fieldDef.Name, fieldDef.Required, 
                    $"Group '{groupId}', WorkScope '{workScope.Id}'", "calculated");
            }
            
            // Validate generic fields
            var genericFields = templateStructure.WorkScopeFieldsDefinition.GenericFields;
            foreach (var fieldDef in genericFields)
            {
                ValidateFieldValue(workScope.GenericFieldValues, fieldDef.Name, fieldDef.Required, 
                    $"Group '{groupId}', WorkScope '{workScope.Id}'", "generic");
                
                // Validate collection fields
                if (fieldDef.Type == GenericFieldType.Collection && fieldDef.NestedFields != null)
                {
                    ValidateCollectionField(workScope, fieldDef, groupId);
                }
            }
        }
        
        private void ValidateFieldValue(Dictionary<string, object?> values, string fieldName, 
            bool required, string context, string fieldType)
        {
            if (required && !values.ContainsKey(fieldName))
            {
                errors.Add($"{context}: Required {fieldType} field '{fieldName}' is missing");
                return;
            }
            
            if (required && values.TryGetValue(fieldName, out var value) && value == null)
            {
                errors.Add($"{context}: Required {fieldType} field '{fieldName}' cannot be null");
            }
        }
        
        private void ValidateCollectionField(CostEstimateWorkScope workScope, 
            GenericFieldDefinition fieldDef, string groupId)
        {
            if (workScope.CollectionFieldValues == null || 
                !workScope.CollectionFieldValues.TryGetValue(fieldDef.Name, out var collectionItems))
            {
                if (fieldDef.Required)
                {
                    errors.Add($"Group '{groupId}', WorkScope '{workScope.Id}': Required collection field '{fieldDef.Name}' is missing");
                }
                return;
            }
            
            var nestedDef = fieldDef.NestedFields!;
            
            // Validate collection size
            if (nestedDef.MinItems.HasValue && collectionItems.Count < nestedDef.MinItems.Value)
            {
                errors.Add($"Group '{groupId}', WorkScope '{workScope.Id}': Collection '{fieldDef.Name}' has {collectionItems.Count} items, minimum is {nestedDef.MinItems.Value}");
            }
            
            if (nestedDef.MaxItems.HasValue && collectionItems.Count > nestedDef.MaxItems.Value)
            {
                errors.Add($"Group '{groupId}', WorkScope '{workScope.Id}': Collection '{fieldDef.Name}' has {collectionItems.Count} items, maximum is {nestedDef.MaxItems.Value}");
            }
            
            // Validate selectable collection - only one item can be selected
            if (nestedDef.IsSelectableCollection)
            {
                var selectedItems = collectionItems.Where(item => item.IsSelected).ToList();
                
                if (selectedItems.Count > 1)
                {
                    var selectedIds = string.Join(", ", selectedItems.Select(i => i.Id));
                    errors.Add($"Group '{groupId}', WorkScope '{workScope.Id}': Selectable collection '{fieldDef.Name}' can have only one selected item. Found {selectedItems.Count} selected items: {selectedIds}");
                }
            }
            
            // Validate each collection item
            foreach (var item in collectionItems)
            {
                if (nestedDef.CalculatedFields != null)
                {
                    foreach (var nestedFieldDef in nestedDef.CalculatedFields)
                    {
                        if (item.CalculatedFieldValues != null)
                        {
                            ValidateFieldValue(item.CalculatedFieldValues, nestedFieldDef.Name, 
                                nestedFieldDef.Required, 
                                $"Group '{groupId}', WorkScope '{workScope.Id}', Collection '{fieldDef.Name}', Item '{item.Id}'", 
                                "calculated");
                        }
                        else if (nestedFieldDef.Required)
                        {
                            errors.Add($"Group '{groupId}', WorkScope '{workScope.Id}', Collection '{fieldDef.Name}', Item '{item.Id}': Required calculated field '{nestedFieldDef.Name}' is missing");
                        }
                    }
                }
                
                if (nestedDef.GenericFields != null)
                {
                    foreach (var nestedFieldDef in nestedDef.GenericFields)
                    {
                        if (item.GenericFieldValues != null)
                        {
                            ValidateFieldValue(item.GenericFieldValues, nestedFieldDef.Name, 
                                nestedFieldDef.Required, 
                                $"Group '{groupId}', WorkScope '{workScope.Id}', Collection '{fieldDef.Name}', Item '{item.Id}'", 
                                "generic");
                        }
                        else if (nestedFieldDef.Required)
                        {
                            errors.Add($"Group '{groupId}', WorkScope '{workScope.Id}', Collection '{fieldDef.Name}', Item '{item.Id}': Required generic field '{nestedFieldDef.Name}' is missing");
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Wynik walidacji
    /// </summary>
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
