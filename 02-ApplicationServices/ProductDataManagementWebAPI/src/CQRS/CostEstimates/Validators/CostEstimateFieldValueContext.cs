using Business.Implementation.Helpers;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;

namespace CQRS.CostEstimates.Validators
{
    /// <summary>
    /// Kontekst walidacji wartości pola kosztorysu względem definicji w szablonie.
    /// Przekazywany do <see cref="CostEstimateFieldValueValidator"/>.
    /// </summary>
    public record CostEstimateFieldValueContext
    {
        public required FieldType FieldType { get; init; }
        public required string FieldLabel { get; init; }
        public required CostEstimateFieldTypeConfigWeb FieldTypeConfig { get; init; }
        public string? StringValue { get; init; }
        public decimal? DecimalValue { get; init; }
        public bool? BoolValue { get; init; }
        public DateTime? DateTimeValue { get; init; }

        /// <summary>
        /// Tworzy kontekst na podstawie definicji pola z szablonu i wartości z requestu.
        /// </summary>
        public static CostEstimateFieldValueContext From(
            CostEstimateTemplateFieldDefinitionBase fieldDef,
            string? stringValue,
            decimal? decimalValue,
            bool? boolValue,
            DateTime? dateTimeValue)
        {
            CostEstimateFieldTypeConfigWeb config = CostEstimateFieldTypeHelper.GetFieldTypeConfig(fieldDef.FieldType)
                ?? throw new InvalidOperationException(
                    $"No field type configuration found for FieldType {fieldDef.FieldType} (label: {fieldDef.Label})");

            return new CostEstimateFieldValueContext
            {
                FieldType = fieldDef.FieldType,
                FieldLabel = fieldDef.Label,
                FieldTypeConfig = config,
                StringValue = stringValue,
                DecimalValue = decimalValue,
                BoolValue = boolValue,
                DateTimeValue = dateTimeValue
            };
        }
    }
}
