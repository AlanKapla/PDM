# API Fix 02 — IContractorService.SearchByProfileAsync + ContractorService

## Cel
Rozszerz `IContractorService` o metodę wyszukiwania kontrahenta po profilu (nazwa, NIP, adres).
Używana przez handler CQRS aby dopasować kontrahenta z dokumentu do bazy danych.

## Krok 1 — Przeczytaj przed implementacją

Przeczytaj:
- `src/Business/Interfaces/Services/IContractorService.cs` — pełna treść, żeby wiedzieć co dodać
- `src/Business/Implementation/Services/ContractorService.cs` — pełna treść implementacji
- Encja `Contractor` (szukaj w `src/Entities/Models/`) — sprawdź dostępne pola: `Name`, `TaxId`, `Street`, `City`, `TenantId`
- Repozytorium używane przez ContractorService — zrozum jak filtrować

## Krok 2 — Dodaj do IContractorService

W pliku `src/Business/Interfaces/Services/IContractorService.cs` dodaj metodę:

```csharp
/// <summary>
/// Wyszukuje kontrahenta po profilu z dokumentu.
/// Kolejność dopasowania: NIP (dokładne) → Nazwa (zawiera, case-insensitive).
/// Zwraca null jeśli brak dopasowania.
/// </summary>
Task<Contractor?> SearchByProfileAsync(
    string? name,
    string? taxId,
    string tenantId,
    CancellationToken cancellationToken);
```

Uwaga: `tenantId` jako string bo tak jest w istniejących metodach — sprawdź konwencję i dopasuj (może być Guid).

## Krok 3 — Zaimplementuj w ContractorService

W pliku `src/Business/Implementation/Services/ContractorService.cs` dodaj implementację:

### Logika wyszukiwania (kolejność priorytetów):

1. **Po TaxId (NIP)** — najdokładniejsze dopasowanie:
   - Normalizuj oba NIP-y: usuń `-`, ` `, i inne nie-cyfrowe znaki
   - Porównuj znormalizowane wartości
   - Jeśli dopasowanie → zwróć kontrahenta

2. **Po nazwie** — case-insensitive contains:
   - Szukaj `contractor.Name.Contains(name, StringComparison.OrdinalIgnoreCase)`
   - Jeśli dopasowanie → zwróć kontrahenta

3. Jeśli brak dopasowania → zwróć `null`

### Implementacja:

```csharp
public async Task<Contractor?> SearchByProfileAsync(
    string? name,
    string? taxId,
    Guid tenantId,  // użyj właściwego typu zgodnego z resztą serwisu
    CancellationToken cancellationToken)
{
    // Pobierz aktywnych kontrahentów tenanta
    IEnumerable<Contractor> contractors = await _repository.GetAllAsync(
        c => c.TenantId == tenantId && !c.IsDeleted,
        cancellationToken);

    // 1. Szukaj po NIP
    if (!string.IsNullOrWhiteSpace(taxId))
    {
        string normalizedTaxId = NormalizeTaxId(taxId);
        Contractor? byTaxId = contractors.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c.TaxId) &&
            NormalizeTaxId(c.TaxId) == normalizedTaxId);

        if (byTaxId is not null)
        {
            return byTaxId;
        }
    }

    // 2. Szukaj po nazwie
    if (!string.IsNullOrWhiteSpace(name))
    {
        Contractor? byName = contractors.FirstOrDefault(c =>
            c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (byName is not null)
        {
            return byName;
        }
    }

    return null;
}

private static string NormalizeTaxId(string taxId) =>
    new string(taxId.Where(char.IsDigit).ToArray());
```

### Ważne:
- Dopasuj metodę repozytorium (`GetAllAsync`, `ListAsync`, itp.) do tego co jest w ContractorService
- Dopasuj typ `tenantId` (Guid czy string) do konwencji w serwisie
- `IsDeleted` — sprawdź czy encja Contractor dziedziczy z `DeletableEntity` (jeśli tak, użyj tego pola)
- Jeśli repozytorium zwraca `IReadOnlyList` lub `IQueryable` — dostosuj LINQ

## Krok 4 — Weryfikacja

```
dotnet build src/Business/Business.csproj
```
Nie powinno być błędów.
