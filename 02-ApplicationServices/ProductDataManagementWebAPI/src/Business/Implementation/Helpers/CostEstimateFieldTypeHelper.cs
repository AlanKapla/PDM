using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.CostEstimates;

namespace Business.Implementation.Helpers
{
    /// <summary>
    /// Helper z konfiguracją typów pól w szablonie kosztorysu
    /// </summary>
    public static class CostEstimateFieldTypeHelper
    {
        /// <summary>
        /// Zwraca pełną konfigurację wszystkich dostępnych typów pól
        /// Zgrupowane według FieldScope
        /// </summary>
        public static Dictionary<int, CostEstimateFieldTypeConfigWeb[]> FieldTypeConfigurations => new()
        {
            // GROUP HEADER FIELDS (0-9)
            [(int)FieldScope.Group] = new[]
                {
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupName,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Nazwa etapu",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupDescription,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Opis etapu",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupNumber,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Numer etapu",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupStartDate,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Data rozpoczęcia",
                        ValueTypeName: "DateTime",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: true,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupEndDate,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Data zakończenia",
                        ValueTypeName: "DateTime",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: true,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupStatus,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Status etapu",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupNotes,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Uwagi",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupResponsible,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Odpowiedzialny",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupBudget,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Budżet etapu",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.GroupPriority,
                        FieldScope: (int)FieldScope.Group,
                        NamePl: "Priorytet",
                        ValueTypeName: "int",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    )
                },

            // ITEM SYSTEM FIELDS (100-199)
            [(int)FieldScope.ItemSystem] = new[]
                {
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemSystemName,
                        FieldScope: (int)FieldScope.ItemSystem,
                        NamePl: "Nazwa pozycji",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemSystemQuantity,
                        FieldScope: (int)FieldScope.ItemSystem,
                        NamePl: "Ilość",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemSystemUnit,
                        FieldScope: (int)FieldScope.ItemSystem,
                        NamePl: "Jednostka",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemSystemOptions,
                        FieldScope: (int)FieldScope.ItemSystem,
                        NamePl: "Opcje/Warianty",
                        ValueTypeName: "collection",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: true
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemSystemSelected,
                        FieldScope: (int)FieldScope.ItemSystem,
                        NamePl: "Zaznaczenie",
                        ValueTypeName: "bool",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: true,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemSystemFiles,
                        FieldScope: (int)FieldScope.ItemSystem,
                        NamePl: "Pliki",
                        ValueTypeName: "file",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false,
                        IsFile: true
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemSystemCategory,
                        FieldScope: (int)FieldScope.ItemSystem,
                        NamePl: "Kategoria",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemSystemIsWorkScope,
                        FieldScope: (int)FieldScope.ItemSystem,
                        NamePl: "Zakres pracy harmonogramu",
                        ValueTypeName: "bool",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: true,
                        IsCollection: false
                    )
                },

            // ITEM CALCULATED FIELDS (200-299)
            [(int)FieldScope.ItemCalculated] = new[]
                {
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemCalculatedUnitPriceNet,
                        FieldScope: (int)FieldScope.ItemCalculated,
                        NamePl: "Cena jednostkowa netto",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemCalculatedVatRate,
                        FieldScope: (int)FieldScope.ItemCalculated,
                        NamePl: "Stawka VAT (%)",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemCalculatedUnitPriceGross,
                        FieldScope: (int)FieldScope.ItemCalculated,
                        NamePl: "Cena jednostkowa brutto",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemCalculatedValueNet,
                        FieldScope: (int)FieldScope.ItemCalculated,
                        NamePl: "Wartość netto",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemCalculatedValueGross,
                        FieldScope: (int)FieldScope.ItemCalculated,
                        NamePl: "Wartość brutto",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemCalculatedUnitVat,
                        FieldScope: (int)FieldScope.ItemCalculated,
                        NamePl: "VAT jednostkowy",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemCalculatedTotalVat,
                        FieldScope: (int)FieldScope.ItemCalculated,
                        NamePl: "VAT całkowity",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    },

            // ITEM GENERIC FIELDS (300-399)
            [(int)FieldScope.ItemGeneric] = new[]
                {
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemGenericNumber,
                        FieldScope: (int)FieldScope.ItemGeneric,
                        NamePl: "Liczba",
                        ValueTypeName: "decimal",
                        IsNumeric: true,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemGenericString,
                        FieldScope: (int)FieldScope.ItemGeneric,
                        NamePl: "Tekst",
                        ValueTypeName: "string",
                        IsNumeric: false,
                        IsText: true,
                        IsDate: false,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemGenericBoolean,
                        FieldScope: (int)FieldScope.ItemGeneric,
                        NamePl: "Tak/Nie",
                        ValueTypeName: "bool",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: false,
                        IsBoolean: true,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemGenericDate,
                        FieldScope: (int)FieldScope.ItemGeneric,
                        NamePl: "Data",
                        ValueTypeName: "DateTime",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: true,
                        IsBoolean: false,
                        IsCollection: false
                    ),
                    new CostEstimateFieldTypeConfigWeb(
                        FieldType: (int)FieldType.ItemGenericDateTime,
                        FieldScope: (int)FieldScope.ItemGeneric,
                        NamePl: "Data i czas",
                        ValueTypeName: "DateTime",
                        IsNumeric: false,
                        IsText: false,
                        IsDate: true,
                        IsBoolean: false,
                        IsCollection: false
                    )
                }
        };

        /// <summary>
        /// Zwraca konfigurację dla konkretnego typu pola
        /// </summary>
        public static CostEstimateFieldTypeConfigWeb? GetFieldTypeConfig(FieldType fieldType)
        {
            var allConfigs = FieldTypeConfigurations;
            
            foreach (var scopeConfigs in allConfigs.Values)
            {
                var config = scopeConfigs.FirstOrDefault(c => c.FieldType == (int)fieldType);
                if (config != null)
                {
                    return config;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Zwraca konfigurację dla konkretnego typu pola (int overload)
        /// </summary>
        public static CostEstimateFieldTypeConfigWeb? GetFieldTypeConfig(int fieldType)
        {
            var allConfigs = FieldTypeConfigurations;
            
            foreach (var scopeConfigs in allConfigs.Values)
            {
                var config = scopeConfigs.FirstOrDefault(c => c.FieldType == fieldType);
                if (config != null)
                {
                    return config;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Określa FieldScope na podstawie FieldType używając słownika konfiguracji
        /// </summary>
        public static FieldScope? DetermineFieldScopeFromFieldType(int fieldType)
        {
            var config = GetFieldTypeConfig(fieldType);
            return config != null ? (FieldScope)config.FieldScope : null;
        }
        
        /// <summary>
        /// Sprawdza czy dany typ pola jest kolekcją (nie powinien być walidowany bezpośrednio)
        /// </summary>
        public static bool IsCollectionFieldType(FieldType fieldType)
        {
            var config = GetFieldTypeConfig(fieldType);
            return config?.IsCollection ?? false;
        }
    }
}
