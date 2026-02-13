using Entities.Models.CostEstimates;

namespace Entities.Helpers
{
    /// <summary>
    /// Helper class for converting between old field type enums and new unified FieldType enum
    /// Used for backward compatibility and migration purposes
    /// </summary>
    public static class FieldTypeConverter
    {
        /// <summary>
        /// Convert old GroupHeaderFieldType value to new FieldType
        /// </summary>
        public static FieldType FromGroupHeaderFieldType(int oldValue)
        {
            return oldValue switch
            {
                0 => FieldType.GroupName,
                1 => FieldType.GroupDescription,
                2 => FieldType.GroupNumber,
                3 => FieldType.GroupStartDate,
                4 => FieldType.GroupEndDate,
                5 => FieldType.GroupStatus,
                6 => FieldType.GroupNotes,
                7 => FieldType.GroupResponsible,
                8 => FieldType.GroupBudget,
                9 => FieldType.GroupPriority,
                _ => throw new ArgumentException($"Invalid GroupHeaderFieldType value: {oldValue}", nameof(oldValue))
            };
        }

        /// <summary>
        /// Convert old SystemFieldType value to new FieldType
        /// </summary>
        public static FieldType FromSystemFieldType(int oldValue)
        {
            return oldValue switch
            {
                0 => FieldType.ItemSystemName,
                1 => FieldType.ItemSystemQuantity,
                2 => FieldType.ItemSystemUnit,
                _ => throw new ArgumentException($"Invalid SystemFieldType value: {oldValue}", nameof(oldValue))
            };
        }

        /// <summary>
        /// Convert old CalculatedFieldType value to new FieldType
        /// </summary>
        public static FieldType FromCalculatedFieldType(int oldValue)
        {
            return oldValue switch
            {
                0 => FieldType.ItemCalculatedUnitPriceNet,
                1 => FieldType.ItemCalculatedVatRate,
                2 => FieldType.ItemCalculatedUnitPriceGross,
                3 => FieldType.ItemCalculatedValueNet,
                4 => FieldType.ItemCalculatedValueGross,
                5 => FieldType.ItemCalculatedUnitVat,
                6 => FieldType.ItemCalculatedTotalVat,
                _ => throw new ArgumentException($"Invalid CalculatedFieldType value: {oldValue}", nameof(oldValue))
            };
        }

        /// <summary>
        /// Convert old GenericFieldType value to new FieldType
        /// </summary>
        public static FieldType FromGenericFieldType(int oldValue)
        {
            return oldValue switch
            {
                0 => FieldType.ItemGenericNumber,
                2 => FieldType.ItemGenericString,
                3 => FieldType.ItemGenericBoolean,
                4 => FieldType.ItemGenericDate,
                5 => FieldType.ItemGenericDateTime,
                _ => throw new ArgumentException($"Invalid GenericFieldType value: {oldValue}", nameof(oldValue))
            };
        }

        /// <summary>
        /// Convert FieldScope and old enum value to new FieldType
        /// </summary>
        public static FieldType FromScopeAndOldValue(FieldScope scope, int oldValue)
        {
            return scope switch
            {
                FieldScope.Group => FromGroupHeaderFieldType(oldValue),
                FieldScope.ItemSystem => FromSystemFieldType(oldValue),
                FieldScope.ItemCalculated => FromCalculatedFieldType(oldValue),
                FieldScope.ItemGeneric => FromGenericFieldType(oldValue),
                _ => throw new ArgumentException($"Invalid FieldScope value: {scope}", nameof(scope))
            };
        }

        /// <summary>
        /// Get FieldScope from FieldType
        /// </summary>
        public static FieldScope GetFieldScope(FieldType fieldType)
        {
            var value = (int)fieldType;
            
            if (value >= 0 && value <= 99)
                return FieldScope.Group;
            
            if (value >= 100 && value <= 199)
                return FieldScope.ItemSystem;
            
            if (value >= 200 && value <= 299)
                return FieldScope.ItemCalculated;
            
            if (value >= 300 && value <= 399)
                return FieldScope.ItemGeneric;
            
            throw new ArgumentException($"Invalid FieldType value: {fieldType}", nameof(fieldType));
        }

        /// <summary>
        /// Get old enum value from new FieldType (for backward compatibility)
        /// Returns the value within the scope (0-9 for Group, 0-2 for System, etc.)
        /// </summary>
        public static int ToOldEnumValue(FieldType fieldType)
        {
            var scope = GetFieldScope(fieldType);
            var value = (int)fieldType;
            
            return scope switch
            {
                FieldScope.Group => value, // 0-9
                FieldScope.ItemSystem => value - 100, // 100-102 -> 0-2
                FieldScope.ItemCalculated => value - 200, // 200-206 -> 0-6
                FieldScope.ItemGeneric => value - 300, // 300-305 -> 0-5
                _ => throw new ArgumentException($"Invalid FieldScope: {scope}", nameof(fieldType))
            };
        }

        /// <summary>
        /// Convert old ColumnFieldSource to new FieldScope
        /// </summary>
        public static FieldScope FromColumnFieldSource(int oldValue)
        {
            return oldValue switch
            {
                0 => FieldScope.Group,
                1 => FieldScope.ItemSystem,
                2 => FieldScope.ItemCalculated,
                3 => FieldScope.ItemGeneric,
                _ => throw new ArgumentException($"Invalid ColumnFieldSource value: {oldValue}", nameof(oldValue))
            };
        }
    }
}
