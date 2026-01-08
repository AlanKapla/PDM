using System.Text.Json.Serialization;

namespace Entities.Models.CostEstimateData
{
    /// <summary>
    /// Root model dla wypełnionych danych kosztorysu
    /// Odpowiada strukturze zdefiniowanej w CostEstimateTemplateStructure
    /// </summary>
    public class CostEstimateDataModel
    {
        /// <summary>
        /// Lista grup w kosztorysie (hierarchiczna struktura)
        /// </summary>
        [JsonPropertyName("groups")]
        public List<CostEstimateGroup> Groups { get; set; } = new();
        
        /// <summary>
        /// Obliczone sumy dla całego kosztorysu
        /// </summary>
        [JsonPropertyName("totals")]
        public Dictionary<string, decimal>? Totals { get; set; }
        
        /// <summary>
        /// Metadata kosztorysu
        /// </summary>
        [JsonPropertyName("metadata")]
        public CostEstimateMetadata? Metadata { get; set; }
    }
    
    /// <summary>
    /// Grupa w kosztorysie (hierarchiczna)
    /// </summary>
    public class CostEstimateGroup
    {
        /// <summary>
        /// Unikalny ID grupy w kosztorysie
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;
        
        /// <summary>
        /// ID grupy nadrzędnej (null dla grup głównych)
        /// </summary>
        [JsonPropertyName("parentId")]
        public string? ParentId { get; set; }
        
        /// <summary>
        /// Poziom zagnieżdżenia (0 = główny poziom)
        /// </summary>
        [JsonPropertyName("level")]
        public int Level { get; set; }
        
        /// <summary>
        /// Numer grupy (automatyczny lub ręczny)
        /// </summary>
        [JsonPropertyName("number")]
        public string? Number { get; set; }
        
        /// <summary>
        /// Kolejność w obrębie poziomu
        /// </summary>
        [JsonPropertyName("order")]
        public int Order { get; set; }
        
        /// <summary>
        /// Wartości pól nagłówka grupy
        /// Key = GroupHeaderFieldType (jako string), Value = wartość
        /// </summary>
        [JsonPropertyName("headerValues")]
        public Dictionary<string, object?> HeaderValues { get; set; } = new();
        
        /// <summary>
        /// Zakresy robót w tej grupie
        /// </summary>
        [JsonPropertyName("workScopes")]
        public List<CostEstimateWorkScope> WorkScopes { get; set; } = new();
        
        /// <summary>
        /// Podgrupy (zagnieżdżone)
        /// </summary>
        [JsonPropertyName("subGroups")]
        public List<CostEstimateGroup>? SubGroups { get; set; }
        
        /// <summary>
        /// Obliczone sumy dla grupy
        /// </summary>
        [JsonPropertyName("groupTotals")]
        public Dictionary<string, decimal>? GroupTotals { get; set; }
    }
    
    /// <summary>
    /// Zakres robót w grupie
    /// </summary>
    public class CostEstimateWorkScope
    {
        /// <summary>
        /// Unikalny ID zakresu robót
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;
        
        /// <summary>
        /// Kolejność
        /// </summary>
        [JsonPropertyName("order")]
        public int Order { get; set; }
        
        /// <summary>
        /// ID użytkownika przypisanego do tego zakresu robót
        /// </summary>
        [JsonPropertyName("assignedUserId")]
        public string? AssignedUserId { get; set; }
        
        /// <summary>
        /// Wartości pól obliczeniowych
        /// Key = nazwa pola (field.Name), Value = wartość
        /// </summary>
        [JsonPropertyName("calculatedFieldValues")]
        public Dictionary<string, object?> CalculatedFieldValues { get; set; } = new();
        
        /// <summary>
        /// Wartości pól generycznych
        /// Key = nazwa pola (field.Name), Value = wartość
        /// </summary>
        [JsonPropertyName("genericFieldValues")]
        public Dictionary<string, object?> GenericFieldValues { get; set; } = new();
        
        /// <summary>
        /// Wartości pól kolekcji
        /// Key = nazwa pola collection, Value = lista elementów
        /// </summary>
        [JsonPropertyName("collectionFieldValues")]
        public Dictionary<string, List<CostEstimateCollectionItem>>? CollectionFieldValues { get; set; }
    }
    
    /// <summary>
    /// Element w kolekcji zagnieżdżonej
    /// </summary>
    public class CostEstimateCollectionItem
    {
        /// <summary>
        /// Unikalny ID elementu w kolekcji
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;
        
        /// <summary>
        /// Czy ten element jest zaznaczony (selected)
        /// Używane gdy kolekcja ma IsSelectableCollection=true
        /// Tylko jeden element może mieć IsSelected=true
        /// </summary>
        [JsonPropertyName("isSelected")]
        public bool IsSelected { get; set; }
        
        /// <summary>
        /// Wartości pól obliczeniowych w kolekcji
        /// </summary>
        [JsonPropertyName("calculatedFieldValues")]
        public Dictionary<string, object?>? CalculatedFieldValues { get; set; }
        
        /// <summary>
        /// Wartości pól generycznych w kolekcji
        /// </summary>
        [JsonPropertyName("genericFieldValues")]
        public Dictionary<string, object?>? GenericFieldValues { get; set; }
    }
    
    /// <summary>
    /// Metadata kosztorysu - tylko user-specific customizacje
    /// </summary>
    public class CostEstimateMetadata
    {
        /// <summary>
        /// Data ostatniej modyfikacji danych
        /// </summary>
        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }
        
        /// <summary>
        /// ID użytkownika który ostatnio modyfikował
        /// </summary>
        [JsonPropertyName("lastModifiedBy")]
        public Guid? LastModifiedBy { get; set; }
        
        /// <summary>
        /// Wersja schematu/struktury
        /// </summary>
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; }
        
        /// <summary>
        /// Dodatkowe informacje
        /// </summary>
        [JsonPropertyName("additionalInfo")]
        public Dictionary<string, string>? AdditionalInfo { get; set; }
        
        /// <summary>
        /// Customizacje UI dla grup (kolory nagłówków, ikony, stan zwinięcia)
        /// Key: group.Id, Value: customizacja
        /// User-specific overrides - nie strukturalna konfiguracja
        /// </summary>
        [JsonPropertyName("groupCustomizations")]
        public Dictionary<string, GroupUiCustomization>? GroupCustomizations { get; set; }
        
        /// <summary>
        /// Customizacje UI dla work scopes (kolory wierszy, wyróżnienia)
        /// Key: workScope.Id, Value: customizacja
        /// User-specific overrides - nie strukturalna konfiguracja
        /// </summary>
        [JsonPropertyName("workScopeCustomizations")]
        public Dictionary<string, WorkScopeUiCustomization>? WorkScopeCustomizations { get; set; }
    }
    
    /// <summary>
    /// Customizacja UI dla grupy - user-specific overrides
    /// </summary>
    public class GroupUiCustomization
    {
        /// <summary>
        /// Kolor nagłówka grupy (np. "#FF5733", "red", "primary")
        /// Override koloru z szablonu dla tego konkretnego kosztorysu
        /// </summary>
        [JsonPropertyName("headerColor")]
        public string? HeaderColor { get; set; }
        
        /// <summary>
        /// Kolor tła nagłówka
        /// </summary>
        [JsonPropertyName("headerBackgroundColor")]
        public string? HeaderBackgroundColor { get; set; }
        
        /// <summary>
        /// Ikona grupy (override z szablonu)
        /// </summary>
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
        
        /// <summary>
        /// Czy grupa jest zwinięta - stan UI użytkownika
        /// </summary>
        [JsonPropertyName("collapsed")]
        public bool? Collapsed { get; set; }
        
        /// <summary>
        /// Wyróżnienie (highlight) grupy - oznaczenie użytkownika
        /// </summary>
        [JsonPropertyName("highlighted")]
        public bool? Highlighted { get; set; }
        
        /// <summary>
        /// Notatki/komentarze użytkownika do grupy
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
    
    /// <summary>
    /// Customizacja UI dla work scope - user-specific overrides
    /// </summary>
    public class WorkScopeUiCustomization
    {
        /// <summary>
        /// Kolor wiersza (np. "#FFE5E5" dla czerwonego tła)
        /// User marking - nie strukturalna konfiguracja
        /// </summary>
        [JsonPropertyName("rowColor")]
        public string? RowColor { get; set; }
        
        /// <summary>
        /// Kolor tekstu w wierszu
        /// </summary>
        [JsonPropertyName("textColor")]
        public string? TextColor { get; set; }
        
        /// <summary>
        /// Wyróżnienie (highlight) wiersza - oznaczenie użytkownika
        /// </summary>
        [JsonPropertyName("highlighted")]
        public bool? Highlighted { get; set; }
        
        /// <summary>
        /// Znaczniki/tagi (np. "ważne", "do weryfikacji", "problem")
        /// User-specific tags dla tego wiersza
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
        
        /// <summary>
        /// Notatki/komentarze użytkownika do work scope
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
