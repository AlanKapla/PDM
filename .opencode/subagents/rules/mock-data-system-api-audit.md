# Pełny Audyt API Endpointów — PDM (Project Data Management)

> Data: 2026-06-10
> Cel: Katalog wszystkich endpointów REST API wywoływanych przez frontend na potrzeby systemu mock danych.

---

## Podsumowanie

| Moduł | Liczba endpointów | Krytyczne (startowe) |
|-------|:-----------------:|:--------------------:|
| Tenants / Organizacje | 13 | 3 |
| Projekty | 7 | 2 |
| Członkowie projektu | 4 | 1 |
| Kosztorysy (Cost Estimate) | 22 | 2 |
| Szablony kosztorysów | 10 | 2 |
| Wydatki projektu (ProjectCost) | 9 | 1 |
| Cost Tracker (kontrola kosztów) | 10 | 3 |
| Dashboard | 2 | 1 |
| Harmonogram prac (WorkSchedule) | 27 | 2 |
| Pliki | 12 | 1 |
| Chat / Wiadomości | 25 | 3 |
| Kontrahenci | 5 | 1 |
| Użytkownicy / Auth | 4 | 3 |
| Powiadomienia | 5 | 2 |
| Słowniki | 1 | 1 |
| AI Cost (parsowanie) | 2 | 0 |
| Health | 1 | 0 |
| **RAZEM** | **161** | **28** |

### SignalR Huby (nie REST, ale mockowane)
| Hub | Ścieżka |
|-----|---------|
| NotificationHub | /api/hubs/notifications |
| MessageHub | /api/hubs/messages |
| AIHub | /api/hubs/ai |
| ChatHub | /api/hubs/chat |
## 2. Projekty (projectApi.ts)
**Kontroler:** `ProjectController.cs` — route: `api/tenants/{tenantId}/projects`

| # | Funkcja | Metoda | URL | Parametry | Zwraca |
|---|---------|--------|-----|-----------|--------|
| 14 | `getTenantProjects` | **GET** | `/api/tenants/{tenantId}/projects` | path: `tenantId` | `ProjectDetailsWeb[]` lista |
| 15 | `getProjectDetails` / `getProject` | **GET** | `/api/tenants/{tenantId}/projects/{projectId}` | path: `tenantId`, `projectId` | `ProjectDetailsWeb` |
| 16 | `createProject` | **POST** | `/api/tenants/{tenantId}/projects` | path: `tenantId`, body: `{ tenantId, name }` | `ProjectDetailsWeb` |
| 17 | `getProjectsDictionary` | **GET** | `/api/tenants/{tenantId}/projects/dictionary` | path: `tenantId` | `Record<Guid, string>` |
| 18 | `toggleProjectStatus` | **PATCH** | `/api/tenants/{tenantId}/projects/{projectId}/status?isActive=` | path + query: `isActive` | `204 No Content` |
| 19 | `updateProject` | **PUT** | `/api/tenants/{tenantId}/projects/{projectId}` | path + body: `{ Name }` | `ProjectDetailsWeb` |
| 20 | `setProjectCurrency` | **PUT** | `/api/tenants/{tenantId}/projects/{projectId}/currency` | path + body: `SetProjectCurrencyRequest` | `204 No Content` |

### Krytyczne (startowe):
- **#14** `getTenantProjects` — lista projektów na stronie głównej tenanta
- **#15** `getProjectDetails` — detale projektu przy wejściu w projekt

---

## 3. Członkowie projektu (projectApi.ts)
**Kontroler:** `ProjectController.cs`

| # | Funkcja | Metoda | URL | Parametry | Zwraca |
|---|---------|--------|-----|-----------|--------|
| 21 | `getProjectMembers` | **GET** | `/api/tenants/{tenantId}/projects/{projectId}/members` | path | `ProjectMemberWeb[]` lista |
| 22 | `addProjectMember` | **POST** | `/api/tenants/{tenantId}/projects/{projectId}/members` | path + body: `{ userId, modules }` | `204 No Content` |
| 23 | `removeProjectMember` | **DELETE** | `/api/tenants/{tenantId}/projects/{projectId}/members/{userId}` | path | `204 No Content` |
| 24 | `updateProjectMemberPermissions` | **PATCH** | `/api/tenants/{tenantId}/projects/{projectId}/members/{userId}/role` | path + body: `{ isAdmin, modules }` | `204 No Content` |

### Krytyczne (startowe):
- **#21** `getProjectMembers` — lista członków przy wejściu w ustawienia projektu
---

## 4. Kosztorysy — Cost Estimate (costEstimateApi.ts)
**Kontroler:** `CostEstimateController.cs` — route: `api/tenants/{tenantId}/projects/{projectId}/cost-estimate`

### Główne operacje
| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 25 | `getFieldTypeConfigurations` | **GET** | `/api/cost-estimate-template/field-type-configurations` | `Record<string, Config[]>` |
| 26 | `getCostEstimatesByScope` | **GET** | `/api/.../cost-estimate/{scope}` (all/mine/shared) | `CostEstimateListItemWeb[]` |
| 27 | `getCostEstimateDetails` | **GET** | `/api/.../cost-estimate/details/{id}` | `CostEstimateDetailsWeb` **ZŁOŻONY** |
| 28 | `createCostEstimate` | **POST** | `/api/.../cost-estimate` | `string` (ID) |
| 29 | `updateCostEstimate` | **PUT** | `/api/.../cost-estimate/{id}` | `void` |
| 30 | `deleteCostEstimate` | **DELETE** | `/api/.../cost-estimate/{id}` | `void` |
| 31 | `copyCostEstimate` | **POST** | `/api/.../cost-estimate/{id}/copy` | `string[]` |

### Operacje na grupach
| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 32 | `addGroup` | **POST** | `/api/.../cost-estimate/{ceId}/groups` | `string` (ID) |
| 33 | `deleteGroup` | **DELETE** | `/api/.../cost-estimate/{ceId}/groups/{groupId}` | `void` |
| 34 | `upsertGroupField` | **PATCH** | `/api/.../cost-estimate/{ceId}/groups/{groupId}/fields` | `string` |
| 35 | `reorderGroups` | **PUT** | `/api/.../cost-estimate/{ceId}/groups/reorder` | `void` |

### Operacje na pozycjach (items)
| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 36 | `addItem` | **POST** | `/api/.../cost-estimate/{ceId}/items` | `string` (ID) |
| 37 | `deleteItem` | **DELETE** | `/api/.../cost-estimate/{ceId}/items/{itemId}` | `void` |
| 38 | `upsertItemField` | **PATCH** | `/api/.../cost-estimate/{ceId}/items/{itemId}/fields` | `string` |
| 39 | `reorderItems` | **PUT** | `/api/.../cost-estimate/{ceId}/groups/{groupId}/items/reorder` | `void` |
| 40 | `moveItem` | **PATCH** | `/api/.../cost-estimate/{ceId}/items/{itemId}/move` | `void` |

### Udostępnianie, kalkulacja, pliki, AI
| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 41 | `shareCostEstimate` | **POST** | `/api/.../cost-estimate/{ceId}/shares` | `void` |
| 42 | `updateCostEstimateShares` | **PUT** | `/api/.../cost-estimate/{ceId}/shares` | `void` |
| 43 | `recalculate` | **POST** | `/api/.../cost-estimate/{ceId}/recalculate` | `void` |
| 44 | `uploadCostEstimateItemFiles` | **POST** | `/api/.../cost-estimate/{ceId}/items/{itemId}/files` (form-data) | `string[]` |
| 45 | `generateAIPreview` | **POST** | `/api/.../cost-estimate/generate-ai-preview` | `AICostEstimatePreviewDto` |
| 46 | `createFromAIPreview` | **POST** | `/api/.../cost-estimate/create-from-ai-preview` | `string` (ID) |

### Krytyczne (startowe):
- **#26** `getCostEstimatesByScope` — lista kosztorysów
- **#27** `getCostEstimateDetails` — szczegóły kosztorysu (pełna hierarchia)

---

## 5. Szablony kosztorysów (costEstimateTemplateApi.ts)
**Kontroler:** `CostEstimateTemplateController.cs` — route: `api/cost-estimate-template`

| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 47 | `getTemplates` | **GET** | `/api/cost-estimate-template` | lista |
| 48 | `getTemplateDetails` | **GET** | `/api/cost-estimate-template/{id}` | **ZŁOŻONY** |
| 49 | `createTemplate` | **POST** | `/api/cost-estimate-template` | `string` (ID) |
| 50 | `updateTemplate` | **PUT** | `/api/cost-estimate-template/{id}` | `void` |
| 51 | `approveVersion` | **POST** | `/api/cost-estimate-template/{templateId}/versions/{versionId}/approve` | `void` |
| 52 | `getDefaultTemplates` | **GET** | `/api/cost-estimate-template/defaults` | lista |
| 53 | `getDefaultTemplate` | **GET** | `/api/cost-estimate-template/defaults/{slug}` | **ZŁOŻONY** |
| 54 | `createFromDefault` | **POST** | `/api/cost-estimate-template/defaults/{slug}` | `string` (ID) |
| 55 | `deleteTemplate` | **DELETE** | `/api/cost-estimate-template/{id}` | `void` |
| 56 | `duplicateTemplate` | **POST** | `/api/cost-estimate-template/{id}/duplicate` | `string` (ID) |

### Krytyczne (startowe):
- **#47** `getTemplates` — lista szablonów
- **#48** `getTemplateDetails` — struktura szablonu

---

## 6. Wydatki projektu — ProjectCost (projectApi.ts)
**Kontroler:** `ProjectCostController.cs` — route: `api/tenants/{tenantId}/projects/{projectId}/cost`

| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 57 | `getProjectCosts` | **GET** | `/api/.../cost/{scope}` | `ProjectCostListItemWeb[]` |
| 58 | `getProjectUserCosts` | **GET** | `/api/.../cost/mine` (deprecated) | lista |
| 59 | `createProjectCost` | **POST** | `/api/.../cost` (form-data) | `ProjectCostListItemWeb` |
| 60 | `updateProjectCost` | **PUT** | `/api/.../cost/{costId}` (form-data) | `ProjectCostListItemWeb` |
| 61 | `deleteProjectCost` | **DELETE** | `/api/.../cost/{costId}` | `void` |
| 62 | `submitProjectCostForApproval` | **POST** | `/api/.../cost/{costId}/submit` | `ProjectCostListItemWeb` |
| 63 | `withdrawProjectCostFromApproval` | **POST** | `/api/.../cost/{costId}/withdraw` | `ProjectCostListItemWeb` |
| 64 | `approveProjectCost` | **POST** | `/api/.../cost/{costId}/approve` | `ProjectCostListItemWeb` |
| 65 | `rejectProjectCost` | **POST** | `/api/.../cost/{costId}/reject` | `ProjectCostListItemWeb` |

### Krytyczne (startowe):
- **#57** `getProjectCosts` — lista wydatków
---

## 7. Cost Tracker (costTrackerApi.ts + dashboardApi.ts)
**Kontroler:** `CostTrackerController.cs` — route: `api/tenants/{tenantId}/projects/{projectId}/cost-trackers`

| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 66 | `getByProject` | **GET** | `/api/.../cost-trackers/by-project` | `CostTrackerDetailsWeb` **ZŁOŻONY** |
| 67 | `getByEstimate` | **GET** | `/api/.../cost-trackers/by-estimate/{costEstimateId}` | `CostEstimateSummaryWeb` |
| 68 | `getCosts` | **GET** | `/api/.../cost-trackers/costs` | `TrackedCostWeb[]` |
| 69 | `getCostDetails` | **GET** | `/api/.../cost-trackers/costs/{costId}` | `TrackedCostWeb` |
| 70 | `getItemCosts` | **GET** | `/api/.../cost-trackers/by-estimate/{ceId}/items/{itemId}/costs` | `TrackedCostWeb[]` |
| 71 | `createCost` / `createTrackedCost` | **POST** | `/api/.../cost-trackers/costs` (form-data) | `TrackedCostWeb` |
| 72 | `updateCost` / `updateTrackedCost` | **PUT** | `/api/.../cost-trackers/costs/{costId}` (form-data) | `TrackedCostWeb` |
| 73 | `deleteCost` / `deleteTrackedCost` | **DELETE** | `/api/.../cost-trackers/costs/{costId}` | `void` |
| 74 | `updateBudget` (costTrackerApi) | **PUT** | `/api/.../cost-trackers/{costTrackerId}/budget` | `void` |
| 75 | `getLinkOptions` | **GET** | `/api/.../cost-trackers/link-options` | `CostLinkOptionsWeb` |
| 76 | `updateTrackerBudget` (dashboardApi) | **PUT** | `/api/.../cost-trackers/budget` | `void` |

### Krytyczne (startowe):
- **#66** `getByProject` — dashboard kosztów
- **#68** `getCosts` — lista kosztów
- **#75** `getLinkOptions` — opcje linkowania

---

## 8. Dashboard (dashboardApi.ts)
**Kontroler:** `ProjectDashboardController.cs` — route: `api/tenants/{tenantId}/projects/{projectId}/dashboard`

| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 77 | `getProjectDashboard` | **GET** | `/api/.../dashboard` | `ProjectDashboardWeb` **BARDZO ZŁOŻONY** |

### Krytyczne (startowe):
- **#77** `getProjectDashboard` — główny dashboard projektu

---

## 9. Harmonogram prac — WorkSchedule (projectApi.ts + workScheduleApi.ts)
**Kontroler:** `WorkScheduleController.cs` — route: `api/tenants/{tenantId}/projects/{projectId}/work-schedule`

| # | Funkcja | Metoda | URL | Zwraca |
|---|---------|--------|-----|--------|
| 78 | `createWorkSchedule` | **POST** | `/api/.../work-schedule` | `string` (ID) |
| 79 | `getWorkSchedules` | **GET** | `/api/.../work-schedule/{scope}` | lista |
| 80 | `getMyWorkSchedules` | **GET** | `/api/.../work-schedule/mine` (deprecated) | lista |
| 81 | `getWorkSchedule` / `getDetails` | **GET** | `/api/.../work-schedule/details/{wsId}` | **BARDZO ZŁOŻONY** |
| 82 | `updateWorkSchedule` / `renameSchedule` | **PUT** | `/api/.../work-schedule/{wsId}` | `204` |
| 83 | `deleteWorkSchedule` | **DELETE** | `/api/.../work-schedule/{wsId}` | `void` |
| 84 | `syncWorkScheduleWithEstimate` / `syncWithEstimate` | **POST** | `/api/.../work-schedule/{wsId}/sync-with-estimate` | `204` |
| 85 | `generateScheduleFromEstimateAI` | **POST** | `/api/.../work-schedule/{wsId}/generate-from-ai` | `WorkScheduleDetailsWeb` |
| 86 | `addStage` | **POST** | `/api/.../work-schedule/{wsId}/stages` | `string` (ID) |
| 87 | `deleteStage` | **DELETE** | `/api/.../work-schedule/{wsId}/stages/{stageId}` | `204` |
| 88 | `renameStage` | **PATCH** | `/api/.../work-schedule/{wsId}/stages/{stageId}/name` | `204` |
| 89 | `reorderStages` | **PUT** | `/api/.../work-schedule/{wsId}/stages/order` | `204` |
| 90 | `moveStage` | **PATCH** | `/api/.../work-schedule/{wsId}/stages/{stageId}/parent` | `204` |
| 91 | `addWork` | **POST** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works` | `string` (ID) |
| 92 | `deleteWork` | **DELETE** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}` | `204` |
| 93 | `renameWork` | **PATCH** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/name` | `204` |
| 94 | `reorderWorks` | **PUT** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/order` | `204` |
| 95 | `moveWork` | **PATCH** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/stage` | `204` |
| 96 | `setPeriods` | **PUT** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/periods` | `204` |
| 97 | `setWorkColor` | **PATCH** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/color-rgb` | `204` |
| 98 | `setWorkIsClosed` | **PATCH** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/is-closed` | `204` |
| 99 | `setPeriodIsClosed` | **PATCH** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/periods/{periodId}/is-closed` | `204` |
| 100 | `setAssignments` | **PUT** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/assignments` | `204` |
| 101 | `addComment` | **POST** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/comments` | `string` (ID) |
| 102 | `updateComment` | **PUT** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/comments/{commentId}` | `204` |
| 103 | `deleteComment` | **DELETE** | `/api/.../work-schedule/{wsId}/stages/{stageId}/works/{workId}/comments/{commentId}` | `204` |
| 104 | `setDependencies` | **PUT** | `/api/.../work-schedule/{wsId}/dependencies` | `WorkScheduleDetailsWeb` |
| 105 | `getMyAssignedWorks` | **GET** | `/api/user/assigned-works` | lista |

### Krytyczne (startowe):
- **#79** `getWorkSchedules` — lista harmonogramów
- **#81** `getWorkSchedule` / `getDetails` — szczegóły harmonogramu
