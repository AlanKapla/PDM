namespace Entities.Models.Files
{
    /// <summary>
    /// Typ dostępu do pliku w ramach udostępnienia
    /// </summary>
    public enum ProjectFileAccess
    {
        /// <summary>
        /// Dostęp zabroniony - plik jest wykluczony z udostępnionej paczki
        /// </summary>
        Deny = 0,
        
        /// <summary>
        /// Dostęp dozwolony - plik jest jawnie udostępniony
        /// </summary>
        Allow = 1
    }
}
