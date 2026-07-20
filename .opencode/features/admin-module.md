# Feature: Moduł administratora (Super Admin)

## Opis
Dedykowany moduł UI i osobna schemata API (`/api/admin`) dla operacji
systemowych dostępnych wyłącznie dla użytkowników z rolą SYSTEM.SUPERADMIN.

## Zakres
- UI: strona `/admin` (tryb demo + wejście do użytkowników)
- UI: podstrona `/admin/users` — lista użytkowników, szczegóły, wysyłka maili
- API: `GET /api/admin/users`, `POST /api/admin/users/{id}/welcome-email`, `POST /api/admin/welcome-emails/send`
- Nawigacja: pozycja „Panel administratora” w dropdownie avatara (SuperAdmin only)
- Policy ASP.NET: `SuperAdminOnly` na kontrolerze admina

## Wymagania funkcjonalne
1. SuperAdmin widzi wejście do modułu admina w menu avatara
2. Strona `/admin` zawiera panel trybu demo
3. Strona `/admin/users` zawiera tabelę użytkowników (status maila powitalnego + data)
4. Wiersz tabeli jest klikalny i otwiera podgląd szczegółów
5. Możliwość wysyłki maila do jednego użytkownika oraz bulk „Wyślij maile powitalne”
6. Zwykli użytkownicy nie widzą modułu i dostają redirect z `/admin`
7. Endpointy admina pod prefixem `api/admin`

## Kryteria akceptacji
- [x] `GET /api/admin/users` zwraca listę użytkowników dla SuperAdmin
- [x] `POST /api/admin/users/{id}/welcome-email` wysyła mail do jednego usera
- [x] `POST /api/admin/welcome-emails/send` działa dla SuperAdmin
- [x] Non-SuperAdmin dostaje 403 (policy + handler check)
- [x] Stary endpoint `/api/user/send-welcome-emails` usunięty
- [x] Demo mode działa z panelu admina
- [ ] Build API i UI bez błędów
- [ ] Testy handlera przechodzą
