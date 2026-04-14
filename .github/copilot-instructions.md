# 🧭 Kontekst projektu

Nowoczesna aplikacja webowa:

| Warstwa | Technologia |
|--------|-------------|
| Backend | .NET Web API (CQRS + MediatR) |
| Frontend | React 18 SPA + TypeScript + Vite |
| UI Library | Chakra UI 2 |
| Autentykacja | Azure AD B2C / Microsoft Entra External ID (MSAL) |
| Real-time | SignalR (chat, powiadomienia) |
| HTTP | axios z interceptorami Bearer token |
| Routing | React Router DOM 7 |
| i18n | i18next + react-i18next |

Cele projektu: **czytelny, prosty i łatwy w utrzymaniu kod**  
Priorytety: **architektura • bezpieczeństwo • testowalność • spójność**

**Zasady komentarzy Copilota:** pisz **po polsku**, podawaj **powód**, proponuj **gotowe poprawki (`suggested change`)**.

---

# 🔍 Ogólne praktyki programistyczne

### ✔ Czytelność ponad "spryt"
- preferuj prosty, jednoznaczny kod
- zgłaszaj nadmiernie złożone warunki i długie metody

### ✔ Nazewnictwo
- nazwy mają wskazywać **co to jest** i **po co istnieje**
- unikaj skrótów typu `svc`, `mgr`, `obj`

### ✔ DRY
- reaguj na duplikację logiki
- nie proponuj nadmiernej abstrakcji przy małej skali powtórzeń

### ✔ Obsługa błędów
- zgłaszaj `catch` bez logowania
- unikaj zjadania wyjątków

### ✔ Komentarze
- dobre komentarze tłumaczą **dlaczego**, a nie **co**
- jeśli komentarz wyjaśnia **co robi kod**, preferuj uproszczenie kodu

---

# 🏗 Architektura API (backend)

### Warstwy
`Controller → Command/Query → Serwisy → Repozytoria`  
⚠ zgłaszaj logikę biznesową w kontrolerach i w repozytoriach

### Kontrakty API
- RESTful nazewnictwo endpointów
- spójne statusy HTTP (200/201/204/400/401/403/404/422/500)  
⚠ zwracanie `200` z tekstem błędu — do poprawy

### Modele / DTO
- API zwraca **web modele**, nie encje bazy  
⚠ zgłaszaj eksport encji bazodanowych do JSON

### Walidacja i async
- walidacja na wejściu do API
- operacje IO powinny używać `async/await`  
⚠ zgłaszaj `.Result`, `.Wait()`, blokujące wywołania

---

# 💻 Architektura frontendu (React)

## Struktura katalogów `src/`

```
api/          - funkcje HTTP (jeden plik = jeden zasób API)
components/   - komponenty UI (chat/, common/, CostEstimate/, CostTracker/)
config/       - konfiguracja (authConfig.ts — MSAL)
constants/    - stałe (roleCodes.ts — kody ról i uprawnień)
context/      - globalny stan React Context (auth, chat unread, projekt cache)
hooks/        - własne hooki (logika biznesowa i dostęp do danych)
i18n/         - konfiguracja i18next + pliki tłumaczeń
layout/       - MainLayout.tsx (Header + Sidebar + Breadcrumbs)
lib/          - inicjalizacja bibliotek zewnętrznych
pages/        - strony (jedna strona = jeden URL)
routes/       - AppRouter, ProtectedRoute, PublicRoute
services/     - integracje zewnętrzne (SignalR hubs)
theme/        - Chakra UI theme
types/        - interfejsy TypeScript i enumy
utils/        - helper functions (formatters, obliczenia, obsługa błędów)
```

## Warstwa API (`src/api/`)

- **`axiosClient.ts`** — centralna instancja axios: `baseURL`, interceptor Bearer token (MSAL `acquireTokenSilent`), retry na 401 z `forceRefresh`, fallback `loginRedirect` przy `InteractionRequiredAuthError`
- **`[zasób]Api.ts`** — jeden plik na zasób (np. `projectApi.ts`, `costEstimateApi.ts`)
- Pliki API eksportują obiekty z asynchronicznymi metodami, nie klasy
- Wszystkie wywołania HTTP przechodzą przez `axiosClient`, nigdy przez surowy `fetch` ani osobną instancję axios

⚠ `apiClient.ts` i `authService.ts` w `services/` są zdeprecjonowane — nie używaj  
⚠ nie twórz wywołań axios poza `src/api/`

## Warstwa hooków (`src/hooks/`)

Wzorzec `useXxx` — każdy hook ma jedno zadanie:

| Hook | Odpowiedzialność |
|------|-----------------|
| `useFetch` | generyczny: `{ data, loading, error, execute, reset }` |
| `useGlobalCache` | cache z TTL 5 min, współdzielony między komponentami, guard przed race condition |
| `useProjectCache` | cache szczegółów projektu |
| `useForm` | stan formularza: `values`, `errors`, `touched`, `validate()` |
| `useFieldAutosave` | auto-save pól |
| `useAuth` | dostęp do MSAL (nie do `AuthContext`) |
| `useProjectPermissions` | uprawnienia użytkownika do projektu |
| `useResourcePermissions` | uprawnienia do zasobu |
| `useTenantPermissions` | uprawnienia do tenanta |
| `useToastNotification` | `showSuccess(msg)`, `showError(msg)` |
| `useModal` / `useModalItemEdit` | wrapper `useDisclosure` Chakry |
| `useChat` (`useChatList`, `useChatMessages`) | lista czatów, wiadomości, SignalR events |
| `useCalculations` | obliczenia finansowe |
| `useTimelineData` | dane osi czasu |

⚠ logikę pobierania danych umieszczaj w hooku, nie bezpośrednio w stronie  
⚠ zbyt duży `useEffect` — rozbij na osobne hooki  
⚠ nie duplikuj logiki cache — używaj `useGlobalCache`

## Warstwa kontekstu (`src/context/`)

Tylko 3 konteksty — **nie dodawaj nowych bez wyraźnej potrzeby**:

| Context | Odpowiedzialność |
|---------|-----------------|
| `AuthContext` | `isAuthenticated`, `user: UserProfile`, `loading`, `login()`, `logout()`, `refreshUser()` — inicjalizuje też SignalR |
| `ChatUnreadContext` | licznik nieprzeczytanych wiadomości, `markChatAsRead(chatId)` |
| `ProjectCacheContext` | cache szczegółów projektów między stronami |

⚠ nie przechowuj w kontekście danych, które należą tylko do jednego widoku  
⚠ do danych użytkownika używaj `useContext(AuthContext)`, nie `useAuth` (MSAL)

## Warstwa serwisów (`src/services/`)

Tylko SignalR hubs — nie logika biznesowa:

- **`chatHubService.ts`** — singleton, `HubConnectionBuilder` + MSAL token factory, events chatu, reconnect `[0, 2s, 5s, 10s, 30s]`
- **`notificationHubService.ts`** — singleton, hub powiadomień

⚠ `authService.ts`, `userService.ts`, `apiClient.ts` — zdeprecjonowane, nie używaj

## Warstwa typów (`src/types/`)

Jeden plik na domenę: `auth.types.ts`, `project.types.ts`, `chat.types.ts`, `costEstimate.types.new.ts`, itd.

- `costEstimate.types.ts` — **zdeprecjonowany**, używaj `costEstimate.types.new.ts`
- Enums z helperami (np. `TenantRole`, `ProjectRole`, `ResourceScope`) definiuj w plikach typów
- Nazewnictwo sufiks `Web` dla DTO z backendu (np. `ProjectDetailsWeb`, `CostEstimateGroupWeb`)

⚠ nie używaj `any` — zawsze jawny typ lub `unknown` z type guard  
⚠ nie używaj `as unknown as T` bez komentarza wyjaśniającego powód

## Warstwa utils (`src/utils/`)

- **`handleApiError.ts`** — centralna obsługa błędów axios → `{ title, description }`, mapuje `ApiExceptionReason` ze słownika `apiExceptionReasonMessages`
- **`formatters.ts`** — daty, pieniądze, inne formatowanie
- **`fieldValidation.ts`** — walidacja pól formularza
- **`calculationEngine.ts`**, **`costEstimateCalculations.ts`** — logika obliczeniowa (nie w komponentach)
- **`recalculateCostEstimateDetails.ts`** — przeliczanie całego kosztorysu

⚠ logikę obliczeniową trzymaj w `utils/`, nie w komponentach ani stronach

## Strony (`src/pages/`)

- Każda strona odpowiada jednemu URL z `AppRouter.tsx`
- Strona orkiestruje hooki i komponenty — sama nie powinna zawierać logiki biznesowej
- Routing: `ProtectedRoute` (wymaga auth + `TenantAccessGuard`), `PublicRoute` (przekierowuje zalogowanych)

⚠ zbyt duże strony (np. `ProjectDetails.tsx`) — przy modyfikacji wydzielaj logikę do hooków

## Komponenty (`src/components/`)

- Małe, jednozadaniowe
- `common/` — współdzielone (LoadingSpinner, ConfirmDialog)
- Feature-based: `chat/`, `CostEstimate/`, `CostTracker/`
- Props typowane jawnie jako interface, nie inline

⚠ komponent robiący „wszystko naraz" — do podziału  
⚠ nie wywołuj API bezpośrednio w komponencie — użyj hooka

## Autentykacja i autoryzacja

- **MSAL** (`@azure/msal-browser`, `@azure/msal-react`) — Azure AD B2C / Entra External ID
- Token pobierany przez `acquireTokenSilent` w interceptorze `axiosClient.ts`
- Uprawnienia sprawdzaj przez hooki: `useProjectPermissions`, `useResourcePermissions`, `useTenantPermissions`
- Kody ról i uprawnień w `src/constants/roleCodes.ts`

⚠ nie hardkoduj sprawdzania uprawnień — używaj hooków uprawnień  
⚠ nie loguj tokenów ani danych wrażliwych

---

# 🔗 Komunikacja API ↔ Frontend

- Typy z backendu mają sufiks `Web` (np. `ProjectDetailsWeb` ↔ `ProjectDetailsWebModel` w API)
- Zawsze obsługuj: `loading`, `error`, `null/undefined`
- Błędy API normalizuj przez `handleApiError()` przed wyświetleniem użytkownikowi
- Toast powiadomienia przez `useToastNotification()`, nigdy bezpośrednio przez `useToast`

⚠ zakładanie, że dane zawsze istnieją (`data!`) jest błędem  
⚠ raw `axios.get/post` poza `src/api/` — niezgodne z architekturą

---

# 🔐 Bezpieczeństwo

| Obszar | Sprawdzaj |
|--------|----------|
| Wejście | walidacja danych, unikanie injection |
| Autoryzacja | sprawdzaj uprawnienia przez hooki przed renderowaniem akcji destructywnych |
| Dane wrażliwe | nie logować tokenów MSAL, nie eksponować `userPermissions` w URL |
| MSAL | używaj tylko `acquireTokenSilent` — nigdy nie przechowuj tokenów w `localStorage` ręcznie |

---

# ⚙ CQRS — Commands • Queries • Web modele

### Commands → **zmieniają stan**
- powinny zwracać prosty wynik (`Id` / `Success`)
- mogą odczytywać dane jedynie w celu **walidacji lub logiki biznesowej**
⚠ rozbudowane projekcje danych pod UI w Command → przenieść do Query

### Queries → **odczyt danych**
- wyłącznie **read-only**
- zwracają **web modele dopasowane do potrzeb ekranu/API**
⚠ jakakolwiek zmiana stanu w Query = błąd

### Web modele (DTO / ViewModel)
- reprezentują **kontrakt API**
- mogą różnić się od modeli domenowych
- sufiks `Web` w nazwie (np. `ProjectDetailsWeb`, `CostEstimateGroupWeb`)
⚠ web model nie powinien zawierać pól technicznych ani poufnych

### Serwisy / helpery
- serwisy = logika domenowa / integracje
- helpery = prostsze operacje techniczne  
⚠ „god-service" z wieloma odpowiedzialnościami → do podziału

---

# 🔄 Poprawny przepływ żądania
