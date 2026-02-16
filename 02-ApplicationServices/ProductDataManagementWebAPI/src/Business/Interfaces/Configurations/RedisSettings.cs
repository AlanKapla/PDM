namespace Business.Interfaces.Configurations;

/// <summary>
/// Ustawienia konfiguracyjne dla połączenia z Redis
/// </summary>
public sealed class RedisSettings
{
    /// <summary>
    /// Nazwa sekcji w appsettings.json
    /// </summary>
    public const string SectionName = "Redis";
    
    /// <summary>
    /// Connection string do serwera Redis (np. "localhost:6379" lub Azure Redis connection string)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Domyślny czas wygaśnięcia cache w minutach
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;
}
