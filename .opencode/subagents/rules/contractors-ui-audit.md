# Audyt UI — Feature: Kontrahenci (Contractors)

> **Data:** 2026-05-18  
> **Zakres:** Warstwa UI (React/TypeScript) — projekt `01-Applications/ProjectDataManagementUI`

---

## BLOK 1 — Stan obecny UI (obszary powiązane z feature)

| Komponent/Strona | Lokalizacja | Opis | Powiązane z feature |
|---|---|---|---|
| `CostForm` | `src/components/CostTracker/CostForm.tsx` | Formularz kosztów TrackedCost (nazwa, kwota, numer faktury, wykonawca, data, pliki). Pole `contractor` to wolnotypowy `<Input>`. | ✅ Pole `contractor` do zastąpienia przez `ContractorPicker` |
| `CostFormDrawer` | `src/components/CostTracker/CostFormDrawer.tsx` | Drawer wrapper nad `CostForm` — tryb dodawania/edycji kosztu śledzenia. Inicjalizuje `contractor: ""` i przekazuje wartość do API. | ✅ EMPTY_FORM + inicjalizacja z `cost.contractor` |
| `CostFormModal` | `src/components/CostTracker/CostFormModal.tsx` | Modal 4-krokowy (wybór kosztorysu → etap → pozycja → formularz). Reużywa `CostForm` w ostatnim kroku, inicjalizuje `contractor: ""` w EMPTY_FORM. | ✅ EMPTY_FORM + submit z `contractor: string` |
| `CostModal` | `src/features/dashboard/components/CostModal.tsx` | **Niezależna** implementacja formularza dla TrackedCost i ProjectCost (używana w dashboardzie). Własny `CostFormState` z `contractor: string`. **Nie reużywa `CostForm`**. | ✅ Własne pole contractor do zastąpienia |
| `CostListDrawer` | `src/components/CostTracker/CostListDrawer.tsx` | Drawer z listą kosztów — wyświetla `"Wykonawca: {cost.contractor}"` | ✅ Wyświetla `contractor` z `TrackedCostWeb` |
| `ProjectAdditionalCostsSection` | `src/components/CostTracker/ProjectAdditionalCostsSection.tsx` | Sekcja kosztów dodatkowych projektu w trackerze — wyświetla `{cost.contractor ?? "—"}` | ✅ Wyświetla `contractor` z `TrackedCostWeb` |
| `ExpenseCard` | `src/components/ExpenseCard.tsx` | Karta mobilna kosztu projektowego — pokazuje `cost.contractor` w meta-linii. Używa `ProjectCostListItemWeb`. | ✅ Wyświetla `contractor` z `ProjectCostListItemWeb` |
| `ProjectSimpleCosts` | `src/pages/ProjectSimpleCosts.tsx` | Strona kosztów projektowych — wyświetla `{cost.contractor \|\| "-"}` w trzech różnych zakładkach (linie 261, 431, 556). Odczytuje `cost.contractor` przy init formularza edycji (linie 776, 812). | ✅ Wyświetla i odczytuje `contractor` |
| `TenantDetails` | `src/pages/TenantDetails.tsx` | Strona zarządzania tenantem (członkowie, zaproszenia, nazwa, status). **Brak sekcji kontrahentów.** | ✅ Potencjalne miejsce na link do ContractorsPage |
| `Sidebar` | `src/components/Sidebar.tsx` | Nawigacja boczna: Projekty, Wiadomości, Zarządzanie (`/tenants/managed`), Zaproszenia, Zaplanowane prace, Szablony kosztorysów, Ustawienia. **Brak wpisu dla kontrahentów.** | ✅ Nowy wpis nawigacyjny (dla TENANT.ADMIN) |
| `AppRouter` | `src/routes/AppRouter.tsx` | Router aplikacji. Brak trasy dla kontrahentów. Ścieżki tenant-level: `/tenants/:tenantId`, `/tenants/managed`, `/tenants/invitations`. | ✅ Nowa trasa do dodania |
| `dashboardApi` | `src/features/dashboard/services/dashboardApi.ts` | API service dla dashboardu — serializuje `contractor` do FormData (linie 46, 76) | ✅ Zmiana na `contractorId` |
| `costTrackerApi` | `src/api/costTrackerApi.ts` | API client dla TrackedCost — `buildCostFormData` appenduje `contractor` jako string | ✅ Zmiana na `contractorId` |

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|---|---|---|---|
| Brak `ContractorPicker` komponentu | Nowy komponent | KRYTYCZNY | Select/combobox do wyboru kontrahenta z listy tenanta. Musi obsługiwać prop `canQuickAdd` dla PROJECT.ADMIN / TENANT.ADMIN |
| Brak `ContractorsPage` | Nowa strona | KRYTYCZNY | Lista kontrahentów tenanta z CRUD (dodaj/edytuj/usuń). Chroniona rolą TENANT.ADMIN |
| Brak `contractorApi.ts` | Nowy API client | KRYTYCZNY | Wywołania REST dla endpointów `/tenants/{tenantId}/contractors` |
| Brak `useContractors` hook | Nowy hook | KRYTYCZNY | `useQuery` + `useMutation` dla kontrahentów; query key zgodny z wzorcem `costTrackerKeys` |
| Brak trasy w routerze | Modyfikacja AppRouter | WYSOKI | Nowa trasa `/contractors` (scoped do active tenant) lub `/tenants/:tenantId/contractors` |
| Brak nawigacji w Sidebar | Modyfikacja Sidebar | WYSOKI | Wpis „Kontrahenci" widoczny tylko dla TENANT.ADMIN |
| Brak `contractor.types.ts` | Nowy typ | WYSOKI | Typy `ContractorWeb`, `CreateContractorCommand`, `UpdateContractorCommand` |
| `CostForm` nie przekazuje `tenantId` | Modyfikacja props | WYSOKI | `ContractorPicker` potrzebuje `tenantId` — `CostForm` musi otrzymać go jako prop |
| `CostModal` (dashboard) ma własny formularz | Modyfikacja komponentu | WYSOKI | Niezależna implementacja `contractor: string` — musi być zaktualizowana niezależnie od `CostForm` |
| Quick-add modal kontrahenta | Nowy komponent | ŚREDNI | Mały modal inline (imię/email/NIP) wywołany z `ContractorPicker` — tylko PROJECT.ADMIN i TENANT.ADMIN |

---

## BLOK 3 — Typy TypeScript

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|---|---|---|---|
| `ContractorWeb` | `src/types/contractor.types.ts` | **NOWY** | `{ id: string; tenantId: string; name: string; nip?: string \| null; email?: string \| null; phone?: string \| null; createdAt: string; }` |
| `ContractorListItemWeb` | `src/types/contractor.types.ts` | **NOWY** | Uproszczona wersja do list/select: `{ id: string; name: string; nip?: string \| null }` |
| `CreateContractorCommand` | `src/types/contractor.types.ts` | **NOWY** | `{ name: string; nip?: string \| null; email?: string \| null; phone?: string \| null }` |
| `UpdateContractorCommand` | `src/types/contractor.types.ts` | **NOWY** | Jak `CreateContractorCommand` + `{ id: string }` |
| `CostFormValues` | `src/types/costTracker.types.ts` | **MODYFIKACJA** | `contractor?: string` → `contractorId?: string \| null` |
| `CreateCostRequest` | `src/types/costTracker.types.ts` | **MODYFIKACJA** | `contractor?: string` → `contractorId?: string \| null` |
| `UpdateCostRequest` | `src/types/costTracker.types.ts` | **MODYFIKACJA** | Dziedziczy z `CreateCostRequest` — zmiana automatyczna |
| `TrackedCostWeb` | `src/types/costTracker.types.ts` | **MODYFIKACJA** | `contractor: string \| null` → `contractorId: string \| null; contractorName: string \| null` |
| `ProjectCostListItemWeb` | `src/types/project.types.ts` | **MODYFIKACJA** | `contractor: string \| null` → `contractorId: string \| null; contractorName: string \| null` |
| `CreateProjectCostCommand` | `src/types/project.types.ts` | **MODYFIKACJA** | `contractor?: string \| null` → `contractorId?: string \| null` |
| `UpdateProjectCostCommand` | `src/types/project.types.ts` | **MODYFIKACJA** | `contractor?: string \| null` → `contractorId?: string \| null` |
| `TrackedCostWeb` | `src/features/dashboard/types/projectDashboard.types.ts` | **MODYFIKACJA** | `contractor: string \| null` → `contractorId: string \| null; contractorName: string \| null` |
| `CreateTrackedCostRequest` | `src/features/dashboard/types/projectDashboard.types.ts` | **MODYFIKACJA** | `contractor?: string \| null` → `contractorId?: string \| null` |
| `UpdateTrackedCostRequest` | `src/features/dashboard/types/projectDashboard.types.ts` | **MODYFIKACJA** | `contractor?: string \| null` → `contractorId?: string \| null` |

> **Uwaga:** `TrackedCostWeb` istnieje w **dwóch miejscach** — `src/types/costTracker.types.ts` i `src/features/dashboard/types/projectDashboard.types.ts`. Oba muszą być zaktualizowane.

---

## BLOK 4 — Serwisy API (src/api/)

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint | Opis |
|---|---|---|---|---|
| `contractorApi.getAll` | `src/api/contractorApi.ts` | **NOWA** | `GET /tenants/{tenantId}/contractors` | Lista kontrahentów tenanta |
| `contractorApi.getById` | `src/api/contractorApi.ts` | **NOWA** | `GET /tenants/{tenantId}/contractors/{id}` | Szczegóły jednego kontrahenta |
| `contractorApi.create` | `src/api/contractorApi.ts` | **NOWA** | `POST /tenants/{tenantId}/contractors` | Dodaj kontrahenta |
| `contractorApi.update` | `src/api/contractorApi.ts` | **NOWA** | `PUT /tenants/{tenantId}/contractors/{id}` | Edytuj kontrahenta |
| `contractorApi.delete` | `src/api/contractorApi.ts` | **NOWA** | `DELETE /tenants/{tenantId}/contractors/{id}` | Usuń kontrahenta |
| `buildCostFormData` | `src/api/costTrackerApi.ts` | **MODYFIKACJA** | — | Zmiana `form.append('contractor', data.contractor)` → `form.append('contractorId', data.contractorId ?? '')` |
| `createTrackedCost` | `src/features/dashboard/services/dashboardApi.ts` | **MODYFIKACJA** | — | `formData.append('contractor', ...)` → `formData.append('contractorId', ...)` |
| `updateTrackedCost` | `src/features/dashboard/services/dashboardApi.ts` | **MODYFIKACJA** | — | Jak wyżej |
| ProjectCost create/update | `src/api/projectApi.ts` | **MODYFIKACJA** | — | Formularz danych: `contractor` → `contractorId` |

**Wzorzec dla `contractorApi.ts`** (na podstawie `tenantApi.ts`):
```typescript
import { axiosClient } from './axiosClient';
import type { ContractorWeb, ContractorListItemWeb, CreateContractorCommand, UpdateContractorCommand } from '../types/contractor.types';

export const contractorApi = {
  getAll: async (tenantId: string): Promise<ContractorListItemWeb[]> => {
    const res = await axiosClient.get<ContractorListItemWeb[]>(`/tenants/${tenantId}/contractors`);
    return res.data;
  },
  // ...
};
```

---

## BLOK 5 — Hooki React Query

| Hook | Plik | Nowy/Modyfikacja | Query/Mutation | Opis |
|---|---|---|---|---|
| `contractorKeys` | `src/hooks/queries/useContractors.ts` | **NOWY** | — | Query key factory: `all`, `byTenant(tenantId)`, `detail(tenantId, id)` |
| `useContractors` | `src/hooks/queries/useContractors.ts` | **NOWY** | Query | Lista wszystkich kontrahentów tenanta; `enabled: Boolean(tenantId)` |
| `useCreateContractor` | `src/hooks/queries/useContractors.ts` | **NOWY** | Mutation | `useMutation` + `invalidateQueries(contractorKeys.byTenant(...))` |
| `useUpdateContractor` | `src/hooks/queries/useContractors.ts` | **NOWY** | Mutation | Edycja; invalidate `byTenant` |
| `useDeleteContractor` | `src/hooks/queries/useContractors.ts` | **NOWY** | Mutation | Usunięcie; invalidate `byTenant` |
| `index.ts` | `src/hooks/queries/index.ts` | **MODYFIKACJA** | — | Dodać export: `useContractors, contractorKeys, useCreateContractor, ...` |

**Wzorzec** (na podstawie `useCostTracker.ts`):
```typescript
export const contractorKeys = {
  all: ['contractors'] as const,
  byTenant: (tenantId: string) => ['contractors', tenantId] as const,
  detail: (tenantId: string, id: string) => ['contractors', tenantId, id] as const,
};

export function useContractors(tenantId: string | undefined) {
  return useQuery<ContractorListItemWeb[]>({
    queryKey: contractorKeys.byTenant(tenantId ?? ''),
    queryFn: () => contractorApi.getAll(tenantId!),
    enabled: Boolean(tenantId),
  });
}
```

---

## BLOK 6 — Nowe komponenty

| Komponent | Lokalizacja | Opis | Zależy od |
|---|---|---|---|
| `ContractorPicker` | `src/components/ContractorPicker.tsx` | Select z listą kontrahentów (pobieranych przez `useContractors`). Props: `tenantId`, `value: string \| null`, `onChange(id: string \| null)`, `canQuickAdd?: boolean`, `isDisabled?`. Opcja pusta ("Brak"). Jeśli `canQuickAdd=true` — przycisk `+` otwierający `ContractorQuickAddModal`. | `useContractors`, `ContractorQuickAddModal` |
| `ContractorQuickAddModal` | `src/components/ContractorQuickAddModal.tsx` | Mały modal z formularzem (name wymagane, nip/email opcjonalne). Używa `AppModal`. Po zapisie odświeża query i selektuje nowo dodanego. | `AppModal`, `useCreateContractor` |
| `ContractorsPage` | `src/pages/ContractorsPage.tsx` | Strona tenant-level (zarządzanie). Tabela/lista kontrahentów z akcjami: Dodaj, Edytuj, Usuń. Chroniona przez `useTenantPermissions().canEdit`. Używa `AppModal` lub `DeleteAlertDialog`. | `useContractors`, `AppModal`, `DeleteAlertDialog`, `useTenantPermissions` |

---

## BLOK 7 — Modyfikacje istniejących komponentów

| Komponent | Plik | Typ zmiany | Opis |
|---|---|---|---|
| `CostForm` | `src/components/CostTracker/CostForm.tsx` | Pole + props | (1) Dodać prop `tenantId: string` do `CostFormProps`. (2) Zamienić `<Input>` dla wykonawcy na `<ContractorPicker tenantId={tenantId} value={values.contractorId ?? null} onChange={(id) => set({ contractorId: id })} canQuickAdd={canQuickAdd} isDisabled={isSubmitting} />`. (3) Zmienić typ w `CostFormValues.contractorId`. (4) Dodać prop `canQuickAdd?: boolean`. |
| `CostFormDrawer` | `src/components/CostTracker/CostFormDrawer.tsx` | Init + props + submit | (1) Dodać `tenantId` do propsu `CostForm`. (2) EMPTY_FORM: `contractor: ""` → `contractorId: null`. (3) Init z `cost`: `cost.contractorId ?? null`. (4) Submit: `contractorId: values.contractorId \|\| undefined`. (5) Dodać logikę `canQuickAdd` z `useTenantPermissions`. |
| `CostFormModal` | `src/components/CostTracker/CostFormModal.tsx` | Init + submit | (1) EMPTY_FORM: `contractor: ""` → `contractorId: null`. (2) Submit: zmiana pola `contractor` → `contractorId`. (3) Przekazać `tenantId` do `CostForm` (już jest w propsach). |
| `CostModal` (dashboard) | `src/features/dashboard/components/CostModal.tsx` | Form state + pole | (1) `CostFormState.contractor: string` → `contractorId: string \| null`. (2) Init z `cost.contractorId`. (3) Zastąpić `<Input>` dla wykonawcy przez `<ContractorPicker>`. (4) Submit: `contractor: form.contractor \|\| null` → `contractorId: form.contractorId`. |
| `CostListDrawer` | `src/components/CostTracker/CostListDrawer.tsx` | Wyświetlanie | Zmienić `cost.contractor` → `cost.contractorName` |
| `ProjectAdditionalCostsSection` | `src/components/CostTracker/ProjectAdditionalCostsSection.tsx` | Wyświetlanie | Zmienić `cost.contractor ?? "—"` → `cost.contractorName ?? "—"` |
| `ExpenseCard` | `src/components/ExpenseCard.tsx` | Wyświetlanie | Zmienić `cost.contractor` → `cost.contractorName` w meta-partsach |
| `ProjectSimpleCosts` | `src/pages/ProjectSimpleCosts.tsx` | Wyświetlanie + formularz | (1) Linie 261, 431, 556: `cost.contractor` → `cost.contractorName`. (2) Linie 776, 812: `contractor: cost.contractor` → `contractorId: cost.contractorId`. (3) Form state init: `contractor: ""` → `contractorId: null`. |
| `AppRouter` | `src/routes/AppRouter.tsx` | Nowa trasa | Dodać `<Route path="/contractors" element={<ProtectedRoute><ContractorsPage /></ProtectedRoute>} />` |
| `Sidebar` | `src/components/Sidebar.tsx` | Nawigacja warunkowa | Dodać wpis „Kontrahenci" (ikona `Users` z lucide-react), widoczny tylko gdy `useTenantPermissions().canEdit === true`. Umieścić po „Zarządzanie". |
| `dashboardApi` | `src/features/dashboard/services/dashboardApi.ts` | FormData | Zmienić `formData.append('contractor', data.contractor)` → `formData.append('contractorId', ...)` w `createTrackedCost` i `updateTrackedCost` |
| `costTrackerApi` | `src/api/costTrackerApi.ts` | FormData | Zmienić `if (data.contractor) form.append('contractor', data.contractor)` → `if (data.contractorId) form.append('contractorId', data.contractorId)` |

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---|---|---|
| Strony tenant-level (nie project-level) | `/tenants/managed` → `ManagedTenants.tsx`, `/cost-estimate-templates` → `CostEstimateTemplates.tsx` | ✅ `ContractorsPage` jako `/contractors` (scoped do active tenant, jak szablony) |
| Query key factory | `costTrackerKeys`, `tenantKeys`, `projectKeys` — każdy plik definiuje `const xyzKeys = { all, byX, detail }` | ✅ `contractorKeys` musi mieć ten sam wzorzec |
| API client jako `const xxxApi = {}` | `costTrackerApi`, `tenantApi`, `projectApi` | ✅ `contractorApi` jako named export z obiektu |
| Eksport przez `hooks/queries/index.ts` | Wszystkie query hooks eksportowane centralnie | ✅ Dodać eksport `useContractors` etc. |
| `AppModal` dla modali CRUD | `AddProjectMemberModal`, `CreateCostEstimateModal` — wszystkie używają `AppModal` | ✅ `ContractorQuickAddModal` i formularze na ContractorsPage muszą używać `AppModal` |
| `DeleteAlertDialog` | Używany w `ProjectSimpleCosts`, `TenantDetails`, `ProjectCosts` | ✅ `ContractorsPage` musi używać `DeleteAlertDialog` do usuwania |
| Formatowanie brakujących wartości | `"-"` lub `"—"` jako fallback w tabelach | ✅ Wyświetlać `contractorName ?? "—"` |
| Role checking pattern | `useTenantPermissions().canEdit` dla operacji tenant-level; `useProjectPermissions(id).roleCode` dla projektu | ✅ ContractorsPage: guard `!tenantPerms.canEdit`. Quick-add: `tenantPerms.canEdit \|\| projectPerms.roleCode === RoleCodes.PROJECT_ADMIN` |
| Mobilny design | `useBreakpointValue`, `AppModal` full-screen na mobile, karty zamiast tabel | ✅ `ContractorsPage` musi być responsive |

---

## BLOK 9 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---|---|---|---|
| 1 | **Breaking change** — zmiana `contractor: string` → `contractorId + contractorName` dotyka 12+ plików jednocześnie | Wszystkie typy i komponenty wyświetlające | 🔴 WYSOKI — błędy TypeScript w całym projekcie po zmianie typów | Zmienić typy i wszystkie miejsca użycia w jednej sesji implementacji; uruchomić `tsc --noEmit` po każdej grupie zmian |
| 2 | **Dwa niezależne `TrackedCostWeb`** — `src/types/costTracker.types.ts` i `src/features/dashboard/types/projectDashboard.types.ts` definiują ten sam interfejs osobno | `projectDashboard.types.ts` | 🔴 WYSOKI — można pominąć jeden przy refaktorze | Zaktualizować oba pliki; rozważyć konsolidację (out-of-scope dla tego feature) |
| 3 | **`CostForm` nie ma `tenantId`** — `ContractorPicker` potrzebuje tenantId do pobrania listy | `CostForm.tsx` | 🟡 ŚREDNI — złamanie interfejsu komponentu | Dodać `tenantId: string` do `CostFormProps`; sprawdzić wszystkie miejsca gdzie `CostForm` jest renderowany (`CostFormDrawer`, `CostFormModal`) |
| 4 | **`CostModal` (dashboard) jest odrębną implementacją** — ma własny formularz niepowiązany z `CostForm.tsx` | `src/features/dashboard/components/CostModal.tsx` | 🟡 ŚREDNI — łatwo pominąć przy implementacji | Zaktualizować niezależnie; `ContractorPicker` musi być zaimportowany bezpośrednio |
| 5 | **`useProjectPermissions` wymaga `projectId`** — quick-add w `ContractorPicker` musi wiedzieć, czy user jest PROJECT.ADMIN | `ContractorPicker.tsx` | 🟡 ŚREDNI — `ContractorPicker` jest komponentem tenant-level, nie project-level | Przyjąć `canQuickAdd` jako prop przekazywany z parent (nie sprawdzać roli wewnątrz Pickera) |
| 6 | **Brak `TENANT_CONTRACTORS_MANAGE` permission** — `useTenantPermissions` nie ma dedykowanego uprawnienia dla kontrahentów | `src/constants/roleCodes.ts`, backend | 🟡 ŚREDNI — można reużyć `TENANT_EDIT` lub dodać nowe | Potwierdzić z backendem czy dodawany jest nowy permission code; jeśli tak — dodać do `PermissionCodes` i `useTenantPermissions` |
| 7 | **Dane historyczne** — istniejące koszty mają `contractor: string` bez powiązania z `Contractor` encją | API / baza danych | 🟡 ŚREDNI — dane legacy nie będą miały `contractorId` | Wymagana strategia migracji po stronie backendu; UI musi obsłużyć `contractorId: null` + `contractorName: null` gracefully |
| 8 | **`ProjectSimpleCosts.tsx` — 3 zakładki z `cost.contractor`** | `src/pages/ProjectSimpleCosts.tsx` linie 261, 431, 556 | 🟢 NISKI — mechanicznie prosta zmiana | Zamienić na `cost.contractorName` we wszystkich trzech miejscach |
| 9 | **Nawigacja sidebar jest warunkowa** — jeśli `ContractorsPage` ma być tylko dla TENANT.ADMIN, sidebar musi ukryć wpis dla TENANT.MEMBER | `src/components/Sidebar.tsx` | 🟢 NISKI | Sprawdzić `useTenantPermissions().canEdit` w SidebarContent |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---|---|
| Nowe komponenty | 3 (`ContractorPicker`, `ContractorQuickAddModal`, `ContractorsPage`) |
| Zmodyfikowane komponenty | 10 (`CostForm`, `CostFormDrawer`, `CostFormModal`, `CostModal`, `CostListDrawer`, `ProjectAdditionalCostsSection`, `ExpenseCard`, `ProjectSimpleCosts`, `AppRouter`, `Sidebar`) |
| Nowe API services | 1 (`contractorApi.ts`) |
| Zmodyfikowane API services | 2 (`costTrackerApi.ts`, `dashboardApi.ts`) + `projectApi.ts` |
| Nowe hooki | 1 plik (`useContractors.ts` z 4+ eksportami) + aktualizacja `index.ts` |
| Nowe typy TypeScript | 1 plik (`contractor.types.ts` z 4 typami) |
| Zmodyfikowane typy | 3 pliki, 11 interfejsów |
| Pytania domenowe | 4 |

---

## Pytania domenowe wymagające decyzji

1. **Nowy permission code?** — Czy backend dodaje `TENANT_CONTRACTORS_MANAGE` jako oddzielne uprawnienie, czy korzystamy z istniejącego `TENANT_EDIT`? Odpowiedź wpływa na `roleCodes.ts`, `useTenantPermissions` i guard na `ContractorsPage`.

2. **Kto może wybrać kontrahenta w formularzu kosztu?** — Czy *każdy* user mogący dodać koszt może wybrać z listy (readonly picker), czy tylko PROJECT.ADMIN może w ogóle widzieć listę? Odpowiedź wpływa na prop `canQuickAdd` vs `isDisabled`.

3. **Co z danymi historycznymi?** — Czy istniejące koszty z tekstowym `contractor` są migrowane do nowej encji, czy UI musi wyświetlać `contractorName` (z backendu) i obsługiwać `null` dla legacy? Czy API zwróci fallback `contractorName` z legacy pola?

4. **Trasa dla `ContractorsPage`?** — `/contractors` (jak `/projects`, scoped do active tenant) czy `/tenants/:tenantId/contractors` (explicit tenant context)? Preferencja dla spójności z istniejącym patternerm: `/contractors` jest spójne z `/cost-estimate-templates` — polecane.
