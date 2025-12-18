using Business.Implementation.Validators;
using Entities.Models.CostEstimateData;
using Entities.Models.CostEstimateTemplateDefinitions;
using FluentValidation;

namespace CQRS.CostEstimates.Validators
{
    /// <summary>
    /// Bazowy walidator dla Create i Update CostEstimate
    /// Zawiera wspólną logikę walidacji danych kosztorysu względem schematu szablonu
    /// </summary>
    public static class BaseCostEstimateValidator
    {
        /// <summary>
        /// Waliduje dane kosztorysu względem struktury szablonu
        /// Używa CostEstimateSchemaValidator do szczegółowej walidacji
        /// </summary>
        public static bool ValidateDataAgainstTemplate(
            CostEstimateDataModel data,
            CostEstimateTemplateStructure templateStructure,
            out string errorMessage)
        {
            if (templateStructure == null)
            {
                errorMessage = "Template structure not found";
                return false;
            }

            if (data == null)
            {
                errorMessage = "Cost estimate data cannot be null";
                return false;
            }

            var validator = new CostEstimateSchemaValidator(templateStructure);
            var result = validator.Validate(data);

            if (!result.IsValid)
            {
                errorMessage = "Data does not match template structure: " + string.Join("; ", result.Errors);
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Konfiguruje podstawowe reguły walidacji wspólne dla Create i Update
        /// </summary>
        public static void ConfigureCommonRules<T>(AbstractValidator<T> validator) where T : class
        {
            // Nazwa
            validator.RuleFor(x => GetName(x))
                .NotEmpty().WithMessage("Cost estimate name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            // Opis
            validator.RuleFor(x => GetDescription(x))
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
                .When(x => GetDescription(x) != null);

            // Data
            validator.RuleFor(x => GetData(x))
                .NotNull().WithMessage("Cost estimate data is required");

            // Groups w Data
            validator.When(x => GetData(x) != null, () =>
            {
                validator.RuleFor(x => GetData(x).Groups)
                    .NotEmpty().WithMessage("Cost estimate must have at least one group");
            });
        }

        private static string GetName(object obj)
        {
            return obj.GetType().GetProperty("Name")?.GetValue(obj) as string ?? string.Empty;
        }

        private static string? GetDescription(object obj)
        {
            return obj.GetType().GetProperty("Description")?.GetValue(obj) as string;
        }

        private static CostEstimateDataModel GetData(object obj)
        {
            return obj.GetType().GetProperty("Data")?.GetValue(obj) as CostEstimateDataModel 
                ?? new CostEstimateDataModel();
        }
    }
}
