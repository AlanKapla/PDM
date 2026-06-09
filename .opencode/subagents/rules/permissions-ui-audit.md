# Audyt UI — Uproszczenie Systemu Uprawnień Modułowych

**Data:** 2026-05-27  
**Feature:** Uproszczenie permission system — 44 granularne kody → 9 kodów per moduł  
**Zakres audytu:** React/TypeScript UI (`01-Applications/ProjectDataManagementUI`)

---

## BLOK 1 — Stan obecny UI

| Komponent/Plik | Lokalizacja | Opis | Powiązany z feature |
|----------------|-------------|------|---------------------|
| `projectModulePermissions.ts` | `src/types/` | `ModuleAccessLevel` (11 poziomów), `ProjectMemberPresets` (5 presetów), `ProjectMemberModulePermission` interface | Tak — pełna zmiana |
| `project.types.ts` | `src/types/` | `ModulePermissionWeb { module, accessLevel }`, `ProjectMemberWeb.modulePermissions[]` | Tak — `accessLevel` odpada |
| `roleCodes.ts` | `src/constants/` | 44 `PermissionCodes` + helper functions (`hasPermission`, etc.) | Tak — zastąpić 9 kodami |
| `useProjectPermissions.ts` | `src/hooks/` | 23 bool flagi: `canView`, `canEdit`, `canViewFiles`, `canReadResources`, `canWriteSharedResources` itd. | Tak — uproszczenie |
| `useResourcePermissions.ts` | `src/hooks/` | `ResourcePermissions` interface: `tabs.showAll/showMine/showShared`, `mine.canCreate/canEdit`, `all.*`, `shared.*` | Tak — pełny redesign |
| `useTenantPermissions.ts` | `src/hooks/` | Opiera się na `TENANT.*` kodach — nie na modułach projektowych | Nie zmienia się |
| `AddProjectMemberModal.tsx` | `src/components/` | 8 `<Select>` per moduł z opcjami z `ACCESS_LEVELS` (do 6 opcji per moduł) | Tak — zamiana na checkboxy |
| `EditProjectMemberModal.tsx` | `src/components/` | Identyczna struktura co Add, inicjalizuje z `member.modulePermissions` | Tak — zamiana na checkboxy |
| `projectApi.ts` | `src/api/` | `addProjectMember(…, modulePermissions: {module,accessLevel}[])`, `updateProjectMemberPermissions(…, modulePermissions: {module,accessLevel}[])` | Tak — zmiana sygnatury |
| `ProjectDetails.tsx` | `src/pages/` | Używa: `canViewFiles`, `canViewEstimates`, `canViewCosts`, `canViewSchedule`, `canViewDashboard`, `canView`, `canEdit`, `canViewMembers`, `canManageMembers` | Pośrednio (hook się zmienia) |
| `ProjectMembers.tsx` | `src/pages/` | Używa: `canViewMembers`, `canManageMembers` | Pośrednio |
| `ProjectParameters.tsx` | `src/pages/` | Używa: `canView`, `canEdit` | Pośrednio |
| `ProjectFiles.tsx` | `src/pages/` | Używa pełnego `ResourcePermissions`: `tabs.*`, `mine.*`, `all.*`, `shared.*`, `raw.loading` | Pośrednio |
| `ProjectCosts.tsx` | `src/pages/` | Używa `tabs.*`, `mine.*`, `all.*` | Pośrednio |
| `ProjectSchedules.tsx` | `src/pages/` | Używa `tabs.showAll/showMine`, `all.canEdit/canCreate`, `mine.canEdit/canCreate` | Pośrednio |
| `CostEstimateEditPage.tsx` | `src/pages/` | Używa `mine.canEdit`, `all.canEdit`, `shared.canEdit`, `mine.canShare`, `all.canShare` | Pośrednio |
| `WorkScheduleView.tsx` | `src/pages/` | Przekazuje `permissions: ResourcePermissions` do `<GanttProvider>` | Pośrednio |
| `GanttContext.tsx` | `src/components/gantt/` | Typ `ResourcePermissions` jako prop GanttProvider | Pośrednio |
| `CostFormModal.tsx` | `src/components/CostTracker/` | `const { canEdit: isProjectAdmin } = useProjectPermissions(projectId)` | Pośrednio |
| `CostFormDrawer.tsx` | `src/components/CostTracker/` | `const { canEdit: isProjectAdmin } = useProjectPermissions(projectId)` | Pośrednio |
| `dashboard/CostModal.tsx` | `src/features/dashboard/` | `const { canEdit: isProjectAdmin } = useProjectPermissions(projectId)` | Pośrednio |

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|-------------|-----|-----------|------|
| Brak UI checkboxów do modułów (zamiast dropdownów) | Komponent (modal) | KRYTYCZNY | `AddProjectMemberModal` i `EditProjectMemberModal` mają `<Select>` z poziomami dostępu — trzeba zastąpić `<Checkbox>` |
| Brak nowego modelu API payload | Serwis API | KRYTYCZNY | `{ module, accessLevel }[]` → `modules: number[]` w `projectApi.ts` |
| Brak nowego `PermissionCodes` z 9 kodami | Stała | KRYTYCZNY | Stare 44 kody nie będą zwracane przez backend |
| `useResourcePermissions` — model tabs/mine/all/shared staje się bez znaczenia | Hook | WYSOKI | Z jednym kodem per moduł nie ma rozróżnienia na "własne/wszystkie/udostępnione" |
| Rozstrzygnięcie UX: czy zachować zakładki w plikach/kosztorysach? | Decyzja UX | WYSOKI | Patrz Pytania domenowe |
| `canEdit` vs `canView` dla `PROJECT.SETTINGS` — oba mapują na jeden kod | Hook | ŚREDNI | Semantyka `canEdit` traci znaczenie |
| `canManageMembers` vs `canViewMembers` — oba mapują na `PROJECT.MEMBERS` | Hook | ŚREDNI | Semantycznie są teraz tożsame |
| Dead code: `canManageStatus`, `canReadMessages`, `canWriteMessages`, `canDeleteMessages`, `canListRoles` | Hook | NISKI | Zdefiniowane w hooku ale nigdzie nieużywane w komponentach/stronach |

---

## BLOK 3 — Typy TypeScript

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|-----|------|-----------------|------------|
| `ModuleAccessLevel` | `src/types/projectModulePermissions.ts` | **USUNĄĆ** | Cały const + type odpada |
| `ProjectMemberPresets` | `src/types/projectModulePermissions.ts` | **USUNĄĆ** | 5 presetów (Admin/Editor/Viewer/Contractor/Investor) odpada |
| `ProjectMemberModulePermission` | `src/types/projectModulePermissions.ts` | **MODYFIKACJA** | Usunąć `accessLevel: ModuleAccessLevel` — zostaje tylko `{ module: ProjectModule }` |
| `ModulePermissionWeb` | `src/types/project.types.ts` | **MODYFIKACJA lub USUNIĘCIE** | Usunąć `accessLevel: number` — zostaje `{ module: number }` LUB zastąpić `modules: number[]` w `ProjectMemberWeb` |
| `ProjectMemberWeb.modulePermissions` | `src/types/project.types.ts` | **MODYFIKACJA** | `modulePermissions: ModulePermissionWeb[]` → `modules: number[]` (lista ID włączonych modułów) |
| `ResourcePermissions` | `src/hooks/useResourcePermissions.ts` | **MODYFIKACJA** | Cała struktura `tabs/mine/all/shared` do przepisania pod nowy model |
| `PermissionCode` | `src/constants/roleCodes.ts` | **MODYFIKACJA** | Aktualizacja type-alias po zmianie `PermissionCodes` obiektu |

### Nowe typy do rozważenia

```typescript
// src/types/project.types.ts
export interface ProjectMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  joinedAt: string;
  isAdmin: boolean;
  modules: number[];  // lista ID modułów z dostępem (zamiast modulePermissions[])
}

// src/types/projectModulePermissions.ts — po zmianach
export const ProjectModule = { /* bez zmian */ } as const;
export type ProjectModule = typeof ProjectModule[keyof typeof ProjectModule];
// ModuleAccessLevel — USUNĄĆ
// ProjectMemberPresets — USUNĄĆ
// ProjectMemberModulePermission — USUNĄĆ (lub uprościć do { module: ProjectModule })
```

---

## BLOK 4 — Serwisy API (`src/api/`)

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint | Opis |
|-------------|------|-----------------|---------|------|
| `addProjectMember` | `src/api/projectApi.ts` | **MODYFIKACJA** | `POST /tenants/{tenantId}/projects/{projectId}/members` | Zmiana `modulePermissions: {module,accessLevel}[]` → `modules: number[]` |
| `updateProjectMemberPermissions` | `src/api/projectApi.ts` | **MODYFIKACJA** | `PATCH /tenants/{tenantId}/projects/{projectId}/members/{userId}/role` | Zmiana `modulePermissions: {module,accessLevel}[]` → `modules: number[]` |

### Obecny payload:
```typescript
// addProjectMember — STARY
{
  tenantId, projectId, userId,
  modulePermissions: [
    { module: 2, accessLevel: 6 },   // Files: Write
    { module: 3, accessLevel: 10 },  // Estimates: Admin
  ]
}

// addProjectMember — NOWY
{
  tenantId, projectId, userId,
  modules: [2, 3]  // Files + Estimates: dostęp = true/false
}
```

---

## BLOK 5 — Hooki React Query

| Hook | Plik | Nowy/Modyfikacja | Query/Mutation | Opis |
|------|------|-----------------|---------------|------|
| `useProjectPermissions` | `src/hooks/useProjectPermissions.ts` | **MODYFIKACJA** | Query (via `useProjectDetails`) | Kompletny rewrite logiki — 23 flagi → ~12 prostych |
| `useResourcePermissions` | `src/hooks/useResourcePermissions.ts` | **MODYFIKACJA** | Custom hook (wraps useProjectPermissions) | Pełny redesign: tabs/mine/all/shared → simplified |

### `useProjectPermissions` — mapowanie stare → nowe:

| Stara flaga | Stary kod | Nowa flaga | Nowy kod |
|-------------|-----------|-----------|----------|
| `canView` | `PROJECT.SETTINGS.VIEW` | `canSettings` lub zachować `canView` | `PROJECT.SETTINGS` |
| `canEdit` | `PROJECT.SETTINGS.EDIT` | tożsame z `canView` — rozważyć usunięcie | `PROJECT.SETTINGS` |
| `canViewMembers` | `PROJECT.MEMBERS.VIEW` | `canMembers` lub zachować | `PROJECT.MEMBERS` |
| `canManageMembers` | `PROJECT.MEMBERS.MANAGE` | tożsame z `canViewMembers` — rozważyć merge | `PROJECT.MEMBERS` |
| `canManageStatus` | `PROJECT.STATUS.TOGGLE` | → `canSettings` (lub usunąć — dead code) | `PROJECT.SETTINGS` |
| `canViewFiles` | OR 7 kodów FILES | zachować | `PROJECT.FILES` |
| `canViewEstimates` | OR 7 kodów ESTIMATES | zachować | `PROJECT.ESTIMATES` |
| `canViewCosts` | OR 2 kody COSTS | zachować | `PROJECT.COSTS` |
| `canViewSchedule` | OR 7 kodów SCHEDULE | zachować | `PROJECT.SCHEDULE` |
| `canViewDashboard` | `PROJECT.DASHBOARD.VIEW` | zachować | `PROJECT.DASHBOARD` |
| `canReadResources` | `PROJECT.FILES.READ_OWN` | **USUNĄĆ** — nieużywane bezpośrednio poza `useResourcePermissions` | — |
| `canWriteResources` | `PROJECT.FILES.WRITE_OWN` | **USUNĄĆ** | — |
| `canReadSharedResources` | `PROJECT.FILES.READ_SHARED` | **USUNĄĆ** | — |
| `canWriteSharedResources` | `PROJECT.FILES.WRITE_SHARED` | **USUNĄĆ** | — |
| `canReadAllResources` | `PROJECT.FILES.READ_ALL` | **USUNĄĆ** | — |
| `canWriteAllResources` | `PROJECT.FILES.WRITE_ALL` | **USUNĄĆ** | — |
| `canShareResources` | `PROJECT.FILES.SHARE` | **USUNĄĆ** | — |
| `hasAnyResourceAccess` | derived | zachować — teraz `canViewFiles \|\| canViewEstimates` | — |
| `canReadMessages` | `CHAT.READ` | **USUNĄĆ** (dead code) | — |
| `canWriteMessages` | `CHAT.WRITE` | **USUNĄĆ** (dead code) | — |
| `canDeleteMessages` | `CHAT.DELETE` | **USUNĄĆ** (dead code) | — |
| `canListRoles` | `ROLE.LIST` | **USUNĄĆ** (dead code) | — |
| (nowe) `canChat` | — | DODAĆ | `CHAT` |
| (nowe) `canTracker` | — | DODAĆ | `PROJECT.TRACKER` |

### `useResourcePermissions` — nowe mapowanie

Po uproszczeniu (jedno `PROJECT.FILES` = pełny dostęp do plików), podział na tabs/mine/all/shared **traci znaczenie semantyczne**. Dwie opcje:

**Opcja A (minimalne zmiany interfejsu — zachować strukturę):**
```typescript
// tabs — wszystkie pokazane jeśli hasFiles
tabs: {
  showAll: permissions.canViewFiles,
  showMine: permissions.canViewFiles,
  showShared: permissions.canViewFiles,
},
// mine/all/shared — pełny dostęp jeśli ma moduł
mine: {
  canCreate: permissions.canViewFiles,
  canEdit: permissions.canViewFiles,
  canDelete: permissions.canViewFiles,
  canShare: permissions.canViewFiles,
  canManageShare: permissions.canViewFiles,
},
all: { /* same */ },
shared: {
  canEdit: permissions.canViewFiles,
  canReadOnly: false,
},
```
→ Minimalny impact na strony używające ResourcePermissions.

**Opcja B (uproszczenie interfejsu — breaking change w stronach):**
```typescript
export interface ResourcePermissions {
  canAccess: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
  hasAnyAccess: boolean;
}
```
→ Wymaga refaktoru w 5 stronach + GanttContext.

**Rekomendacja: Opcja A** — zachowuje interfejs, minimalizuje ryzyko błędów w stronach.

---

## BLOK 6 — Nowe komponenty

Brak nowych komponentów do stworzenia. Modyfikowane istniejące.

---

## BLOK 7 — Modyfikacje istniejących komponentów

| Komponent | Plik | Typ zmiany | Opis |
|-----------|------|-----------|------|
| `AddProjectMemberModal` | `src/components/AddProjectMemberModal.tsx` | **Refaktor UI** | Usunąć `ACCESS_LEVELS` const. Usunąć `<Select>` per moduł. Dodać `<Checkbox>` per moduł (9 checkboxów). Zmienić state z `Record<number,number>` na `Set<number>` lub `number[]`. Zmienić `handleAddMember` aby wysyłał `modules: number[]` |
| `EditProjectMemberModal` | `src/components/EditProjectMemberModal.tsx` | **Refaktor UI** | Identyczne zmiany. Inicjalizacja z `member.modules` (tablica ID) zamiast `member.modulePermissions.map(mp => mp.module)` |
| `CostFormModal` | `src/components/CostTracker/CostFormModal.tsx` | **Drobna** | `canEdit` z `useProjectPermissions` → teraz mapuje na `PROJECT.SETTINGS` — semantycznie poprawne, kod bez zmian |
| `CostFormDrawer` | `src/components/CostTracker/CostFormDrawer.tsx` | **Drobna** | j.w. |
| `dashboard/CostModal` | `src/features/dashboard/components/CostModal.tsx` | **Drobna** | j.w. |
| `GanttContext` | `src/components/gantt/GanttContext.tsx` | **Zależna** | Importuje `ResourcePermissions` type — zaktualizuje się automatycznie jeśli Opcja A |

### Szczegóły — `AddProjectMemberModal.tsx`

```tsx
// STARE — do usunięcia
const ACCESS_LEVELS: Record<number, Array<{ value: number; label: string }>> = { /* 8 entries */ };

// STARE state
const [modulePermissions, setModulePermissions] = useState<Record<number, number>>({});

// NOWE state
const [selectedModules, setSelectedModules] = useState<Set<number>>(new Set());

// STARE render
<Select value={currentLevel} onChange={...}>
  {options.map(opt => <option .../>)}
</Select>

// NOWE render
<Checkbox
  isChecked={selectedModules.has(mod.id)}
  onChange={(e) => {
    setSelectedModules(prev => {
      const next = new Set(prev);
      e.target.checked ? next.add(mod.id) : next.delete(mod.id);
      return next;
    });
  }}
>
  {mod.label}
</Checkbox>

// STARE payload
const permissionsArray = Object.entries(modulePermissions)
  .filter(([, level]) => level > 0)
  .map(([mod, level]) => ({ module: Number(mod), accessLevel: level }));
await projectApi.addProjectMember(tenantId, projectId, userId, permissionsArray);

// NOWE payload
const modules = Array.from(selectedModules);
await projectApi.addProjectMember(tenantId, projectId, userId, modules);
```

### Szczegóły — `EditProjectMemberModal.tsx`

```tsx
// STARE inicjalizacja
const initialPermissions = (): Record<number, number> => {
  const map: Record<number, number> = {};
  for (const mp of member.modulePermissions) {
    map[mp.module] = mp.accessLevel;
  }
  return map;
};

// NOWE inicjalizacja
const initialModules = (): Set<number> => new Set(member.modules);
```

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---------|------------------------|--------------------------------|
| Checkbox do zaznaczania opcji | `<Checkbox>` używany w innych formularzach (np. AddChatMemberModal) | TAK — użyć wzorca |
| Toasty powiadomień | `showApiSuccess`, `showError` w obu modalach | NIE — bez zmian |
| `DataCard` jako kontener | Używany w AddProjectMemberModal | NIE — bez zmian |
| Invalidacja React Query cache | `queryClient.invalidateQueries(projectKeys.*)` | NIE — bez zmian |
| `AppModal` pattern | EditProjectMemberModal używa `<AppModal>` — poprawnie | NIE — bez zmian |
| `handleApiError` | Oba modale — spójne | NIE — bez zmian |
| Formatowanie modułów | `MODULES` lista z id+label w obu modalach — duplikacja kodu | OPCJONALNIE wydzielić wspólną stałą |

---

## BLOK 9 — Dostępność (WCAG AA / AXE)

### Kontrast kolorów

| Element | Kolor tekstu | Kolor tła | Kontrast (szac.) | Status |
|---------|-------------|-----------|-----------------|--------|
| Tytuł modalu | domyślny ciemny | white | >4.5:1 | ✓ |
| Label modułu (`neutral.600`) | `neutral.600` | white | ~6.5:1 | ✓ |
| Email (`neutral.500`) | `neutral.500` | white | ~4.5:1 | ⚠ sprawdź |
| Tekst sekcji (`neutral.500` uppercase) | `neutral.500` | white | ~4.5:1 | ⚠ sprawdź |

#### Flagi dla nowych checkboxów:

- `<Checkbox>` z Chakra UI ma wbudowane ARIA — ✓ domyślnie dostępny
- Każdy `<Checkbox>` musi mieć opisowy tekst (label) — etykiety modułów jako dzieci checkboxa są poprawne
- Jeśli planowane jest zastosowanie tylko ikonki bez tekstu dla modułów — dodać `aria-label`

### Atrybuty ARIA

| Komponent | Problem | Rekomendacja |
|-----------|---------|-------------|
| Stare `<Select>` dla poziomów dostępu | brak, Chakra obsługuje domyślnie | — |
| Nowe `<Checkbox>` | Chakra UI dodaje ARIA automatycznie | ✓ |
| Grid modułów w modalach | `<Grid>` jest layoutem — nie wymaga role | ✓ |
| `<ModalCloseButton>` w `AddProjectMemberModal` | Chakra UI dodaje aria-label "Close" | ✓ |

### Zarządzanie fokusem

- `AddProjectMemberModal` używa `<Modal>` z Chakra → focus trap automatyczny ✓
- `EditProjectMemberModal` używa `<AppModal>` (wraps `<Modal>`) → focus trap ✓

### Testy AXE

- Brak testów AXE dla obu modalach uprawnień
- Nie blokuje wdrożenia, ale zalecane dodanie w ramach zadania testowego

### Podsumowanie dostępności

| Kategoria | Status | Uwagi |
|----------|--------|-------|
| Kontrast kolorów | ⚠ | `neutral.500` przy treści może być na granicy 4.5:1 — verify |
| Atrybuty ARIA | ✓ | Chakra `<Checkbox>` obsługuje automatycznie |
| Klawiatura / fokus | ✓ | Modal z Chakra = focus trap |
| Testy AXE | ✗ | Brak testów — warto dodać |

---

## BLOK 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---------|---------------|--------|-------------|
| 1 | `useResourcePermissions` zakłada granularne uprawnienia (tabs/mine/all/shared) | `src/hooks/useResourcePermissions.ts` | WYSOKI — wszystkie strony plików/kosztorysów/harmonogramów przestaną działać poprawnie | Opcja A: zachować strukturę, wszystkie property = `canViewFiles` (etc.) |
| 2 | `ProjectFiles.tsx`, `ProjectCosts.tsx` mają logikę zakładek "Wszystkie/Moje/Udostępnione" | Pages | WYSOKI — zakładki będą zawsze widoczne (lub zawsze ukryte) | Po stronie UX: czy zachować zakładki? Jeśli tak → Opcja A w `useResourcePermissions` |
| 3 | `CostEstimateEditPage.tsx` używa `mine.canEdit`, `all.canEdit`, `shared.canEdit` — trzy różne stany | `src/pages/CostEstimateEditPage.tsx` | WYSOKI — z Opcją A wszystkie 3 będą = true jeśli ma estimates | Nie jest problemem z Opcją A |
| 4 | `canEdit` i `canView` dla PROJECT.SETTINGS mapują na ten sam kod | `src/hooks/useProjectPermissions.ts` | ŚREDNI — logicznie poprawne (mając settings możesz i edytować i podglądać), ale mylące semantycznie | Zachować oba dla kompatybilności wstecznej, oba = `hasPermission(…, PROJECT.SETTINGS)` |
| 5 | `canManageMembers` vs `canViewMembers` — oba = `PROJECT.MEMBERS` | `src/hooks/useProjectPermissions.ts` | NISKI — `ProjectDetails.tsx` używa `(canViewMembers \|\| canManageMembers)` → działa | Zachować oba jako aliasy |
| 6 | `GanttContext` przekazuje `ResourcePermissions` jako prop — zmiana struktury go złamie | `src/components/gantt/GanttContext.tsx` + Gantt dzieci | ŚREDNI | Opcja A gwarantuje backward compatibility |
| 7 | `PROJECT.TRACKER` (moduł 8) jest pominięty w `MODULES` array obu modali! | `AddProjectMemberModal.tsx`, `EditProjectMemberModal.tsx` | BUG — tracker nie jest konfigurowalny przez UI | Dodać `{ id: 8, label: "Tracker" }` do `MODULES` przy okazji refaktoru |
| 8 | Dead code w `useProjectPermissions`: `canManageStatus`, `canReadMessages`, `canWriteMessages`, `canDeleteMessages`, `canListRoles` | `src/hooks/useProjectPermissions.ts` | NISKI — dead code | Usunąć przy okazji |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe komponenty | 0 |
| Zmodyfikowane komponenty | 5 |
| Nowe hooki | 0 |
| Zmodyfikowane hooki | 2 |
| Zmodyfikowane typy TypeScript | 4 |
| Zmodyfikowane stałe | 1 |
| Zmodyfikowane serwisy API | 1 |
| Strony z ryzykiem pośrednim | 8 |
| Naruszenia WCAG AA | 0 (⚠ 1 do weryfikacji) |
| BUG znaleziony | 1 (Tracker brakuje w MODULES) |
| Pytania domenowe | 3 |

---

## Pytania domenowe wymagające decyzji

1. **Zakładki "Wszystkie/Moje/Udostępnione"** w `ProjectFiles`, `ProjectCosts`, `ProjectSchedules`, `CostEstimateEditPage`: Z jednym kodem `PROJECT.FILES` user ma pełny dostęp lub brak. Czy zakładki są nadal potrzebne UX-owo? Jeśli tak — wszystkie będą zawsze widoczne gdy user ma moduł. Jeśli nie — strony wymagają refaktoru widoku.

2. **`canEdit` vs `canView` dla Settings**: Oba mapują na `PROJECT.SETTINGS`. Czy zachować dwa property dla kompatybilności (kód bez zmian w `ProjectDetails.tsx`, `ProjectParameters.tsx`), czy ujednolicić do jednego `canSettings`?

3. **`PROJECT.STATUS.TOGGLE` znika** — był osobnym kodem nie należącym do żadnego modułu. Teraz `canManageStatus` ma mapować na `PROJECT.SETTINGS`? Potwierdzić z backendem czy toggle statusu projektu jest pod `PROJECT.SETTINGS` czy zostaje osobnym mechanizmem.

---

## Lista WSZYSTKICH plików do modyfikacji

| # | Plik | Typ zmiany | Priorytet |
|---|------|-----------|-----------|
| 1 | `src/types/projectModulePermissions.ts` | Usunąć `ModuleAccessLevel`, `ProjectMemberPresets`; uproszczenie `ProjectMemberModulePermission` | KRYTYCZNY |
| 2 | `src/types/project.types.ts` | `ModulePermissionWeb` → usunąć `accessLevel`; `ProjectMemberWeb.modulePermissions[]` → `modules: number[]` | KRYTYCZNY |
| 3 | `src/constants/roleCodes.ts` | Zastąpić 44 `PermissionCodes` → 9 nowych kodów; aktualizacja `PermissionCode` type | KRYTYCZNY |
| 4 | `src/hooks/useProjectPermissions.ts` | Rewrite: 7 granularnych flag files/estimates/costs/schedule → single checks; usunąć dead code; dodać `canChat`, `canTracker` | KRYTYCZNY |
| 5 | `src/hooks/useResourcePermissions.ts` | Redesign (Opcja A): `tabs/mine/all/shared` → wszystkie = `canViewFiles`/`canViewEstimates` etc. per kontekst | WYSOKI |
| 6 | `src/components/AddProjectMemberModal.tsx` | Usunąć `ACCESS_LEVELS`; `<Select>` → `<Checkbox>`; state `Record<number,number>` → `Set<number>`; dodać Tracker (id: 8); nowy payload | KRYTYCZNY |
| 7 | `src/components/EditProjectMemberModal.tsx` | Identyczne zmiany co Add; inicjalizacja z `member.modules`; dodać Tracker | KRYTYCZNY |
| 8 | `src/api/projectApi.ts` | `addProjectMember`: `modulePermissions: {module,accessLevel}[]` → `modules: number[]`; `updateProjectMemberPermissions`: j.w. | KRYTYCZNY |
| 9 | `src/pages/ProjectFiles.tsx` | Weryfikacja po zmianie `useResourcePermissions` — przy Opcji A: brak zmian kodu | NISKI (przy Opcji A) |
| 10 | `src/pages/ProjectCosts.tsx` | j.w. | NISKI |
| 11 | `src/pages/ProjectSchedules.tsx` | j.w. | NISKI |
| 12 | `src/pages/CostEstimateEditPage.tsx` | j.w. | NISKI |
| 13 | `src/pages/WorkScheduleView.tsx` | j.w. | NISKI |
| 14 | `src/components/gantt/GanttContext.tsx` | j.w. (zależy od `ResourcePermissions` type) | NISKI |
| 15 | `src/pages/ProjectDetails.tsx` | Weryfikacja po zmianie useProjectPermissions — przy zachowaniu nazw flag: brak zmian | NISKI |
| 16 | `src/pages/ProjectMembers.tsx` | j.w. | NISKI |
| 17 | `src/pages/ProjectParameters.tsx` | j.w. | NISKI |
| 18 | `src/components/CostTracker/CostFormModal.tsx` | j.w. (`canEdit` zachowane) | NISKI |
| 19 | `src/components/CostTracker/CostFormDrawer.tsx` | j.w. | NISKI |
| 20 | `src/features/dashboard/components/CostModal.tsx` | j.w. | NISKI |
