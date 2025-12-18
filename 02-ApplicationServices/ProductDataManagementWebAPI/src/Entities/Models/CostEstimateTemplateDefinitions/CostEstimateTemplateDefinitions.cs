using System.Text.Json.Serialization;

namespace Entities.Models.CostEstimateTemplateDefinitions
{
    #region Enums

    /// <summary>
    /// Typ pola obliczeniowego z logiką cenową
    /// </summary>
    public enum CalculatedFieldType
    {
        /// <summary>
        /// Cena jednostkowa netto (decimal) - bierze udział w obliczeniach
        /// </summary>
        UnitPriceNet = 0,
        
        /// <summary>
        /// Stawka VAT w procentach (decimal) - bierze udział w obliczeniach
        /// </summary>
        VatRate = 1,
        
        /// <summary>
        /// Cena jednostkowa brutto (decimal) - obliczana lub wprowadzana
        /// UnitPriceGross = UnitPriceNet * (1 + VatRate/100)
        /// </summary>
        UnitPriceGross = 2,
        
        /// <summary>
        /// Ilość (decimal) - bierze udział w obliczeniach
        /// </summary>
        Quantity = 3,
        
        /// <summary>
        /// Wartość netto (decimal) - obliczana
        /// ValueNet = UnitPriceNet * Quantity
        /// </summary>
        ValueNet = 4,
        
        /// <summary>
        /// Wartość brutto (decimal) - obliczana
        /// ValueGross = UnitPriceGross * Quantity
        /// </summary>
        ValueGross = 5,
        
        /// <summary>
        /// Wartość VAT jednostkowa (decimal) - obliczana
        /// UnitVat = UnitPriceNet * (VatRate / 100)
        /// lub UnitVat = UnitPriceGross - UnitPriceNet
        /// </summary>
        UnitVat = 6,
        
        /// <summary>
        /// Wartość VAT całkowita (decimal) - obliczana
        /// TotalVat = ValueNet * (VatRate / 100)
        /// lub TotalVat = UnitVat * Quantity
        /// lub TotalVat = ValueGross - ValueNet
        /// </summary>
        TotalVat = 7
    }

    /// <summary>
    /// Typ pola generycznego bez logiki obliczeniowej
    /// </summary>
    public enum GenericFieldType
    {
        /// <summary>
        /// Liczba całkowita
        /// </summary>
        Integer = 0,
        
        /// <summary>
        /// Liczba dziesiętna
        /// </summary>
        Decimal = 1,
        
        /// <summary>
        /// Ciąg znaków
        /// </summary>
        String = 2,
        
        /// <summary>
        /// Wartość logiczna (true/false)
        /// </summary>
        Boolean = 3,
        
        /// <summary>
        /// Data
        /// </summary>
        Date = 4,
        
        /// <summary>
        /// Data i czas
        /// </summary>
        DateTime = 5,
        
        /// <summary>
        /// Kolekcja zagnieżdżonych pól
        /// </summary>
        Collection = 10
    }

    /// <summary>
    /// Typ pola nagłówka grupy
    /// </summary>
    public enum GroupHeaderFieldType
    {
        /// <summary>
        /// Nazwa grupy (string) - zawsze wymagana
        /// </summary>
        GroupName = 0,
        
        /// <summary>
        /// Opis grupy (string)
        /// </summary>
        GroupDescription = 1,
        
        /// <summary>
        /// Numer grupy (string/int) - automatycznie generowany lub ręczny
        /// </summary>
        GroupNumber = 2,
        
        /// <summary>
        /// Data rozpoczęcia (DateTime)
        /// </summary>
        StartDate = 3,
        
        /// <summary>
        /// Data zakończenia (DateTime)
        /// </summary>
        EndDate = 4,
        
        /// <summary>
        /// Status grupy (string) - np. "Planowane", "W trakcie", "Zakończone"
        /// </summary>
        Status = 5,
        
        /// <summary>
        /// Uwagi do grupy (string)
        /// </summary>
        Notes = 6,
        
        /// <summary>
        /// Odpowiedzialny (string) - osoba/firma odpowiedzialna za grupę
        /// </summary>
        Responsible = 7,
        
        /// <summary>
        /// Budżet grupy (decimal)
        /// </summary>
        Budget = 8,
        
        /// <summary>
        /// Priorytet (int) - np. 1-5
        /// </summary>
        Priority = 9
    }

    /// <summary>
    /// Zakres sumowania wartości
    /// </summary>
    public enum SummaryScope
    {
        /// <summary>
        /// Sumowanie w obrębie grupy
        /// </summary>
        Group = 0,
        
        /// <summary>
        /// Sumowanie w całym kosztorysie
        /// </summary>
        Total = 1,
        
        /// <summary>
        /// Sumowanie w grupie i w całości
        /// </summary>
        Both = 2
    }

    #endregion

    #region Main Structure

    /// <summary>
    /// Reprezentuje pełną definicję szablonu kosztorysu
    /// </summary>
    public class CostEstimateTemplateStructure
    {
        /// <summary>
        /// Czy można dodawać nowe grupy podczas wypełniania kosztorysu
        /// </summary>
        [JsonPropertyName("canAddGroups")]
        public bool CanAddGroups { get; set; }
        
        /// <summary>
        /// Czy można rozgałęziać grupy (tworzyć podgrupy)
        /// </summary>
        [JsonPropertyName("canBranchGroups")]
        public bool CanBranchGroups { get; set; }
        
        /// <summary>
        /// Maksymalny poziom zagnieżdżenia grup (null = bez limitu)
        /// </summary>
        [JsonPropertyName("maxGroupLevel")]
        public int? MaxGroupLevel { get; set; }
        
        /// <summary>
        /// Definicja tego, jak może wyglądać grupa w kosztorysie
        /// (pojedynczy szablon, a nie kolekcja faktycznych grup)
        /// </summary>
        [JsonPropertyName("groupDefinition")]
        public CostEstimateGroupDefinition GroupDefinition { get; set; } = default!;
        
        /// <summary>
        /// Definicja pól dla zakresów robót (wspólna dla wszystkich grup)
        /// </summary>
        [JsonPropertyName("workScopeFieldsDefinition")]
        public CostEstimateWorkScopeFieldsDefinition WorkScopeFieldsDefinition { get; set; } = default!;
        
        /// <summary>
        /// Konfiguracja podsumowania - które pola mają być sumowane
        /// </summary>
        [JsonPropertyName("summaryConfiguration")]
        public CostEstimateSummaryConfiguration? SummaryConfiguration { get; set; }
        
        /// <summary>
        /// Konfiguracja UI - układ kolumn, widoczność, szerokości
        /// Definiuje domyślny wygląd dla wszystkich kosztorysów z tego szablonu
        /// </summary>
        [JsonPropertyName("uiConfiguration")]
        public CostEstimateUiConfiguration? UiConfiguration { get; set; }
    }

    #endregion

    #region Group Definition

    /// <summary>
    /// Reprezentuje definicję tego, jak może wyglądać grupa w kosztorysie
    /// Grupa to nagłówek hierarchiczny - zawiera tylko pola nagłówkowe, nie work scope fields
    /// </summary>
    public class CostEstimateGroupDefinition
    {
        /// <summary>
        /// Czy automatycznie numerować grupy
        /// </summary>
        [JsonPropertyName("autoNumbered")]
        public bool AutoNumbered { get; set; }
        
        /// <summary>
        /// Format numeracji (np. "{0}" dla "1", "Etap {0}" dla "Etap 1", "{0:00}" dla "01")
        /// </summary>
        [JsonPropertyName("numberFormat")]
        public string? NumberFormat { get; set; }
        
        /// <summary>
        /// Pola nagłówka grupy - definiuje które pola mają być dostępne w nagłówku
        /// </summary>
        [JsonPropertyName("headerFields")]
        public List<GroupHeaderFieldDefinition> HeaderFields { get; set; } = new List<GroupHeaderFieldDefinition>();
    }

    /// <summary>
    /// Definicja pojedynczego pola nagłówka grupy
    /// </summary>
    public class GroupHeaderFieldDefinition
    {
        /// <summary>
        /// Typ pola nagłówka
        /// </summary>
        [JsonPropertyName("type")]
        public GroupHeaderFieldType Type { get; set; }
        
        /// <summary>
        /// Niestandardowa etykieta (jeśli null, użyj domyślnej dla typu)
        /// </summary>
        [JsonPropertyName("customLabel")]
        public string? CustomLabel { get; set; }
        
        /// <summary>
        /// Czy pole jest wymagane
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; }
        
        /// <summary>
        /// Czy pole jest widoczne
        /// </summary>
        [JsonPropertyName("visible")]
        public bool Visible { get; set; } = true;
        
        /// <summary>
        /// Kolejność wyświetlania
        /// </summary>
        [JsonPropertyName("order")]
        public int Order { get; set; }
        
        /// <summary>
        /// Wartość domyślna (jako string)
        /// </summary>
        [JsonPropertyName("defaultValue")]
        public string? DefaultValue { get; set; }
        
        /// <summary>
        /// Dozwolone wartości (dla pól typu Status, Priority itp.)
        /// </summary>
        [JsonPropertyName("allowedValues")]
        public List<string>? AllowedValues { get; set; }
        
        /// <summary>
        /// Placeholder/wskazówka dla pola
        /// </summary>
        [JsonPropertyName("placeholder")]
        public string? Placeholder { get; set; }
        
        /// <summary>
        /// Czy pole jest tylko do odczytu
        /// </summary>
        [JsonPropertyName("readOnly")]
        public bool ReadOnly { get; set; }
        
        /// <summary>
        /// Format wyświetlania (np. "yyyy-MM-dd" dla dat)
        /// </summary>
        [JsonPropertyName("displayFormat")]
        public string? DisplayFormat { get; set; }
        
        /// <summary>
        /// Rozszerzony tekst pomocy/tooltip
        /// </summary>
        [JsonPropertyName("helpText")]
        public string? HelpText { get; set; }
        
        /// <summary>
        /// Link do dokumentacji zewnętrznej
        /// </summary>
        [JsonPropertyName("helpUrl")]
        public string? HelpUrl { get; set; }
        
        /// <summary>
        /// Nazwa ikony (np. "calendar", "user", "flag")
        /// </summary>
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
        
        /// <summary>
        /// Kolor pola (np. "#FF5733", "primary", "danger")
        /// </summary>
        [JsonPropertyName("color")]
        public string? Color { get; set; }
    }

    #endregion

    #region Work Scope Definition

    /// <summary>
    /// Reprezentuje definicję pól dla zakresów robót
    /// </summary>
    public class CostEstimateWorkScopeFieldsDefinition
    {
        /// <summary>
        /// Pola obliczeniowe z logiką cenową
        /// </summary>
        [JsonPropertyName("calculatedFields")]
        public List<CalculatedFieldDefinition> CalculatedFields { get; set; } = new List<CalculatedFieldDefinition>();
        
        /// <summary>
        /// Pola generyczne bez logiki obliczeniowej
        /// </summary>
        [JsonPropertyName("genericFields")]
        public List<GenericFieldDefinition> GenericFields { get; set; } = new List<GenericFieldDefinition>();
        
        /// <summary>
        /// Reguły walidacji krzyżowej między polami
        /// </summary>
        [JsonPropertyName("crossFieldValidationRules")]
        public List<CrossFieldValidationRule>? CrossFieldValidationRules { get; set; }
    }

    #endregion

    #region Base Field Definition

    /// <summary>
    /// Bazowa klasa dla wszystkich definicji pól
    /// </summary>
    public abstract class BaseFieldDefinition
    {
        /// <summary>
        /// Unikalna nazwa pola (używana jako klucz)
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Etykieta wyświetlana użytkownikowi
        /// </summary>
        [JsonPropertyName("label")]
        public string Label { get; set; } = default!;
        
        /// <summary>
        /// Opis/podpowiedź dla pola
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        /// <summary>
        /// Wartość domyślna (jako string, interpretowana w zależności od typu)
        /// </summary>
        [JsonPropertyName("defaultValue")]
        public string? DefaultValue { get; set; }
        
        /// <summary>
        /// Kolejność wyświetlania pola
        /// </summary>
        [JsonPropertyName("order")]
        public int Order { get; set; }
        
        /// <summary>
        /// Czy pole jest wymagane
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; }
        
        /// <summary>
        /// Czy pole jest widoczne
        /// </summary>
        [JsonPropertyName("visible")]
        public bool Visible { get; set; } = true;
        
        /// <summary>
        /// Warunek wyświetlania pola (np. "otherField == 'value'")
        /// Pole widoczne tylko gdy warunek spełniony
        /// </summary>
        [JsonPropertyName("visibilityCondition")]
        public string? VisibilityCondition { get; set; }
        
        /// <summary>
        /// Warunek wymagalności pola (np. "includeInstallation == true")
        /// </summary>
        [JsonPropertyName("requiredCondition")]
        public string? RequiredCondition { get; set; }
        
        /// <summary>
        /// Rozszerzony tekst pomocy/tooltip (może zawierać HTML/Markdown)
        /// </summary>
        [JsonPropertyName("helpText")]
        public string? HelpText { get; set; }
        
        /// <summary>
        /// Link do dokumentacji zewnętrznej
        /// </summary>
        [JsonPropertyName("helpUrl")]
        public string? HelpUrl { get; set; }
        
        /// <summary>
        /// Nazwa sekcji/zakładki do grupowania pól
        /// Pola z tym samym groupName będą wyświetlane razem
        /// </summary>
        [JsonPropertyName("groupName")]
        public string? GroupName { get; set; }
        
        /// <summary>
        /// Nazwa ikony (np. "dollar-sign", "calendar", "tool")
        /// </summary>
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
        
        /// <summary>
        /// Kolor pola/labela (np. "#FF5733", "primary", "danger")
        /// </summary>
        [JsonPropertyName("color")]
        public string? Color { get; set; }
        
        /// <summary>
        /// Tagi/kategorie dla łatwiejszego wyszukiwania i filtrowania
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
        
        /// <summary>
        /// Dowolne metadata jako string (JSON serialized)
        /// Przechowuje dowolne dodatkowe dane w formacie JSON
        /// </summary>
        [JsonPropertyName("metadata")]
        public string? Metadata { get; set; }
    }

    #endregion

    #region Calculated Field Definition

    /// <summary>
    /// Definicja pola obliczeniowego z logiką cenową
    /// </summary>
    public class CalculatedFieldDefinition : BaseFieldDefinition
    {
        /// <summary>
        /// Typ pola obliczeniowego
        /// </summary>
        [JsonPropertyName("type")]
        public CalculatedFieldType Type { get; set; }
        
        /// <summary>
        /// Jednostka miary (np. "m²", "szt", "mb", "godz.")
        /// Używana dla pól typu Quantity
        /// </summary>
        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
        
        /// <summary>
        /// Format wyświetlania (np. "N2" dla dwóch miejsc po przecinku, "C" dla waluty)
        /// </summary>
        [JsonPropertyName("displayFormat")]
        public string? DisplayFormat { get; set; }
        
        /// <summary>
        /// Czy pole jest sortowalne
        /// </summary>
        [JsonPropertyName("sortable")]
        public bool Sortable { get; set; }
        
        /// <summary>
        /// Czy pole jest filtrowalne
        /// </summary>
        [JsonPropertyName("filterable")]
        public bool Filterable { get; set; }
        
        /// <summary>
        /// Czy pole jest sumowane
        /// </summary>
        [JsonPropertyName("summable")]
        public bool Summable { get; set; }
        
        /// <summary>
        /// Zakres sumowania (jeśli Summable = true)
        /// </summary>
        [JsonPropertyName("summaryScope")]
        public SummaryScope? SummaryScope { get; set; }
        
        /// <summary>
        /// Czy pole jest obliczane automatycznie
        /// </summary>
        [JsonPropertyName("autoCalculated")]
        public bool AutoCalculated { get; set; }
        
        /// <summary>
        /// Formuła obliczeniowa (np. "unitPriceNet * quantity")
        /// Używana gdy AutoCalculated = true
        /// </summary>
        [JsonPropertyName("calculationFormula")]
        public string? CalculationFormula { get; set; }
        
        /// <summary>
        /// Czy pole jest tylko do odczytu (typowo dla pól obliczanych)
        /// </summary>
        [JsonPropertyName("readOnly")]
        public bool ReadOnly { get; set; }
    }

    #endregion

    #region Generic Field Definition

    /// <summary>
    /// Definicja pola generycznego bez logiki obliczeniowej
    /// </summary>
    public class GenericFieldDefinition : BaseFieldDefinition
    {
        /// <summary>
        /// Typ pola generycznego
        /// </summary>
        [JsonPropertyName("type")]
        public GenericFieldType Type { get; set; }
        
        /// <summary>
        /// Format wyświetlania (np. "N2" dla liczb, "yyyy-MM-dd" dla dat)
        /// </summary>
        [JsonPropertyName("displayFormat")]
        public string? DisplayFormat { get; set; }
        
        /// <summary>
        /// Czy pole jest sortowalne
        /// </summary>
        [JsonPropertyName("sortable")]
        public bool Sortable { get; set; }
        
        /// <summary>
        /// Czy pole jest filtrowalne
        /// </summary>
        [JsonPropertyName("filterable")]
        public bool Filterable { get; set; }
        
        /// <summary>
        /// Minimalna wartość (dla pól numerycznych)
        /// </summary>
        [JsonPropertyName("minValue")]
        public decimal? MinValue { get; set; }
        
        /// <summary>
        /// Maksymalna wartość (dla pól numerycznych)
        /// </summary>
        [JsonPropertyName("maxValue")]
        public decimal? MaxValue { get; set; }
        
        /// <summary>
        /// Minimalna długość (dla pól tekstowych)
        /// </summary>
        [JsonPropertyName("minLength")]
        public int? MinLength { get; set; }
        
        /// <summary>
        /// Maksymalna długość (dla pól tekstowych)
        /// </summary>
        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }
        
        /// <summary>
        /// Wyrażenie regularne dla walidacji (dla pól tekstowych)
        /// </summary>
        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }
        
        /// <summary>
        /// Dozwolone wartości (dla pól tekstowych jako enum)
        /// </summary>
        [JsonPropertyName("allowedValues")]
        public List<string>? AllowedValues { get; set; }
        
        /// <summary>
        /// Placeholder/wskazówka dla pola
        /// </summary>
        [JsonPropertyName("placeholder")]
        public string? Placeholder { get; set; }
        
        /// <summary>
        /// Definicje pól zagnieżdżonych (używane gdy Type = Collection)
        /// </summary>
        [JsonPropertyName("nestedFields")]
        public GenericFieldCollectionDefinition? NestedFields { get; set; }
    }

    /// <summary>
    /// Definicja kolekcji zagnieżdżonych pól
    /// </summary>
    public class GenericFieldCollectionDefinition
    {
        /// <summary>
        /// Pola obliczeniowe w kolekcji
        /// </summary>
        [JsonPropertyName("calculatedFields")]
        public List<CalculatedFieldDefinition>? CalculatedFields { get; set; }
        
        /// <summary>
        /// Pola generyczne w kolekcji
        /// </summary>
        [JsonPropertyName("genericFields")]
        public List<GenericFieldDefinition>? GenericFields { get; set; }
        
        /// <summary>
        /// Minimalna liczba elementów w kolekcji
        /// </summary>
        [JsonPropertyName("minItems")]
        public int? MinItems { get; set; }
        
        /// <summary>
        /// Maksymalna liczba elementów w kolekcji
        /// </summary>
        [JsonPropertyName("maxItems")]
        public int? MaxItems { get; set; }
        
        /// <summary>
        /// Czy kolekcja jest typu "wybierz jedną opcję"
        /// Jeśli true, użytkownik może zaznaczyć (IsSelected) tylko jeden element z kolekcji
        /// Używane np. dla wariantów wykończenia, gdzie użytkownik wybiera jedną opcję spośród wielu
        /// </summary>
        [JsonPropertyName("isSelectableCollection")]
        public bool IsSelectableCollection { get; set; }
        
        /// <summary>
        /// Czy sumować pola obliczeniowe wewnątrz tej kolekcji
        /// Jeśli true, wartości z pól calculated będą sumowane (np. suma cen wszystkich opcji)
        /// Jeśli false, pola calculated w kolekcji nie będą sumowane
        /// </summary>
        [JsonPropertyName("enableCalculatedFieldsSummation")]
        public bool EnableCalculatedFieldsSummation { get; set; }
        
        /// <summary>
        /// Lista nazw pól obliczeniowych z kolekcji, które mają być sumowane
        /// Uwzględniane tylko gdy EnableCalculatedFieldsSummation = true
        /// Jeśli null lub pusta, sumowane będą wszystkie pola z Summable = true
        /// Dla IsSelectableCollection=true sumowane będą tylko wartości z zaznaczonego elementu
        /// </summary>
        [JsonPropertyName("summableCalculatedFields")]
        public List<string>? SummableCalculatedFields { get; set; }
        
        /// <summary>
        /// Konfiguracja UI dla pól w kolekcji - układ i szerokości kolumn
        /// Definiuje jak wyświetlać pola wewnątrz elementów kolekcji
        /// Jeśli null, używany jest domyślny układ z field.order i field.visible
        /// </summary>
        [JsonPropertyName("uiConfiguration")]
        public CostEstimateUiConfiguration? UiConfiguration { get; set; }
    }

    #endregion

    #region Cross-Field Validation

    /// <summary>
    /// Reguła walidacji krzyżowej między polami
    /// </summary>
    public class CrossFieldValidationRule
    {
        /// <summary>
        /// Unikalna nazwa reguły
        /// </summary>
        [JsonPropertyName("ruleName")]
        public string RuleName { get; set; } = default!;
        
        /// <summary>
        /// Wyrażenie logiczne do walidacji (np. "endDate >= startDate")
        /// </summary>
        [JsonPropertyName("expression")]
        public string Expression { get; set; } = default!;
        
        /// <summary>
        /// Komunikat błędu wyświetlany gdy walidacja nie powiedzie się
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = default!;
        
        /// <summary>
        /// Czy reguła jest aktywna
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }

    #endregion

    #region Summary Configuration

    /// <summary>
    /// Konfiguracja podsumowania kosztorysu - określa które pola mają być sumowane
    /// </summary>
    public class CostEstimateSummaryConfiguration
    {
        /// <summary>
        /// Lista nazw pól obliczeniowych, które mają być sumowane w grupach
        /// </summary>
        [JsonPropertyName("groupSummaryFields")]
        public List<string> GroupSummaryFields { get; set; } = new List<string>();
        
        /// <summary>
        /// Lista nazw pól obliczeniowych, które mają być sumowane w całym kosztorysie
        /// </summary>
        [JsonPropertyName("totalSummaryFields")]
        public List<string> TotalSummaryFields { get; set; } = new List<string>();
        
        /// <summary>
        /// Czy wyświetlać podsumowanie grup
        /// </summary>
        [JsonPropertyName("showGroupSummary")]
        public bool ShowGroupSummary { get; set; }
        
        /// <summary>
        /// Czy wyświetlać podsumowanie całkowite
        /// </summary>
        [JsonPropertyName("showTotalSummary")]
        public bool ShowTotalSummary { get; set; }
    }

    #endregion

    #region UI Configuration

    /// <summary>
    /// Konfiguracja UI - układ i szerokości kolumn
    /// </summary>
    public class CostEstimateUiConfiguration
    {
        /// <summary>
        /// Układ kolumn - lista nazw pól w kolejności wyświetlania
        /// Tylko pola obecne na tej liście będą widoczne
        /// Jeśli null lub pusta, używany jest domyślny układ z field.order i field.visible
        /// </summary>
        [JsonPropertyName("columnLayout")]
        public List<string>? ColumnLayout { get; set; }
        
        /// <summary>
        /// Szerokości kolumn - nazwa pola => szerokość (np. "200px", "15%", "auto")
        /// Jeśli pole nie jest w słowniku, używana jest automatyczna szerokość
        /// </summary>
        [JsonPropertyName("columnWidths")]
        public Dictionary<string, string>? ColumnWidths { get; set; }
    }

    #endregion
}
