using Entities.Models.CostEstimates;

namespace Business.Implementation.Helpers
{
    /// <summary>
    /// Domyślny schemat kolumn kosztorysu generowany przy tworzeniu nowego kosztorysu.
    /// </summary>
    public static class DefaultCostEstimateFieldSchemaFactory
    {
        private sealed record DefaultFieldDefinition(
            string FieldKey,
            string FieldName,
            CostEstimateFieldType FieldType,
            bool AppliesToGroup);

        private static readonly DefaultFieldDefinition[] DefaultFields =
        [
            new("name", "Nazwa", CostEstimateFieldType.Name, true),
            new("actions", "Akcje", CostEstimateFieldType.Actions, true),
            new("quantity", "Ilość", CostEstimateFieldType.Quantity, false),
            new("unit", "Jednostka", CostEstimateFieldType.Unit, false),
            new("unitPriceNet", "Cena netto", CostEstimateFieldType.UnitPriceNet, false),
            new("vatRate", "Stawka VAT", CostEstimateFieldType.VatRate, false),
            new("unitPriceGross", "Cena brutto", CostEstimateFieldType.UnitPriceGross, false),
            new("netValue", "Wartość netto", CostEstimateFieldType.NetValue, true),
            new("grossValue", "Wartość brutto", CostEstimateFieldType.GrossValue, true),
            new("vatValue", "Wartość VAT", CostEstimateFieldType.VatValue, false),
            new("isSelected", "Sumuj", CostEstimateFieldType.IsSelected, false),
            new("isStageWork", "Zakres harmonogramu", CostEstimateFieldType.IsStageWork, false),
            new("files", "Plik", CostEstimateFieldType.Files, false),
        ];

        public static List<CostEstimateFieldSchema> CreateDefaultSchema(Guid costEstimateId, DateTime createdAt)
        {
            List<CostEstimateFieldSchema> fields = new();

            for (int i = 0; i < DefaultFields.Length; i++)
            {
                DefaultFieldDefinition definition = DefaultFields[i];
                CostEstimateFieldSchema field = new()
                {
                    Id = Guid.NewGuid(),
                    CostEstimateId = costEstimateId,
                    FieldKey = definition.FieldKey,
                    FieldName = definition.FieldName,
                    FieldType = definition.FieldType,
                    IsBasicField = true,
                    IsAdditionalField = false,
                    Order = i,
                    CreatedAt = createdAt,
                };
                fields.Add(field);
            }

            return fields;
        }
    }
}
