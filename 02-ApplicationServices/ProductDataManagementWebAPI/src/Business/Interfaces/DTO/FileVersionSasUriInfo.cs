namespace Business.Interfaces.DTO;

/// <summary>
/// Informacje o SAS URI dla wersji pliku z TTL 5 minut krótszym niż UDK
/// </summary>
public sealed record FileVersionSasUriInfo
{
    /// <summary>
    /// ID wersji pliku
    /// </summary>
    public required Guid VersionId { get; init; }

    /// <summary>
    /// SAS URI do wyświetlania (inline)
    /// </summary>
    public required string SasUriView { get; init; }

    /// <summary>
    /// SAS URI do pobierania (attachment)
    /// </summary>
    public required string SasUriDownload { get; init; }

    /// <summary>
    /// Czas wygaśnięcia SAS URI
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
