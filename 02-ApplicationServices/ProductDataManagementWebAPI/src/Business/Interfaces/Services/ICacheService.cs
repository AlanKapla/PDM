namespace Business.Interfaces.Services;

/// <summary>
/// Serwis cache oparty na Redis do przechowywania i pobierania danych tymczasowych
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Pobiera wartość z cache lub wykonuje funkcję fabryczną i zapisuje wynik w cache
    /// </summary>
    /// <typeparam name="T">Typ cachowanej wartości (musi być klasą)</typeparam>
    /// <param name="key">Klucz cache</param>
    /// <param name="factory">Funkcja asynchroniczna zwracająca wartość do cachowania</param>
    /// <param name="expiration">Opcjonalny czas wygaśnięcia cache</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Wartość z cache lub wynik funkcji fabrycznej</returns>
    Task<T?> GetOrAddAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Pobiera wiele wartości z cache jednocześnie używając MGET
    /// </summary>
    /// <typeparam name="T">Typ cachowanej wartości (musi być klasą)</typeparam>
    /// <param name="keys">Lista kluczy cache do pobrania</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Słownik [klucz -> wartość] dla znalezionych wpisów</returns>
    Task<Dictionary<string, T>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Zapisuje wiele wartości w cache jednocześnie używając MSET
    /// </summary>
    /// <typeparam name="T">Typ cachowanej wartości (musi być klasą)</typeparam>
    /// <param name="items">Słownik [klucz -> wartość] do zapisania</param>
    /// <param name="expiration">Opcjonalny czas wygaśnięcia cache</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    Task SetManyAsync<T>(
        Dictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Usuwa pojedynczy klucz z cache
    /// </summary>
    /// <param name="key">Klucz cache do usunięcia</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    Task RemoveCacheByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa wszystkie klucze pasujące do wzorca Redis
    /// </summary>
    /// <param name="pattern">Wzorzec Redis (np. "project:*" lub "user:123:*")</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    Task RemoveCacheContainsAsync(string pattern, CancellationToken cancellationToken = default);
}
