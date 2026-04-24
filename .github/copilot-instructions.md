# Copilot Instructions — Project Data Management (PDM / Brickly)

## Struktura projektu

```
PDM/
├── 01-Applications/
│   ├── BricklyLandingPage/          # Landing page (Vite + React + TypeScript)
│   └── ProjectDataManagementUI/     # Główna aplikacja frontend (React + Chakra UI)
│       └── src/
│           ├── api/                 # Klienty API (axiosClient, *Api.ts per domena)
│           ├── components/          # Komponenty współdzielone
│           │   ├── ui/              # Bazowe elementy UI (AppModal, DeleteAlertDialog, EmptyState…)
│           │   └── common/          # Ogólne komponenty pomocnicze (DataCard, LoadingSpinner…)
│           ├── config/              # Konfiguracja MSAL, środowiska
│           ├── constants/           # Stałe (roleCodes.ts, PermissionCodes)
│           ├── context/             # React Contexts (AuthContext, ProjectCacheContext…)
│           ├── features/            # Moduły domenowe (dashboard/)
│           │   └── dashboard/
│           │       ├── components/  # Komponenty domenowe (DashboardHeader, FinancialOverview…)
│           │       ├── hooks/       # Hooki domenowe (useProjectDashboard…)
│           │       ├── services/    # Klienty API domenowe (dashboardApi.ts)
│           │       ├── types/       # Typy domenowe (projectDashboard.types.ts)
│           │       └── utils/       # Helpery domenowe (formatters, colors)
│           ├── hooks/               # Hooki globalne (useModal, useAuth, useProjectPermissions…)
│           ├── i18n/                # Internacjonalizacja (i18next)
│           ├── layout/              # Layout aplikacji (Sidebar, Header…)
│           ├── lib/                 # Zewnętrzne konfiguracje bibliotek
│           ├── pages/               # Strony (routowane przez AppRouter)
│           ├── routes/              # Routing (AppRouter, ProtectedRoute, PublicRoute)
│           ├── services/            # Serwisy (SignalR hubs, authService, userService)
│           ├── theme/               # Chakra UI theme + design tokens
│           │   └── tokens/
│           │       └── colors.ts    # JEDYNE źródło prawdy dla kolorów (appColors)
│           ├── types/               # Typy globalne per domena (project.types.ts, auth.types.ts…)
│           └── utils/               # Helpery ogólne
│
└── 02-ApplicationServices/
    └── ProductDataManagementWebAPI/
        └── src/
            ├── WebApi/              # Punkt wejścia — Program.cs, Controllers, Middleware
            │   ├── Controllers/     # BaseApiController + kontrolery endpointów
            │   ├── Authorization/   # PermissionAuthorizationHandler, PermissionRequirement
            │   ├── Extensions/      # ServiceCollectionExtensions, ApplicationBuilderExtensions
            │   ├── Hubs/            # SignalR hubs
            │   ├── Middleware/      # ApiExceptionMiddleware
            │   └── Services/        # CurrentUser (HttpContext)
            ├── CQRS/                # Commands, Queries, Handlers, Behaviours
            │   ├── Behaviours/      # ValidationBehavior, AuthorizationBehavior, TransactionBehavior
            │   ├── Projects/        # Commands + Queries per domena
            │   ├── CostEstimates/
            │   ├── CostEstimateTemplates/
            │   ├── CostTrackers/
            │   ├── WorkSchedules/
            │   ├── Files/
            │   ├── Chats/
            │   ├── Notifications/
            │   ├── Tenants/
            │   ├── Users/
            │   ├── Roles/
            │   ├── ProjectCosts/
            │   ├── Messages/
            │   └── AI/
            ├── Business/            # Logika domenowa
            │   ├── Interfaces/
            │   │   ├── Constants/   # PermissionCodes.cs, RoleCodes.cs, ResourceScope.cs
            │   │   ├── Exceptions/  # ApiException.cs i pochodne
            │   │   ├── WebModels/   # Web modele per domena
            │   │   └── Model/       # ICurrentUser, ResourceRef, snapshots
            │   └── Implementation/
            │       ├── Services/    # AccessService, CurrentUser, PermissionsVersionService
            │       └── Validators/  # FluentValidation validators
            ├── Business.Contracts/  # Zdarzenia domenowe / wiadomości SignalR
            ├── Entities/            # EF Core — DbContext, encje, migracje, konfiguracje
            │   ├── Context/         # AppDbContext.cs
            │   ├── Models/          # BaseEntity + encje domenowe
            │   ├── Configurations/  # IEntityTypeConfiguration<T>
            │   └── Migrations/      # EF Core migrations
            ├── Repositories/        # IRepository<T>, IReadRepository<T>, implementacje
            ├── Chat/                # Rejestracja SignalR chat hub
            └── FileUpload/          # Upload do Azure Blob
```

---

## Stack technologiczny

### API (backend)

| Technologia | Wersja |
|---|---|
| .NET / ASP.NET Core | 10.0 (`net10.0`) |
| MediatR | 13.0.0 |
| FluentValidation | 12.0.0 |
| FluentValidation.AspNetCore | 11.3.1 |
| Microsoft.EntityFrameworkCore + SqlServer | 10.0.1 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.1 |
| Microsoft.AspNetCore.Authentication.Google | 10.0.1 |
| Microsoft.Identity.Web | 4.2.0 |
| Azure.Identity | 1.17.1 |
| Swashbuckle.AspNetCore | 6.6.2 |
| SignalR | wbudowany w ASP.NET Core 10 |

Nullable reference types: **włączone** (`<Nullable>enable</Nullable>`).
Implicit usings: **włączone** (`<ImplicitUsings>enable</ImplicitUsings>`).

### UI (frontend)

| Technologia | Wersja |
|---|---|
| React | 18.2.0 |
| TypeScript | ~5.9.3 |
| Vite | ^7.2.2 |
| Chakra UI | 2.10.9 |
| Emotion (React/Styled) | ^11.14.x |
| Framer Motion | ^12.23.24 |
| React Router DOM | ^7.9.5 |
| Axios | ^1.13.2 |
| @azure/msal-browser + msal-react | ^4.27.0 / ^3.0.23 |
| @microsoft/signalr | ^10.0.0 |
| @dnd-kit (core/sortable/utilities) | ^6.3.1 / ^10.0.0 |
| lucide-react | ^0.554.0 |
| react-icons | ^5.5.0 |
| i18next + react-i18next | ^25.6.3 / ^16.3.5 |
| jwt-decode | ^4.0.0 |
| js-cookie | ^3.0.5 |

---

## API — zasady i wzorce

### Struktura CQRS

Każda operacja to para `{Nazwa}{Command|Query}` + `{Nazwa}{Command|Query}Handler` + opcjonalnie `{Nazwa}{Command|Query}Validator`, umieszczone w katalogu `CQRS/{Domena}/{Nazwa}/`.

**Interfejsy bazowe:**
- `IRequestCommand<TResponse>` — dla operacji zmieniających stan
- `IRequestQuery<TResponse>` — dla operacji odczytu
- `IAuthorizableRequest` — dodaj gdy handler wymaga autoryzacji po stronie pipeline

**Pipeline behaviors (kolejność rejestracji):**
1. `ValidationBehavior<TRequest, TResponse>` — FluentValidation, rzuca `ValidationApiException`
2. `AuthorizationBehavior<TRequest, TResponse>` — sprawdza `IAuthorizableRequest.PermissionCode`
3. `AssignedAuthorizationBehavior<TRequest, TResponse>` — dla `IAssignedAuthorizableRequest`
4. `TransactionBehavior<TRequest, TResponse>` — transakcja EF Core dla Commands

### Wzorzec URL routingu

```
/api/tenants/{tenantId}/[zasób]
/api/tenants/{tenantId}/project/{projectId}/[zasób]
```

**HTTP status konwencje:**
- `200 OK` — odczyt danych
- `201 Created` — tworzenie zasobu, z `CreatedAtAction`
- `204 NoContent` — operacja bez wyniku (delete, update bez zwracania)
- `400 Bad Request` — błąd walidacji
- `401 Unauthorized` — brak ważnego tokenu
- `403 Forbidden` — brak uprawnień
- `404 Not Found` — zasób nie istnieje
- `409 Conflict` — konflikt stanu

### Konwencje C# — obligatoryjne

#### Zakaz `var` — zawsze explicit type

```csharp
// ZAKAZANE
var project = await projectRepository.GetFirstBySearch(...);
var result = await Send(query);

// POPRAWNIE
Project project = await projectRepository.GetFirstBySearch(...);
ProjectDetailsWeb result = await Send(query);
```

#### Records — zawsze `{ get; init; }` lub primary constructor

```csharp
// ZAKAZANE
public class CreateProjectCommand { public Guid TenantId { get; set; } }

// POPRAWNIE — primary constructor (preferuj dla prostych record)
public record GetProjectDetailsQuery(Guid TenantId, Guid ProjectId)
    : IRequestQuery<ProjectDetailsWeb>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectView;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}

// POPRAWNIE — body constructor (gdy wymagane są dodatkowe metody lub interfejsy)
public record CreateProjectCommand(Guid TenantId, string Name)
    : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.TenantProjectCreate;
    public ResourceRef GetResource() => new ResourceRef(TenantId: TenantId);
}
```

#### Nawiasy klamrowe przy każdym bloku

```csharp
// ZAKAZANE
if (project == null) throw new NotFoundApiException(...);

// POPRAWNIE
if (project is null)
{
    throw new NotFoundApiException(nameof(Project), projectId.ToString());
}
```

#### Null check — preferuj `is null` / `is not null` zamiast `== null`

```csharp
if (project is null) { ... }
if (member is not null) { ... }
```

### SOLID

- **S** — każda klasa i metoda ma jedno zadanie; `Handle()` nie miesza logiki DB, mapowania i biznesu
- **O** — nowe zachowanie przez nowe klasy/handlery, nie modyfikowanie istniejących handlerów
- **L** — klasy bazowe (`{Domain}HandlerBase`) definiują kontrakt, pochodne nie naruszają
- **I** — `IReadRepository<T>` dla handlerów tylko odczytujących, `IRepository<T>` tylko gdy zapis
- **D** — handlery zależą od interfejsów (`IRepository<T>`, `ICurrentUser`), nigdy od konkretnych implementacji

### DRY

- Mapowanie encja → web model: zawsze prywatna metoda `Map{Entity}To{Web}(Entity e)`
- Wspólna logika kilku handlerów tej samej domeny: klasa bazowa `{Domain}HandlerBase`
- Kody uprawnień, nazwy zasobów, stałe: wyłącznie w `Business.Interfaces.Constants.*`
- `GetAndValidate{Entity}Async` — prywatna metoda do pobierania + rzucania `NotFoundApiException`

### Clean Code — atomiczne metody

```csharp
public async Task<ProjectDetailsWeb> Handle(
    CreateProjectCommand request,
    CancellationToken cancellationToken)
{
    // Handle() jest wyłącznie orkiestratorem
    Project project = BuildProject(request);
    ProjectMember member = await CreateAdminMemberAsync(project.Id, cancellationToken);
    await SaveChangesAsync(project, member, cancellationToken);
    return MapProjectToWeb(project);
}

// Prywatne metody z jasną intencją
private Project BuildProject(CreateProjectCommand request) { ... }
private async Task<ProjectMember> CreateAdminMemberAsync(Guid projectId, ...) { ... }
private ProjectDetailsWeb MapProjectToWeb(Project project) { ... }
```

- Max ~20 linii na metodę
- Brak magic stringów — kody uprawnień w `PermissionCodes`, kody ról w `RoleCodes`
- Komentarze opisują „dlaczego", nie „co"

### Hermetyzacja encji

- Stan wewnętrzny encji modyfikowany wyłącznie przez metody publiczne z intencją biznesową
- Pola nawigacyjne EF Core jako `ICollection<T>` tylko gdy potrzebne; nie udostępniaj kolekcji przez właściwości publiczne bezpośrednio

### Walidacja (FluentValidation)

Jeden validator per command/query, automatycznie wykrywany przez pipeline:

```csharp
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required")
            .MaximumLength(100).WithMessage("Project name cannot exceed 100 characters");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required");
    }
}
```

### Obsługa błędów

Rzucaj przez dedykowane klasy z `Business.Interfaces.Exceptions`:

```csharp
throw new NotFoundApiException(nameof(Project), projectId.ToString());
throw new ValidationApiException("Opis błędu walidacji");
throw new ForbiddenApiException();
throw new ConflictApiException("Zasób już istnieje");
throw new UnauthorizedApiException();
```

`ApiExceptionMiddleware` serializuje do JSON:
```json
{
  "error": "NotFound",
  "message": "Project with ID '...' was not found.",
  "objectType": "Project",
  "objectId": "..."
}
```

`ApiExceptionReason`: `ValidationError`, `NotFound`, `Unauthorized`, `Forbidden`, `Conflict`, `InvalidOperation`

### Autoryzacja

**Kontrolery** — `[Authorize(Policy = PermissionCodes.XxxYyy)]`:

```csharp
[HttpGet("{projectId}")]
[Authorize(Policy = PermissionCodes.ProjectView)]
public async Task<IActionResult> GetProjectDetails(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId)
{
    GetProjectDetailsQuery query = new GetProjectDetailsQuery(tenantId, projectId);
    ProjectDetailsWeb result = await Send(query);
    return Ok(result);
}
```

**Commands/Queries z IAuthorizableRequest** — pipeline sprawdza przez `AuthorizationBehavior`.

**Kody uprawnień** (`Business.Interfaces.Constants.PermissionCodes`):

```
TENANT.LIST.AVAILABLE / TENANT.ADMIN.LIST.AVAILABLE
TENANT.VIEW / TENANT.EDIT / TENANT.MEMBERS.MANAGE / TENANT.STATUS.MANAGE / TENANT.PROJECT.CREATE
PROJECT.VIEW / PROJECT.EDIT
PROJECT.MEMBERS.VIEW / PROJECT.MEMBERS.MANAGE
PROJECT.STATUS.MANAGE
PROJECT.RESOURCES.READ / PROJECT.RESOURCES.WRITE / PROJECT.RESOURCES.SHARE
PROJECT.RESOURCES.READ_SHARED / PROJECT.RESOURCES.WRITE_SHARED
PROJECT.RESOURCES.READ_ALL / PROJECT.RESOURCES.WRITE_ALL
PROJECT.RESOURCES.READ_SINGLE / PROJECT.RESOURCES.WRITE_OWN
ROLE.LIST
```

**Kody ról** (`Business.Interfaces.Constants.RoleCodes`):
- `SYSTEM.SUPERADMIN` — systemowy, nie przypisywalny
- `TENANT.ADMIN`, `TENANT.MEMBER`
- `PROJECT.ADMIN`, `PROJECT.EDITOR`, `PROJECT.VIEWER`

### Repozytoria

`IReadRepository<T>` (extends `IRepository<T>`) — używaj dla handlerów tylko odczytujących.
`IRepository<T>` — gdy handler zapisuje dane.

```csharp
// Pobierz z include
Project project = await projectRepository.GetFirstBySearch(
    p => p.TenantId == tenantId && p.Id == projectId,
    cancellationToken,
    include => include.Include(p => p.Members))
    ?? throw new NotFoundApiException(nameof(Project), projectId.ToString());

// Pobierz po Id
Project project = await projectRepository.GetById(projectId)
    ?? throw new NotFoundApiException(nameof(Project), projectId.ToString());

// Projekcja (optymalizacja zapytań)
List<Guid> ids = await projectRepository.SelectAsync(
    p => p.TenantId == tenantId,
    p => p.Id,
    cancellationToken);

// Bulk delete
await projectRepository.ExecuteDeleteAsync(
    p => p.ProjectId == projectId,
    cancellationToken);

// Sprawdź istnienie
bool exists = await projectRepository.AnyAsync(p => p.Name == name, cancellationToken);

// Zapis
await projectRepository.Insert(entity);
await projectRepository.SaveChangesAsync(cancellationToken);
```

### Web modele

Zawsze immutable `record` z sufiksem `Web`, umieszczone w `Business.Interfaces.WebModels/{Domena}/`:

```csharp
// Primary constructor (preferuj dla prostych modeli)
public record ProjectDetailsWeb(
    Guid Id,
    Guid TenantId,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    string CreatedByUserName,
    string UserRoleCode,
    int MembersCount,
    HashSet<string> UserPermissions
);

// Body style (gdy wymagane atrybuty lub xml docs)
public record ProjectMemberWeb
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string RoleCode { get; init; }
}
```

- Nigdy nie zwracaj encji EF Core bezpośrednio
- Web model nie zawiera pól technicznych EF (klucze relacji, shadow properties)

### Encje EF Core

Wszystkie encje dziedziczą `BaseEntity` (`Guid Id { get; set; } = Guid.NewGuid()`).
Konfiguracja przez `IEntityTypeConfiguration<T>` w `Entities/Configurations/`.

**Istniejące encje:**
`Project`, `Tenant`, `TenantMember`, `TenantInvitation`, `User`, `UserSession`,
`ProjectMember`, `ProjectGroup`, `ProjectGroupMember`,
`ProjectFile`, `ProjectFileVersion`, `ProjectFileAccess`, `ProjectFilePackage`, `ProjectFileVersionComment`,
`ProjectCost`, `SharedProjectCost`, `SharedProjectFile`,
`Role`, `RolePermission`, `Permission`,
`WorkSchedule`, `WorkScheduleStage`, `WorkScheduleStageWork`,
`WorkScheduleStageWorkAssignment`, `WorkScheduleStageWorkDependency`,
`WorkScheduleStageWorkPeriod`, `WorkScheduleStageWorkComment`,
`WorkDependencyType`, (WorkItemLinks/),
`CostTracker` (CostTrackers/), kosztorysy (CostEstimates/), szablony (CostEstimateTemplates/),
`Chat`, `ChatMember`, `MessageHistory`, `Notification`

Migracje: `Entities/Migrations/` — nazwy opisowe, np. `add-dashboard-feature`.

---

## UI — zasady i wzorce

### Struktura katalogów — gdzie co tworzyć

| Co tworzysz | Gdzie |
|---|---|
| Komponent domenowy (tylko dla 1 feature) | `src/features/{feature}/components/` |
| Hook domenowy | `src/features/{feature}/hooks/` |
| Typy domenowe | `src/features/{feature}/types/{feature}.types.ts` |
| Serwis API domenowy | `src/features/{feature}/services/{feature}Api.ts` |
| Komponent współdzielony (bazowy UI) | `src/components/ui/` |
| Komponent pomocniczy (universal) | `src/components/common/` |
| Typy globalne per domena API | `src/types/{domain}.types.ts` |
| Hook globalny | `src/hooks/use{Feature}{Action}.ts` |
| Stała / enum | `src/constants/{name}.ts` |
| Token designu | `src/theme/tokens/colors.ts` (kolory) lub nowy plik w `src/theme/tokens/` |
| Klient API | `src/api/{domain}Api.ts` |
| Context | `src/context/{Name}Context.tsx` |

### Typy — zakaz `any`

```typescript
// ZAKAZANE
const handleData = (data: any) => { ... }
const [items, setItems] = useState<any[]>([]);
const response = await axiosClient.get<any>('/endpoint');

// POPRAWNIE
const handleData = (data: ProjectDetailsWeb) => { ... }
const [items, setItems] = useState<ProjectDetailsWeb[]>([]);
const response = await axiosClient.get<ProjectDetailsWeb[]>('/endpoint');
```

- Props komponentów: zawsze `interface {Component}Props` lub `type {Component}Props`
- Hooki: deklaruj `interface Use{Feature}Result { ... }` i zwracaj explicite ten typ
- Odpowiedzi API: definiuj ręcznie `interface {Entity}Web` w `types/` lub `features/{domain}/types/`
- Enumy: używaj `as const` objects lub TypeScript `enum`

### Komponenty współdzielone — zakaz duplikacji

**Modale:** zawsze używaj `AppModal` z `src/components/ui/AppModal.tsx`:

```tsx
import AppModal from '../components/ui/AppModal';

<AppModal
  isOpen={isOpen}
  onClose={onClose}
  title="Dodaj projekt"
  actionLabel="Utwórz"
  onAction={handleSubmit}
  isActionLoading={isLoading}
>
  {/* treść modala */}
</AppModal>
```

Zakaz tworzenia własnych implementacji `Modal` per feature — extend `AppModal`.

**Alert Dialog / Potwierdzenie usunięcia:** używaj `DeleteAlertDialog` z `src/components/ui/`:

```tsx
<DeleteAlertDialog
  isOpen={isOpen}
  onClose={onClose}
  onConfirm={handleDelete}
  title="Usuń projekt"
  description="Tej operacji nie można cofnąć."
/>
```

**Loading/Empty/Error stany:** `LoadingState`, `EmptyState`, `ErrorState` z `src/components/ui/`.
**Spinner:** `LoadingSpinner` z `src/components/common/`.

### Design tokens — kolory, czcionki, spacing

**Jedyne źródło prawdy dla kolorów:** `src/theme/tokens/colors.ts`

```typescript
import { appColors } from "@/theme/tokens/colors";

// W komponentach — przez Chakra token
bg="primary.600"
colorScheme="primary"

// Lub przez import
bg={appColors.primary[600]}
```

**Palety kolorów zarejestrowane w `theme.ts`:**
- `primary` — niebieski (nagłówki, CTA, aktywne stany, focus ring)
- `level1` — zielony (komponenty kosztorysu, sukces, Dodaj)
- `level2` — fioletowy (opcje, sumowania, obliczenia)
- `action` — teal (akcje drugorzędne, zapis, udostępnij)

**Zakaz hardkodowania kolorów bezpośrednio:**

```tsx
// ZAKAZANE
<Box bg="#2B6CB0" color="#276749">

// POPRAWNIE
<Box bg="primary.600" color="level1.700">
```

Spacing, rozmiary czcionek: zawsze przez Chakra tokens (`px={4}`, `fontSize="sm"`, itd.).

### Hooki — zasady

```typescript
// Wzorzec dla hooka domenowego — zawsze explicite typowany wynik
export interface UseProjectDashboardResult {
  data: ProjectDashboardWeb | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useProjectDashboard(
  tenantId: string,
  projectId: string
): UseProjectDashboardResult {
  const [data, setData] = useState<ProjectDashboardWeb | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDashboard = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await getProjectDashboard(tenantId, projectId);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd ładowania');
    } finally {
      setIsLoading(false);
    }
  }, [tenantId, projectId]);

  useEffect(() => {
    fetchDashboard();
  }, [fetchDashboard]);

  return { data, isLoading, error, refetch: fetchDashboard };
}
```

**Reguły hooków:**
- Logika fetchu/state dla danej domeny: wydziel do hooka `use{Feature}{Action}` lub `use{Feature}`
- Zakaz duplikowania logiki fetch między komponentami tej samej domeny
- Hooki modali: używaj `useModal` z `src/hooks/useModal.ts` (`isOpen`, `onOpen`, `onClose`, `toggle`)
- Sprawdzanie uprawnień: `useProjectPermissions(projectId)` lub `useTenantPermissions()`

### Komponenty — zasady

```tsx
// Wzorzec dla komponentu domenowego
export interface DashboardHeaderProps {
  data: ProjectDashboardWeb;
  projectName: string;
}

export function DashboardHeader({ data, projectName }: DashboardHeaderProps): React.ReactElement {
  const { financialSummary } = data;
  // Tylko renderowanie — logika w hookach
  return (
    <Box mb={5}>
      <Text fontSize="lg" fontWeight="semibold">{projectName}</Text>
    </Box>
  );
}
```

- Jeden plik = jeden komponent = jedna odpowiedzialność
- Logika biznesowa i fetch w hookach, nie w komponentach
- Komponenty domenowe eksportowane jako named exports
- `components/ui/` używa default export (konwencja biblioteki)
- Eventy obsługiwane przez callbacki przekazane przez props

### Nazewnictwo

| Element | Konwencja | Przykład |
|---|---|---|
| Komponent | `PascalCase.tsx` | `ProjectCard.tsx` |
| Hook | `camelCase.ts` z `use` | `useProjectDashboard.ts` |
| Plik typów domenowych | `{domain}.types.ts` | `projectDashboard.types.ts` |
| Typy/Interfejsy TypeScript | `PascalCase` | `ProjectDetailsWeb`, `UseProjectDashboardResult` |
| Plik API | `{domain}Api.ts` | `dashboardApi.ts` |
| Stała/enum | `PascalCase` (object as const) | `PermissionCodes`, `RoleCodes` |
| Context | `{Name}Context.tsx` | `AuthContext.tsx` |

**Sufiksy interfejsów:**
- Odpowiedź API: `{Entity}Web` (np. `ProjectDetailsWeb`)
- Props komponentu: `{Component}Props` (np. `DashboardHeaderProps`)
- Wynik hooka: `Use{Feature}Result` (np. `UseProjectDashboardResult`)

### Klient API

Wszystkie żądania przez `axiosClient` z `src/api/axiosClient.ts` (dodaje JWT Bearer automatycznie):

```typescript
// Wzorzec funkcji API domenowej
export const dashboardApi = {
  getProjectDashboard: async (
    tenantId: string,
    projectId: string
  ): Promise<ProjectDashboardWeb> => {
    const response = await axiosClient.get<ProjectDashboardWeb>(
      `/tenants/${tenantId}/projects/${projectId}/dashboard`
    );
    return response.data;
  },
};
```

### Autoryzacja w UI

Sprawdzanie uprawnień przez dedykowane hooki:

```typescript
// Projekt
const { canEdit, canManageMembers, canWriteResources } = useProjectPermissions(projectId);

// Tenant
const { canManageMembers, canCreateProject } = useTenantPermissions();
```

Kody uprawnień i ról: `src/constants/roleCodes.ts` (`PermissionCodes`, `RoleCodes`).

---

## Przykłady — wzorcowy kod

### API: wzorcowy Command z autoryzacją

```csharp
// CQRS/Projects/CreateProject/CreateProjectCommand.cs
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.CreateProject
{
    public record CreateProjectCommand(Guid TenantId, string Name)
        : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantProjectCreate;

        public ResourceRef GetResource() => new ResourceRef(TenantId: TenantId);
    }
}
```

### API: wzorcowy Query z autoryzacją

```csharp
// CQRS/Projects/GetProjectDetails/GetProjectDetailsQuery.cs
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.GetProjectDetails
{
    public record GetProjectDetailsQuery(Guid TenantId, Guid ProjectId)
        : IRequestQuery<ProjectDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
```

### API: wzorcowy Handler

```csharp
public class GetProjectDetailsQueryHandler : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsWeb>
{
    private readonly IReadRepository<Project> projectRepository;
    private readonly ICurrentUser currentUser;

    public GetProjectDetailsQueryHandler(
        IReadRepository<Project> projectRepository,
        ICurrentUser currentUser)
    {
        this.projectRepository = projectRepository;
        this.currentUser = currentUser;
    }

    public async Task<ProjectDetailsWeb> Handle(
        GetProjectDetailsQuery request,
        CancellationToken cancellationToken)
    {
        Project project = await GetAndValidateProjectAsync(
            request.TenantId, request.ProjectId, cancellationToken);

        return MapProjectToWeb(project);
    }

    private async Task<Project> GetAndValidateProjectAsync(
        Guid tenantId, Guid projectId, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetFirstBySearch(
            p => p.TenantId == tenantId && p.Id == projectId,
            cancellationToken);

        if (project is null)
        {
            throw new NotFoundApiException(nameof(Project), projectId.ToString());
        }

        return project;
    }

    private ProjectDetailsWeb MapProjectToWeb(Project project) =>
        new ProjectDetailsWeb(
            Id: project.Id,
            TenantId: project.TenantId,
            Name: project.Name,
            IsActive: project.IsActive,
            CreatedAt: project.CreatedAt,
            CreatedByUserId: project.CreatedByUserId,
            CreatedByUserName: string.Empty,
            UserRoleCode: string.Empty,
            MembersCount: 0,
            UserPermissions: new HashSet<string>()
        );
}
```

### API: wzorcowy Validator

```csharp
// CQRS/Projects/CreateProject/CreateProjectCommandValidator.cs
using FluentValidation;

namespace CQRS.Projects.CreateProject
{
    public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required")
                .MaximumLength(100).WithMessage("Project name cannot exceed 100 characters");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");
        }
    }
}
```

### API: wzorcowy Controller

```csharp
[Route("api/tenants/{tenantId}/project")]
[ApiController]
public class ProjectController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("{projectId}")]
    [Authorize(Policy = PermissionCodes.ProjectView)]
    public async Task<IActionResult> GetProjectDetails(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId)
    {
        GetProjectDetailsQuery query = new GetProjectDetailsQuery(tenantId, projectId);
        ProjectDetailsWeb result = await Send(query);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.TenantProjectCreate)]
    public async Task<IActionResult> CreateProject(
        [FromRoute] Guid tenantId,
        [FromBody] CreateProjectCommand command)
    {
        command = command with { TenantId = tenantId };
        ProjectDetailsWeb result = await Send(command);
        return CreatedAtAction(nameof(GetProjectDetails), new { tenantId, projectId = result.Id }, result);
    }

    [HttpDelete("{projectId}")]
    [Authorize(Policy = PermissionCodes.ProjectEdit)]
    public async Task<IActionResult> DeleteProject(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId)
    {
        DeleteProjectCommand command = new DeleteProjectCommand(tenantId, projectId);
        await Send(command);
        return NoContent();
    }
}
```

### API: wzorcowy Web Model

```csharp
// Business/Interfaces/WebModels/Projects/ProjectDetailsWeb.cs
namespace Business.Interfaces.WebModels.Projects
{
    public record ProjectDetailsWeb(
        Guid Id,
        Guid TenantId,
        string Name,
        bool IsActive,
        DateTime CreatedAt,
        Guid CreatedByUserId,
        string CreatedByUserName,
        string UserRoleCode,
        int MembersCount,
        HashSet<string> UserPermissions
    );
}
```

### UI: wzorcowy typ domenowy

```typescript
// src/features/dashboard/types/projectDashboard.types.ts

export enum FinancialStatus {
  NoBudget   = 0,
  NoCosts    = 1,
  InProgress = 2,
  NearLimit  = 3,
  OverBudget = 4,
}

export interface FinancialSummaryWeb {
  totalBudgetNet: number | null;
  totalCostsNet: number | null;
  financialStatus: FinancialStatus;
  additionalCostsNet: number | null;
}

export interface ProjectDashboardWeb {
  projectId: string;
  referenceDate: string;
  generatedAt: string;
  financialSummary: FinancialSummaryWeb;
}
```

### UI: wzorcowy hook

```typescript
// src/features/dashboard/hooks/useProjectDashboard.ts
import { useState, useEffect, useCallback } from 'react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { getProjectDashboard } from '../services/dashboardApi';

export interface UseProjectDashboardResult {
  data: ProjectDashboardWeb | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useProjectDashboard(
  tenantId: string,
  projectId: string
): UseProjectDashboardResult {
  const [data, setData] = useState<ProjectDashboardWeb | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDashboard = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await getProjectDashboard(tenantId, projectId);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd ładowania dashboardu');
    } finally {
      setIsLoading(false);
    }
  }, [tenantId, projectId]);

  useEffect(() => {
    fetchDashboard();
  }, [fetchDashboard]);

  return { data, isLoading, error, refetch: fetchDashboard };
}
```

### UI: wzorcowy komponent domenowy

```tsx
// src/features/dashboard/components/DashboardHeader.tsx
import React from 'react';
import { Box, HStack, Text, Badge } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';

export interface DashboardHeaderProps {
  data: ProjectDashboardWeb;
  projectName: string;
}

export function DashboardHeader({ data, projectName }: DashboardHeaderProps): React.ReactElement {
  const { financialSummary } = data;

  return (
    <Box mb={5}>
      <Text fontSize="lg" fontWeight="semibold" color="gray.800" mb={1}>
        {projectName}
      </Text>
      <HStack wrap="wrap" spacing={2}>
        <Badge colorScheme="gray" px={2} py={1} borderRadius="full" fontSize="xs">
          Budżet: <strong>{financialSummary.totalBudgetNet}</strong>
        </Badge>
      </HStack>
    </Box>
  );
}
```

### UI: wzorcowe użycie AppModal

```tsx
import { useModal } from '../hooks/useModal';
import AppModal from '../components/ui/AppModal';

function ProjectActionsPanel() {
  const createModal = useModal();
  const [isLoading, setIsLoading] = useState(false);

  const handleCreate = async () => {
    setIsLoading(true);
    try {
      await createProject(formData);
      createModal.onClose();
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <Button onClick={createModal.onOpen}>Nowy projekt</Button>
      <AppModal
        isOpen={createModal.isOpen}
        onClose={createModal.onClose}
        title="Utwórz projekt"
        actionLabel="Utwórz"
        actionColorScheme="green"
        onAction={handleCreate}
        isActionLoading={isLoading}
      >
        {/* formularz */}
      </AppModal>
    </>
  );
}
```

### UI: wzorcowy klient API w feature

```typescript
// src/features/dashboard/services/dashboardApi.ts
import { axiosClient } from '../../../api/axiosClient';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';

export async function getProjectDashboard(
  tenantId: string,
  projectId: string
): Promise<ProjectDashboardWeb> {
  const response = await axiosClient.get<ProjectDashboardWeb>(
    `/tenants/${tenantId}/projects/${projectId}/dashboard`
  );
  return response.data;
}
```

---

## Zmienne środowiskowe

### API (`appsettings.json`)

```json
{
  "ConnectionStrings": { "DefaultConnection": "" },
  "AzureAdB2C": { "Instance": "", "Domain": "", "ClientId": "", "TenantId": "", "ClientSecret": "" },
  "Azure": { "ClientId": "", "TenantId": "", "ClientSecret": "" },
  "Redis": { "ConnectionString": "", "IsEnabled": true, "DefaultExpirationMinutes": 60 },
  "BlobStorage": { "ContainerUrl": "", "QueueUrl": "" },
  "CorsSettings": { "AllowedOrigins": [] },
  "Frontend": { "BaseUrl": "", "HomePath": "/" },
  "EmailSettings": { "Provider": "SendGrid", "SendGrid": { "ApiKey": "", "DefaultFromEmail": "" } }
}
```

### UI (`.env`)

```
VITE_API_BASE_URL=https://...
```
