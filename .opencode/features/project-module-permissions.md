# Feature: Project Module Permissions

## Cel
Zastąpienie roli projektowej (`ProjectMember.RoleId → Role`) granularnymi uprawnieniami per-moduł (`ProjectMemberModulePermission`).

## Kluczowe decyzje projektowe

### TenantMember — BEZ ZMIAN
`TenantMember.RoleId` zostaje. `TenantAdmin`/`TenantMember` nadal używają tabeli `Role`/`RolePermission`.

### ProjectMember — NOWY MODEL
- Usuń `RoleId`, `MemberRole` z `ProjectMember`
- Dodaj kolekcję `ICollection<ProjectMemberModulePermission> ModulePermissions`
- Dodaj `bool IsAdmin` (true = ma pełen dostęp do wszystkich modułów, używane przez TenantAdmin inject i UI)

### Nowe enum: ProjectModule
```
Settings=0, Members=1, Files=2, Estimates=3, Costs=4, Schedule=5, Dashboard=6, Chat=7
```

### Nowy enum: ModuleAccessLevel
```
None=0, ViewShared=1, View=2, Read=3, WriteAssigned=4, WriteShared=5,
Write=6, WriteAll=7, Edit=8, Manage=9, Admin=10
```

### Nowe PermissionCodes (PEŁNA LISTA)

```csharp
// TENANT CONTEXT
TENANT.CONTEXT.LIST
TENANT.CONTEXT.ADMIN_LIST
ROLE.LIST  // zachowane dla TenantAdmin

// TENANT SETTINGS
TENANT.SETTINGS.VIEW
TENANT.SETTINGS.EDIT
TENANT.MEMBERS.MANAGE   // bez zmian
TENANT.STATUS.TOGGLE
TENANT.PROJECTS.CREATE

// PROJECT SETTINGS
PROJECT.SETTINGS.VIEW
PROJECT.SETTINGS.EDIT
PROJECT.STATUS.TOGGLE
PROJECT.DASHBOARD.VIEW  // NOWE - wcześniej PROJECT.EDIT

// PROJECT MEMBERS
PROJECT.MEMBERS.VIEW   // bez zmian
PROJECT.MEMBERS.MANAGE // bez zmian
PROJECT.ROLES.LIST     // nowe - dla UI przypisywania

// PROJECT FILES
PROJECT.FILES.READ_SHARED
PROJECT.FILES.READ_OWN
PROJECT.FILES.READ_ALL
PROJECT.FILES.WRITE_ASSIGNED
PROJECT.FILES.WRITE_SHARED
PROJECT.FILES.WRITE_OWN
PROJECT.FILES.WRITE_ALL
PROJECT.FILES.SHARE

// PROJECT ESTIMATES
PROJECT.ESTIMATES.READ_SHARED
PROJECT.ESTIMATES.READ_OWN
PROJECT.ESTIMATES.READ_ALL
PROJECT.ESTIMATES.WRITE_ASSIGNED
PROJECT.ESTIMATES.WRITE_SHARED
PROJECT.ESTIMATES.WRITE_OWN
PROJECT.ESTIMATES.WRITE_ALL
PROJECT.ESTIMATES.SHARE

// PROJECT COSTS (CostTracker + ProjectCosts)
PROJECT.COSTS.VIEW
PROJECT.COSTS.WRITE
PROJECT.COSTS.SHARE

// PROJECT SCHEDULE
PROJECT.SCHEDULE.READ_SHARED
PROJECT.SCHEDULE.READ_OWN
PROJECT.SCHEDULE.READ_ALL
PROJECT.SCHEDULE.WRITE_ASSIGNED
PROJECT.SCHEDULE.WRITE_SHARED
PROJECT.SCHEDULE.WRITE_OWN
PROJECT.SCHEDULE.WRITE_ALL
PROJECT.SCHEDULE.SHARE

// CHAT
CHAT.READ
CHAT.WRITE
CHAT.MEMBERS.MANAGE
CHAT.RENAME
CHAT.DELETE
```

### ModulePermissionTranslator — mapa (Module, Level) → PermissionCodes

**Settings:**
- None → []
- View → [PROJECT.SETTINGS.VIEW, PROJECT.DASHBOARD.VIEW]
- Edit → [PROJECT.SETTINGS.VIEW, PROJECT.SETTINGS.EDIT, PROJECT.DASHBOARD.VIEW]
- Admin → [PROJECT.SETTINGS.VIEW, PROJECT.SETTINGS.EDIT, PROJECT.STATUS.TOGGLE, PROJECT.DASHBOARD.VIEW]

**Members:**
- None → []
- View → [PROJECT.MEMBERS.VIEW]
- Manage → [PROJECT.MEMBERS.VIEW, PROJECT.MEMBERS.MANAGE, ROLE.LIST]

**Files:**
- None → []
- ViewShared → [PROJECT.SETTINGS.VIEW, PROJECT.FILES.READ_SHARED]
- WriteAssigned → [PROJECT.SETTINGS.VIEW, PROJECT.FILES.READ_SHARED, PROJECT.FILES.WRITE_ASSIGNED]
- WriteShared → [PROJECT.SETTINGS.VIEW, PROJECT.FILES.READ_SHARED, PROJECT.FILES.WRITE_ASSIGNED, PROJECT.FILES.WRITE_SHARED]
- Write → [PROJECT.SETTINGS.VIEW, PROJECT.FILES.READ_SHARED, PROJECT.FILES.READ_OWN, PROJECT.FILES.WRITE_ASSIGNED, PROJECT.FILES.WRITE_SHARED, PROJECT.FILES.WRITE_OWN]
- WriteAll → [PROJECT.SETTINGS.VIEW, PROJECT.FILES.READ_SHARED, PROJECT.FILES.READ_OWN, PROJECT.FILES.READ_ALL, PROJECT.FILES.WRITE_ASSIGNED, PROJECT.FILES.WRITE_SHARED, PROJECT.FILES.WRITE_OWN, PROJECT.FILES.WRITE_ALL]
- Admin → [...WriteAll, PROJECT.FILES.SHARE]

**Estimates:** (identycznie jak Files ale z PROJECT.ESTIMATES.*)

**Costs:**
- None → []
- View → [PROJECT.COSTS.VIEW]
- Write → [PROJECT.COSTS.VIEW, PROJECT.COSTS.WRITE]
- Admin → [PROJECT.COSTS.VIEW, PROJECT.COSTS.WRITE, PROJECT.COSTS.SHARE]

**Schedule:**
- None → []
- ViewShared → [PROJECT.SETTINGS.VIEW, PROJECT.SCHEDULE.READ_SHARED]
- WriteAssigned → [PROJECT.SETTINGS.VIEW, PROJECT.SCHEDULE.READ_SHARED, PROJECT.SCHEDULE.WRITE_ASSIGNED]
- Write → [PROJECT.SETTINGS.VIEW, PROJECT.SCHEDULE.READ_SHARED, PROJECT.SCHEDULE.READ_OWN, PROJECT.SCHEDULE.WRITE_ASSIGNED, PROJECT.SCHEDULE.WRITE_OWN]
- WriteAll → [...Write, PROJECT.SCHEDULE.READ_ALL, PROJECT.SCHEDULE.WRITE_ALL]
- Admin → [...WriteAll, PROJECT.SCHEDULE.SHARE]

**Dashboard:**
- None → []
- View → [PROJECT.DASHBOARD.VIEW] (NOTE: Settings.View already includes this)

**Chat:**
- None → []
- Read → [CHAT.READ]
- Write → [CHAT.READ, CHAT.WRITE]
- Manage → [CHAT.READ, CHAT.WRITE, CHAT.MEMBERS.MANAGE, CHAT.RENAME, CHAT.DELETE]

### SuperAdminFallbackPermissions (nowe kody)
```
TenantReadOnly: [TENANT.CONTEXT.LIST, TENANT.CONTEXT.ADMIN_LIST, ROLE.LIST, TENANT.SETTINGS.VIEW]
ProjectReadOnly: [PROJECT.SETTINGS.VIEW, PROJECT.MEMBERS.VIEW, PROJECT.FILES.READ_ALL, PROJECT.ESTIMATES.READ_ALL, PROJECT.SCHEDULE.READ_ALL, PROJECT.COSTS.VIEW, PROJECT.DASHBOARD.VIEW]
```

### TenantAdmin — pełne uprawnienia projektowe (bez BaseRole)
W `BuildProjectSnapshotAsync` STEP 2 (isTenantAdmin): zamiast ładować z Role table, użyj `ModulePermissionTranslator.GetAllPermissions()` — zwraca union wszystkich Admin poziomów.

### isProjectAdmin — nowa logika
```csharp
isProjectAdmin = isTenantAdmin || permissions.Contains(PermissionCodes.ProjectSettingsEdit) && permissions.Contains(PermissionCodes.ProjectMembersManage)
```

### ProjectCtxSnapshot — usuń ProjectRoleId
```csharp
public record ProjectCtxSnapshot(
    Guid ProjectId,
    Guid TenantId,
    HashSet<string> ProjectPermissionCodes,
    bool IsProjectAdmin,
    bool IsActive
);
```

### UserRoleCode (backwards compat)
W handlerach GetProjectDetails/GetTenantProjects/UpdateProject/CreateProject:
- TenantAdmin → "TENANT.ADMIN"
- SuperAdmin → "SYSTEM.SUPERADMIN"  
- IsAdmin=true (membership) → "PROJECT.ADMIN"
- Has PROJECT.FILES.WRITE_OWN → "PROJECT.EDITOR"
- Otherwise → "PROJECT.VIEWER"
Helper: `ProjectMember.IsAdmin` + permissions check

### CQRS PermissionCode mapping

| Operacja | Stary kod | Nowy kod |
|----------|-----------|----------|
| GetProjectDetails | PROJECT.VIEW | PROJECT.SETTINGS.VIEW |
| UpdateProject | PROJECT.EDIT | PROJECT.SETTINGS.EDIT |
| UpdateProjectBudget | PROJECT.EDIT | PROJECT.SETTINGS.EDIT |
| SetProjectCurrency | PROJECT.EDIT | PROJECT.SETTINGS.EDIT |
| ToggleProjectStatus | PROJECT.STATUS.MANAGE | PROJECT.STATUS.TOGGLE |
| GetProjectDashboard | PROJECT.EDIT | PROJECT.DASHBOARD.VIEW |
| GetProjectMembers | PROJECT.MEMBERS.VIEW | (bez zmian) |
| AddProjectMember | PROJECT.MEMBERS.MANAGE | (bez zmian) |
| RemoveProjectMember | PROJECT.MEMBERS.MANAGE | (bez zmian) |
| UpdateProjectMemberRole | PROJECT.MEMBERS.MANAGE | (bez zmian) |
| GetProjectFilePackages | PROJECT.VIEW | PROJECT.FILES.READ_SHARED |
| GetPackageFiles | PROJECT.VIEW | PROJECT.FILES.READ_SHARED |
| GetFileVersions | PROJECT.VIEW | PROJECT.FILES.READ_SHARED |
| GetVersionComments | PROJECT.VIEW | PROJECT.FILES.READ_SHARED |
| AddFileVersionComment | PROJECT.RESOURCES.READ_SHARED | PROJECT.FILES.WRITE_ASSIGNED |
| UploadProjectFiles | PROJECT.RESOURCES.WRITE | PROJECT.FILES.WRITE_OWN |
| CreatePackageAndUploadFiles | PROJECT.RESOURCES.WRITE | PROJECT.FILES.WRITE_OWN |
| DeleteProjectFile | PROJECT.RESOURCES.WRITE | PROJECT.FILES.WRITE_OWN |
| UploadProjectFileVersion | PROJECT.RESOURCES.WRITE_SHARED | PROJECT.FILES.WRITE_SHARED |
| ShareProjectFiles | PROJECT.RESOURCES.SHARE | PROJECT.FILES.SHARE |
| UpdateFileShare | PROJECT.RESOURCES.SHARE | PROJECT.FILES.SHARE |
| GetCostEstimates (All) | PROJECT.RESOURCES.READ_ALL | PROJECT.ESTIMATES.READ_ALL |
| GetCostEstimates (Mine) | PROJECT.RESOURCES.READ | PROJECT.ESTIMATES.READ_OWN |
| GetCostEstimates (Shared) | PROJECT.RESOURCES.READ_SHARED | PROJECT.ESTIMATES.READ_SHARED |
| GetCostEstimateDetails | PROJECT.RESOURCES.READ_SINGLE | PROJECT.ESTIMATES.READ_SHARED |
| CreateCostEstimate | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| UpdateCostEstimate | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| DeleteCostEstimate | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| AddCostEstimateItem | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| DeleteCostEstimateItem | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| UpsertCostEstimateItemField | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| AddCostEstimateGroup | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| DeleteCostEstimateGroup | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| UpsertCostEstimateGroupField | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| ReorderCostEstimateItems | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| ReorderCostEstimateGroups | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| MoveCostEstimateItem | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| UploadCostEstimateFieldFiles | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| CopyCostEstimate | PROJECT.RESOURCES.WRITE | PROJECT.ESTIMATES.WRITE_OWN |
| RecalculateCostEstimate | PROJECT.RESOURCES.WRITE_SHARED | PROJECT.ESTIMATES.WRITE_SHARED |
| ShareCostEstimate | PROJECT.RESOURCES.SHARE | PROJECT.ESTIMATES.SHARE |
| UpdateCostEstimateShares | PROJECT.RESOURCES.SHARE | PROJECT.ESTIMATES.SHARE |
| GetProjectCosts | PROJECT.VIEW | PROJECT.COSTS.VIEW |
| GetCostLinkOptions | PROJECT.VIEW | PROJECT.COSTS.VIEW |
| CreateProjectCost | PROJECT.RESOURCES.WRITE | PROJECT.COSTS.WRITE |
| UpdateProjectCost | PROJECT.RESOURCES.WRITE | PROJECT.COSTS.WRITE |
| DeleteProjectCost | PROJECT.RESOURCES.WRITE | PROJECT.COSTS.WRITE |
| UpdateCostShare | PROJECT.RESOURCES.WRITE | PROJECT.COSTS.WRITE |
| ShareProjectCosts | PROJECT.RESOURCES.SHARE | PROJECT.COSTS.SHARE |
| CreateTrackedCost | PROJECT.EDIT | PROJECT.COSTS.WRITE |
| UpdateTrackedCost | PROJECT.EDIT | PROJECT.COSTS.WRITE |
| DeleteTrackedCost | PROJECT.EDIT | PROJECT.COSTS.WRITE |
| GetWorkSchedules | PROJECT.VIEW | PROJECT.SCHEDULE.READ_SHARED |
| GetMyWorkSchedules | PROJECT.VIEW | PROJECT.SCHEDULE.READ_SHARED |
| GetWorkSchedule | PROJECT.RESOURCES.READ_SINGLE | PROJECT.SCHEDULE.READ_SHARED |
| CreateWorkSchedule | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| UpdateWorkSchedule | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| DeleteWorkSchedule | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| AddWorkScheduleStage | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| DeleteWorkScheduleStage | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| RenameWorkScheduleStage | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| AddWorkScheduleStageWork | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| DeleteWorkScheduleStageWork | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| RenameWorkScheduleStageWork | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| SetWorkScheduleStageWorkPeriods | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| SetWorkScheduleDependencies | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| SetWorkScheduleStageWorkAssignments | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| SetWorkScheduleStageWorkColorRgb | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| MoveWorkScheduleStage | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| MoveWorkScheduleStageWork | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| ReorderWorkScheduleStages | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| ReorderWorkScheduleStageWorks | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| SyncWorkScheduleWithEstimate | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| AddWorkScheduleStageWorkComment | PROJECT.RESOURCES.WRITE_OWN | PROJECT.SCHEDULE.WRITE_ASSIGNED |
| UpdateWorkScheduleStageWorkComment | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| DeleteWorkScheduleStageWorkComment | PROJECT.RESOURCES.WRITE | PROJECT.SCHEDULE.WRITE_OWN |
| SetWorkScheduleStageWorkIsClosed | PROJECT.RESOURCES.WRITE_OWN | PROJECT.SCHEDULE.WRITE_ASSIGNED |
| SetWorkScheduleStageWorkPeriodIsClosed | PROJECT.RESOURCES.WRITE_OWN | PROJECT.SCHEDULE.WRITE_ASSIGNED |
| GetChatMessages (CQRS/Messages) | PROJECT.RESOURCES.READ | PROJECT.SETTINGS.VIEW |
| SendMessage (CQRS/Messages) | PROJECT.RESOURCES.WRITE | PROJECT.SETTINGS.VIEW |
| MarkMessagesAsRead | PROJECT.VIEW | PROJECT.SETTINGS.VIEW |
| GetTenantProjects | TENANT.VIEW | TENANT.SETTINGS.VIEW |
| GetTenantMembers | TENANT.VIEW | TENANT.SETTINGS.VIEW |
| GetProjectsDictionary | TENANT.VIEW | TENANT.SETTINGS.VIEW |
| GetContractors | TENANT.VIEW | TENANT.SETTINGS.VIEW |
| GetContractor | TENANT.VIEW | TENANT.SETTINGS.VIEW |
| UpdateTenant | TENANT.EDIT | TENANT.SETTINGS.EDIT |
| GetTenantDetails | TENANT.EDIT | TENANT.SETTINGS.EDIT |
| CreateContractor | TENANT.EDIT | TENANT.SETTINGS.EDIT |
| UpdateContractor | TENANT.EDIT | TENANT.SETTINGS.EDIT |
| DeleteContractor | TENANT.EDIT | TENANT.SETTINGS.EDIT |
| InviteTenantMember | TENANT.MEMBERS.MANAGE | (bez zmian) |
| RemoveTenantMember | TENANT.MEMBERS.MANAGE | (bez zmian) |
| RemoveTenantInvitation | TENANT.MEMBERS.MANAGE | (bez zmian) |
| UpdateTenantMemberRole | TENANT.MEMBERS.MANAGE | (bez zmian) |
| ToggleTenantStatus | TENANT.STATUS.MANAGE | TENANT.STATUS.TOGGLE |
| CreateProject | TENANT.PROJECT.CREATE | TENANT.PROJECTS.CREATE |

### RolePermissionSeedData — co usunąć
- Usunąć wszystkie `RP(RoleCodes.ProjectAdmin, ...)`, `RP(RoleCodes.ProjectEditor, ...)`, `RP(RoleCodes.ProjectViewer, ...)` — projekt nie używa Role table
- Zachować RoleCodes.ProjectAdmin/Editor/Viewer w `RoleCodes.cs` dla backwards compat (TenantAdmin inject)
- Zachować `Role` seedy dla projektu (mogą być przydatne w przyszłości jako UI labels), ale nie `RolePermission` seeds

### GetTenantProjectsQueryHandler — nowa logika isProjectAdmin
```csharp
// Zamiast: membership!.MemberRole?.Code == RoleCodes.ProjectAdmin
// Używaj: membership!.IsAdmin
bool isProjectAdmin = hasProjectMembership && membership!.IsAdmin;

// userRoleCode derivation:
string userRoleCode = membership!.IsAdmin ? RoleCodes.ProjectAdmin
    : permissions.Contains(PermissionCodes.ProjectFilesWriteOwn) ? RoleCodes.ProjectEditor
    : RoleCodes.ProjectViewer;
```

### GetProjectDetailsQueryHandler, UpdateProjectCommandHandler
Analogicznie — zastąpić `MemberRole?.Code` przez `membership.IsAdmin`.

### UserService.GetProjectMembersByIdsAsync
```csharp
// Zamiast: RoleCode = pm.MemberRole?.Code
// Używaj: RoleCode = pm.IsAdmin ? RoleCodes.ProjectAdmin : null
```

### CopyCostEstimateCommandValidator
```csharp
// Zamiast: membership.MemberRole?.Code.IsProjectAdmin() != true
// Używaj: !membership.IsAdmin
```

### Testy — co zaktualizować
- `AccessServiceTests.cs` — usunąć `ProjectRoleId` z ProjectCtxSnapshot constructor
- `InMemoryUserContextCacheTests.cs` — j.w.
- `WebModelFactory.cs` — zaktualizować snapshot factory
- `GetProjectDetailsQueryHandlerTests.cs` — zmienić oczekiwany `UserRoleCode` (nadal PROJECT.EDITOR, bo IsAdmin=false + has WRITE_OWN)
- `CreateProjectCommandHandlerTests.cs` — zmienić jeśli używa RoleId

## Nowa encja ProjectMemberModulePermission

```csharp
public class ProjectMemberModulePermission
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectModule Module { get; set; }
    public ModuleAccessLevel AccessLevel { get; set; }
    public ProjectMember ProjectMember { get; set; } = default!;
}
```

PK: (TenantId, ProjectId, UserId, Module)

## EF migracja
Po zmianach uruchomić:
```
dotnet ef migrations add migration-3-module-permissions --project src/Entities --startup-project src/WebApi
```

## RolePermissionSeedData — nowe PermissionSeed
Seedy dla nowych kodów (PermissionSeed) muszą być zaktualizowane. 
Stare PROJECT.RESOURCES.* i PROJECT.VIEW/EDIT zastąpione przez nowe kody.
