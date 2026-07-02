# UI Audit — Feature: technical-documentation-rag (MVP)

**Feature:** technical-documentation-rag  
**Data audytu:** 2026-06-22  
**Audytowane obszary:** `01-Applications/ProjectDataManagementUI`  
**Decyzje MVP (zatwierdzone):** jeden kod `PROJECT.TECHNICAL_DOCUMENTATION`, kolejka Azure Storage Queue (async), auto-retry max 3 + ręczny retry w UI, brak RAG, dedykowany `TechnicalDocumentationHub` (SignalR), osobna encja/blob (osobny upload flow), osobny endpoint `count` na kafelek ProjectDetails

---

## BLOK 1 — Stan obecny UI

| Komponent/Strona | Lokalizacja | Opis | Powiązane z feature |
|-----------------|------------|------|---------------------|
| `ProjectDetails` | `src/pages/ProjectDetails.tsx` | Strona projektu z `SimpleGrid` kafelków szybkiego dostępu (Członkowie, Harmonogramy, Pliki, Wydatki, Kosztorysy, Dashboard, Parametry). Kafelki: `Box as="button"`, ikona + etykieta, **bez liczników**. Widoczność przez `useProjectPermissions`. | ✅ Miejsce na nowy kafelek „Dokumentacja techniczna” z count |
| `AppRouter` | `src/routes/AppRouter.tsx` | Routing projektowy: `/projects/:projectId/{members,schedules,files,costs,cost-estimates,dashboard,parameters}`. **Brak tras** dla dokumentacji technicznej. | ✅ Wymaga 2 nowych tras (lista + szczegóły) |
| `Breadcrumbs` | `src/components/Breadcrumbs.tsx` | Mapowanie ścieżek projektowych na segmenty. **Brak** `technical-documentation`. | ⚠️ Wymaga rozszerzenia |
| `useProjectPermissions` | `src/hooks/useProjectPermissions.ts` | Flagi bool per moduł (`canViewFiles`, `canViewCosts`, …) oparte na `hasPermission` + `PermissionCodes`. **Brak** flagi dla dokumentacji technicznej. | ✅ Wymaga rozszerzenia |
| `roleCodes.ts` | `src/constants/roleCodes.ts` | `PermissionCodes` — 9 kodów modułowych (`PROJECT.FILES`, `PROJECT.ESTIMATES`, …). **Brak** `PROJECT.TECHNICAL_DOCUMENTATION`. | ✅ Wymaga nowego kodu |
| `projectModulePermissions.ts` | `src/types/projectModulePermissions.ts` | `ProjectModule` enum (Settings, Files, Estimates, Costs, Schedule, DashboardTracker). **Brak** modułu TechnicalDocumentation. | ✅ Wymaga nowego modułu + etykiety |
| `ProjectModulePermissionPicker` | `src/components/ProjectModulePermissionPicker.tsx` | Checkboxy modułów przy zaproszeniu/edycji członka. Iteruje `SELECTABLE_MODULES` z enum — **automatycznie wymaga** nowego wpisu w enum. | ✅ Wymaga nowego modułu w enum |
| `ProjectSchedules` | `src/pages/ProjectSchedules.tsx` | Wzorzec strony modułu: `BackToProjectButton`, `LoadingSpinner`, `EmptyState`, tabela z klikalnymi wierszami, przycisk tworzenia zależny od uprawnień. | ✅ Wzorzec listy dokumentacji |
| `ProjectCosts` | `src/pages/ProjectCosts.tsx` | Wzorzec listy z zakładkami, `StatusBadge`/kolorowe `Badge`, nawigacja do szczegółów po kliknięciu wiersza. | ✅ Wzorzec listy + statusy |
| `AppModal` | `src/components/ui/AppModal.tsx` | Standardowy wrapper modala (mobile full-screen, desktop centered). Używany w `AICostImportModal`, `CostModal`. | ✅ Wzorzec dla upload modala |
| `DocumentDropzone` | `src/components/ui/DocumentDropzone.tsx` | Drag-and-drop **pojedynczego** pliku. Domyślnie `.jpg,.jpeg,.png`, max 20 MB. Ma `aria-label`. | ⚠️ Wymaga rozszerzenia lub nowego komponentu multi-file |
| `UploadFilesModal` | `src/components/UploadFilesModal.tsx` | Upload wielu plików do ProjectFile (paczki/katalogi). Zbyt złożony (tryb new/existing, katalogi). **Nie używa AppModal**. Walidacja przez `FILE_UPLOAD` (10 MB). | ⚠️ Wzorzec multi-file, ale nie do reuse |
| `FileFieldRenderer` | `src/components/CostEstimate/FileFieldRenderer.tsx` | Multi-file upload PDF/JPG, max **50 MB**/plik, walidacja MIME, podgląd obrazów/PDF. Zbyt domenowy (kosztorys). | ✅ Wzorzec walidacji 50 MB + preview |
| `AICostImportModal` | `src/components/CostTracker/AICostImportModal.tsx` | `AppModal` + `DocumentDropzone` + `useMutation` + toast błędów. Synchroniczny AI parse. | ✅ Wzorzec AppModal + upload (single file) |
| `StatusBadge` | `src/components/ui/StatusBadge.tsx` | Generyczny badge: `pending`, `completed`, … **Brak** `processing`, `failed`. | ⚠️ Wymaga rozszerzenia lub dedykowanego badge |
| `notificationHubService` | `src/services/notificationHubService.ts` | Singleton SignalR: `HubConnectionBuilder`, `accessTokenFactory` (MSAL), `on*Received` listeners, `startConnection`. Path: `/api/hubs/notifications`. | ✅ Wzorzec dla `technicalDocumentationHubService` |
| `chatHubService` | `src/services/chatHubService.ts` | Drugi hub SignalR z tym samym wzorcem singleton. Path: `/api/hubs/chat`. | ✅ Wzorzec hub service |
| `NotificationBell` | `src/components/NotificationBell.tsx` | SignalR listener → `showSuccess/showError` toast + `queryClient.invalidateQueries`. | ✅ Wzorzec toast po zdarzeniu hub |
| `useChat.ts` | `src/hooks/useChat.ts` | Hook z `useEffect` subskrybującym zdarzenia hub + aktualizacja stanu/cache. | ✅ Wzorzec hooka nasłuchującego hub |
| `FILE_UPLOAD` | `src/utils/constants.ts` | `ALLOWED_TYPES: pdf/jpeg`, `MAX_FILE_SIZE: 10 MB`. | ⚠️ Feature wymaga 50 MB — nie używać bezpośrednio |
| `ProjectFiles` | `src/pages/ProjectFiles.tsx` | `isPreviewSupported` + `window.open(sasUrlView)` dla PDF/obrazów. | ✅ Wzorzec preview plików źródłowych |
| `GenerateCostEstimateWithAIModal` | `src/components/GenerateCostEstimateWithAIModal.tsx` | Multi-step modal, `Spinner` + `Progress` podczas przetwarzania AI. | ✅ Wzorzec wskaźnika postępu async |
| `EmptyState` / `LoadingSpinner` / `BackToProjectButton` | `src/components/common/` | Standardowe stany listy modułu projektowego. | ✅ Reuse w nowych stronach |
| **Brak implementacji** | — | Grep `TechnicalDocumentation` / `technical-documentation` w UI → **0 wyników**. Brak typów, API, hooków, stron, hub service. | 🔴 Cały feature do zbudowania |

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|-------------|-----|-----------|------|
| Cały moduł UI dokumentacji technicznej | feature | **Krytyczny** | Brak stron, routingu, API client, typów, hooków — greenfield |
| `PermissionCodes.ProjectTechnicalDocumentation` | stała | **Krytyczny** | Jeden kod MVP nie istnieje w `roleCodes.ts` |
| `ProjectModule.TechnicalDocumentation` + etykieta | typ/enum | **Krytyczny** | Brak w `projectModulePermissions.ts` — picker członków nie pokaże modułu |
| `canViewTechnicalDocumentation` w `useProjectPermissions` | hook | **Krytyczny** | Ukrywanie kafelka i stron bez uprawnienia |
| Kafelek „Dokumentacja techniczna” z count | komponent | **Krytyczny** | Nowy kafelek w `ProjectDetails` + wywołanie endpointu `count` |
| Trasy `/technical-documentation` i `/technical-documentation/:id` | routing | **Krytyczny** | `AppRouter.tsx` + `Breadcrumbs.tsx` |
| `technicalDocumentationApi.ts` | API client | **Krytyczny** | list, get, create (multipart 202), count, retry, download/preview URLs |
| `technicalDocumentation.types.ts` | typy TS | **Krytyczny** | Encja, status enum, `ProjectTechnicalDocumentationDetails` (pełny model JSON) |
| `ProjectTechnicalDocumentationListPage` | strona | **Krytyczny** | Lista z statusami Pending/Processing/Completed/Failed |
| `ProjectTechnicalDocumentationDetailsPage` | strona | **Krytyczny** | Szczegóły, JSON view, pliki, preview, retry przy Failed |
| `AddTechnicalDocumentationModal` | komponent | **Krytyczny** | AppModal: nazwa, opis, multi-file PDF/JPG 50 MB, submit → 202 + optimistic Pending |
| `technicalDocumentationHubService.ts` | serwis SignalR | **Krytyczny** | Hub `/api/hubs/technical-documentation`, event zakończenia przetwarzania |
| `useTechnicalDocumentationHub` | hook | **Krytyczny** | Subskrypcja hub → invalidate list/detail + toast Completed/Failed |
| `useTechnicalDocumentation` (queries/mutations) | hook RQ | **Krytyczny** | list, detail, count, create, retry |
| `TechnicalDocumentationStatusBadge` | komponent | **Wysoki** | Pending (żółty), Processing (niebieski + Spinner), Completed (zielony), Failed (czerwony) |
| `TechnicalDocumentationDetailsView` | komponent | **Wysoki** | Czytelny widok JSON: kondygnacje, pomieszczenia, materiały, instalacje (Accordion) |
| `MultiDocumentDropzone` | komponent UI | **Wysoki** | Multi-file PDF/JPG, max 50 MB/plik — `DocumentDropzone` obsługuje tylko 1 plik |
| Przycisk „Ponów przetwarzanie” przy Failed | komponent/akcja | **Wysoki** | Ręczny retry MVP — brak precedensu w UI |
| Komunikat „Trwa przetwarzanie” na stronie szczegółów | komponent | **Wysoki** | Zamiast JSON gdy status Pending/Processing |
| Breadcrumbs dla nowych tras | komponent | **Normalny** | Segmenty „Dokumentacja techniczna” + nazwa dokumentacji |
| Testy AXE nowych komponentów | testy | **Normalny** | Brak testów — wymagane per skill ui-accessibility |
| Eksport hooków w `hooks/queries/index.ts` | barrel | **Normalny** | Spójność z innymi modułami |
| Mock data (demo mode) | mock | **Normalny** | Opcjonalnie w `mockHandlers.ts` jeśli demo ma działać offline |

---

## BLOK 3 — Typy TypeScript

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|-----|------|-----------------|------------|
| `TechnicalDocumentationStatus` | `src/types/technicalDocumentation.types.ts` | **Nowy** | `Pending = 0, Processing = 1, Completed = 2, Failed = 3` |
| `TechnicalDocumentationFileWeb` | `src/types/technicalDocumentation.types.ts` | **Nowy** | `id, fileName, contentType, fileSize, sasUriPreview?, sasUriDownload?` |
| `TechnicalDocumentationListItemWeb` | `src/types/technicalDocumentation.types.ts` | **Nowy** | `id, name, description?, status, fileCount, createdAt, completedAt?, errorMessage?` |
| `TechnicalDocumentationDetailsWeb` | `src/types/technicalDocumentation.types.ts` | **Nowy** | Pełny rekord: pola listy + `details?: ProjectTechnicalDocumentationDetailsWeb`, `files: TechnicalDocumentationFileWeb[]`, `retryCount?` |
| `ProjectTechnicalDocumentationDetailsWeb` | `src/types/technicalDocumentation.types.ts` | **Nowy** | Mirror C# modelu z feature spec (ProjectInfo, Drawings[], Roof, Installations[], MaterialsSummary[], TotalAreaM2) |
| `CreateTechnicalDocumentationRequest` | `src/types/technicalDocumentation.types.ts` | **Nowy** | `{ name: string; description?: string; files: File[] }` — używany przez modal |
| `TechnicalDocumentationCountWeb` | `src/types/technicalDocumentation.types.ts` | **Nowy** | `{ count: number }` — odpowiedź endpointu count |
| `TechnicalDocumentationProcessingEvent` | `src/types/technicalDocumentation.types.ts` | **Nowy** | Payload SignalR: `{ documentationId, projectId, status, name, errorMessage? }` |
| `PermissionCodes.ProjectTechnicalDocumentation` | `src/constants/roleCodes.ts` | **Modyfikacja** | `"PROJECT.TECHNICAL_DOCUMENTATION"` |
| `ProjectModule.TechnicalDocumentation` | `src/types/projectModulePermissions.ts` | **Modyfikacja** | Nowy enum value (np. `7`) + `PROJECT_MODULE_LABELS` |

### Przykładowa definicja statusu i list item

```typescript
// src/types/technicalDocumentation.types.ts

export const TechnicalDocumentationStatus = {
  Pending: 0,
  Processing: 1,
  Completed: 2,
  Failed: 3,
} as const;

export type TechnicalDocumentationStatus =
  (typeof TechnicalDocumentationStatus)[keyof typeof TechnicalDocumentationStatus];

export interface TechnicalDocumentationListItemWeb {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  status: TechnicalDocumentationStatus;
  fileCount: number;
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
}
```

---

## BLOK 4 — Serwisy API (src/api/)

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint (propozycja) | Opis |
|-------------|------|-----------------|----------------------|------|
| `getCount` | `technicalDocumentationApi.ts` | **Nowa** | `GET /tenants/{t}/projects/{p}/technical-documentation/count` | Liczba dokumentacji na kafelek ProjectDetails |
| `getList` | `technicalDocumentationApi.ts` | **Nowa** | `GET /tenants/{t}/projects/{p}/technical-documentation` | Lista dokumentacji projektu |
| `getById` | `technicalDocumentationApi.ts` | **Nowa** | `GET /tenants/{t}/projects/{p}/technical-documentation/{id}` | Szczegóły + JSON + pliki |
| `create` | `technicalDocumentationApi.ts` | **Nowa** | `POST /tenants/{t}/projects/{p}/technical-documentation` | Multipart: `name`, `description?`, `files[]`. Odpowiedź **202** + `{ id }` |
| `retry` | `technicalDocumentationApi.ts` | **Nowa** | `POST /tenants/{t}/projects/{p}/technical-documentation/{id}/retry` | Ręczny retry przy Failed → status Pending |
| `technicalDocumentationApi` (obiekt) | `src/api/technicalDocumentationApi.ts` | **Nowy plik** | — | Wzorzec jak `costTrackerApi.ts` / `aiCostApi.ts` |

### Implementacja create (multipart, 202)

```typescript
// src/api/technicalDocumentationApi.ts
import { axiosClient } from './axiosClient';
import type {
  TechnicalDocumentationListItemWeb,
  TechnicalDocumentationDetailsWeb,
  TechnicalDocumentationCountWeb,
} from '../types/technicalDocumentation.types';

const MAX_FILE_SIZE = 52_428_800; // 50 MB

export const technicalDocumentationApi = {
  getCount: async (tenantId: string, projectId: string): Promise<TechnicalDocumentationCountWeb> => {
    const res = await axiosClient.get<TechnicalDocumentationCountWeb>(
      `/tenants/${tenantId}/projects/${projectId}/technical-documentation/count`
    );
    return res.data;
  },

  getList: async (tenantId: string, projectId: string): Promise<TechnicalDocumentationListItemWeb[]> => {
    const res = await axiosClient.get<TechnicalDocumentationListItemWeb[]>(
      `/tenants/${tenantId}/projects/${projectId}/technical-documentation`
    );
    return res.data;
  },

  getById: async (
    tenantId: string,
    projectId: string,
    documentationId: string
  ): Promise<TechnicalDocumentationDetailsWeb> => {
    const res = await axiosClient.get<TechnicalDocumentationDetailsWeb>(
      `/tenants/${tenantId}/projects/${projectId}/technical-documentation/${documentationId}`
    );
    return res.data;
  },

  create: async (
    tenantId: string,
    projectId: string,
    data: { name: string; description?: string; files: File[] }
  ): Promise<{ id: string }> => {
    const form = new FormData();
    form.append('name', data.name);
    if (data.description) {
      form.append('description', data.description);
    }
    data.files.forEach((file) => form.append('files', file));

    const res = await axiosClient.post<{ id: string }>(
      `/tenants/${tenantId}/projects/${projectId}/technical-documentation`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return res.data; // HTTP 202 — axios nie traktuje jako błąd
  },

  retry: async (
    tenantId: string,
    projectId: string,
    documentationId: string
  ): Promise<void> => {
    await axiosClient.post(
      `/tenants/${tenantId}/projects/${projectId}/technical-documentation/${documentationId}/retry`
    );
  },
};
```

**Uwaga:** Walidacja 50 MB i MIME (`application/pdf`, `image/jpeg`) po stronie UI przed wysłaniem — wzorzec z `FileFieldRenderer.tsx` (linie 52–126), **nie** z `FILE_UPLOAD` (10 MB).

---

## BLOK 5 — Hooki React Query

| Hook | Plik | Nowy/Modyfikacja | Query/Mutation | Opis |
|------|------|-----------------|---------------|------|
| `technicalDocumentationKeys` | `src/hooks/queries/useTechnicalDocumentation.ts` | **Nowy** | query keys | `all`, `list(t,p)`, `detail(t,p,id)`, `count(t,p)` |
| `useTechnicalDocumentationCount` | `src/hooks/queries/useTechnicalDocumentation.ts` | **Nowy** | `useQuery` | Count na kafelek — `enabled` gdy user ma uprawnienie |
| `useTechnicalDocumentationList` | `src/hooks/queries/useTechnicalDocumentation.ts` | **Nowy** | `useQuery` | Lista dokumentacji |
| `useTechnicalDocumentationDetails` | `src/hooks/queries/useTechnicalDocumentation.ts` | **Nowy** | `useQuery` | Szczegóły pojedynczej dokumentacji |
| `useCreateTechnicalDocumentation` | `src/hooks/queries/useTechnicalDocumentation.ts` | **Nowy** | `useMutation` | Upload → invalidate list + count; optimistic Pending opcjonalnie |
| `useRetryTechnicalDocumentation` | `src/hooks/queries/useTechnicalDocumentation.ts` | **Nowy** | `useMutation` | Ręczny retry → invalidate detail + list |
| `useTechnicalDocumentationHub` | `src/hooks/useTechnicalDocumentationHub.ts` | **Nowy** | `useEffect` + hub | Nasłuch SignalR, toast, invalidate queries |
| `useProjectPermissions` | `src/hooks/useProjectPermissions.ts` | **Modyfikacja** | — | Dodać `canViewTechnicalDocumentation`, `canWriteTechnicalDocumentation` |

### Wzorzec `useTechnicalDocumentationHub`

```typescript
// src/hooks/useTechnicalDocumentationHub.ts
// Wzorzec: NotificationBell (linie 99–115) + useChat.ts

export function useTechnicalDocumentationHub(
  tenantId: string | undefined,
  projectId: string | undefined
): void {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToastNotification();

  useEffect(() => {
    if (!tenantId || !projectId) return;

    technicalDocumentationHubService.startConnection().catch(() => {});

    const unsubscribe = technicalDocumentationHubService.onProcessingCompleted(
      (event: TechnicalDocumentationProcessingEvent) => {
        if (event.projectId !== projectId) return;

        queryClient.invalidateQueries({
          queryKey: technicalDocumentationKeys.list(tenantId, projectId),
        });
        queryClient.invalidateQueries({
          queryKey: technicalDocumentationKeys.detail(tenantId, projectId, event.documentationId),
        });
        queryClient.invalidateQueries({
          queryKey: technicalDocumentationKeys.count(tenantId, projectId),
        });

        if (event.status === TechnicalDocumentationStatus.Completed) {
          showSuccess('Przetwarzanie zakończone', `Dokumentacja „${event.name}” jest gotowa.`);
        } else if (event.status === TechnicalDocumentationStatus.Failed) {
          showError(
            'Przetwarzanie nie powiodło się',
            event.errorMessage ?? `Dokumentacja „${event.name}” — błąd przetwarzania.`
          );
        }
      }
    );

    return unsubscribe;
  }, [tenantId, projectId, queryClient, showSuccess, showError]);
}
```

**Polling fallback:** Gdy użytkownik jest na liście ze statusami Pending/Processing, rozważyć `refetchInterval: 5000` w `useTechnicalDocumentationList` jako degradację gdy hub rozłączony (wzorzec graceful degradation jak Redis w API).

---

## BLOK 6 — Nowe komponenty

| Komponent | Lokalizacja | Opis | Zależy od |
|-----------|------------|------|-----------|
| `ProjectTechnicalDocumentationPage` | `src/pages/ProjectTechnicalDocumentationPage.tsx` | Lista dokumentacji: tabela/karty, status badge, przycisk „Dodaj dokumentację”, hub listener | `useTechnicalDocumentationList`, `useTechnicalDocumentationHub`, permissions |
| `ProjectTechnicalDocumentationDetailsPage` | `src/pages/ProjectTechnicalDocumentationDetailsPage.tsx` | Nagłówek (nazwa, opis, status), pliki z preview, JSON view lub stan processing/failed | `useTechnicalDocumentationDetails`, retry mutation |
| `AddTechnicalDocumentationModal` | `src/components/technicalDocumentation/AddTechnicalDocumentationModal.tsx` | AppModal: FormControl nazwa (required), opis (textarea), MultiDocumentDropzone, submit | `useCreateTechnicalDocumentation`, `AppModal` |
| `TechnicalDocumentationStatusBadge` | `src/components/technicalDocumentation/TechnicalDocumentationStatusBadge.tsx` | Badge + opcjonalny Spinner dla Processing | Chakra `Badge`, `Spinner` |
| `TechnicalDocumentationDetailsView` | `src/components/technicalDocumentation/TechnicalDocumentationDetailsView.tsx` | Accordion: ProjectInfo, Drawings (pomieszczenia, ściany, otwory), Roof, Installations, MaterialsSummary, TotalAreaM2 | `ProjectTechnicalDocumentationDetailsWeb` |
| `TechnicalDocumentationFileList` | `src/components/technicalDocumentation/TechnicalDocumentationFileList.tsx` | Lista plików źródłowych z przyciskiem Podgląd/Pobierz | Wzorzec `ProjectFiles` preview |
| `MultiDocumentDropzone` | `src/components/ui/MultiDocumentDropzone.tsx` | Multi-file drag-drop, PDF/JPG, max 50 MB/plik, lista wybranych plików z usuwaniem | Rozszerzenie `DocumentDropzone` |
| `technicalDocumentationHubService` | `src/services/technicalDocumentationHubService.ts` | Singleton SignalR hub | Wzorzec `chatHubService.ts` |

### Routing docelowy

```
/projects/:projectId/technical-documentation           → lista
/projects/:projectId/technical-documentation/:docId      → szczegóły
```

### Logika widoku szczegółów (status-driven)

```
Pending / Processing:
  → Alert info + Spinner + „Trwa przetwarzanie dokumentacji…”
  → Lista plików źródłowych (bez JSON)

Completed:
  → TechnicalDocumentationDetailsView (JSON)
  → TechnicalDocumentationFileList z preview

Failed:
  → Alert error + errorMessage
  → Przycisk „Ponów przetwarzanie” (retry mutation)
  → Lista plików źródłowych
```

### Kafelek ProjectDetails (z count)

```tsx
{permissions.canViewTechnicalDocumentation && (
  <Box as="button" ... onClick={() => navigate(`/projects/${projectId}/technical-documentation`)}>
    <VStack spacing={3}>
      <Icon as={Blueprint} boxSize={8} color="teal.600" /> {/* np. lucide Blueprint / FileSearch */}
      <Text fontWeight="bold" fontSize="md">Dokumentacja techniczna</Text>
      {count !== undefined && (
        <Badge colorScheme="teal" borderRadius="full">{count}</Badge>
      )}
    </VStack>
  </Box>
)}
```

**Uwaga:** Obecne kafelki w `ProjectDetails` **nie wyświetlają count** — to nowy wzorzec w tym widoku (dotychczas tylko ikona + etykieta). Endpoint `count` jest wymagany osobno (decyzja MVP).

---

## BLOK 7 — Modyfikacje istniejących komponentów

| Komponent | Plik | Typ zmiany | Opis |
|-----------|------|-----------|------|
| `ProjectDetails` | `src/pages/ProjectDetails.tsx` | Modyfikacja `SimpleGrid` (~L659) | Nowy kafelek z count z `useTechnicalDocumentationCount`, warunek `canViewTechnicalDocumentation` |
| `AppRouter` | `src/routes/AppRouter.tsx` | Nowe `<Route>` | 2 trasy pod `ProtectedRoute` |
| `Breadcrumbs` | `src/components/Breadcrumbs.tsx` | Rozszerzenie mapowania (~L70) | `technical-documentation` + opcjonalnie nazwa z query/detail |
| `useProjectPermissions` | `src/hooks/useProjectPermissions.ts` | Nowe flagi | `canViewTechnicalDocumentation`, `canWriteTechnicalDocumentation` via `PROJECT.TECHNICAL_DOCUMENTATION` |
| `roleCodes.ts` | `src/constants/roleCodes.ts` | Nowy kod | `ProjectTechnicalDocumentation: "PROJECT.TECHNICAL_DOCUMENTATION"` |
| `projectModulePermissions.ts` | `src/types/projectModulePermissions.ts` | Nowy moduł | Enum value + label „Dokumentacja techniczna” |
| `hooks/queries/index.ts` | `src/hooks/queries/index.ts` | Eksport | Export nowych hooków i keys |
| `AuthContext` / `main.tsx` | opcjonalnie | Hub lifecycle | Rozważyć `startConnection` hub przy wejściu w projekt (jak notifications w AuthContext) — lub lazy start w hooku strony |

### `useProjectPermissions` — proponowane flagi

```typescript
// Jeden kod MVP — view i write z tego samego kodu (lub isAdmin)
canViewTechnicalDocumentation:
  canViewAllResources ||
  hasPermission(permissions, PermissionCodes.ProjectTechnicalDocumentation),

canWriteTechnicalDocumentation:
  canViewAllResources ||
  hasPermission(permissions, PermissionCodes.ProjectTechnicalDocumentation),
```

Przycisk „Dodaj dokumentację” widoczny gdy `canWriteTechnicalDocumentation` (zgodnie ze spec: write = dodawanie).

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---------|------------------------|--------------------------------|
| Kafelek modułu na ProjectDetails | `Box as="button"`, ikona lucide, `_hover` transform | ✅ Identyczny styl; **nowy element:** Badge z count |
| Strona listy modułu | `ProjectSchedules`, `ProjectCosts`: `MainLayout`, `BackToProjectButton`, `EmptyState` | ✅ Ten sam układ |
| Klikalny wiersz tabeli | `cursor="pointer"` + `_hover` na `<Tr>`, nawigacja do szczegółów | ✅ Lista → szczegóły po kliknięciu |
| Modal tworzenia | `AppModal` (nie raw Chakra Modal) | ✅ `AddTechnicalDocumentationModal` → AppModal |
| Upload plików | `FileFieldRenderer`: PDF/JPG 50 MB; `DocumentDropzone`: single 20 MB | ✅ 50 MB, multi-file — nowy `MultiDocumentDropzone` |
| Statusy async | `GenerateCostEstimateWithAIModal`: Spinner/Progress | ✅ Processing = Spinner obok badge |
| Preview plików | `ProjectFiles`: `window.open(sasUrlView)` | ✅ Ten sam mechanizm dla plików źródłowych |
| SignalR + toast | `NotificationBell`: hub event → toast + invalidate | ✅ Identyczny flow dla TechnicalDocumentationHub |
| Hub service singleton | `notificationHubService`, `chatHubService` | ✅ `technicalDocumentationHubService` — ten sam wzorzec MSAL token |
| Uprawnienia modułowe | `useProjectPermissions` + `PermissionCodes` | ✅ Jeden kod `PROJECT.TECHNICAL_DOCUMENTATION` |
| Zakładki Mine/All/Shared | `useResourcePermissions` w plikach/kosztorysach | ❌ **Nie stosować** — dokumentacja techniczna nie ma scope'ów w MVP |
| Formatowanie dat | `formatDate` z `utils/formatters` | ✅ `createdAt`, `completedAt` |
| Obsługa błędów API | `handleApiError` + `showApiError` | ✅ W modalu i mutacjach |
| i18n | Brak — stringi po polsku inline | ✅ Wszystkie etykiety po polsku |
| Nazewnictwo plików | `src/pages/Project*.tsx`, `src/components/{domain}/` | ✅ `ProjectTechnicalDocumentationPage`, folder `technicalDocumentation/` |

---

## BLOK 9 — Dostępność (WCAG AA / AXE) — OBOWIĄZKOWY

### Kontrast kolorów

| Element | Kolor tekstu | Kolor tła | Kontrast (szac.) | Status |
|---------|-------------|-----------|-----------------|--------|
| Etykieta kafelka | `fontWeight="bold"` domyślny | white/cardBg | ~11:1 | ✓ |
| Count badge na kafelku | `teal.800` (propozycja) | `teal.100` | ~7:1 | ✓ |
| Status Pending | `yellow.800` | `yellow.100` | ~7:1 | ✓ |
| Status Processing | `blue.800` | `blue.100` | ~8:1 | ✓ |
| Status Completed | `green.800` | `green.100` | ~7:1 | ✓ |
| Status Failed | `red.800` | `red.100` | ~7:1 | ✓ |
| Opis pomocniczy na liście | `neutral.600` | white | ~5.7:1 | ✓ |
| Placeholder MultiDocumentDropzone | `gray.400` | `gray.50` | ~4.5:1 | ⚠ sprawdź przy implementacji |
| `errorMessage` przy Failed | `red.600` | white | ~5.9:1 | ✓ |
| Tekst w Accordion JSON | `neutral.700` | white | ~11:1 | ✓ |

**Flagi:** Unikać `color="neutral.500"` lub `gray.400` dla treści głównej (nie placeholder). `ProjectModulePermissionPicker` używa `neutral.500` dla nagłówka sekcji — przy dodaniu modułu zachować istniejący wzorzec.

### Atrybuty ARIA

| Komponent | Problem | Rekomendacja |
|-----------|---------|-------------|
| Kafelek `Box as="button"` | Brak jawnego `aria-label` gdy jest tylko ikona+tekst | Tekst „Dokumentacja techniczna” wystarczy; dodać `aria-label` z count: `Dokumentacja techniczna, ${count} pozycji` |
| `MultiDocumentDropzone` | Nowy komponent — ryzyko `div onClick` | Użyć `<label htmlFor>` jak `DocumentDropzone` ✓; `aria-label` na strefie drop |
| Ukryty `<input type="file" multiple>` | Brak opisu | `aria-label="Wybierz pliki PDF lub JPG"` |
| Spinner Processing na liście | Brak live region | `role="status"` + `aria-live="polite"` na kontenerze Spinner+tekst |
| Przycisk „Ponów przetwarzanie” | Musi być opisowy | Tekst widoczny + `aria-describedby` wskazujący na `errorMessage` |
| Ikony lucide w przyciskach | Duplikacja SR | `aria-hidden="true"` na ikonach obok tekstu |
| Accordion szczegółów JSON | Sekcje bez etykiet | Chakra AccordionButton ma domyślnie tekst — upewnić się że nagłówki są opisowe (np. „Kondygnacja: Parter”) |
| Toast po SignalR | Ogłoszenie zmiany statusu | Chakra toast ma `role="alert"` — ✓ |

### Zarządzanie fokusem

- `AddTechnicalDocumentationModal` przez `AppModal` → Chakra Modal focus trap ✓
- Po zamknięciu modala po sukcesie: focus wraca do przycisku „Dodaj dokumentację” (domyślne zachowanie Chakra)
- `MultiDocumentDropzone`: osiągalność klawiaturą przez natywny `<label>` + `<input>` — ✓ (wzorzec DocumentDropzone)
- Nawigacja lista → szczegóły: standardowy routing, focus na `<main>` / pierwszy heading — rozważyć `useEffect` scroll to top (jak inne strony projektu)

### Testy AXE

| Komponent | Status | Plik testu (propozycja) |
|-----------|--------|-------------------------|
| `AddTechnicalDocumentationModal` | ✗ brak | `src/components/technicalDocumentation/__tests__/AddTechnicalDocumentationModal.axe.test.tsx` |
| `TechnicalDocumentationStatusBadge` | ✗ brak | `src/components/technicalDocumentation/__tests__/TechnicalDocumentationStatusBadge.axe.test.tsx` |
| `MultiDocumentDropzone` | ✗ brak | `src/components/ui/__tests__/MultiDocumentDropzone.axe.test.tsx` |
| `TechnicalDocumentationDetailsView` | ✗ brak | `src/components/technicalDocumentation/__tests__/TechnicalDocumentationDetailsView.axe.test.tsx` |
| `ProjectTechnicalDocumentationPage` | ✗ brak | `src/pages/__tests__/ProjectTechnicalDocumentationPage.axe.test.tsx` |

Wzorzec testów: `src/components/ui/__tests__/SharedComponents.axe.test.tsx` + `renderWithChakra` + `toHaveNoViolations`.

### Podsumowanie dostępności

| Kategoria | Status | Uwagi |
|----------|--------|-------|
| Kontrast kolorów | ⚠ | Placeholder w dropzone do weryfikacji; reszta — tokeny Chakra OK |
| Atrybuty ARIA | ⚠ | Nowe komponenty wymagają świadomego ARIA (dropzone, live region processing) |
| Klawiatura / fokus | ✓ | AppModal + label/input pattern |
| Testy AXE | ✗ | 5 nowych testów do dodania |

---

## BLOK 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---------|---------------|--------|-------------|
| 1 | **Cały feature nie istnieje w UI** | — | Krytyczne | Implementacja greenfield według tego audytu; zależność od gotowości API + hub |
| 2 | Jeden kod uprawnień vs read/write w spec | `useProjectPermissions` | Wysokie | MVP: ten sam kod dla view i write; jeśli backend rozdzieli — osobne flagi później |
| 3 | `DocumentDropzone` single-file, 20 MB default | `DocumentDropzone.tsx` | Wysokie | Nowy `MultiDocumentDropzone` z `maxSizeMB={50}`, `accept=".pdf,.jpg,.jpeg"` — nie modyfikować istniejącego bez potrzeby (AICostImport używa 20 MB) |
| 4 | `FILE_UPLOAD.MAX_FILE_SIZE` = 10 MB | `constants.ts` | Średnie | Nie używać dla tego feature; lokalna stała 50 MB w komponencie/API client |
| 5 | Brak komponentu JSON view w repo | nowy komponent | Wysokie | Zbudować `TechnicalDocumentationDetailsView` z Accordion — nie raw `JSON.stringify` jako jedyny widok |
| 6 | Długie przetwarzanie async (kolejka) | lista + szczegóły | Wysokie | Hub primary + opcjonalny `refetchInterval` gdy są Pending/Processing; unikać blokowania UI |
| 7 | Hub connection lifecycle | `technicalDocumentationHubService` | Średnie | Lazy start w hooku strony projektu; `stopConnection` przy unmount opcjonalnie; wzorzec notifications (singleton global) |
| 8 | Toast flood przy wielu dokumentacjach | `useTechnicalDocumentationHub` | Średnie | Toast tylko gdy event dotyczy bieżącego `projectId`; opcjonalnie tylko gdy user na stronie dokumentacji |
| 9 | Count na kafelku — nowy wzorzec | `ProjectDetails` | Niski | Osobny lightweight query; nie ładować pełnej listy dla count |
| 10 | Retry UI bez precedensu | szczegóły Failed | Średnie | Przycisk + `DeleteAlertDialog` lub bezpośredni retry z potwierdzeniem — ustalić UX |
| 11 | `ProjectModule` enum value | backend sync | Średnie | Uzgodnić numeric ID z `ProjectModule.cs` (propozycja: `7`) |
| 12 | Demo mode / mock | `mockHandlers.ts` | Niski | Bez mocków feature niedostępny w demo — dodać jeśli wymagane |
| 13 | Brak RAG w MVP | — | Niski | Nie implementować wyszukiwania semantycznego; UI tylko lista + JSON view |
| 14 | UploadFilesModal nie nadaje się do reuse | — | Niski | Osobny flow zgodnie z decyzją MVP (osobna encja, nie ProjectFile) |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe komponenty | 8 (`ProjectTechnicalDocumentationPage`, `ProjectTechnicalDocumentationDetailsPage`, `AddTechnicalDocumentationModal`, `TechnicalDocumentationStatusBadge`, `TechnicalDocumentationDetailsView`, `TechnicalDocumentationFileList`, `MultiDocumentDropzone`, `technicalDocumentationHubService`) |
| Zmodyfikowane komponenty | 6 (`ProjectDetails`, `AppRouter`, `Breadcrumbs`, `useProjectPermissions`, `roleCodes`, `projectModulePermissions` + barrel `hooks/queries/index.ts`) |
| Nowe hooki | 2 pliki (`useTechnicalDocumentation.ts` z 5 hookami RQ, `useTechnicalDocumentationHub.ts`) |
| Nowe typy TypeScript | ~10 interfejsów/enum w `technicalDocumentation.types.ts` + 2 modyfikacje enum |
| Nowe wywołania API | 5 (`getCount`, `getList`, `getById`, `create`, `retry`) w `technicalDocumentationApi.ts` |
| Naruszenia WCAG AA (do naprawy przy implementacji) | 4 (placeholder kontrast, ARIA live region, ARIA dropzone, brak testów AXE) |
| Pytania domenowe | 6 |

### Priorytetyzacja luk

| Priorytet | Liczba luk | Kluczowe elementy |
|-----------|------------|-------------------|
| **Krytyczne** | 14 | API, typy, routing, strony, modal upload, hub SignalR, uprawnienia, kafelek z count |
| **Wysokie** | 6 | Status badge, JSON view, multi-dropzone, retry UI, komunikat processing, hub hook |
| **Normalne** | 4 | Breadcrumbs, testy AXE, barrel exports, mock demo |

### Pytania domenowe wymagające decyzji

1. **Read vs write przy jednym kodzie `PROJECT.TECHNICAL_DOCUMENTATION`:** Czy posiadanie kodu daje jednocześnie odczyt i zapis (dodawanie), czy backend w przyszłości rozdzieli na dwa kody? MVP UI zakłada **jedna flaga = pełny dostęp do modułu** (poza `isAdmin` / `canViewAllResources`).

2. **Nazwa trasy URL:** `technical-documentation` vs `technical-docs` vs `documentation`? Rekomendacja: `/technical-documentation` (zgodna z nazwą encji API).

3. **Toast SignalR — zakres:** Czy toast po Completed/Failed ma się pokazywać globalnie (jak NotificationBell), czy tylko gdy user jest na stronie listy/szczegółów dokumentacji danego projektu? Rekomendacja: filtrować po `projectId`; toast zawsze gdy user w kontekście tego projektu.

4. **Retry — UX potwierdzenia:** Czy ręczny retry wymaga `AlertDialog` („Czy ponowić przetwarzanie?”), czy bezpośredni przycisk? Auto-retry max 3 jest po stronie backendu — UI pokazuje `retryCount`?

5. **Widok JSON — poziom szczegółowości MVP:** Czy wystarczy Accordion z sekcjami (ProjectInfo, Drawings, Roof, …), czy wymagany też tryb „surowy JSON” (collapsible `<pre>`) dla debug/support?

6. **Ikona kafelka:** `FileText` jest już używana przez Pliki i Kosztorysy. Rekomendacja: `Blueprint`, `ScanLine` lub `FileSearch` z lucide-react dla odróżnienia wizualnego.

---

*Audyt przeprowadzony bez modyfikacji kodu produkcyjnego. Stan UI: feature w 0% — wymaga pełnej implementacji frontendowej po gotowości warstwy API i `TechnicalDocumentationHub`.*
