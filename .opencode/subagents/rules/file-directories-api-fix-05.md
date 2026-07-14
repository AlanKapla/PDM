# API Fix 05 — GetProjectFilePackages: budowanie drzewa katalogów

## Cel
Zmiana handlera `GetProjectFilePackagesQueryHandler` z zwracania płaskiej listy na zwracanie drzewa katalogów (root nodes z zagnieżdżonymi `SubCatalogs`).

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Kontekst

Po zmianach z fix-01 i fix-02:
- `ProjectFilePackageDto` ma `ParentId`
- `ProjectFilePackageWeb` ma `ParentId` i `SubCatalogs`

`GetAccessiblePackagesAsync` w serwisie zwraca **wszystkie** dostępne paczki (root + subkatalogi) — musimy je zorganizować w drzewo.

## Decyzje domenowe (zatwierdzone przez użytkownika)
- Jeśli user ma dostęp do subkatalogu ale nie do rodzica — subkatalog pojawia się jako root node
- `TotalFiles` liczy tylko pliki bezpośrednio w katalogu (shallow), nie rekurencyjnie
- scope=All: admin widzi wszystkie katalogi (własne i cudze) zagnieżdżone pod właścicielami/hierarchią

## Zmiany w `GetProjectFilePackagesQueryHandler.cs`

Aktualna logika (uproszczona):
1. `GetAccessiblePackagesAsync` → `Dictionary<Guid, ProjectFilePackageDto>`
2. `GetAccessibleFileCountsAsync` → `Dictionary<Guid, int>`
3. `GetProjectMembersByIdsAsync` → member info
4. `foreach` → `MapToPackageWeb(...)` → flat list

**Docelowa logika:**

```
1. GetAccessiblePackagesAsync → Dictionary<Guid, ProjectFilePackageDto>
2. GetAccessibleFileCountsAsync → Dictionary<Guid, int>
3. GetProjectMembersByIdsAsync → member info
4. Zbuduj Dictionary<Guid, ProjectFilePackageWeb> webNodesById — bez SubCatalogs
5. Drugi przebieg: dla każdego węzła z ParentId != null:
     - jeśli rodzic jest w webNodesById → dodaj do jego SubCatalogs
     - jeśli rodzic NIE jest w webNodesById → traktuj jako root (user ma dostęp do dziecka ale nie do rodzica)
6. Zwróć tylko root nodes: webNodesById.Values.Where(n => n.ParentId == null || rodzic_nie_istnieje_w_słowniku)
   posortowane po CreatedAt desc
```

### Implementacja pomocnicza — budowanie drzewa:

```csharp
// Po zmapowaniu wszystkich węzłów do web modeli:
var webNodesById = accessiblePackages.ToDictionary(
    kvp => kvp.Key,
    kvp => MapToPackageWeb(kvp.Value, fileCounts, members));

// Zbiór wszystkich ID węzłów które trafiły do jakiegoś rodzica
var attachedAsChild = new HashSet<Guid>();

foreach (var node in webNodesById.Values)
{
    if (node.ParentId.HasValue && webNodesById.TryGetValue(node.ParentId.Value, out var parent))
    {
        parent.SubCatalogs.Add(node);
        attachedAsChild.Add(node.Id);
    }
}

// Root nodes: nie mają ParentId lub ich rodzic nie jest dostępny
var rootNodes = webNodesById.Values
    .Where(n => !attachedAsChild.Contains(n.Id))
    .OrderByDescending(n => n.CreatedAt)
    .ToList();

return rootNodes;
```

**Uwaga:** `ProjectFilePackageWeb` ma `SubCatalogs { get; init; }` jako `List<>` — to `init` property. Żeby móc dodawać elementy po inicjalizacji, upewnij się że `SubCatalogs` nie jest `init` (zmień na `get; set;`) LUB użyj innego podejścia (np. collect + `with` lub mutable intermediate DTO).

Najprostsze rozwiązanie: zmień `SubCatalogs { get; init; }` na `SubCatalogs { get; } = new();` lub `SubCatalogs { get; set; } = new();` w `ProjectFilePackageWeb`.

Alternatywnie: zbierz dzieci w `Dictionary<Guid, List<ProjectFilePackageWeb>>` przed zbudowaniem web modeli, i przy `MapToPackageWeb` przekaż od razu zebraną listę dzieci.

### MapToPackageWeb — dodać ParentId:
```csharp
// W mapowaniu dodać:
ParentId = dto.ParentId,
SubCatalogs = new List<ProjectFilePackageWeb>() // początkowo puste, wypełnione w drugiej pętli
```

## Weryfikacja
```
dotnet build src/CQRS/CQRS.csproj
```
Build musi przejść bez błędów.
