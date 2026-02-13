using Entities.Models.CostEstimates;

namespace Business.Implementation.Helpers
{
    /// <summary>
    /// Helper do konwersji wartości pól na odpowiednie typy w zależności od FieldType
    /// </summary>
    public static class FieldValueConverter
    {
        /// <summary>
        /// Ustawia wartość w odpowiednim polu typowanym na podstawie FieldType
        /// </summary>
        public static void SetTypedValue(CostEstimateFieldValueBase fieldValue, int fieldType, string? stringValue, decimal? decimalValue, bool? boolValue, DateTime? dateTimeValue)
        {
            var config = CostEstimateFieldTypeHelper.GetFieldTypeConfig(fieldType);
            
            if (config == null)
            {
                // Jeśli nie znaleziono konfiguracji, spróbuj zapisać jako string
                fieldValue.StringValue = stringValue;
                return;
            }

            // Wyczyść wszystkie wartości przed ustawieniem nowej
            fieldValue.StringValue = null;
            fieldValue.DecimalValue = null;
            fieldValue.BoolValue = null;
            fieldValue.DateTimeValue = null;

            // Ustaw wartość w odpowiednim polu na podstawie typu z konfiguracji
            if (config.IsText)
            {
                fieldValue.StringValue = stringValue;
            }
            else if (config.IsNumeric)
            {
                fieldValue.DecimalValue = decimalValue;
            }
            else if (config.IsBoolean)
            {
                fieldValue.BoolValue = boolValue;
            }
            else if (config.IsDate)
            {
                fieldValue.DateTimeValue = dateTimeValue;
            }
            else
            {
                // Fallback - zapisz jako string
                fieldValue.StringValue = stringValue;
            }
        }

        /// <summary>
        /// Pobiera wartość z odpowiedniego pola typowanego na podstawie FieldType
        /// </summary>
        public static (string? stringValue, decimal? decimalValue, bool? boolValue, DateTime? dateTimeValue) GetTypedValue(CostEstimateFieldValueBase fieldValue, int fieldType)
        {
            var config = CostEstimateFieldTypeHelper.GetFieldTypeConfig(fieldType);
            
            if (config == null)
            {
                // Jeśli nie znaleziono konfiguracji, zwróć string value
                return (fieldValue.StringValue, null, null, null);
            }

            if (config.IsText)
            {
                return (fieldValue.StringValue, null, null, null);
            }
            else if (config.IsNumeric)
            {
                return (null, fieldValue.DecimalValue, null, null);
            }
            else if (config.IsBoolean)
            {
                return (null, null, fieldValue.BoolValue, null);
            }
            else if (config.IsDate)
            {
                return (null, null, null, fieldValue.DateTimeValue);
            }

            // Fallback
            return (fieldValue.StringValue, null, null, null);
        }
    }
}
