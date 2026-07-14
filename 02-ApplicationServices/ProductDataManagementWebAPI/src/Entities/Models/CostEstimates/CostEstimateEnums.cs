namespace Entities.Models.CostEstimates
{
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

    /// <summary>
    /// Typ pola dodatkowego w kosztorysie (płaska struktura, bez FieldDefinition)
    /// </summary>
    public enum AdditionalFieldType
    {
        String = 0,
        Decimal = 1,
        Boolean = 2,
        DateTime = 3
    }

    /// <summary>
    /// Typ kolumny w schemacie kosztorysu — pola dodatkowe (0–9) i podstawowe (100+).
    /// </summary>
    public enum CostEstimateFieldType
    {
        Text = 0,
        Number = 1,
        Boolean = 2,
        Date = 3,
        Select = 4,

        Name = 100,
        Quantity = 101,
        Unit = 102,
        UnitPriceNet = 103,
        VatRate = 104,
        UnitPriceGross = 105,
        NetValue = 106,
        GrossValue = 107,
        VatValue = 108,
        IsSelected = 109,
        IsStageWork = 110,
        Files = 111,
        Actions = 112,
        ItemSystemOptions = 113,
    }
}
