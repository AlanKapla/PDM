using Business.Interfaces.DTO;

namespace Business.Interfaces.Services;

/// <summary>
/// Serwis do zarządzania i cachowania danych plików projektów
/// </summary>
public interface IProjectFilesService
{
    /// <summary>
    /// Pobiera wszystkie paczki plików dla projektu jako słownik [PackageId -> PackageDto]
    /// </summary>
    /// <param name="tenantId">ID tenanta</param>
    /// <param name="projectId">ID projektu</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Słownik paczek zaindeksowany według ID paczki</returns>
    Task<Dictionary<Guid, ProjectFilePackageDto>> GetProjectFilePackagesAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie pliki dla projektu pogrupowane według paczek jako słownik [PackageId -> Lista plików]
    /// </summary>
    /// <param name="tenantId">ID tenanta</param>
    /// <param name="projectId">ID projektu</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Słownik list plików zaindeksowany według ID paczki</returns>
    Task<Dictionary<Guid, List<ProjectFileCacheDto>>> GetProjectPackageFilesAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie wersje plików dla projektu pogrupowane według plików jako słownik [FileId -> Lista wersji]
    /// </summary>
    /// <param name="tenantId">ID tenanta</param>
    /// <param name="projectId">ID projektu</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Słownik list wersji zaindeksowany według ID pliku</returns>
    Task<Dictionary<Guid, List<ProjectFileVersionDto>>> GetProjectFilesVersionsAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie komentarze dla wersji plików projektu pogrupowane według wersji jako słownik [VersionId -> Lista komentarzy]
    /// </summary>
    /// <param name="tenantId">ID tenanta</param>
    /// <param name="projectId">ID projektu</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Słownik list komentarzy zaindeksowany według ID wersji</returns>
    Task<Dictionary<Guid, List<ProjectFileVersionCommentDto>>> GetProjectFileVersionsCommentsAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wybrane wersje plików z cache
    /// </summary>
    /// <param name="tenantId">ID tenanta</param>
    /// <param name="projectId">ID projektu</param>
    /// <param name="versionIds">Zbiór ID wersji do pobrania</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Słownik wersji zaindeksowany według ID wersji wraz z CreatedByUserIds</returns>
    Task<ProjectFileVersionsResult> GetVersionsByIdsAsync(
        Guid tenantId,
        Guid projectId,
        HashSet<Guid> versionIds,
        CancellationToken cancellationToken = default);
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
