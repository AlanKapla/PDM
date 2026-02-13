namespace Business.Interfaces.Services
{
    /// <summary>
    /// Serwis sprawdzający dostęp do plików i paczek w modelu Package + Allow/Deny
    /// </summary>
    public interface IFileAccessService
    {
        /// <summary>
        /// Zwraca IDs paczek do których user ma dostęp w projekcie
        /// </summary>
        Task<HashSet<Guid>> GetAccessiblePackageIdsAsync(
            Guid userId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca informacje o dostępie do plików w paczce
        /// Jeśli IsPackageShared = true → ExcludedFileIds zawiera pliki z Deny
        /// Jeśli IsPackageShared = false → AllowedFileIds zawiera pliki z Allow
        /// </summary>
        Task<PackageAccessInfo> GetPackageAccessInfoAsync(
            Guid userId,
            Guid packageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sprawdza czy user ma dostęp do konkretnego pliku
        /// </summary>
        Task<bool> HasAccessToFileAsync(
            Guid userId,
            Guid packageId,
            Guid fileId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca słownik: PackageId -> Liczba dostępnych plików dla użytkownika
        /// Uwzględnia logikę Allow/Deny:
        /// - Paczka udostępniona → wszystkie pliki OPRÓCZ wykluczeń (Deny)
        /// - Paczka NIE udostępniona → tylko pliki z jawnym Allow
        /// </summary>
        Task<Dictionary<Guid, int>> GetAccessibleFileCountsAsync(
            Guid userId,
            HashSet<Guid> packageIds,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Informacje o dostępie do paczki
    /// </summary>
    public record PackageAccessInfo
    {
        /// <summary>
        /// Czy cała paczka jest udostępniona (FileId=null, Access=Allow)
        /// </summary>
        public bool IsPackageShared { get; init; }

        /// <summary>
        /// IDs plików wykluczonych (Deny) - gdy IsPackageShared = true
        /// Handler: pobierz wszystkie pliki WHERE NOT IN (ExcludedFileIds)
        /// </summary>
        public HashSet<Guid> ExcludedFileIds { get; init; } = new();

        /// <summary>
        /// IDs plików dozwolonych (Allow) - gdy IsPackageShared = false
        /// Handler: pobierz tylko pliki WHERE IN (AllowedFileIds)
        /// </summary>
        public HashSet<Guid> AllowedFileIds { get; init; } = new();
    }
}




