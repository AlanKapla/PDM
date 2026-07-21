# Feature: Statystyki aktywności użytkowników (MVP)

## Opis
Zbieranie i przechowywanie zdarzeń aktywności użytkowników: logowanie (B2C/MSAL)
oraz wejście w tryb demo. Dane: IP (z serwera), timestamp, route/endpoint.
SuperAdmin ma osobną sekcję w module admina do podglądu logów.

## Zakres
- DB: encja `UserActivityLog` + migracja EF
- API: `POST /api/activity/login`, `POST /api/activity/demo`, `GET /api/admin/activity-logs`
- UI: fire-and-forget po sukcesie logowania oraz w `enterDemoMode`
- UI: osobna sekcja/strona w `/admin` (jak cold-mail / users) — tabela logów

## Decyzje domenowe (zatwierdzone)
1. **Zapis do DB** — tak
2. **Panel SuperAdmin** — tak, osobna sekcja w adminie
3. **Dwa dedykowane POST** — nie generyczny `/api/activity`
4. **IP** — zawsze z serwera (`RemoteIpAddress` / `X-Forwarded-For`), nie z body
5. **Body** — opcjonalnie `route` (string, może być pusty)
6. **UI** — fire-and-forget; błąd nie blokuje logowania ani demo
7. **Demo POST** — `AllowAnonymous` (sesja demo bez JWT)
8. **Login POST** — wymaga JWT (użytkownik po B2C); UserId / AzureAdB2CObjectId z claims jeśli dostępne
9. **EventType** — enum: `Login` | `DemoEnter`

## Wymagania funkcjonalne
1. Po udanym logowaniu B2C/MSAL UI wywołuje `POST /api/activity/login` (nie blokuje UX)
2. Przy `enterDemoMode` UI wywołuje `POST /api/activity/demo` (nie blokuje UX)
3. Każde zdarzenie zapisuje: EventType, IpAddress, OccurredAtUtc, Route, opcjonalnie UserId / AzureAdB2CObjectId
4. SuperAdmin widzi listę logów w osobnej sekcji admina
5. Non-SuperAdmin nie ma dostępu do GET (403 / redirect jak reszta admina)

## Szkic API
| Method | Route | Auth | Cel |
|--------|-------|------|-----|
| `POST` | `/api/activity/login` | JWT Bearer | Zapis zdarzenia Login |
| `POST` | `/api/activity/demo` | AllowAnonymous | Zapis zdarzenia DemoEnter |
| `GET` | `/api/admin/activity-logs` | SuperAdminOnly | Lista logów (paginacja/filtr opcjonalnie w MVP) |

### Body POST (opcjonalne)
```json
{ "route": "/home" }
```

### Response GET (szkic)
```json
[
  {
    "id": "...",
    "eventType": "Login",
    "ipAddress": "1.2.3.4",
    "occurredAtUtc": "2026-07-21T12:00:00Z",
    "route": "/auth/callback",
    "userId": "...",
    "azureAdB2CObjectId": "..."
  }
]
```

## Szkic UI
- Po sukcesie sesji MSAL (AuthCallback / AuthContext): `recordLoginActivity(route)` — void, catch ignore
- `DemoContext.enterDemoMode`: `recordDemoActivity(route)` — void, catch ignore
- Admin hub: karta → `/admin/activity-logs` — tabela (timestamp, typ, IP, route, user)

## Kryteria akceptacji
- [x] Encja + migracja w DB
- [x] `POST /api/activity/login` zapisuje Login z IP z serwera
- [x] `POST /api/activity/demo` AllowAnonymous zapisuje DemoEnter z IP z serwera
- [x] `GET /api/admin/activity-logs` tylko SuperAdmin
- [x] UI wywołuje login i demo fire-and-forget
- [x] UI: osobna sekcja admina z listą logów
- [x] Build API i UI bez błędów
- [x] Podstawowe testy handlerów / walidatorów

## Poza zakresem (MVP)
- Middleware logujący wszystkie requesty
- Retencja / anonimizacja RODO
- Export CSV / dashboardy / analytics
- Rate limiting na POST activity
- UserAgent / geoIP
