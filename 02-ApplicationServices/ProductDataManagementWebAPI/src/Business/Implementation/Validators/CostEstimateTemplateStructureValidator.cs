using Entities.Models.CostEstimateTemplateDefinitions;

namespace Business.Implementation.Validators
{
    /// <summary>
    /// Walidator sprawdzający poprawność struktury szablonu kosztorysu
    /// Waliduje reguły biznesowe dotyczące definicji pól (np. które pola mogą być summable)
    /// </summary>
    public class CostEstimateTemplateStructureValidator
    {
        private readonly CostEstimateTemplateStructure templateStructure;
        private readonly List<string> errors = new();
        
        public CostEstimateTemplateStructureValidator(CostEstimateTemplateStructure templateStructure)
        {
            this.templateStructure = templateStructure;
        }
        
        /// <summary>
        /// Waliduje strukturę szablonu
        /// </summary>
        public TemplateStructureValidationResult Validate()
        {
            errors.Clear();
            
            if (templateStructure == null)
            {
                errors.Add("Template structure cannot be null");
                return new TemplateStructureValidationResult(false, errors);
            }
            
            // Validate summable fields
            ValidateSummableFields();
            
            // Validate collection summable fields
            ValidateCollectionSummableFields();
            
            return new TemplateStructureValidationResult(errors.Count == 0, errors);
        }
        
        /// <summary>
        /// Waliduje czy tylko ValueNet, ValueGross, UnitVat i TotalVat są summable
        /// </summary>
        private void ValidateSummableFields()
        {
            if (templateStructure.WorkScopeFieldsDefinition?.CalculatedFields == null)
            {
                return;
            }
            
            foreach (var fieldDef in templateStructure.WorkScopeFieldsDefinition.CalculatedFields)
            {
                if (fieldDef.Summable)
                {
                    ValidateSummableFieldType(fieldDef.Type, fieldDef.Name, null);
                }
            }
        }
        
        /// <summary>
        /// Waliduje czy pola w kolekcjach mogą być summable
        /// </summary>
        private void ValidateCollectionSummableFields()
        {
            if (templateStructure.WorkScopeFieldsDefinition?.GenericFields == null)
            {
                return;
            }
            
            foreach (var fieldDef in templateStructure.WorkScopeFieldsDefinition.GenericFields)
            {
                if (fieldDef.Type == GenericFieldType.Collection && fieldDef.NestedFields?.CalculatedFields != null)
                {
                    foreach (var nestedFieldDef in fieldDef.NestedFields.CalculatedFields)
                    {
                        if (nestedFieldDef.Summable)
                        {
                            ValidateSummableFieldType(nestedFieldDef.Type, nestedFieldDef.Name, fieldDef.Name);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Waliduje czy dany typ pola może być summable
        /// Tylko ValueNet, ValueGross, UnitVat i TotalVat mogą być sumowane
        /// </summary>
        /// <param name="fieldType">Typ pola do walidacji</param>
        /// <param name="fieldName">Nazwa pola</param>
        /// <param name="collectionName">Nazwa kolekcji (null jeśli pole nie jest w kolekcji)</param>
        private void ValidateSummableFieldType(CalculatedFieldType fieldType, string fieldName, string? collectionName)
        {
            // Only ValueNet, ValueGross, UnitVat and TotalVat can be summable
            if (fieldType != CalculatedFieldType.ValueNet &&
                fieldType != CalculatedFieldType.ValueGross &&
                fieldType != CalculatedFieldType.TotalVat)
            {
                var context = collectionName != null 
                    ? $"in collection '{collectionName}' " 
                    : "";
                
                errors.Add($"Field '{fieldName}' of type '{fieldType}' {context}cannot be summable. Only ValueNet, ValueGross, UnitVat and TotalVat fields can be summed");
            }
        }
    }
    
    /// <summary>
    /// Wynik walidacji struktury szablonu
    /// </summary>
    public class TemplateStructureValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
        
        public TemplateStructureValidationResult(bool isValid, List<string> errors)
        {
            IsValid = isValid;
            Errors = errors.AsReadOnly();
        }
    }
}
