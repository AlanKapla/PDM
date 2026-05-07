# Audyt Entities.csproj — Raport kompleksowy

> **Data audytu:** 2025
> **Projekt:** `Entities.csproj` — PDM / Brickly
> **Zakres:** Encje, konfiguracje EF Core, relacje, multitenancy, dziedziczenie
> **Tryb:** Tylko analiza — ZERO zmian w kodzie

---

## BLOK 1 — MAPA ENCJI

### 1.1 Tożsamość i użytkownicy (Identity)

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `User` | `Entities.Models` | `BaseEntity` | ✗ | ✗ | ✓ | ✗ |
| `UserSession` | `Entities.Models` | `BaseEntity` | ✗ | ✗ | ✓ | ✗ |
| `TenantInvitation` | `Entities.Models` | *(brak)* | ✓ | ✗ | ✓ | ✗ |
| `UserProfileBase` | `Entities.Models` | `BaseEntity` (abstract) | ✗ | ✗ | ✗ | ✗ |
| `TenantPreferencesProfile` | `Entities.Models` | `UserProfileBase` | ✗ | ✗ | ✗ | ✗ |
| `PermissionsVersionProfile` | `Entities.Models` | `UserProfileBase` | ✗ | ✗ | ✗ | ✗ |

- **`User`** — konto użytkownika z identyfikatorem AzureAD B2C; globalny (cross-tenant).
- **`UserSession`** — sesja refresh-token użytkownika.
- **`TenantInvitation`** — zaproszenie do tenanta; zawiera token, daty wygaśnięcia i status.
- **`UserProfileBase`** — TPH base class dla profili użytkownika (ustawienia, wersja uprawnień).
- **`TenantPreferencesProfile`** — aktywny tenant w UI.
- **`PermissionsVersionProfile`** — wersja cacha uprawnień użytkownika.

---

### 1.2 Tenant i organizacja

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `Tenant` | `Entities.Models` | `BaseEntity` | ✗ (jest Tenantam) | ✗ | ✓ | ✗ |
| `TenantMember` | `Entities.Models` | *(brak)* | ✓ | ✗ | ✓ | ✗ |
| `Role` | `Entities.Models` | `BaseEntity` | ✗ | ✗ | ✓ | ✓ |
| `Permission` | `Entities.Models` | `BaseEntity` | ✗ | ✗ | ✓ | ✓ |
| `RolePermission` | `Entities.Models` | *(brak)* | ✗ | ✗ | ✓ | ✗ |

- **`Tenant`** — firma/organizacja; korzeń izolacji danych.
- **`TenantMember`** — composite PK `(TenantId, UserId)`; przynależność użytkownika do tenanta z rolą.
- **`Role`** — rola (builtin/custom) o zakresie `Tenant` lub `Project`.
- **`Permission`** — uprawnienie atomowe; skojarzone z rolami przez `RolePermission`.
- **`RolePermission`** — composite PK `(RoleId, PermissionId)`; relacja M:N.

---

### 1.3 Projekt

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `Project` | `Entities.Models` | `BaseEntity` | ✓ | ✗ (jest Projektem) | ✓ | ✗ |
| `ProjectMember` | `Entities.Models` | *(brak)* | ✓ | ✓ | ✗ *(JoinedAt)* | ✗ |
| `ProjectGroup` | `Entities.Models` | `BaseEntity` | ✗ | ✓ | ✗ | ✗ |
| `ProjectGroupMember` | `Entities.Models` | *(brak)* | ✓ | ✓ | ✗ | ✗ |

- **`Project`** — inwestycja; zawiera budżet (net/gross).
- **`ProjectMember`** — composite PK `(TenantId, ProjectId, UserId)`; członek projektu z rolą.
- **`ProjectGroup`** — grupa/podgrupa członków projektu.
- **`ProjectGroupMember`** — composite PK `(ProjectGroupId, ProjectId, TenantId, UserId)`.

---

### 1.4 Kosztorys (CostEstimate)

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `CostEstimate` | `Entities.Models.CostEstimates` | `DeletableEntity` | ✓ | ✓ | ✓ | ✓ |
| `CostEstimateGroup` | `Entities.Models.CostEstimates` | `DeletableEntity` | ✗ | ✗ | ✓ | ✓ |
| `CostEstimateItem` | `Entities.Models.CostEstimates` | `DeletableEntity` | ✗ | ✗ | ✓ | ✓ |
| `CostEstimateItemFieldValue` | `Entities.Models.CostEstimates` | `CostEstimateFieldValueBase` → `BaseEntity` | ✗ | ✗ | ✓ | ✓* |
| `CostEstimateGroupFieldValue` | `Entities.Models.CostEstimates` | `CostEstimateFieldValueBase` → `BaseEntity` | ✗ | ✗ | ✓ | ✓* |
| `CostEstimateFieldFile` | `Entities.Models.CostEstimates` | `BaseEntity` ⚠️ | ✗ | ✗ | ✓ | ✗ |
| `SharedCostEstimate` | `Entities.Models.CostEstimates` | `BaseEntity` | ✓ | ✓ | ✗ *(SharedAt)* | ✗ |

*UpdatedAt w FieldValueBase obsługiwane przez hook w `SaveChangesAsync`

- **`CostEstimate`** — kosztorys projektu oparty na szablonie; posiada status i obliczone sumy.
- **`CostEstimateGroup`** — zagnieżdżona grupa kosztorysu; samoodniesienie przez `ParentGroupId`.
- **`CostEstimateItem`** — pozycja kosztorysu z hierarchią `None/Option/Component`.
- **`CostEstimateItemFieldValue`** — wartość pola pozycji (typowana: string/decimal/bool/datetime).
- **`CostEstimateGroupFieldValue`** — wartość pola nagłówka grupy.
- **`CostEstimateFieldFile`** — plik załączony do pola kosztorysu; **⚠️ ręczne soft-delete bez `DeletableEntity`**.
- **`SharedCostEstimate`** — udostępnienie kosztorysu użytkownikowi projektu.

---

### 1.5 Szablon kosztorysu (CostEstimateTemplate)

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `CostEstimateTemplate` | `Entities.Models.CostEstimateTemplates` | `BaseEntity` ⚠️ | ✗ | ✗ | ✓ | ✓ |
| `CostEstimateTemplateCurrency` | `Entities.Models.CostEstimateTemplates` | `BaseEntity` | ✗ | ✗ | ✗ | ✗ |
| `CostEstimateTemplateUnit` | `Entities.Models.CostEstimateTemplates` | `BaseEntity` | ✗ | ✗ | ✗ | ✗ |
| `CostEstimateTemplateCategory` | `Entities.Models.CostEstimateTemplates` | `BaseEntity` | ✗ | ✗ | ✗ | ✗ |
| `CostEstimateTemplateFieldDefinitionBase` | `Entities.Models.CostEstimateTemplates` | `BaseEntity` (abstract) | ✗ | ✗ | ✗ | ✗ |
| `CostEstimateTemplateGroupFieldDefinition` | `Entities.Models.CostEstimateTemplates` | `CostEstimateTemplateFieldDefinitionBase` | ✗ | ✗ | ✗ | ✗ |
| `CostEstimateTemplateItemSystemFieldDefinition` | `Entities.Models.CostEstimateTemplates` | `CostEstimateTemplateFieldDefinitionBase` | ✗ | ✗ | ✗ | ✗ |
| `CostEstimateTemplateItemCalculatedFieldDefinition` | `Entities.Models.CostEstimateTemplates` | `CostEstimateTemplateFieldDefinitionBase` | ✗ | ✗ | ✗ | ✗ |
| `CostEstimateTemplateItemGenericFieldDefinition` | `Entities.Models.CostEstimateTemplates` | `CostEstimateTemplateFieldDefinitionBase` | ✗ | ✗ | ✗ | ✗ |

- **`CostEstimateTemplate`** — szablon per user (OwnerId); **brak TenantId** — ryzyko cross-tenant; **⚠️ ręczne soft-delete bez `DeletableEntity`**.
- **`CostEstimateTemplateCurrency/Unit/Category`** — słowniki wartości szablonu.
- **`CostEstimateTemplateFieldDefinitionBase`** — TPH base; definiuje kolumny pól grupy i pozycji.
- Pochodne: `GroupFieldDefinition`, `ItemSystemFieldDefinition`, `ItemCalculatedFieldDefinition`, `ItemGenericFieldDefinition`.

---

### 1.6 Harmonogram (WorkSchedule)

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `WorkSchedule` | `Entities.Models` | `DeletableEntity` | ✓ | ✓ | ✓ | ✗ |
| `WorkScheduleStage` | `Entities.Models` | `DeletableEntity` | ✓ | ✓ | ✗ | ✓ |
| `WorkScheduleStageWork` | `Entities.Models` | `DeletableEntity` | ✓ | ✓ | ✗ | ✓ |
| `WorkScheduleStageWorkPeriod` | `Entities.Models` | `BaseEntity` | ✓ | ✓ | ✗ | ✗ |
| `WorkScheduleStageWorkAssignment` | `Entities.Models` | *(brak)* | ✓ | ✓ | ✗ | ✗ |
| `WorkScheduleStageWorkComment` | `Entities.Models` | `BaseEntity` | ✓ | ✗ ⚠️ | ✓ | ✗ |
| `WorkScheduleStageWorkDependency` | `Entities.Models` | `BaseEntity` | ✓ | ✓ | ✗ | ✗ |

- **`WorkSchedule`** — harmonogram projektu; opcjonalne powiązanie z kosztorysem.
- **`WorkScheduleStage`** — etap harmonogramu; zagnieżdżony (ParentStageId); powiązany z grupą kosztorysu.
- **`WorkScheduleStageWork`** — zakres pracy; denormalizacja `PlannedStartDate`/`PlannedEndDate`.
- **`WorkScheduleStageWorkPeriod`** — okno czasowe (StartDate/EndDate) dla zakresu pracy.
- **`WorkScheduleStageWorkAssignment`** — przypisanie członka projektu do zakresu pracy.
- **`WorkScheduleStageWorkComment`** — komentarz do zakresu pracy; **brak ProjectId ⚠️**.
- **`WorkScheduleStageWorkDependency`** — zależność FS/SS/FF/SF między pracami.

---

### 1.7 Koszty (ProjectCost / TrackedCost)

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `ProjectCost` | `Entities.Models` | `DeletableEntity` | ✓ | ✓ | ✓ | ✓ |
| `SharedProjectCost` | `Entities.Models` | `BaseEntity` | ✓ | ✓ | ✗ *(SharedAt)* | ✗ |
| `TrackedCost` | `Entities.Models.CostTrackers` | `DeletableEntity` | ✓ | ✓ | ✓ | ✓ |
| `TrackedCostAttachment` | `Entities.Models.CostTrackers` | `DeletableEntity` | ✗ ⚠️ | ✗ ⚠️ | ✓ | ✗ |
| `ProjectCostTrackedCostLink` | `Entities.Models.CostTrackers` | `BaseEntity` | ✗ | ✗ | ✗ *(LinkedAt)* | ✗ |

- **`ProjectCost`** — koszt zgłoszony przez członka projektu; opcjonalny dokument blob.
- **`SharedProjectCost`** — udostępnienie kosztu innemu członkowi projektu.
- **`TrackedCost`** — zatwierdzony, śledzony koszt projektu; może być powiązany z pozycją kosztorysu lub zakresem pracy.
- **`TrackedCostAttachment`** — załącznik do TrackedCost; **brak TenantId/ProjectId ⚠️**.
- **`ProjectCostTrackedCostLink`** — tabela łącząca ProjectCost z TrackedCost; **brak DbSet i konfiguracji EF ⚠️**.

---

### 1.8 Pliki projektu (ProjectFile)

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `ProjectFilePackage` | `Entities.Models` | `DeletableEntity` | ✓ | ✓ | ✓ | ✗ |
| `ProjectFile` | `Entities.Models` | `DeletableEntity` | ✓ | ✓ | ✓ | ✗ |
| `ProjectFileVersion` | `Entities.Models` | `DeletableEntity` | ✗ ⚠️ | ✗ ⚠️ | ✓ | ✗ |
| `ProjectFileVersionComment` | `Entities.Models` | `DeletableEntity` | ✓ | ✓ | ✓ | ✗ *(EditedAt)* |
| `SharedProjectFile` | `Entities.Models` | `BaseEntity` | ✓ | ✓ | ✗ *(SharedAt)* | ✗ |

- **`ProjectFilePackage`** — paczka/folder grupujący pliki; unikalność `(TenantId, ProjectId, OwnerId, Name)`.
- **`ProjectFile`** — plik projektu z metadanymi; wskaźnik na aktualną wersję.
- **`ProjectFileVersion`** — wersja pliku z danymi Blob Storage; **brak TenantId/ProjectId ⚠️**.
- **`ProjectFileVersionComment`** — komentarz do wersji pliku.
- **`SharedProjectFile`** — wpis udostępnienia paczki lub pliku z typem dostępu Allow/Deny.

---

### 1.9 Komunikacja

| Encja | Namespace | Dziedziczy po | TenantId | ProjectId | CreatedAt | UpdatedAt |
|-------|-----------|--------------|----------|-----------|-----------|-----------|
| `Chat` | `Entities.Models` | `BaseEntity` | ✓ *(nullable)* | ✓ *(nullable)* | ✓ | ✗ |
| `ChatMember` | `Entities.Models` | `BaseEntity` | ✗ | ✗ | ✗ *(JoinedAt)* | ✗ |
| `MessageHistory` | `Entities.Models` | `BaseEntity` | ✗ | ✗ | ✓ | ✗ *(EditedAt)* |
| `Notification` | `Entities.Models` | `BaseEntity` | ✓ | ✓ *(nullable)* | ✓ (`DateTimeOffset`) ⚠️ | ✗ |

- **`Chat`** — czat globalny lub projektowy; TenantId/ProjectId opcjonalne.
- **`ChatMember`** — członek chatu; brak nawigacji do `User` w konfiguracji.
- **`MessageHistory`** — wiadomość czatu; soft-delete przez `DeletedAt` (computed property), **nie przez `DeletableEntity`**.
- **`Notification`** — powiadomienie systemowe; `CreatedAt` jako `DateTimeOffset` — jedyna encja w projekcie z tym typem ⚠️.

---

## BLOK 2 — HIERARCHIA DZIEDZICZENIA

### 2.1 Diagram

```
Object
└── BaseEntity (Id: Guid = NewGuid())
    ├── DeletableEntity [abstract] (IsDeleted: bool, DeletedAt: DateTime?)
    │   ├── CostEstimate
    │   ├── CostEstimateGroup
    │   ├── CostEstimateItem
    │   ├── ProjectCost
    │   ├── TrackedCost
    │   ├── TrackedCostAttachment
    │   ├── WorkSchedule
    │   ├── WorkScheduleStage
    │   ├── WorkScheduleStageWork
    │   ├── ProjectFile
    │   ├── ProjectFilePackage
    │   ├── ProjectFileVersion
    │   └── ProjectFileVersionComment
    │
    ├── UserProfileBase [abstract] (UserId: Guid)
    │   ├── TenantPreferencesProfile
    │   └── PermissionsVersionProfile
    │
    ├── CostEstimateFieldValueBase [abstract]
    │   ├── CostEstimateItemFieldValue
    │   └── CostEstimateGroupFieldValue
    │
    ├── CostEstimateTemplateFieldDefinitionBase [abstract]
    │   ├── CostEstimateTemplateGroupFieldDefinition
    │   ├── CostEstimateTemplateItemSystemFieldDefinition
    │   ├── CostEstimateTemplateItemCalculatedFieldDefinition
    │   └── CostEstimateTemplateItemGenericFieldDefinition
    │
    ├── User, UserSession, Tenant, Project, ProjectGroup
    ├── Role, Permission
    ├── Chat, ChatMember, MessageHistory, Notification
    ├── WorkScheduleStageWorkPeriod, WorkScheduleStageWorkComment
    ├── WorkScheduleStageWorkDependency, ProjectCostTrackedCostLink
    ├── SharedCostEstimate, SharedProjectFile, SharedProjectCost
    ├── CostEstimateTemplateCurrency, CostEstimateTemplateUnit
    ├── CostEstimateTemplateCategory
    ├── CostEstimateFieldFile  ⚠️ (ma IsDeleted/DeletedAt ręcznie)
    └── CostEstimateTemplate   ⚠️ (ma IsDeleted/DeletedAt ręcznie)

(bez klasy bazowej — composite PK lub własne Id)
├── TenantMember
├── ProjectMember
├── ProjectGroupMember
├── RolePermission
├── WorkScheduleStageWorkAssignment
└── TenantInvitation  ⚠️ (własne public Guid Id)
```

### 2.2 Problemy dziedziczenia

| Encja | Problem | Rekomendacja |
|-------|---------|-------------|
| `CostEstimateTemplate` | Implementuje soft-delete (`IsDeleted`, `DeletedAt`) ręcznie mimo istnienia `DeletableEntity`; brak `GlobalQueryFilter` | Zmienić dziedziczenie na `DeletableEntity` |
| `CostEstimateFieldFile` | Implementuje soft-delete (`IsDeleted`, `DeletedAt`) ręcznie; brak `GlobalQueryFilter` | Zmienić dziedziczenie na `DeletableEntity` |
| `MessageHistory` | `DeletedAt` jako pole manualne; `IsDeleted` jako computed property `=> DeletedAt.HasValue`; brak filtra EF | Zdecydować: przenieść do `DeletableEntity` lub zostawić jako wzorzec wiadomości (soft-hide) |
| `TenantInvitation` | Własne `public Guid Id` zamiast dziedziczenia po `BaseEntity` | Dodać `: BaseEntity` i usunąć ręczne `Id` |

---

## BLOK 3 — MAPA RELACJI

### 3.1 Tabela relacji

| Encja A | Relacja | Encja B | FK pole | Nullable | DeleteBehavior | EF skonfigurowane |
|---------|---------|---------|---------|----------|---------------|------------------|
| `Project` | N:1 | `Tenant` | `TenantId` | ✗ | Cascade | ✓ |
| `ProjectMember` | N:1 | `Project` | `ProjectId` | ✗ | Cascade | ✓ |
| `ProjectMember` | N:1 | `TenantMember` | `(TenantId, UserId)` | ✗ | Restrict | ✓ |
| `ProjectMember` | N:1 | `Role` | `RoleId` | ✓ | SetNull | ✓ |
| `ProjectGroup` | N:1 | `Project` | `ProjectId` | ✗ | Cascade | ✓ |
| `ProjectGroupMember` | N:1 | `ProjectGroup` | `ProjectGroupId` | ✗ | Cascade | ✓ |
| `ProjectGroupMember` | N:1 | `ProjectMember` | `(ProjectId, TenantId, UserId)` | ✗ | NoAction | ✓ |
| `TenantMember` | N:1 | `Tenant` | `TenantId` | ✗ | Cascade | ✓ |
| `TenantMember` | N:1 | `User` | `UserId` | ✗ | Cascade | ✓ |
| `UserSession` | N:1 | `User` | `UserId` | ✗ | Cascade | ✓ |
| `WorkSchedule` | N:1 | `Project` | `ProjectId` | ✗ | Cascade | ✓ |
| `WorkSchedule` | N:1 | `CostEstimate` | `CostEstimateId` | ✓ | SetNull | ✓ |
| `WorkScheduleStage` | N:1 | `WorkSchedule` | `WorkScheduleId` | ✗ | Cascade | ✓ |
| `WorkScheduleStage` | N:1 | `WorkScheduleStage` (self) | `ParentStageId` | ✓ | Restrict | ✓ |
| `WorkScheduleStage` | N:1 | `CostEstimateGroup` | `CostEstimateGroupId` | ✓ | SetNull | ✓ |
| `WorkScheduleStageWork` | N:1 | `WorkScheduleStage` | `WorkScheduleStageId` | ✗ | Cascade | ✓ |
| `WorkScheduleStageWork` | N:1 | `CostEstimateItem` | `CostEstimateItemId` | ✓ | SetNull | ✓ |
| `WorkScheduleStageWorkPeriod` | N:1 | `WorkScheduleStageWork` | `WorkScheduleStageWorkId` | ✗ | Cascade (via HasMany) | ✓ |
| `WorkScheduleStageWorkAssignment` | N:1 | `WorkScheduleStageWork` | `WorkScheduleStageWorkId` | ✗ | Cascade | ✓ |
| `WorkScheduleStageWorkComment` | N:1 | `WorkScheduleStageWork` | `WorkScheduleStageWorkId` | ✗ | Cascade | ✓ |
| `WorkScheduleStageWorkDependency` | N:1 | `WorkSchedule` | `WorkScheduleId` | ✗ | Cascade | ✓ |
| `WorkScheduleStageWorkDependency` | N:1 | `WorkScheduleStageWork` (predecessor) | `PredecessorWorkId` | ✗ | Restrict | ✓ |
| `WorkScheduleStageWorkDependency` | N:1 | `WorkScheduleStageWork` (successor) | `SuccessorWorkId` | ✗ | Restrict | ✓ |
| `CostEstimate` | N:1 | `CostEstimateTemplate` | `TemplateId` | ✗ | Restrict | ✓ |
| `CostEstimate` | N:1 | `Project` | `ProjectId` | ✗ | Restrict | ✓ |
| `CostEstimate` | N:1 | `User` (Owner) | `OwnerId` | ✗ | Restrict | ✓ |
| `CostEstimateGroup` | N:1 | `CostEstimate` | `CostEstimateId` | ✗ | Restrict | ✓ |
| `CostEstimateGroup` | N:1 | `CostEstimateGroup` (self) | `ParentGroupId` | ✓ | Restrict | ✓ |
| `CostEstimateItem` | N:1 | `CostEstimate` | `CostEstimateId` | ✗ | Restrict | ✓ |
| `CostEstimateItem` | N:1 | `CostEstimateGroup` | `GroupId` | ✗ | Cascade | ✓ |
| `CostEstimateItem` | N:1 | `CostEstimateItem` (self) | `ParentItemId` | ✓ | Restrict | ✓ |
| `CostEstimateItemFieldValue` | N:1 | `CostEstimateItem` | `ItemId` | ✗ | Cascade | ✓ |
| `CostEstimateItemFieldValue` | N:1 | `CostEstimateTemplateFieldDefinitionBase` | `FieldDefinitionId` | ✗ | Cascade | ✓ |
| `CostEstimateGroupFieldValue` | N:1 | `CostEstimateGroup` | `GroupId` | ✗ | Cascade | ✓ |
| `CostEstimateGroupFieldValue` | N:1 | `CostEstimateTemplateGroupFieldDefinition` | `FieldDefinitionId` | ✗ | Cascade | ✓ |
| `CostEstimateFieldFile` | N:1 | `CostEstimateItemFieldValue` | `FieldValueId` | ✗ | Cascade | ✓ |
| `CostEstimateFieldFile` | N:1 | `CostEstimate` | `CostEstimateId` | ✗ | Restrict | ✓ |
| `SharedCostEstimate` | N:1 | `CostEstimate` | `CostEstimateId` | ✗ | Restrict | ✓ |
| `ProjectCost` | N:1 | `Project` | `ProjectId` | ✗ | Restrict | ✓ |
| `SharedProjectCost` | N:1 | `ProjectCost` | `ProjectCostId` | ✗ | Cascade | ✓ |
| `TrackedCost` | N:1 | `CostEstimateItem` | `CostEstimateItemId` | ✓ | SetNull | ✓ |
| `TrackedCost` | N:1 | `WorkScheduleStageWork` | `WorkScheduleStageWorkId` | ✓ | SetNull | ✓ |
| `TrackedCostAttachment` | N:1 | `TrackedCost` | `TrackedCostId` | ✗ | **Restrict** ⚠️ | ✓ |
| `ProjectCostTrackedCostLink` | N:1 | `ProjectCost` | `ProjectCostId` | ✗ | ? | ✗ ⚠️ |
| `ProjectCostTrackedCostLink` | N:1 | `TrackedCost` | `TrackedCostId` | ✗ | ? | ✗ ⚠️ |
| `ProjectFile` | N:1 | `ProjectFilePackage` | `ProjectFilePackageId` | ✗ | Restrict | ✓ |
| `ProjectFile` | N:1 | `Project` | `ProjectId` | ✗ | Cascade | ✓ |
| `ProjectFile` | N:1 | `ProjectFileVersion` (current) | `CurrentVersionId` | ✓ | Restrict | ✓ |
| `ProjectFileVersion` | N:1 | `ProjectFile` | `ProjectFileId` | ✗ | Cascade | ✓ |
| `ProjectFileVersionComment` | N:1 | `ProjectFileVersion` | `ProjectFileVersionId` | ✗ | Cascade | ✓ |
| `SharedProjectFile` | N:1 | `ProjectFilePackage` | `ProjectFilePackageId` | ✗ | Restrict | ✓ |
| `SharedProjectFile` | N:1 | `ProjectFile` | `ProjectFileId` | ✓ | Cascade | ✓ |
| `ChatMember` | N:1 | `Chat` | `ChatId` | ✗ | Cascade | ✓ |
| `MessageHistory` | N:1 | `Chat` | `ChatId` | ✗ | Cascade | ✓ |
| `MessageHistory` | N:1 | `MessageHistory` (self) | `ReplyToMessageId` | ✓ | Restrict | ✓ |
| `Notification` | N:1 | `Tenant` | `TenantId` | ✗ | ? | ✗ ⚠️ |
| `TenantInvitation` | N:1 | `User` | `InvitedByUserId` | ✗ | ? | ✗ ⚠️ |
| `RolePermission` | N:1 | `Role` | `RoleId` | ✗ | Cascade | ✓ |
| `RolePermission` | N:1 | `Permission` | `PermissionId` | ✗ | Cascade | ✓ |
| `UserProfileBase` | N:1 | `User` | `UserId` | ✗ | Cascade | ✓ (duplikat!) |

### 3.2 Problemy relacji

| Relacja | Problem | Ryzyko |
|---------|---------|--------|
| `TrackedCostAttachment → TrackedCost` | `OnDelete(Restrict)` zamiast `Cascade` — usunięcie TrackedCost nie usuwa załączników | Orphaned records; błąd FK przy próbie usunięcia TrackedCost bez ręcznego czyszczenia |
| `ProjectCostTrackedCostLink` | Brak konfiguracji EF i DbSet; relacja FK istnieje tylko w modelu | Brak klucza PK, indeksów, reguł DELETE; tabela może być niespójna z migracjami |
| `Notification → Tenant` | Brak konfiguracji relacji w `NotificationConfiguration` | EF może użyć domyślnego Cascade niezgodnie z intencją |
| `TenantInvitation → User` | Brak konfiguracji EF w całości | Brak indeksów, MaxLength, DeleteBehavior; domyślne zachowanie EF |
| `UserProfileBase → User` | Duplikat konfiguracji: `UserProfileConfiguration` + inline w `AppDbContext.OnModelCreating`; discriminator w AppDbContext nie zawiera `PermissionsVersion` | Konflikt konfiguracji; `PermissionsVersionProfile` może nie być mapowany poprawnie |
| `WorkScheduleStageWorkAssignment → Tenant/Project` | Redundantne FK bezpośrednio do `Tenant` i `Project` mimo że dane dostępne przez `ProjectMember` | Over-engineering FK; dodatkowe indeksy; złożona konfiguracja |

---

## BLOK 4 — KONFIGURACJE EF

### 4.1 Indeksy

**Zdefiniowane indeksy (kluczowe):**

| Encja | Pola indeksu | Composite | Unikalny |
|-------|-------------|-----------|---------|
| `User` | `Email` | ✗ | ✓ |
| `User` | `AzureAdB2CObjectId` | ✗ | ✓ |
| `Role` | `(Scope, Code)` | ✓ | ✓ |
| `Permission` | `Code` | ✗ | ✓ |
| `WorkSchedule` | `(TenantId, ProjectId, IsDeleted)` | ✓ | ✗ |
| `WorkScheduleStage` | `(WorkScheduleId, Order)` | ✓ | ✗ |
| `WorkScheduleStage` | `CostEstimateGroupId` | ✗ | ✗ |
| `WorkScheduleStageWork` | `CostEstimateItemId` | ✗ | ✗ |
| `WorkScheduleStageWorkDependency` | `(WorkScheduleId, PredecessorWorkId, SuccessorWorkId, DependencyType)` | ✓ | ✓ |
| `CostEstimate` | `(TenantId, ProjectId)` | ✓ | ✗ |
| `CostEstimateGroup` | `(CostEstimateId, ParentGroupId)` | ✓ | ✗ |
| `CostEstimateItem` | `(GroupId, Order)` | ✓ | ✗ |
| `CostEstimateItemFieldValue` | `(ItemId, FieldDefinitionId)` | ✓ | ✗ |
| `CostEstimateGroupFieldValue` | `(GroupId, FieldDefinitionId)` | ✓ | ✓ |
| `ProjectFilePackage` | `(TenantId, ProjectId, OwnerId, Name)` + `HasFilter("[IsDeleted]=0")` | ✓ | ✓ |
| `ProjectFileVersion` | `(ProjectFileId, VersionNumber)` | ✓ | ✓ |
| `SharedProjectFile` | `(ProjectFilePackageId, ProjectFileId, SharedWithUserId)` | ✓ | ✓ |
| `SharedProjectCost` | `(ProjectCostId, SharedWithUserId)` | ✓ | ✓ |
| `SharedCostEstimate` | `(CostEstimateId, SharedWithUserId)` | ✓ | ✓ |
| `ChatMember` | `(ChatId, UserId)` | ✓ | ✓ |
| `MessageHistory` | `(ChatId, CreatedAt)` | ✓ | ✗ |
| `FieldDefinitionBase` | `(TemplateId, FieldName)` | ✓ | ✗ |

**Brakujące indeksy:**

| Encja | Brakujący indeks | Powód |
|-------|-----------------|-------|
| `TenantInvitation` | `(TenantId, Email)`, `Token` (unique), `Status`, `ExpiresAt` | Brak konfiguracji EF; zapytania per tenant/email/token |
| `TrackedCost` | `(TenantId, ProjectId, IsDeleted)` composite | Standardowy pattern reszty encji |
| `Notification` | `(TenantId, ProjectId, Readed)` | Filtrowanie per projekt i status |
| `ProjectGroupMember` | `(TenantId, UserId)` | Wyszukiwanie grup użytkownika |
| `CostEstimateTemplate` | `(OwnerId, IsDeleted)` | Brak GlobalQueryFilter; zapytania per owner |
| `CostEstimateFieldFile` | `(FieldValueId, IsDeleted)` | Brak GlobalQueryFilter |
| `ProjectCostTrackedCostLink` | `(ProjectCostId)`, `(TrackedCostId)` | Brak konfiguracji EF |
| `WorkScheduleStageWorkPeriod` | `(TenantId, ProjectId, StartDate, EndDate)` | Zapytania o zakresy dat dla projektu |

---

### 4.2 Precyzje decimal

| Encja | Pole | Ma HasPrecision | Wartość |
|-------|------|----------------|---------|
| `Project` | `BudgetNet`, `BudgetGross` | ✓ | `(18,4)` |
| `CostEstimate` | `TotalNet`, `TotalGross`, `TotalVat` | ✓ | `(18,2)` |
| `CostEstimateGroup` | `TotalNet`, `TotalGross`, `TotalVat` | ✓ | `(18,2)` |
| `CostEstimateItem` | `NetValue`, `GrossValue`, `VatValue` | ✓ | `(18,2)` |
| `CostEstimateItemFieldValue` | `DecimalValue` | ✓ | `(18,6)` |
| `CostEstimateGroupFieldValue` | `DecimalValue` | ✓ | `(18,6)` |
| `ProjectCost` | `NetAmount`, `GrossAmount` | ✓ | `(18,2)` |
| `TrackedCost` | `Net`, `Gross` | ✗ ⚠️ | `HasColumnType("decimal(15,2)")` — niespójna precyzja |

**Pola decimal bez `HasPrecision` (lub z niespójną konfiguracją):**

| Encja | Pole | Problem |
|-------|------|---------|
| `TrackedCost` | `Net`, `Gross` | Używa `HasColumnType("decimal(15,2)")` zamiast `HasPrecision(18,2)`; mniejsza precyzja niż reszta systemu |

---

### 4.3 MaxLength na string

**Pola string bez `HasMaxLength`:**

| Encja | Pole | Problem |
|-------|------|---------|
| `TenantInvitation` | `Email`, `Token` | Brak konfiguracji EF; domyślny `nvarchar(max)` |
| `TrackedCost` | `Number` (nullable) | Brak `HasMaxLength` — `nvarchar(max)` |

---

### 4.4 GlobalQueryFilter

| Encja | Ma filtr | Wyrażenie | Problem |
|-------|---------|-----------|---------|
| `WorkSchedule` | ✓ | `!w.IsDeleted` | OK |
| `WorkScheduleStage` | ✓ | `!s.IsDeleted` | OK |
| `WorkScheduleStageWork` | ✓ | `!w.IsDeleted` | OK |
| `CostEstimate` | ✓ | `!c.IsDeleted` | OK |
| `CostEstimateGroup` | ✓ | `!g.IsDeleted` | OK |
| `CostEstimateItem` | ✓ | `!w.IsDeleted` | OK |
| `ProjectFile` | ✓ | `!pf.IsDeleted` | OK |
| `ProjectFilePackage` | ✓ | `!pfp.IsDeleted` | OK |
| `ProjectFileVersion` | ✓ | `!pfv.IsDeleted` | OK |
| `ProjectFileVersionComment` | ✓ | `!c.IsDeleted` | OK |
| `ProjectCost` | ✓ | `!pc.IsDeleted` | OK |
| `TrackedCost` | ✓ | `!tc.IsDeleted` | OK |
| `TrackedCostAttachment` | ✓ | `!a.IsDeleted` | OK |
| `CostEstimateTemplate` | ✗ ⚠️ | — | Ma `IsDeleted` ale BRAK filtra; zwraca usunięte szablony! |
| `CostEstimateFieldFile` | ✗ ⚠️ | — | Ma `IsDeleted` ale BRAK filtra; zwraca usunięte pliki! |
| `MessageHistory` | ✗ | — | `IsDeleted` jako computed property; nie kwalifikuje się do GlobalQueryFilter |

---

### 4.5 DeleteBehavior nieskonfigurowany

| Relacja | Domyślny EF (SQL Server) | Ryzyko |
|---------|--------------------------|--------|
| `Notification → Tenant` | Cascade (jeśli required) | Usunięcie tenanta kaskadowo usuwa powiadomienia — niezamierzone |
| `TenantInvitation → User` | Cascade | Usunięcie usera usuwa zaproszenia — może być niezamierzone |
| `ProjectCostTrackedCostLink → ProjectCost/TrackedCost` | Nieokreślony (brak konfiguracji) | Brak gwarancji integralności; możliwy orphan lub błąd FK |
| `ChatMember → User` | Brak konfiguracji nawigacji do User | EF ignoruje; niespójna kolekcja |

---

## BLOK 5 — IZOLACJA MULTITENANCY

### 5.1 Izolacja per encja

| Encja | Ma TenantId | Ma ProjectId | Izolacja przez |
|-------|------------|-------------|---------------|
| `Tenant` | ✗ | ✗ | Jest korzeniem |
| `TenantMember` | ✓ | ✗ | Bezpośredni TenantId |
| `TenantInvitation` | ✓ | ✗ | Bezpośredni TenantId |
| `User` | ✗ | ✗ | Cross-tenant (global) |
| `UserSession` | ✗ | ✗ | Przez User |
| `UserProfileBase` | ✗ | ✗ | Przez User |
| `Project` | ✓ | ✗ | Bezpośredni TenantId |
| `ProjectMember` | ✓ | ✓ | Bezpośrednie |
| `ProjectGroup` | ✗ | ✓ | Przez Project |
| `ProjectGroupMember` | ✓ | ✓ | Bezpośrednie |
| `Role` | ✗ | ✗ | Globalne (builtin) |
| `Permission` | ✗ | ✗ | Globalne (builtin) |
| `RolePermission` | ✗ | ✗ | Przez Role |
| `CostEstimate` | ✓ | ✓ | Bezpośrednie |
| `CostEstimateGroup` | ✗ | ✗ | Przez CostEstimate |
| `CostEstimateItem` | ✗ | ✗ | Przez CostEstimate |
| `CostEstimateItemFieldValue` | ✗ | ✗ | Przez CostEstimateItem |
| `CostEstimateGroupFieldValue` | ✗ | ✗ | Przez CostEstimateGroup |
| `CostEstimateFieldFile` | ✗ | ✗ | Przez CostEstimateItemFieldValue |
| `CostEstimateTemplate` | ✗ | ✗ | Tylko przez OwnerId (User) ⚠️ |
| `CostEstimateTemplateCurrency/Unit/Category` | ✗ | ✗ | Przez Template |
| `CostEstimateTemplateFieldDefinitionBase` | ✗ | ✗ | Przez Template |
| `SharedCostEstimate` | ✓ | ✓ | Bezpośrednie |
| `WorkSchedule` | ✓ | ✓ | Bezpośrednie |
| `WorkScheduleStage` | ✓ | ✓ | Bezpośrednie |
| `WorkScheduleStageWork` | ✓ | ✓ | Bezpośrednie |
| `WorkScheduleStageWorkPeriod` | ✓ | ✓ | Bezpośrednie |
| `WorkScheduleStageWorkAssignment` | ✓ | ✓ | Bezpośrednie |
| `WorkScheduleStageWorkComment` | ✓ | ✗ ⚠️ | Brak ProjectId; izolacja częściowa |
| `WorkScheduleStageWorkDependency` | ✓ | ✓ | Bezpośrednie |
| `ProjectCost` | ✓ | ✓ | Bezpośrednie |
| `SharedProjectCost` | ✓ | ✓ | Bezpośrednie |
| `TrackedCost` | ✓ | ✓ | Bezpośrednie |
| `TrackedCostAttachment` | ✗ ⚠️ | ✗ ⚠️ | Tylko przez TrackedCost |
| `ProjectCostTrackedCostLink` | ✗ | ✗ | Przez ProjectCost/TrackedCost |
| `ProjectFile` | ✓ | ✓ | Bezpośrednie |
| `ProjectFilePackage` | ✓ | ✓ | Bezpośrednie |
| `ProjectFileVersion` | ✗ ⚠️ | ✗ ⚠️ | Tylko przez ProjectFile |
| `ProjectFileVersionComment` | ✓ | ✓ | Bezpośrednie |
| `SharedProjectFile` | ✓ | ✓ | Bezpośrednie |
| `Chat` | ✓ *(nullable)* | ✓ *(nullable)* | Słaba — nullable |
| `ChatMember` | ✗ | ✗ | Przez Chat |
| `MessageHistory` | ✗ | ✗ | Przez Chat |
| `Notification` | ✓ | ✓ *(nullable)* | Bezpośrednie |

### 5.2 Encje bez TenantId — ocena ryzyka

| Encja | Izolacja przez rodzica | Ryzyko |
|-------|----------------------|--------|
| `CostEstimateTemplate` | Tylko przez `OwnerId` (User) | **Wysoki** — user z dostępem do kilku tenantów może używać szablonu cross-tenant |
| `ProjectFileVersion` | Przez `ProjectFile` | Niski — zawsze ładowana przez FK do ProjectFile |
| `TrackedCostAttachment` | Przez `TrackedCost` | Niski — zawsze ładowana przez FK |
| `WorkScheduleStageWorkComment` | Przez `WorkScheduleStageWork` | Niski — brak ProjectId utrudnia zapytania bezpośrednie |
| `ChatMember`, `MessageHistory` | Przez `Chat` | Niski — Chat ma TenantId (nullable) |
| `CostEstimateGroup/Item/FieldValue` | Przez `CostEstimate` | Niski — hierarchia dobrze zagnieżdżona |

### 5.3 Encje SharedX

| Encja | Pola kluczowe | Mechanizm izolacji |
|-------|--------------|-------------------|
| `SharedProjectFile` | `(TenantId, ProjectId, ProjectFilePackageId, ProjectFileId?, SharedByUserId, SharedWithUserId)` | TenantId + ProjectId bezpośrednio; unique index na `(PackageId, FileId, SharedWithUserId)` |
| `SharedProjectCost` | `(TenantId, ProjectId, ProjectCostId, SharedByUserId, SharedWithUserId)` | TenantId + ProjectId bezpośrednio; unique index na `(ProjectCostId, SharedWithUserId)` |
| `SharedCostEstimate` | `(TenantId, ProjectId, CostEstimateId, SharedByUserId, SharedWithUserId)` | TenantId + ProjectId bezpośrednio; unique index na `(CostEstimateId, SharedWithUserId)` |

---

## BLOK 6 — SPÓJNOŚĆ MODELU

### 6.1 Niespójności nazewnictwa

| Encja | Problem | Rekomendacja |
|-------|---------|-------------|
| `Notification.Readed` | Błąd gramatyczny | Zmienić na `IsRead` (wymaga migracji) |
| `WorkScheduleStageWork.ColorRgb` | Nazwa techniczna w modelu domenowym | Rozważyć `Color` z konwersją |
| `ProjectCostTrackedCostLink.LinkedAt` | Niespójne z `CreatedAt` hookiem w `SaveChangesAsync` — hook nie ustawi `LinkedAt` automatycznie | Zmienić na `CreatedAt` dla spójności |

### 6.2 Pola obliczane i denormalizacja

| Encja | Pole | Typ | Oznaczone komentarzem | EF mapuje |
|-------|------|-----|-----------------------|-----------|
| `WorkScheduleStageWork` | `PlannedStartDate` | `DateTime?` | ✓ | ✓ |
| `WorkScheduleStageWork` | `PlannedEndDate` | `DateTime?` | ✓ | ✓ |
| `CostEstimate` | `TotalNet`, `TotalGross`, `TotalVat` | `decimal?` | Częściowo | ✓ |
| `CostEstimateGroup` | `TotalNet`, `TotalGross`, `TotalVat` | `decimal?` | ✗ | ✓ |
| `CostEstimateItem` | `NetValue`, `GrossValue`, `VatValue` | `decimal?` | ✓ | ✓ |
| `CostEstimate` | `RootGroups` | `IEnumerable<CostEstimateGroup>` | ✓ | ✗ (`Ignore`) |
| `CostEstimateItem` | `Options`, `Components` | `ICollection<CostEstimateItem>` | ✓ | ✗ (prywatne pole) |
| `MessageHistory` | `IsDeleted` | `bool` | ✗ | ✗ (`Ignore`) |
| `CostEstimateFieldFile` | `IsDeleted` | `bool` | ✗ | ✓ (ręczne pole) |

### 6.3 Encje bez konfiguracji EF

| Encja | Ma DbSet | Ma IEntityTypeConfiguration |
|-------|---------|----------------------------|
| `TenantInvitation` | ✓ | ✗ ⚠️ |
| `ProjectCostTrackedCostLink` | ✗ ⚠️ | ✗ ⚠️ |

### 6.4 Konfiguracje bez pełnego DbSet

| IEntityTypeConfiguration | Encja | Ma DbSet |
|--------------------------|-------|---------|
| `WorkScheduleStageWorkPeriodConfiguration` | `WorkScheduleStageWorkPeriod` | ✗ ⚠️ |
| `UserProfileConfiguration` | `UserProfileBase` | ✓ (jako `UserProfiles`) |

> **Uwaga:** `WorkScheduleStageWorkPeriod` ma konfigurację EF ale **brak DbSet** w `AppDbContext`. Dostępna wyłącznie przez nawigację z `WorkScheduleStageWork`.

### 6.5 Niespójności CreatedAt/UpdatedAt

| Encja | Ma CreatedAt | Ma UpdatedAt | Uzasadnione |
|-------|-------------|-------------|-------------|
| `WorkScheduleStage` | ✗ | ✓ | ⚠️ UpdatedAt bez CreatedAt — odwrócona logika |
| `WorkScheduleStageWork` | ✗ | ✓ | ⚠️ Jak wyżej |
| `WorkScheduleStageWorkPeriod` | ✗ | ✗ | ⚠️ Brak obydwu |
| `WorkScheduleStageWorkDependency` | ✗ | ✗ | Rozważyć `CreatedAt` |
| `ProjectFileVersion` | ✓ | ✗ | ✓ Immutable — OK |
| `ProjectFilePackage` | ✓ | ✗ | Rozważyć `UpdatedAt` |
| `Notification` | ✓ (`DateTimeOffset`) | ✗ | ⚠️ Jedyna encja z `DateTimeOffset` |
| `Chat` | ✓ | ✗ | Rozważyć `LastMessageAt` |
| `ProjectCostTrackedCostLink` | ✗ *(LinkedAt)* | ✗ | Hook `SaveChangesAsync` nie ustawi `LinkedAt` automatycznie |

---

## BLOK 7 — PROBLEMY I REKOMENDACJE

### 7.1 Krytyczne 🔴

| # | Problem | Encja/Relacja | Ryzyko | Rekomendacja |
|---|---------|--------------|--------|-------------|
| C1 | `CostEstimateTemplate` implementuje soft-delete ręcznie poza `DeletableEntity`; brak `GlobalQueryFilter` — usunięte szablony zwracane przez wszystkie zapytania | `CostEstimateTemplate` | Wyświetlanie usuniętych szablonów użytkownikom | Zmienić dziedziczenie `BaseEntity` → `DeletableEntity`; dodać `HasQueryFilter(t => !t.IsDeleted)` |
| C2 | `CostEstimateFieldFile` implementuje soft-delete ręcznie; brak `GlobalQueryFilter` | `CostEstimateFieldFile` | Usunięte pliki pól kosztorysu widoczne w zapytaniach | Jak C1 |
| C3 | `ProjectCostTrackedCostLink` nie ma DbSet ani `IEntityTypeConfiguration` | `ProjectCostTrackedCostLink` | Brak zarządzania tabelą przez EF; brak PK, indeksów, DeleteBehavior | Dodać `DbSet<ProjectCostTrackedCostLink>` i `IEntityTypeConfiguration`; zdefiniować composite PK lub `BaseEntity` |
| C4 | Duplikat konfiguracji `UserProfileBase` — `UserProfileConfiguration` + inline w `AppDbContext.OnModelCreating`; discriminator w `AppDbContext` nie zawiera wartości `PermissionsVersion` | `UserProfileBase` | `PermissionsVersionProfile` może nie być mapowany poprawnie; konflikt konfiguracji | Usunąć inline konfigurację z `AppDbContext`; przenieść do `UserProfileConfiguration`; uzupełnić brakującą wartość discriminatora |
| C5 | `TenantInvitation` dziedziczy wprost z `object`; brak `IEntityTypeConfiguration` | `TenantInvitation` | Brak standardowego `Id = Guid.NewGuid()`; brak indeksów na `Token` (brute-force risk), `Email`, `Status` | Dodać `: BaseEntity`; stworzyć `TenantInvitationConfiguration` |

---

### 7.2 Wysokie 🟠

| # | Problem | Encja/Relacja | Ryzyko | Rekomendacja |
|---|---------|--------------|--------|-------------|
| H1 | `CostEstimateTemplate` nie ma `TenantId`; izolacja jedynie przez `OwnerId (User)`; user może być memberem wielu tenantów | `CostEstimateTemplate` | Cross-tenant data access: zapytania per owner nie mają granicy tenanta | Rozważyć `TenantId?` (nullable — szablony mogą być osobiste) lub model publiczne/prywatne |
| H2 | `TrackedCostAttachment → TrackedCost` ma `OnDelete(Restrict)` zamiast `Cascade` | `TrackedCostAttachment` | Błąd FK przy próbie usunięcia TrackedCost bez jawnego czyszczenia załączników | Zmienić na `OnDelete(Cascade)` lub zapewnić czyszczenie w handlerze |
| H3 | `WorkScheduleStageWorkPeriod` ma konfigurację EF ale brak `DbSet` — nie można wykonać `ExecuteDeleteAsync`/`SelectAsync` bezpośrednio | `WorkScheduleStageWorkPeriod` | Wymuszony dostęp przez Include; utrudnione operacje bulk | Dodać `DbSet<WorkScheduleStageWorkPeriod> WorkScheduleStageWorkPeriods` |
| H4 | `TrackedCost.Net/Gross` używa `HasColumnType("decimal(15,2)")` zamiast `HasPrecision(18,2)` | `TrackedCost` | Utrata precyzji przy dużych kwotach; niespójność z `ProjectCost (18,2)`, `CostEstimateItem (18,2)` | Zmienić na `HasPrecision(18, 2)` |
| H5 | `MessageHistory.IsDeleted` jako computed property bez GlobalQueryFilter — usunięte wiadomości widoczne w zapytaniach | `MessageHistory` | Usunięte wiadomości widoczne w `chat.Messages` | Zdecydować: przenieść do `DeletableEntity` lub dodać własny `HasQueryFilter(m => !m.DeletedAt.HasValue)` |
| H6 | `Notification.CreatedAt` jest `DateTimeOffset` podczas gdy wszystkie inne encje używają `DateTime` | `Notification` | Niespójność porównań dat; problemy przy sortowaniu krzyżowym | Zmienić na `DateTime` (wymaga migracji) |
| H7 | `WorkScheduleStageWork.PlannedStartDate/PlannedEndDate` — denormalizacja aktualizowana przez handlery CQRS bez mechanizmu gwarancji spójności | `WorkScheduleStageWork` | Pola mogą być niespójne jeśli period zostanie usunięty bez aktualizacji nadrzędnego | Rozważyć trigger DB lub dedykowany DomainEvent; wymusić update w każdym handlerze modyfikującym Periods |

---

### 7.3 Normalne 🟡

| # | Problem | Encja/Relacja | Ryzyko | Rekomendacja |
|---|---------|--------------|--------|-------------|
| N1 | `WorkScheduleStage` i `WorkScheduleStageWork` mają `UpdatedAt` bez `CreatedAt` | Obie encje | Brak informacji o czasie utworzenia | Dodać `CreatedAt` |
| N2 | `TrackedCost.Number` (nullable string) bez `HasMaxLength` | `TrackedCost` | `nvarchar(max)` w DB — zbędne zużycie przestrzeni | Dodać `HasMaxLength(100)` |
| N3 | `ProjectFileVersion` nie ma `TenantId/ProjectId` | `ProjectFileVersion` | Bulk operations per projekt niemożliwe bez Join | Rozważyć dodanie `ProjectId` jako denormalizację |
| N4 | `WorkScheduleStageWorkComment` nie ma `ProjectId` | `WorkScheduleStageWorkComment` | Niespójność z analogicznymi encjami; zapytania per projekt wymagają join | Dodać `ProjectId` |
| N5 | `WorkScheduleStageWorkAssignment` posiada redundantne FK bezpośrednio do `Tenant` i `Project` | `WorkScheduleStageWorkAssignment` | Nadmiar FK; złożona konfiguracja | Rozważyć usunięcie bezpośrednich FK do Tenant/Project |
| N6 | `Notification.Readed` — błąd gramatyczny | `Notification` | Code quality | Zmienić na `IsRead` (wymaga migracji) |
| N7 | `TrackedCostAttachment` nie ma `TenantId/ProjectId` | `TrackedCostAttachment` | Trudniejsze zapytania diagnostyczne per projekt | Rozważyć dodanie `ProjectId` |
| N8 | `ProjectGroup` nie ma `TenantId` | `ProjectGroup` | Nieznaczne — Project ma TenantId | Opcjonalnie dodać `TenantId` dla spójności |
| N9 | `Chat.TenantId/ProjectId` nullable — słaba izolacja multitenancy dla czatów projektowych | `Chat` | Możliwy chat bez przypisania do tenanta | Rozważyć oddzielny `ChatType` enum z obowiązkowym TenantId |
| N10 | `CostEstimateFieldFile.CostEstimateId` to denormalizacja bez XML doc dokumentującej intencję | `CostEstimateFieldFile` | Możliwa niespójność przy przenoszeniu FieldValue | Dodać komentarz XML dokumentujący intencję |
| N11 | `ProjectCostTrackedCostLink.LinkedAt` nie jest obsługiwane przez hook `SaveChangesAsync` (który obsługuje tylko `CreatedAt`) | `ProjectCostTrackedCostLink` | Pole nie jest auto-ustawiane | Zmienić na `CreatedAt` lub dodać obsługę `LinkedAt` w SaveChangesAsync |

---

### 7.4 Rekomendacje architektoniczne

1. **Ujednolicenie soft-delete** — `CostEstimateTemplate` i `CostEstimateFieldFile` powinny dziedziczyć `DeletableEntity` zamiast definiować `IsDeleted/DeletedAt` ręcznie. Eliminuje ryzyko pominięcia GlobalQueryFilter przy dodawaniu nowych handlerów.

2. **TenantId na CostEstimateTemplate** — rozważyć model `TenantId?` (null = globalny/publiczny szablon) lub osobna encja `SharedTemplate` dla cross-tenant sharing. Aktualny brak izolacji jest poważną luką dla SaaS.

3. **Rejestracja ProjectCostTrackedCostLink** — tabela łącząca dwie domeny kosztów powinna mieć pełny DbSet i konfigurację. Alternatywnie: ocenić czy relacja M:N jest właściwa, czy wystarczy bezpośredni FK w `TrackedCost → ProjectCost`.

4. **Przeniesienie konfiguracji UserProfile** — inline konfiguracja TPH w `AppDbContext.OnModelCreating` powinna być przeniesiona całkowicie do `IEntityTypeConfiguration`. `AppDbContextModelSnapshot` może mieć rozbieżności z powodu duplikatu.

5. **Spójność DecimalPrecision** — zdefiniować jedną konwencję (`HasPrecision(18,4)` dla budżetów, `HasPrecision(18,2)` dla kosztów) i przestrzegać jej wszędzie. `TrackedCost` używa `(15,2)` co jest niespójne z całym systemem.

6. **DbSet dla WorkScheduleStageWorkPeriod** — nawet jeśli nie jest główną encją roboczą, posiadanie DbSet umożliwia `ExecuteDeleteAsync` i `SelectAsync` bez Join — niezbędne dla wydajnych operacji zbiorczych.

7. **Notification.CreatedAt jako DateTime** — ujednolicenie do `DateTime` (UTC) w całym projekcie eliminuje problemy z porównaniami dat i serializacją JSON.

8. **Audytowy CreatedAt na WorkScheduleStage/Work** — brak `CreatedAt` przy `UpdatedAt` jest architektonicznie odwrócony. Każdy rekord powinien przechowywać czas swojego utworzenia.

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Łączna liczba encji (klas mapowanych) | **47** |
| Encje dziedziczące `DeletableEntity` | **13** |
| Encje z ręcznym soft-delete (bez `DeletableEntity`) | **2** (`CostEstimateTemplate`, `CostEstimateFieldFile`) |
| Encje z `IsDeleted` bez `GlobalQueryFilter` | **2** (`CostEstimateTemplate`, `CostEstimateFieldFile`) |
| Encje bez `IEntityTypeConfiguration` | **2** (`TenantInvitation`, `ProjectCostTrackedCostLink`) |
| Encje bez `DbSet` w AppDbContext | **2** (`WorkScheduleStageWorkPeriod`, `ProjectCostTrackedCostLink`) |
| Relacje bez jawnego `DeleteBehavior` | **4** (`Notification→Tenant`, `TenantInvitation→User`, `ProjectCostTrackedCostLink×2`, `ChatMember→User`) |
| Pola `decimal` bez `HasPrecision` (niespójne) | **2** (`TrackedCost.Net`, `TrackedCost.Gross`) |
| Pola `string` bez `HasMaxLength` | **3** (`TenantInvitation.Email`, `TenantInvitation.Token`, `TrackedCost.Number`) |
| Duplikaty konfiguracji EF | **1** (`UserProfileBase`) |
| Niespójności typów daty | **1** (`Notification.CreatedAt` jako `DateTimeOffset`) |
| Problemy krytyczne 🔴 | **5** |
| Problemy wysokie 🟠 | **7** |
| Problemy normalne 🟡 | **11** |
