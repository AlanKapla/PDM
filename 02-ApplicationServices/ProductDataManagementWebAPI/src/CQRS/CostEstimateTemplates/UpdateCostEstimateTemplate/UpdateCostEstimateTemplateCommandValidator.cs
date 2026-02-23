using Business.Interfaces.WebModels.CostEstimateTemplates;
using FluentValidation;
using Entities.Models.CostEstimates;

namespace CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate
{
    public class UpdateCostEstimateTemplateCommandValidator : AbstractValidator<UpdateCostEstimateTemplateCommand>
    {
        public UpdateCostEstimateTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty().WithMessage("Template ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required")
                .MaximumLength(200).WithMessage("Template name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");

            RuleFor(x => x.MaxGroupLevel)
                .GreaterThan(0).When(x => x.MaxGroupLevel.HasValue)
                .WithMessage("Max group level must be greater than 0");

            RuleFor(x => x.GroupNumberFormat)
                .MaximumLength(50).WithMessage("Group number format cannot exceed 50 characters");

            // FieldName is now Guid (always unique) - no need for uniqueness validation
            RuleFor(x => x.GroupHeaderFields)
                .Must(fields => fields == null || AllFieldNamesAreNotEmpty(fields))
                .When(x => x.UpdateStructure && x.GroupHeaderFields != null)
                .WithMessage("Group header fields must have non-empty FieldNames");
            
            RuleFor(x => x.GroupHeaderFields)
                .Must(fields => fields == null || AllFieldTypesAreUnique(fields))
                .When(x => x.UpdateStructure && x.GroupHeaderFields != null)
                .WithMessage("Group header fields must have unique FieldType values within their scope");

            RuleFor(x => x.SystemFields)
                .Must(fields => fields == null || AllFieldNamesAreNotEmpty(fields))
                .When(x => x.UpdateStructure && x.SystemFields != null)
                .WithMessage("System fields must have non-empty FieldNames");
            
            RuleFor(x => x.SystemFields)
                .Must(fields => fields == null || AllFieldTypesAreUnique(fields))
                .When(x => x.UpdateStructure && x.SystemFields != null)
                .WithMessage("System fields must have unique FieldType values within their scope");

            RuleFor(x => x.CalculatedFields)
                .Must(fields => fields == null || AllFieldNamesAreNotEmpty(fields))
                .When(x => x.UpdateStructure && x.CalculatedFields != null)
                .WithMessage("Calculated fields must have non-empty FieldNames");

            RuleFor(x => x.CalculatedFields)
                .Must(fields => fields == null || AllFieldTypesAreUnique(fields))
                .When(x => x.UpdateStructure && x.CalculatedFields != null)
                .WithMessage("Calculated fields must have unique FieldType values (no duplicate CalculatedFieldType enums)");

            RuleFor(x => x.GenericFields)
                .Must(fields => fields == null || AllFieldNamesAreNotEmpty(fields))
                .When(x => x.UpdateStructure && x.GenericFields != null)
                .WithMessage("Generic fields must have non-empty FieldNames");
           

            RuleFor(x => x)
                .Must(x => UiColumnLayoutReferencesExistingFields(x))
                .When(x => x.UpdateStructure && x.UiConfiguration?.ColumnLayout != null)
                .WithMessage("UI ColumnLayout must only reference existing FieldNames (Guid) from GroupHeaderFields, SystemFields, CalculatedFields or GenericFields");

            // Walidacja hierarchii pól - tylko ItemSystemOptions może mieć child fields
            RuleFor(x => x.SystemFields)
                .Must(fields => OnlyOptionsFieldsHaveChildren(fields))
                .When(x => x.UpdateStructure && x.SystemFields != null)
                .WithMessage("Only SystemFields with FieldType = ItemSystemOptions (103) can have child fields");

            RuleFor(x => x.CalculatedFields)
                .Must(fields => NoChildFieldsAllowed(fields))
                .When(x => x.UpdateStructure && x.CalculatedFields != null)
                .WithMessage("CalculatedFields cannot have child fields - hierarchy is only allowed for ItemSystemOptions");

            RuleFor(x => x.GenericFields)
                .Must(fields => NoChildFieldsAllowed(fields))
                .When(x => x.UpdateStructure && x.GenericFields != null)
                .WithMessage("GenericFields cannot have child fields - hierarchy is only allowed for ItemSystemOptions");

            RuleFor(x => x.GroupHeaderFields)
                .Must(fields => NoChildFieldsAllowed(fields))
                .When(x => x.UpdateStructure && x.GroupHeaderFields != null)
                .WithMessage("GroupHeaderFields cannot have child fields - hierarchy is only allowed for ItemSystemOptions");
            
            RuleFor(x => x.CalculatedFields)
                .Must(fields => OnlySummableFieldsHaveSumFlags(fields))
                .When(x => x.UpdateStructure && x.CalculatedFields != null)
                .WithMessage("Only ValueNet (203), ValueGross (204) and TotalVat (206) fields can have SumInGroup or SumInTotal set to true");
        }

        private bool AllFieldNamesAreNotEmpty(List<FieldDefinitionDto> fields)
        {
            if (fields == null || fields.Count == 0)
            {
                return true;
            }

            // FieldName is Guid - just check it's not empty (Guid.Empty)
            return fields.All(f => f.FieldName != Guid.Empty);
        }

        private bool AllFieldTypesAreUnique(List<FieldDefinitionDto> fields)
        {
            if (fields == null || fields.Count == 0)
            {
                return true;
            }

            var fieldTypes = fields.Select(f => f.FieldType).ToList();
            return fieldTypes.Count == fieldTypes.Distinct().Count();
        }
        
        /// <summary>
        /// Sprawdza czy tylko dozwolone pola mają ustawione flagi sumowania
        /// Dozwolone: ValueNet (203), ValueGross (204), TotalVat (206)
        /// </summary>
        private bool OnlySummableFieldsHaveSumFlags(List<FieldDefinitionDto> fields)
        {
            if (fields == null || fields.Count == 0)
            {
                return true;
            }

            // FieldType values dla pól, które mogą być sumowane
            var summableFieldTypes = new HashSet<int>
            {
                (int)FieldType.ItemCalculatedValueNet,      // 203
                (int)FieldType.ItemCalculatedValueGross,    // 204
                (int)FieldType.ItemCalculatedTotalVat       // 206
            };

            foreach (var field in fields)
            {
                var hasSumFlag = field.SumInGroup || field.SumInTotal;
                
                if (hasSumFlag && !summableFieldTypes.Contains(field.FieldType))
                {
                    return false;
                }
            }

            return true;
        }

        private bool UiColumnLayoutReferencesExistingFields(UpdateCostEstimateTemplateCommand command)
        {
            if (command.UiConfiguration?.ColumnLayout == null)
            {
                return true;
            }

            // Collect all field names (Guid) using LINQ concat
            var allFieldNames = (command.GroupHeaderFields ?? Enumerable.Empty<FieldDefinitionDto>())
                .Select(f => f.FieldName)
                .Concat((command.SystemFields ?? Enumerable.Empty<FieldDefinitionDto>())
                    .Select(f => f.FieldName))
                .Concat((command.CalculatedFields ?? Enumerable.Empty<FieldDefinitionDto>())
                    .Select(f => f.FieldName))
                .Concat((command.GenericFields ?? Enumerable.Empty<FieldDefinitionDto>())
                    .Select(f => f.FieldName))
                .ToHashSet();

            // ColumnLayout nie może być puste gdy są zdefiniowane jakiekolwiek pola
            if (allFieldNames.Count > 0 && command.UiConfiguration.ColumnLayout.Count == 0)
            {
                return false;
            }

            // Wszystkie pola w ColumnLayout muszą istnieć w allFieldNames (Guid.Empty not allowed)
            return command.UiConfiguration.ColumnLayout.All(fieldName =>
                fieldName != Guid.Empty &&
                allFieldNames.Contains(fieldName));
        }

        /// <summary>
        /// Sprawdza czy tylko pola typu ItemSystemOptions (103) mają child fields
        /// </summary>
        private bool OnlyOptionsFieldsHaveChildren(List<FieldDefinitionDto> systemFields)
        {
            if (systemFields == null || systemFields.Count == 0)
            {
                return true;
            }

            // FieldType 103 = ItemSystemOptions - tylko ten typ może mieć child fields
            const int ItemSystemOptionsFieldType = (int)FieldType.ItemSystemOptions;

            foreach (var field in systemFields)
            {
                var hasChildren = field.ChildFields != null && field.ChildFields.Count > 0;

                if (hasChildren && field.FieldType != ItemSystemOptionsFieldType)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Sprawdza czy pola NIE mają child fields (dla typów które nie mogą mieć hierarchii)
        /// </summary>
        private bool NoChildFieldsAllowed(List<FieldDefinitionDto> fields)
        {
            if (fields == null || fields.Count == 0)
            {
                return true;
            }

            return fields.All(f => f.ChildFields == null || f.ChildFields.Count == 0);
        }
    }
}
