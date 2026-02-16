using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;

namespace Business.Interfaces.Services;

/// <summary>
/// Zintegrowany serwis do zarządzania, cachowania i kontroli dostępu do plików projektów
/// </summary>
public interface IProjectFilesService
{
    #region File Access Methods

    /// <summary>
    /// Zwraca IDs paczek do których user ma dostęp w projekcie zgodnie z ResourceScope
    /// </summary>
    Task<HashSet<Guid>> GetAccessiblePackageIdsAsync(
        ICurrentUser currentUser,
        Guid projectId,
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca słownik: PackageId -> Liczba dostępnych plików dla użytkownika zgodnie z ResourceScope
    /// </summary>
    Task<Dictionary<Guid, int>> GetAccessibleFileCountsAsync(
        ICurrentUser currentUser,
        HashSet<Guid> packageIds,
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca informacje o dostępie do plików w paczce zgodnie z ResourceScope
    /// </summary>
    Task<PackageAccessInfo> GetPackageAccessInfoAsync(
        ICurrentUser currentUser,
        Guid packageId,
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza czy user ma dostęp do konkretnego pliku zgodnie z ResourceScope
    /// </summary>
    Task<bool> HasAccessToFileAsync(
        ICurrentUser currentUser,
        Guid packageId,
        Guid fileId,
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Buduje słownik użytkowników mających dostęp do plików w paczce
    /// Uwzględnia Package + Allow/Deny model
    /// </summary>
    Task<Dictionary<Guid, List<Guid>>> GetSharedWithUsersAsync(
        Guid packageId,
        HashSet<Guid> fileIds,
        CancellationToken cancellationToken = default);

    #endregion

    #region File Data Methods

    /// <summary>
    /// Pobiera wszystkie paczki plików dla projektu jako słownik [PackageId -> PackageDto]
    /// </summary>
    Task<Dictionary<Guid, ProjectFilePackageDto>> GetProjectFilePackagesAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie pliki dla projektu pogrupowane według paczek jako słownik [PackageId -> Lista plików]
    /// </summary>
    Task<Dictionary<Guid, List<ProjectFileCacheDto>>> GetProjectPackageFilesAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie wersje plików dla projektu pogrupowane według plików jako słownik [FileId -> Lista wersji]
    /// </summary>
    Task<Dictionary<Guid, List<ProjectFileVersionDto>>> GetProjectFilesVersionsAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie komentarze dla wersji plików projektu pogrupowane według wersji jako słownik [VersionId -> Lista komentarzy]
    /// </summary>
    Task<Dictionary<Guid, List<ProjectFileVersionCommentDto>>> GetProjectFileVersionsCommentsAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wybrane wersje plików z cache
    /// </summary>
    Task<ProjectFileVersionsResult> GetVersionsByIdsAsync(
        Guid tenantId,
        Guid projectId,
        HashSet<Guid> versionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera SAS URI dla wielu wersji plików jednocześnie z cachowaniem
    /// </summary>
    Task<Dictionary<Guid, FileVersionSasUriInfo>> GetFileVersionsSasUrisAsync(
        Guid tenantId,
        Guid projectId,
        params Guid[] versionIds);

    #endregion

    #region Cache Invalidation Methods

    /// <summary>
    /// Invaliduje cache plików, paczek i wersji dla projektu
    /// </summary>
    Task InvalidateProjectFilesCacheAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invaliduje cache wersji plików dla projektu
    /// </summary>
    Task InvalidateProjectVersionsCacheAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invaliduje cache komentarzy dla projektu
    /// </summary>
    Task InvalidateProjectCommentsCacheAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invaliduje cache dostępu do plików dla projektu
    /// </summary>
    Task InvalidateFileAccessCacheAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invaliduje cache SAS URI dla konkretnej wersji pliku
    /// </summary>
    Task InvalidateVersionSasUriAsync(Guid versionId, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Wynik pobierania wersji plików
/// </summary>
public record ProjectFileVersionsResult
{
    /// <summary>
    /// Słownik wersji zaindeksowany według ID wersji
    /// </summary>
    public Dictionary<Guid, ProjectFileVersionDto> Versions { get; init; } = new();

    /// <summary>
    /// Zbiór unikalnych ID użytkowników którzy stworzyli wersje
    /// </summary>
    public HashSet<Guid> CreatedByUserIds { get; init; } = new();
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
    /// </summary>
    public HashSet<Guid> ExcludedFileIds { get; init; } = new();

    /// <summary>
    /// IDs plików dozwolonych (Allow) - gdy IsPackageShared = false
    /// </summary>
    public HashSet<Guid> AllowedFileIds { get; init; } = new();
}
