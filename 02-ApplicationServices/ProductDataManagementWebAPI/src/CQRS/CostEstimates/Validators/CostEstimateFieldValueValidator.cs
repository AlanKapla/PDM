using Entities.Models.CostEstimates;
using FluentValidation;

namespace CQRS.CostEstimates.Validators
{
    /// <summary>
    /// Waliduje wartość pola kosztorysu względem jego definicji w szablonie.
    ///
    /// Sprawdza:
    ///   1. Type-mismatch — czy wartość wysłano w odpowiedniej kolumnie dla danego FieldType
    ///      (np. decimalValue dla pola numerycznego, stringValue dla tekstowego).
    ///      Bez tej walidacji błędnie przypisana wartość jest po cichu ignorowana przez FieldValueConverter.
    ///   2. Zakresy wartości — VatRate [0,1], pola nieujemne (ceny, ilości, budżety).
    ///   3. MaxLength — StringValue max 2000 znaków (limit kolumny nvarchar(2000) w DB).
    ///
    /// Używany przez:
    ///   - <see cref="CQRS.CostEstimates.UpsertCostEstimateItemField.UpsertCostEstimateItemFieldCommandHandler"/>
    ///   - <see cref="CQRS.CostEstimates.UpsertCostEstimateGroupField.UpsertCostEstimateGroupFieldCommandHandler"/>
    ///
    /// Rejestracja DI: services.AddScoped&lt;CostEstimateFieldValueValidator&gt;()
    /// </summary>
    public class CostEstimateFieldValueValidator : AbstractValidator<CostEstimateFieldValueContext>
    {
        public CostEstimateFieldValueValidator()
        {
            // Pola kolekcji (komponenty/warianty) i pliki mają dedykowane endpointy.
            // Próba ustawienia ich wartości przez UpsertField jest błędem klienta.
            When(x => x.FieldTypeConfig.IsCollection, () =>
            {
                RuleFor(x => x.FieldType)
                    .Must(_ => false)
                    .WithName("FieldType")
                    .WithMessage(x =>
                        $"Field '{x.FieldLabel}' [{x.FieldType}]: This is a collection field " +
                        "(options/variants/components). Use the dedicated item endpoints to manage " +
                        "collection members — field value updates are not supported for collection fields.");
            });

            When(x => x.FieldTypeConfig.IsFile, () =>
            {
                RuleFor(x => x.FieldType)
                    .Must(_ => false)
                    .WithName("FieldType")
                    .WithMessage(x =>
                        $"Field '{x.FieldLabel}' [{x.FieldType}]: This is a file field. " +
                        "Use the file upload endpoint — field value updates are not supported for file fields.");
            });

            Unless(x => x.FieldTypeConfig.IsCollection || x.FieldTypeConfig.IsFile, () =>
            {
                AddTypeMismatchRules();
                AddTextRules();
                AddNumericRules();
            });
        }

        private void AddTypeMismatchRules()
        {
            // Pole NUMERYCZNE — wysłano stringValue bez decimalValue.
            // FieldValueConverter cicho odrzuci stringValue dla pól numerycznych.
            RuleFor(x => x.StringValue)
                .Must(v => v == null)
                .When(x => x.FieldTypeConfig.IsNumeric
                            && !x.DecimalValue.HasValue
                            && !string.IsNullOrEmpty(x.StringValue))
                .WithName("StringValue")
                .WithMessage(x =>
                    $"Field '{x.FieldLabel}' [{x.FieldType}]: This is a numeric field. " +
                    "Send the value in 'decimalValue'. The 'stringValue' will be silently ignored.");

            // Pole TEKSTOWE — wysłano decimalValue bez stringValue.
            // FieldValueConverter cicho odrzuci decimalValue dla pól tekstowych.
            RuleFor(x => x.DecimalValue)
                .Must(v => v == null)
                .When(x => x.FieldTypeConfig.IsText
                            && x.DecimalValue.HasValue
                            && string.IsNullOrEmpty(x.StringValue))
                .WithName("DecimalValue")
                .WithMessage(x =>
                    $"Field '{x.FieldLabel}' [{x.FieldType}]: This is a text field. " +
                    "Send the value in 'stringValue'. The 'decimalValue' will be silently ignored.");

            // Pole BOOLOWE — wysłano stringValue bez boolValue.
            RuleFor(x => x.StringValue)
                .Must(v => v == null)
                .When(x => x.FieldTypeConfig.IsBoolean
                            && !x.BoolValue.HasValue
                            && !string.IsNullOrEmpty(x.StringValue))
                .WithName("StringValue")
                .WithMessage(x =>
                    $"Field '{x.FieldLabel}' [{x.FieldType}]: This is a boolean field. " +
                    "Send the value in 'boolValue'. The 'stringValue' will be silently ignored.");

            // Pole DATY — wysłano stringValue bez dateTimeValue.
            RuleFor(x => x.StringValue)
                .Must(v => v == null)
                .When(x => x.FieldTypeConfig.IsDate
                            && !x.DateTimeValue.HasValue
                            && !string.IsNullOrEmpty(x.StringValue))
                .WithName("StringValue")
                .WithMessage(x =>
                    $"Field '{x.FieldLabel}' [{x.FieldType}]: This is a date field. " +
                    "Send the value in 'dateTimeValue'. The 'stringValue' will be silently ignored.");
        }

        private void AddTextRules()
        {
            RuleFor(x => x.StringValue)
                .MaximumLength(2000)
                .When(x => x.FieldTypeConfig.IsText && x.StringValue is not null)
                .WithName("StringValue")
                .WithMessage(x =>
                    $"Field '{x.FieldLabel}' [{x.FieldType}]: Text value exceeds the 2000-character limit " +
                    $"(length: {x.StringValue?.Length ?? 0}). Reduce the value before saving.");
        }

        private void AddNumericRules()
        {
            // VatRate musi być w zakresie [0, 1] — gdzie 0.23 = 23%.
            RuleFor(x => x.DecimalValue)
                .InclusiveBetween(0m, 1m)
                .When(x => x.FieldType == FieldType.ItemCalculatedVatRate && x.DecimalValue.HasValue)
                .WithName("DecimalValue")
                .WithMessage(x =>
                    $"Field '{x.FieldLabel}' [VatRate]: Value must be between 0 and 1 " +
                    $"(e.g. 0.23 = 23%). Received: {x.DecimalValue}.");

            // Pola cen, ilości, budżetów i priorytetów muszą być nieujemne.
            RuleFor(x => x.DecimalValue)
                .GreaterThanOrEqualTo(0m)
                .When(x => IsNonNegativeField(x.FieldType) && x.DecimalValue.HasValue)
                .WithName("DecimalValue")
                .WithMessage(x =>
                    $"Field '{x.FieldLabel}' [{x.FieldType}]: Value cannot be negative. Received: {x.DecimalValue}.");
        }

        private static bool IsNonNegativeField(FieldType fieldType) => fieldType switch
        {
            FieldType.GroupBudget                  => true,
            FieldType.GroupPriority                => true,
            FieldType.ItemSystemQuantity           => true,
            FieldType.ItemCalculatedUnitPriceNet   => true,
            FieldType.ItemCalculatedUnitPriceGross => true,
            FieldType.ItemCalculatedValueNet       => true,
            FieldType.ItemCalculatedValueGross     => true,
            FieldType.ItemCalculatedUnitVat        => true,
            FieldType.ItemCalculatedTotalVat       => true,
            _                                      => false
        };
    }
}
