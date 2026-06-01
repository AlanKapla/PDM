# API Fix 06 — SharePackages: kaskadowe udostępnianie podkatalogów

## Cel
Zmiana `SharePackagesCommandHandler` tak, aby udostępnianie katalogu kaskadowo udostępniało wszystkie jego podkatalogi (nieograniczona głębokość).

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Decyzja domenowa (zatwierdzona)
- Autoryzacja: sprawdzana tylko dla oryginalnych `PackageIds` podanych przez użytkownika (nie potomków)
- Kaskada: API automatycznie rozszerza listę o wszystkich potomków przed pętlą sharowania

## Zmiany w `SharePackagesCommandHandler.cs`

Aktualna logika:
1. Autoryzuje każdą paczkę z `request.PackageIds`
2. Iteruje po `(packageId, userId)` i tworzy `SharedProjectFile`

**Docelowa logika:**

Przed główną pętlą sharowania (po autoryzacji), dodać krok rozwinięcia potomków:

```csharp
// Krok 2.5: Rozwiń PackageIds o wszystkich potomków (kaskada)
IReadOnlyList<ProjectFilePackageDto> allProjectPackages = await GetAllProjectPackagesAsync(
    request.TenantId, request.ProjectId, cancellationToken);

IReadOnlyList<Guid> allPackageIds = ExpandWithDescendants(request.PackageIds, allProjectPackages);
```

### Metoda pomocnicza `GetAllProjectPackagesAsync`:
```csharp
private async Task<IReadOnlyList<ProjectFilePackageDto>> GetAllProjectPackagesAsync(
    Guid tenantId, Guid projectId, CancellationToken cancellationToken)
{
    // Pobierz WSZYSTKIE paczki projektu jednym zapytaniem (nie tylko dostępne dla currentUser)
    // Użyj IReadRepository<ProjectFilePackage> — dodać do zależności handlera jeśli brak
    var packages = await packageRepository.GetBySearch(
        p => p.TenantId == tenantId && p.ProjectId == projectId);
    return packages.Select(p => new ProjectFilePackageDto
    {
        Id = p.Id,
        TenantId = p.TenantId,
        ProjectId = p.ProjectId,
        OwnerId = p.OwnerId,
        Name = p.Name,
        CreatedAt = p.CreatedAt,
        CreatedByUserId = p.CreatedByUserId,
        ParentId = p.ParentId,
        IsDeleted = p.IsDeleted
    }).ToList();
}
```

### Metoda pomocnicza `ExpandWithDescendants`:
```csharp
private static IReadOnlyList<Guid> ExpandWithDescendants(
    IEnumerable<Guid> rootIds,
    IReadOnlyList<ProjectFilePackageDto> allPackages)
{
    // Zbuduj parent → children map
    var childrenByParent = allPackages
        .Where(p => p.ParentId.HasValue)
        .GroupBy(p => p.ParentId!.Value)
        .ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToList());

    var result = new HashSet<Guid>();
    var queue = new Queue<Guid>(rootIds);

    while (queue.Count > 0)
    {
        Guid current = queue.Dequeue();
        if (!result.Add(current)) continue; // już przetworzony

        if (childrenByParent.TryGetValue(current, out List<Guid>? children))
        {
            foreach (Guid child in children)
            {
                queue.Enqueue(child);
            }
        }
    }

    return result.ToList();
}
```

### Zmień `request.PackageIds` na `allPackageIds` w pętli sharowania:

W istniejącej pętli (gdziekolwiek iteruje po `request.PackageIds`) zmień na `allPackageIds`.

**WAŻNE:** Autoryzacja (krok 1) nadal sprawdza tylko oryginalne `request.PackageIds`. Potomkowie nie wymagają oddzielnej autoryzacji — skoro user może udostępnić rodzica, może też udostępnić jego dzieci.

## Weryfikacja
```
dotnet build src/CQRS/CQRS.csproj
```
Build musi przejść bez błędów.
