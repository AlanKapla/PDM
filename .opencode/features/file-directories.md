# Feature: Katalogi z podkatalogami (File Directories)

## Opis

Zmiana mechanizmu "paczek" (packages) na "katalogi" (directories) z obsługą nieograniczonego zagnieżdżania podkatalogów.

## Motywacja

Obecny model jest płaski: `Paczka → Pliki`. Użytkownicy potrzebują hierarchicznej struktury katalogów podobnej do systemu plików.

## Wymagania

### Struktura docelowa
```
Katalog główny (ParentId = null)
├── Pliki (bezpośrednio w katalogu)
├── Podkatalog A (ParentId = root.Id)
│   ├── Pliki
│   └── Podkatalog A1 (ParentId = A.Id)
│       └── Pliki
└── Podkatalog B
    └── Pliki
```

### Zasady
1. **Nieograniczone zagnieżdżenie** — brak limitu głębokości
2. **Pliki w katalogu głównym** — pliki mogą być dodawane bezpośrednio do katalogu głównego (obok podkatalogów)
3. **Tworzenie podkatalogów** — dwa sposoby:
   - Przy wgrywaniu plików (wybierz/stwórz katalog nadrzędny)
   - Oddzielny przycisk "Dodaj katalog" bez plików
4. **Udostępnianie kaskadowe** — udostępnienie katalogu nadrzędnego automatycznie udostępnia wszystkie podkatalogi

### Zmiany nazewnictwa
- UI: "paczka" → "katalog"
- API: kod pozostaje (Package), tylko etykiety UI zmieniają się

## Warstwy do zmiany

### API — Encja
- `ProjectFilePackage`: dodać `ParentId` (nullable Guid), `Parent`, `Children`
- Zmiana unikalności: `(TenantId, ProjectId, OwnerId, Name)` → `(TenantId, ProjectId, OwnerId, ParentId, Name)`
- Nowa migracja EF Core

### API — Business Models
- `ProjectFilePackageWeb`: dodać `parentId` (nullable), `subCatalogs` (List<ProjectFilePackageWeb>)
- `ProjectFilePackageDto`: analogicznie

### API — CQRS
- `CreatePackageAndUploadFiles`: dodać `ParentId` do komendy
- Nowy endpoint `POST /file/directories`: tworzy sam katalog (bez plików)
- `GetProjectFilePackages`: zwracać drzewo rekurencyjnie
- `SharePackages`: kaskadowe udostępnianie podkatalogów

### UI — Typy
- `ProjectFilePackageWeb`: dodać `parentId`, `subCatalogs`

### UI — Komponenty
- `UploadFilesModal`: wybór/tworzenie katalogu nadrzędnego
- `ProjectFiles` page: zagnieżdżony accordion + przycisk "Dodaj podkatalog"
- Rename wszędzie "paczka" → "katalog"

## Pliki kluczowe — API

- `src/Entities/Models/Files/ProjectFilePackage.cs`
- `src/Entities/Configurations/ProjectFilePackageConfiguration.cs`
- `src/Business/Interfaces/WebModels/Files/ProjectFilePackageWeb.cs`
- `src/Business/Interfaces/DTO/ProjectFilePackageDto.cs`
- `src/CQRS/Files/CreatePackageAndUploadFiles/`
- `src/CQRS/Files/GetProjectFilePackages/`
- `src/CQRS/Files/SharePackages/`
- `src/WebApi/Controllers/FileController.cs`

## Pliki kluczowe — UI

- `src/pages/ProjectFiles.tsx`
- `src/components/UploadFilesModal.tsx`
- `src/components/ShareFilesModal.tsx`
- `src/hooks/queries/useProjectFiles.ts`
- `src/types/project.types.ts`
- `src/api/projectApi.ts`
