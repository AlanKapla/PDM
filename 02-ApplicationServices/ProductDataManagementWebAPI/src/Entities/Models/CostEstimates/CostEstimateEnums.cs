namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Zakres pola w szablonie kosztorysu - określa do czego pole należy
    /// </summary>
    public enum FieldScope
    {
        /// <summary>
        /// Pole należy do nagłówka grupy
        /// </summary>
        Group = 0,
        
        /// <summary>
        /// Pole systemowe pozycji (work scope)
        /// </summary>
        ItemSystem = 1,
        
        /// <summary>
        /// Pole obliczeniowe pozycji (work scope)
        /// </summary>
        ItemCalculated = 2,
        
        /// <summary>
        /// Pole generyczne pozycji (work scope)
        /// </summary>
        ItemGeneric = 3
    }

    /// <summary>
    /// Ujednolicony typ pola w szablonie kosztorysu
    /// Łączy wszystkie typy pól z odpowiednimi prefiksami dla czytelności
    /// </summary>
    public enum FieldType
    {
        // ============================================================================
        // GROUP HEADER FIELDS (Prefix: Group)
        // ============================================================================
        
        /// <summary>
        /// Nazwa grupy (string) - zawsze wymagana
        /// Scope: Group
        /// </summary>
        GroupName = 0,
        
        /// <summary>
        /// Opis grupy (string)
        /// Scope: Group
        /// </summary>
        GroupDescription = 1,
        
        /// <summary>
        /// Numer grupy (string) - automatycznie generowany lub ręczny
        /// Scope: Group
        /// </summary>
        GroupNumber = 2,
        
        /// <summary>
        /// Data rozpoczęcia (DateTime)
        /// Scope: Group
        /// </summary>
        GroupStartDate = 3,
        
        /// <summary>
        /// Data zakończenia (DateTime)
        /// Scope: Group
        /// </summary>
        GroupEndDate = 4,
        
        /// <summary>
        /// Status grupy (string)
        /// Scope: Group
        /// </summary>
        GroupStatus = 5,
        
        /// <summary>
        /// Uwagi do grupy (string)
        /// Scope: Group
        /// </summary>
        GroupNotes = 6,
        
        /// <summary>
        /// Odpowiedzialny (string)
        /// Scope: Group
        /// </summary>
        GroupResponsible = 7,
        
        /// <summary>
        /// Budżet grupy (decimal)
        /// Scope: Group
        /// </summary>
        GroupBudget = 8,
        
        /// <summary>
        /// Priorytet (int) - np. 1-5
        /// Scope: Group
        /// </summary>
        GroupPriority = 9,

        // ============================================================================
        // ITEM SYSTEM FIELDS (Prefix: ItemSystem)
        // Range: 100-199
        // ============================================================================
        
        /// <summary>
        /// Nazwa pozycji (string) - wymagane
        /// Scope: ItemSystem
        /// </summary>
        ItemSystemName = 100,
        
        /// <summary>
        /// Ilość (decimal)
        /// Scope: ItemSystem
        /// </summary>
        ItemSystemQuantity = 101,
        
        /// <summary>
        /// Jednostka miary (string)
        /// Scope: ItemSystem
        /// </summary>
        ItemSystemUnit = 102,
        
        /// <summary>
        /// Opcje (collection) - kolekcja wariantów/opcji dla pozycji
        /// Pozycja z tym polem może mieć zagnieżdżone pod-pozycje (opcje)
        /// Opcja NIE MOŻE mieć kolejnych opcji (max 1 poziom zagnieżdżenia)
        /// Scope: ItemSystem
        /// </summary>
        ItemSystemOptions = 103,
        
        /// <summary>
        /// Zaznaczenie (bool) - czy pozycja/opcja jest wybrana
        /// Używane do zaznaczania aktywnej opcji w kolekcji opcji
        /// Scope: ItemSystem
        /// </summary>
        ItemSystemSelected = 104,

        /// <summary>
        /// Pliki (collection) - kolekcja załączonych plików (PDF, JPG)
        /// Pliki przechowywane w Azure Blob Storage, max 50 MB na plik
        /// Scope: ItemSystem
        /// </summary>
        ItemSystemFiles = 105,

        /// <summary>
        /// Kategoria pozycji (string) - wybrana z listy kategorii szablonu lub wpisana ręcznie
        /// Scope: ItemSystem
        /// </summary>
        ItemSystemCategory = 106,

        // ============================================================================
        // ITEM CALCULATED FIELDS (Prefix: ItemCalculated)
        // Range: 200-299
        // ============================================================================
        
        /// <summary>
        /// Cena jednostkowa netto (decimal)
        /// Scope: ItemCalculated
        /// </summary>
        ItemCalculatedUnitPriceNet = 200,
        
        /// <summary>
        /// Stawka VAT (decimal, zakres 0–1, gdzie 0.23 = 23%)
        /// Scope: ItemCalculated
        /// </summary>
        ItemCalculatedVatRate = 201,
        
        /// <summary>
        /// Cena jednostkowa brutto (decimal)
        /// Formula: UnitPriceNet * (1 + VatRate)
        /// Scope: ItemCalculated
        /// </summary>
        ItemCalculatedUnitPriceGross = 202,
        
        /// <summary>
        /// Wartość netto (decimal)
        /// Formula: UnitPriceNet * Quantity
        /// Scope: ItemCalculated
        /// </summary>
        ItemCalculatedValueNet = 203,
        
        /// <summary>
        /// Wartość brutto (decimal)
        /// Formula: UnitPriceGross * Quantity
        /// Scope: ItemCalculated
        /// </summary>
        ItemCalculatedValueGross = 204,
        
        /// <summary>
        /// Wartość VAT jednostkowa (decimal)
        /// Formula: UnitPriceNet * VatRate
        /// Scope: ItemCalculated
        /// </summary>
        ItemCalculatedUnitVat = 205,
        
        /// <summary>
        /// Wartość VAT całkowita (decimal)
        /// Formula: ValueNet * VatRate
        /// Scope: ItemCalculated
        /// </summary>
        ItemCalculatedTotalVat = 206,

        // ============================================================================
        // ITEM GENERIC FIELDS (Prefix: ItemGeneric)
        // Range: 300-399
        // ============================================================================
        
        /// <summary>
        /// Liczba całkowita
        /// Scope: ItemGeneric
        /// </summary>
        ItemGenericNumber = 300,
        
        /// <summary>
        /// Ciąg znaków
        /// Scope: ItemGeneric
        /// </summary>
        ItemGenericString = 301,
        
        /// <summary>
        /// Wartość logiczna (true/false)
        /// Scope: ItemGeneric
        /// </summary>
        ItemGenericBoolean = 302,
        
        /// <summary>
        /// Data
        /// Scope: ItemGeneric
        /// </summary>
        ItemGenericDate = 303,
        
        /// <summary>
        /// Data i czas
        /// Scope: ItemGeneric
        /// </summary>
        ItemGenericDateTime = 304,
    }

    /// <summary>
    /// Status kosztorysu
    /// </summary>
    public enum CostEstimateStatus
    {
        /// <summary>
        /// Wersja robocza
        /// </summary>
        Draft = 0,
        
        /// <summary>
        /// W trakcie wypełniania
        /// </summary>
        InProgress = 1,
        
        /// <summary>
        /// Gotowy do przeglądu
        /// </summary>
        ReadyForReview = 2,
        
        /// <summary>
        /// Zatwierdzony
        /// </summary>
        Approved = 3,
        
        /// <summary>
        /// Odrzucony
        /// </summary>
        Rejected = 4,
        
        /// <summary>
        /// Zarchiwizowany
        /// </summary>
        Archived = 5
    }
}
