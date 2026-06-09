# Audyt API — Feature: File Directories (Katalogi z podkatalogami)

Data audytu: 2026-06-01

---

## PEŁNA ZAWARTOŚĆ PRZECZYTANYCH PLIKÓW

### `src/Entities/Models/Files/ProjectFilePackage.cs`

```csharp
using Entities.Models.Base;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;

namespace Entities.Models.Files
{
    public class ProjectFilePackage : DeletableEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }

        // Navigation
        public Project Project { get; set; } = default!;
        public User Owner { get; set; } = default!;
        public User CreatedByUser { get; set; } = default!;
        public TenantMember OwnerTenantMember { get; set; } = default!;
        public TenantMember CreatedByTenantMember { get; set; } = default!;

        public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
    }
}
```

---

### `src/Entities/Configurations/ProjectFilePackageConfiguration.cs`

```csharp
public class ProjectFilePackageConfiguration : IEntityTypeConfiguration<ProjectFilePackage>
{
    public void Configure(EntityTypeBuilder<ProjectFilePackage> builder)
    {
        builder.HasKey(pfp => pfp.Id);

        builder.Property(pfp => pfp.Name).IsRequired().HasMaxLength(200);
        builder.Property(pfp => pfp.CreatedAt).IsRequired();
        builder.Property(pfp => pfp.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasQueryFilter(pfp => !pfp.IsDeleted);

        builder.HasOne(pfp => pfp.Project).WithMany()
            .HasForeignKey(pfp => pfp.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(pfp => pfp.Owner).WithMany()
            .HasForeignKey(pfp => pfp.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(pfp => pfp.CreatedByUser).WithMany()
            .HasForeignKey(pfp => pfp.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(pfp => pfp.OwnerTenantMember).WithMany()
            .HasForeignKey(pfp => new { pfp.TenantId, pfp.OwnerId })
            .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(pfp => pfp.CreatedByTenantMember).WithMany()
            .HasForeignKey(pfp => new { pfp.TenantId, pfp.CreatedByUserId })
            .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
            .OnDelete(DeleteBehavior.Restrict);

        // OBECNY unique constraint — do zmiany
        builder.HasIndex(pfp => new { pfp.TenantId, pfp.ProjectId, pfp.OwnerId, pfp.Name })
            .IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasIndex(pfp => new { pfp.ProjectId, pfp.TenantId });
        builder.HasIndex(pfp => new { pfp.OwnerId, pfp.ProjectId });
        builder.HasIndex(pfp => new { pfp.ProjectId, pfp.IsDeleted });
    }
}
```

---

### `src/Business/Interfaces/WebModels/Files/ProjectFilePackageWeb.cs`

```csharp
public sealed record ProjectFilePackageWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required Guid OwnerId { get; init; }
    public required string OwnerName { get; init; }
    public List<ProjectFileWeb> Files { get; init; } = new();
    public required int TotalFiles { get; init; }
}
```

---

### `src/Business/Interfaces/DTO/ProjectFilePackageDto.cs`

```csharp
public record ProjectFilePackageDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid OwnerId { get; init; }
    public string Name { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
    public bool IsDeleted { get; init; }
}
```

---

### `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommand.cs`

```csharp
public sealed record CreatePackageAndUploadFilesCommand : ProjectScopedFilesRequestBase, IRequestCommand<Unit>
{
    public required string PackageName { get; init; }
    public List<FileUploadItem> Files { get; init; } = new();
    public override string PermissionCode => PermissionCodes.ProjectFiles;
}
```

---

### `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandHandler.cs`

Handler wstawia `ProjectFilePackage` + pliki + wersje + komentarze, robi upload do Blob, z compensacją w przypadku błędu.
`BuildPackage()` tworzy encję bez `ParentId`.

---

### `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandValidator.cs`

- Waliduje `PackageName` (notEmpty, maxLength)
- Sprawdza unikalność przez `GetFirstBySearch` po `(TenantId, ProjectId, OwnerId, Name)` — **bez ParentId**
- Wymaga co najmniej jednego pliku: `NotEmpty()` na `Files`

---

### `src/CQRS/Files/GetProjectFilePackages/GetProjectFilePackagesQuery.cs`

```csharp
public sealed record GetProjectFilePackagesQuery : ProjectScopedFilesRequestBase, IRequestQuery<List<ProjectFilePackageWeb>>
{
    public required ResourceScope Scope { get; init; }
    public override string PermissionCode => PermissionCodes.ProjectFiles;
    public ResourceScope? GetResourceScope() => Scope;
}
```

---

### `src/CQRS/Files/GetProjectFilePackages/GetProjectFilePackagesQueryHandler.cs`

Handler zwraca **płaską listę** posortowaną po `CreatedAt` desc.
Wywołuje:
1. `GetAccessiblePackagesAsync` → `Dictionary<Guid, ProjectFilePackageDto>`
2. `GetAccessibleFileCountsAsync` → `Dictionary<Guid, int>`
3. `GetProjectMembersByIdsAsync` → `Dictionary<Guid, ProjectMemberUserInfo>` dla ownerNames
4. `MapToPackageWeb(...)` per pakiet

---

### `src/CQRS/Files/SharePackages/ShareProjectFilesCommand.cs`

```csharp
public sealed record SharePackagesCommand : ProjectScopedFilesRequestBase, IRequestCommand<Unit>
{
    public required List<Guid> PackageIds { get; init; }
    public required List<Guid> SharedWithUserIds { get; init; }
    public override string PermissionCode => PermissionCodes.ProjectFiles;
}
```

---

### `src/CQRS/Files/SharePackages/SharePackagesCommandHandler.cs`

1. Autoryzuje każdą paczkę przez `fileAccessGuard.EnsureCanAccessPackageAsync(..., Share)`
2. Pobiera paczki (`GetBySearch`) dla owner lookup
3. Dla każdej pary `(packageId, userId)`: usuwa stare file-level wpisy, tworzy `SharedProjectFile` z `FileId = null` (cała paczka)
4. Zapisuje + invaliduje cache
5. **Brak kaskady do podkatalogów** — dzieli tylko dosłownie wymienione `PackageIds`

---

### `src/WebApi/Controllers/FileController.cs`

Endpointy:
| Method | Route | Handler |
|--------|-------|---------|
| POST | `packages/create` | `CreatePackageAndUploadFiles` |
| GET | `packages/{scope}` | `GetProjectFilePackages` |
| GET | `packages/{packageId}/files/{scope}` | `GetPackageFiles` |
| GET | `files/{fileId}/versions/{scope}` | `GetFileVersions` |
| GET | `files/{fileId}/versions/{versionId}/comments/{scope}` | `GetVersionComments` |
| POST | *(root)* | `UploadFiles` |
| POST | `versions` | `UploadFileVersion` |
| POST | `packages/share` | `SharePackages` |
| DELETE | `{fileId}` | `DeleteProjectFile` |
| POST | `{fileId}/versions` | `UploadNewVersion` |
| PUT | `{fileId}/share` | `UpdateFileShare` |

**Brak endpointu do tworzenia pustego katalogu.**

---

## BLOK 1 — Stan obecny

### Encje
- `ProjectFilePackage` — płaska struktura, brak `ParentId`/`Parent`/`Children`
- Unique constraint: `(TenantId, ProjectId, OwnerId, Name)` — nie uwzględnia katalogu nadrzędnego

### Query
- `GetProjectFilePackages` zwraca płaską listę, brak hierarchii
- `ProjectFilePackageDto` nie ma pola `ParentId` — tree building niemożliwy w handlerze

### Command — Create
- `CreatePackageAndUploadFilesCommand` nie przyjmuje `ParentId`
- `BuildPackage()` nie ustawia `ParentId`
- Walidacja unikalności nie uwzględnia `ParentId`
- Walidator wymaga co najmniej jednego pliku — blokuje tworzenie pustego katalogu

### Command — Share
- `SharePackagesCommandHandler` nie kaskaduje do podkatalogów

### Controller
- Brak endpointu `POST /file/directories` (tworzenie katalogu bez plików)

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|-----------|------|
| Pole `ParentId` w encji | Entities | **KRYTYCZNY** | Brak kolumny i FK dla hierarchii |
| Nawigacje `Parent`/`Children` w encji | Entities | **KRYTYCZNY** | Potrzebne do EF eager loading |
| Zmiana unique constraint | Entities | **KRYTYCZNY** | Nowy indeks z `ParentId` |
| Migracja EF Core | Entities | **KRYTYCZNY** | Nowe pole + nowy indeks |
| `ParentId` w `ProjectFilePackageDto` | Business/DTO | **KRYTYCZNY** | Bez tego handler nie może budować drzewa |
| `ParentId` + `SubCatalogs` w `ProjectFilePackageWeb` | Business/WebModels | **KRYTYCZNY** | Zwracany model musi mieć hierarchię |
| `ParentId` w `CreatePackageAndUploadFilesCommand` | CQRS | **KRYTYCZNY** | Command musi przyjmować katalog nadrzędny |
| Ustawienie `ParentId` w `BuildPackage()` | CQRS | **KRYTYCZNY** | Handler nie zapisuje wartości |
| Walidacja `ParentId` w validatorze | CQRS | **WYSOKI** | Sprawdzenie istnienia + prawa dostępu |
| Zmiana walidacji unikalności | CQRS | **WYSOKI** | Zapytanie musi filtrować po `ParentId` |
| Nowy Command `CreateDirectoryCommand` | CQRS | **WYSOKI** | Tworzenie pustego katalogu (bez plików) |
| Zmiana walidatora — `Files` opcjonalne | CQRS | **WYSOKI** | Lub nowy command/validator bez `NotEmpty` |
| Budowanie drzewa w `GetProjectFilePackagesQueryHandler` | CQRS | **WYSOKI** | Płaska lista → drzewo root-nodes |
| Kaskadowe udostępnianie w `SharePackagesCommandHandler` | CQRS | **WYSOKI** | Rekurencyjne rozwinięcie podkatalogów |
| Nowy endpoint `POST /file/directories` | WebApi | **WYSOKI** | Tworzenie katalogu bez plików |
| Aktualizacja `IProjectFilesService` dla hierarchii | Business | **ŚREDNI** | Czy `GetAccessiblePackagesAsync` zwraca też subkatalogi |

---

## BLOK 3 — Zmiany w encjach/DB

| Encja | Zmiana | Typ | Wymaga migracji |
|-------|--------|-----|----------------|
| `ProjectFilePackage` | Dodać `Guid? ParentId` | Nowe pole (nullable) | **TAK** |
| `ProjectFilePackage` | Dodać `ProjectFilePackage? Parent` | Navigation property | NIE (EF shadow) |
| `ProjectFilePackage` | Dodać `ICollection<ProjectFilePackage> Children` | Navigation property | NIE |
| `ProjectFilePackageConfiguration` | Self-referencing FK `HasOne(Parent).WithMany(Children).HasForeignKey(ParentId).OnDelete(Restrict)` | Relacja | **TAK** |
| `ProjectFilePackageConfiguration` | Usunąć stary index `(TenantId, ProjectId, OwnerId, Name)` | Zmiana indexu | **TAK** |
| `ProjectFilePackageConfiguration` | Nowy index `(TenantId, ProjectId, OwnerId, ParentId, Name)` z filtrem | Nowy indeks unique | **TAK** |
| `ProjectFilePackageConfiguration` | Index dla `ParentId` (lookups potomków) | Nowy indeks | **TAK** |

### Uwagi krytyczne do migracji

**Problem z `OnDelete` dla self-referencing FK w SQL Server:**
SQL Server nie pozwala na `CASCADE` dla self-referencing FK gdy tabela ma już CASCADE z innego FK (kolumna cyklu). Tutaj `ProjectId` już ma `Cascade`, więc `ParentId` MUSI mieć `OnDelete(DeleteBehavior.Restrict)` lub `NoAction`. Przy soft-delete (IsDeleted) to nie problem — parent nie jest kasowany fizycznie, ale trzeba to obsłużyć w logice biznesowej.

**Problem z unique indexem na nullable `ParentId` w SQL Server:**
W SQL Server wartości NULL nie są równe innym NULL w unique indexie — oznacza to, że dwa katalogi główne (`ParentId IS NULL`) z tą samą nazwą mogłyby przejść przez index. Rozwiązanie: użyć filtrowanego indexu z osobnym filtrem dla NULL:
```sql
-- SQL Server nie obsługuje IS NULL w HasFilter bezpośrednio,
-- ale można użyć: HasFilter("[IsDeleted] = 0 AND [ParentId] IS NOT NULL")
-- + osobny index dla root-level z HasFilter("[IsDeleted] = 0 AND [ParentId] IS NULL")
```
Alternatywa: zastąpić NULL specjalnym sentinel GUID (np. `Guid.Empty`) — prostsze, ale mniej idiomatyczne.

---

## BLOK 4 — Nowe Commands/Queries

| Command/Query | Typ | Opis | Handler |
|--------------|-----|------|---------|
| `CreatePackageAndUploadFilesCommand` | **Modyfikacja** | Dodać `Guid? ParentId` | `CreatePackageAndUploadFilesCommandHandler` — `BuildPackage()` ustawia `ParentId` |
| `CreatePackageAndUploadFilesCommandValidator` | **Modyfikacja** | Zmiana walidacji unikalności + walidacja `ParentId` | Istniejący validator |
| `CreateDirectoryCommand` | **NOWY** | Tworzy katalog bez plików; `PackageName` + opcjonalny `ParentId` | Nowy `CreateDirectoryCommandHandler` |
| `CreateDirectoryCommandValidator` | **NOWY** | Walidacja jak CreatePackageAndUploadFiles ale bez Files | Nowy validator |
| `GetProjectFilePackagesQuery` | Bez zmian | Sygnatura identyczna | — |
| `GetProjectFilePackagesQueryHandler` | **Modyfikacja** | Zmiana `MapToPackageWeb` + budowanie drzewa; tylko root-nodes w wyniku | Istniejący handler |

### Szczegóły `CreateDirectoryCommand`
```csharp
public sealed record CreateDirectoryCommand : ProjectScopedFilesRequestBase, IRequestCommand<Unit>
{
    public required string DirectoryName { get; init; }
    public Guid? ParentId { get; init; }
    public override string PermissionCode => PermissionCodes.ProjectFiles;
}
```

### Szczegóły budowania drzewa w `GetProjectFilePackagesQueryHandler`
Obecna pętla przechodzi przez `accessiblePackages` i tworzy płaską listę. Po dodaniu `ParentId` do `ProjectFilePackageDto`:
1. `MapToPackageWeb(...)` bez `SubCatalogs`
2. Buduj `Dictionary<Guid, ProjectFilePackageWeb>` 
3. Drugi przebieg: dla każdego paczki z `ParentId != null` dodaj do `SubCatalogs` rodzica
4. Zwróć tylko root nodes (`ParentId == null`)

**Uwaga:** Jeśli user ma dostęp do subkatalogu ale nie do rodzica — decyzja domenowa (patrz pytania domenowe).

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP Method | Nowy/Modyfikacja | Opis |
|----------|------------|-----------------|------|
| `packages/create` | POST | Modyfikacja | Przyjmuje opcjonalne `ParentId` w formdata |
| `packages/{scope}` | GET | Bez zmian | Zwracany model zmienia kształt (drzewo) |
| `packages/share` | POST | Modyfikacja | Dokumentacja — teraz kaskaduje do dzieci |
| `directories` | POST | **NOWY** | Tworzy katalog bez plików — `[FromBody] CreateDirectoryCommand` |

### Nowy endpoint
```csharp
/// <summary>
/// Create a new empty directory (without uploading files)
/// </summary>
[HttpPost("directories")]
[Authorize(Policy = PermissionCodes.ProjectFiles)]
public async Task<IActionResult> CreateDirectory(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromBody] CreateDirectoryCommand command)
{
    command = command with { TenantId = tenantId, ProjectId = projectId };
    await Send(command);
    return NoContent();
}
```

---

## BLOK 6 — Zmiany w serwisach

| Serwis | Interfejs | Nowy/Modyfikacja | Metody |
|--------|-----------|-----------------|--------|
| `IProjectFilesService` / `ProjectFilesService` | `IProjectFilesService` | Modyfikacja | `GetAccessiblePackagesAsync` musi zwracać `ProjectFilePackageDto` z `ParentId` |
| `SharePackagesCommandHandler` | — | Modyfikacja wewnątrz handlera | Nowa metoda prywatna `GetAllDescendantIdsAsync` |

### Kaskadowe udostępnianie — opcje implementacji

**Opcja A — In-memory (rekomendowana dla małych drzew):**
```csharp
// Po pobraniu packages w kroku 2 handlera:
// Pobierz WSZYSTKIE paczki projektu (lub tylko "children" wskazanych packageIds)
// Zbuduj drzewo in-memory
// Rozwiń PackageIds o wszystkich potomków
```
Zaleta: proste. Wada: N+1 jeśli drzewo jest bardzo głębokie — ale pobranie wszystkich paczek projektu jednym zapytaniem jest akceptowalne.

**Opcja B — Rekurencyjne CTE (SQL):**
Zbyt skomplikowane dla EF Core, wymagałoby raw SQL. Niepotrzebne.

**Rekomendacja — Opcja A:**
W handlerze po kroku 2 (pobieranie paczek dla owner lookup):
1. Pobierz WSZYSTKIE paczki projektu jednym `GetBySearch`
2. Zbuduj `Dictionary<Guid, List<Guid>>` parent → children
3. DFS/BFS od każdego `packageId` w `request.PackageIds` — zbierz wszystkich potomków
4. Scal `request.PackageIds` z potomkami
5. Kontynuuj istniejący loop sharowania

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | **Circular reference w JSON** | WebModels | WYSOKI | `ProjectFilePackageWeb` z `SubCatalogs: List<ProjectFilePackageWeb>` jest OK w odpowiedzi (drzewo idzie tylko w dół). Problem wystąpi TYLKO jeśli `Parent` (nawigacja wsteczna) będzie zmapowany do web modelu — nie mapować `Parent` w `ProjectFilePackageWeb`. |
| 2 | **NULL w unique indexie SQL Server** | Entities/DB | WYSOKI | Dwa katalogi główne (`ParentId IS NULL`) z tą samą nazwą przechodzą przez standard unique index na nullable column. Patrz BLOK 3 — użyć dwóch filtrowanych indeksów lub sentinel GUID. |
| 3 | **Soft-delete i self-referencing FK** | Entities/DB | ŚREDNI | `OnDelete(Restrict)` — usunięcie rodzica (soft) nie kaskaduje do dzieci. Potrzebna logika biznesowa: przed soft-delete rodzica → zrekursuj soft-delete dzieci, lub zablokuj delete jeśli katalog ma dzieci. |
| 4 | **Walidacja `ParentId`** | CQRS | WYSOKI | `ParentId` musi istnieć w tym samym `(TenantId, ProjectId)` i nie może być soft-deleted. Bez tej walidacji można osierociić podkatalogi lub stworzyć katalog pod nieistniejącym rodzicem. |
| 5 | **User ma dostęp do subkatalogu, nie do rodzica** | Business | ŚREDNI | Jeśli ktoś udostępni tylko subkatalog — przy budowaniu drzewa parent nie będzie w `accessiblePackages`. Decyzja: (a) zwróć subkatalog jako root-node, (b) zablokuj dostęp bez rodzica. |
| 6 | **`GetAccessibleFileCountsAsync` a pliki w podkatalogach** | Business | NISKI | `TotalFiles` na katalogu nadrzędnym — czy liczyć tylko pliki bezpośrednio w nim, czy łącznie z podkatalogami? Decyzja domenowa. |
| 7 | **`CreatePackageAndUploadFilesCommand` — `Files` jako opcjonalne** | CQRS | NISKI | Jeśli `Files` stanie się opcjonalne (dla tworzenia pustego katalogu), stary walidator `NotEmpty` złamie istniejące testy. Bezpieczniej: nowy dedykowany `CreateDirectoryCommand` z własnym validatorem. |
| 8 | **Walidator unikalności bez `ParentId`** | CQRS | WYSOKI | Obecna walidacja sprawdza `(TenantId, ProjectId, OwnerId, Name)`. Po dodaniu hierarchii ta walidacja da false-positive dla `Subdir A` pod `Parent1` vs `Subdir A` pod `Parent2`. |
| 9 | **Cache w `IProjectFilesService`** | Business | ŚREDNI | `ProjectFilePackageDto` jest cachowany. Po dodaniu `ParentId` do DTO — cache key i invalidation pozostają bez zmian, ale stare wartości cache mogą nie mieć `ParentId`. Zadbaj o invalidację po migracji. |
| 10 | **Test `SharePackagesCommandHandlerTests`** | Tests | NISKI | Testy nie pokrywają kaskady — po implementacji kaskady testy trzeba rozszerzyć o scenariusze z `Children`. |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 0 |
| Nowe pola w encjach | 1 (`ParentId` nullable Guid) |
| Nowe navigation properties | 2 (`Parent`, `Children`) |
| Nowe Commands | 1 (`CreateDirectoryCommand`) |
| Zmodyfikowane Commands | 1 (`CreatePackageAndUploadFilesCommand` + handler + validator) |
| Nowe Queries | 0 |
| Zmodyfikowane Queries | 1 (`GetProjectFilePackagesQueryHandler` — budowanie drzewa) |
| Nowe endpointy | 1 (`POST /file/directories`) |
| Zmodyfikowane serwisy | 1 (`IProjectFilesService` — `ProjectFilePackageDto` + `GetAccessiblePackagesAsync`) |
| Wymaga migracji DB | **TAK** |
| Testy do aktualizacji | 4 (`CreatePackageAndUploadFilesCommandHandlerTests`, `GetProjectFilePackagesQueryHandlerTests`, `SharePackagesCommandHandlerTests`, `FileControllerTests`) |
| Pytania domenowe | 3 |

---

## Pytania domenowe wymagające decyzji

1. **Dostęp do subkatalogu bez rodzica**: Jeśli user ma `SharedProjectFile` dla subkatalogu, ale rodzic nie jest mu udostępniony — czy subkatalog ma pojawić się jako root-node w odpowiedzi `GetProjectFilePackages`, czy być ukryty?

2. **`TotalFiles` na katalogu nadrzędnym**: Czy `TotalFiles` liczy pliki **bezpośrednio** w katalogu (shallow), czy **łącznie** z wszystkimi podkatalogami (deep/recursive)? Wpływa na implementację `GetAccessibleFileCountsAsync`.

3. **Soft-delete rodzica**: Co dzieje się z podkatalogami przy usuwaniu katalogu nadrzędnego — kaskada soft-delete do dzieci, czy blokada usunięcia jeśli katalog nie jest pusty?

---

## Lista zmian per plik

### `src/Entities/Models/Files/ProjectFilePackage.cs`
- Dodać `public Guid? ParentId { get; set; }`
- Dodać `public ProjectFilePackage? Parent { get; set; }`
- Dodać `public ICollection<ProjectFilePackage> Children { get; set; } = new List<ProjectFilePackage>();`

### `src/Entities/Configurations/ProjectFilePackageConfiguration.cs`
- Dodać konfigurację self-referencing FK:
  ```csharp
  builder.HasOne(pfp => pfp.Parent)
      .WithMany(pfp => pfp.Children)
      .HasForeignKey(pfp => pfp.ParentId)
      .OnDelete(DeleteBehavior.Restrict);
  ```
- Usunąć istniejący unique index `(TenantId, ProjectId, OwnerId, Name)`
- Dodać nowe indeksy filtrowane dla unikalności (patrz BLOK 3)
- Dodać index na `ParentId` dla lookupów dzieci

### `src/Business/Interfaces/DTO/ProjectFilePackageDto.cs`
- Dodać `public Guid? ParentId { get; init; }`

### `src/Business/Interfaces/WebModels/Files/ProjectFilePackageWeb.cs`
- Dodać `public Guid? ParentId { get; init; }`
- Dodać `public List<ProjectFilePackageWeb> SubCatalogs { get; init; } = new();`
- Zmienić `TotalFiles` na `required` pozostaje, ale wartość będzie dotyczyć tylko shallow lub deep (decyzja domenowa)

### `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommand.cs`
- Dodać `public Guid? ParentId { get; init; }`

### `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandHandler.cs`
- W `BuildPackage()` dodać ustawienie `ParentId = request.ParentId`

### `src/CQRS/Files/CreatePackageAndUploadFiles/CreatePackageAndUploadFilesCommandValidator.cs`
- Zmienić walidację unikalności: dodać `pfp.ParentId == command.ParentId` do predykatu
- Dodać walidację `ParentId` (jeśli podany: musi istnieć w tym samym TenantId+ProjectId, nie deleted)

### `src/CQRS/Files/GetProjectFilePackages/GetProjectFilePackagesQueryHandler.cs`
- `MapToPackageWeb` — dodać `ParentId` do mappingu
- Zmienić pętlę na dwuprzebiegową: najpierw stwórz `Dict<Guid, ProjectFilePackageWeb>`, potem przypisz `SubCatalogs` rodzicom
- Zwrócić tylko root nodes (`ParentId == null`)

### `src/CQRS/Files/SharePackages/SharePackagesCommandHandler.cs`
- Przed pętlą sharowania: pobrać wszystkie paczki projektu, zbudować drzewo, rozwinąć `PackageIds` o potomków
- Autoryzacja: autoryzować tylko oryginalne `PackageIds` (nie potomki, bo kaskada jest automatyczna)

### `src/WebApi/Controllers/FileController.cs`
- Dodać endpoint `POST /directories` → `CreateDirectoryCommand`

### NOWE PLIKI
- `src/CQRS/Files/CreateDirectory/CreateDirectoryCommand.cs`
- `src/CQRS/Files/CreateDirectory/CreateDirectoryCommandHandler.cs`
- `src/CQRS/Files/CreateDirectory/CreateDirectoryCommandValidator.cs`

### Testy do aktualizacji
- `tests/CQRS.Tests/Files/CreatePackageAndUploadFilesCommandHandlerTests.cs` — asserty na `BuildPackage` (ParentId)
- `tests/CQRS.Tests/Files/GetProjectFilePackagesQueryHandlerTests.cs` — scenariusze z hierarchią (subkatalogi)
- `tests/CQRS.Tests/Files/SharePackagesCommandHandlerTests.cs` — scenariusze z kaskadą
- `tests/WebApi.Tests/Controllers/FileControllerTests.cs` — nowy endpoint `CreateDirectory`
