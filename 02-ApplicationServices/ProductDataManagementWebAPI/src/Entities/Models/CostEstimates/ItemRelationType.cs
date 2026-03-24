namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Typ relacji pozycji do pozycji nadrzędnej (ParentItem)
    /// Określa czy pozycja jest opcją czy komponentem
    /// </summary>
    public enum ItemRelationType
    {
        /// <summary>
        /// Pozycja główna - nie jest ani opcją ani komponentem (ParentItemId == null)
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Pozycja jest opcją (wariantem) pozycji nadrzędnej lub komponentu
        /// Istnieje gdy parent ma pole ItemSystemOptions w FieldValues
        /// </summary>
        Option = 1,
        
        /// <summary>
        /// Pozycja jest komponentem (składnikiem) pozycji nadrzędnej
        /// Np. robocizna, materiał, koszty stałe
        /// Pozycja nadrzędna z komponentami nie może mieć FieldValues
        /// </summary>
        Component = 2
    }
}
