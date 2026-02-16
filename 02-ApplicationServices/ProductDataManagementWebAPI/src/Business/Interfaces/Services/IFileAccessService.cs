using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Serwis sprawdzający dostęp do plików i paczek w modelu Package + Allow/Deny z cachowaniem per user i scope
    /// </summary>
    public interface IFileAccessService
    {
        /// <summary>
        /// Zwraca IDs paczek do których user ma dostęp w projekcie zgodnie z ResourceScope
        /// </summary>
        /// <param name="currentUser">Aktualnie zalogowany użytkownik</param>
        /// <param name="projectId">ID projektu</param>
        /// <param name="resourceScope">Zakres zasobów (All/Mine/Shared)</param>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>Zbiór ID paczek dostępnych dla użytkownika</returns>
        Task<HashSet<Guid>> GetAccessiblePackageIdsAsync(
            ICurrentUser currentUser,
            Guid projectId,
            ResourceScope resourceScope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca informacje o dostępie do plików w paczce zgodnie z ResourceScope
        /// </summary>
        /// <param name="currentUser">Aktualnie zalogowany użytkownik</param>
        /// <param name="packageId">ID paczki</param>
        /// <param name="resourceScope">Zakres zasobów (All/Mine/Shared)</param>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>Informacje o dostępie do paczki</returns>
        Task<PackageAccessInfo> GetPackageAccessInfoAsync(
            ICurrentUser currentUser,
            Guid packageId,
            ResourceScope resourceScope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sprawdza czy user ma dostęp do konkretnego pliku zgodnie z ResourceScope
        /// </summary>
        /// <param name="currentUser">Aktualnie zalogowany użytkownik</param>
        /// <param name="packageId">ID paczki</param>
        /// <param name="fileId">ID pliku</param>
        /// <param name="resourceScope">Zakres zasobów (All/Mine/Shared)</param>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>True jeśli użytkownik ma dostęp do pliku</returns>
        Task<bool> HasAccessToFileAsync(
            ICurrentUser currentUser,
            Guid packageId,
            Guid fileId,
            ResourceScope resourceScope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca słownik: PackageId -> Liczba dostępnych plików dla użytkownika zgodnie z ResourceScope
        /// </summary>
        /// <param name="currentUser">Aktualnie zalogowany użytkownik</param>
        /// <param name="packageIds">Zbiór ID paczek do sprawdzenia</param>
        /// <param name="resourceScope">Zakres zasobów (All/Mine/Shared)</param>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>Słownik z liczbą dostępnych plików per paczka</returns>
        Task<Dictionary<Guid, int>> GetAccessibleFileCountsAsync(
            ICurrentUser currentUser,
            HashSet<Guid> packageIds,
            ResourceScope resourceScope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Buduje słownik użytkowników mających dostęp do plików w paczce
        /// Uwzględnia Package + Allow/Deny model
        /// </summary>
        /// <param name="packageId">ID paczki</param>
        /// <param name="fileIds">Zbiór ID plików do sprawdzenia</param>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>Słownik [FileId -> Lista UserIds którzy mają dostęp]</returns>
        Task<Dictionary<Guid, List<Guid>>> GetSharedWithUsersAsync(
            Guid packageId,
            HashSet<Guid> fileIds,
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




