# Feature: Cold mail do potencjalnych klientów (Admin)

## Opis
W module administratora (SuperAdmin) możliwość wysyłania cold maili do
potencjalnych klientów: lista adresów e-mail, custom subject + body,
historia wysyłek w DB z filtrem po adresie odbiorcy.

## Zakres
- UI: panel/strona w module admina (`/admin` → wejście do cold mail)
- UI: formularz — textarea (maile, 1/linia), subject, body (plain text, bez WYSIWYG)
- UI: lista historii wysyłek + filtr po e-mailu odbiorcy
- API: endpointy pod `api/admin/cold-mails/...` z policy `SuperAdminOnly`
- DB: encja historii (1 wiersz = 1 odbiorca na wysyłkę)
- Wysyłka: reuse `IEmailSender` + szablon HTML `cold-mail.html` (jedno źródło prawdy)
- Stopka szablonu: WWW / Instagram / Facebook / **Telefon** (ikona `tel:+48798517893` → +48 798 517 893)
- Podgląd UI: `GET /api/admin/cold-mails/template` raz (cache) + lokalne wypełnienie placeholderów (bez API przy każdym keystroke)

## Decyzje domenowe (zatwierdzone)
1. Dostęp: **tylko SuperAdmin** (jak reszta `/api/admin`)
2. Historia: **w DB**, 1 wiersz = 1 odbiorca na wysyłkę
3. Body: plain text / prosty HTML przez zwykły textarea — **bez WYSIWYG**
4. Rate limit: **brak w v1** — walidacja e-maili + max liczba adresów per request

## Wymagania funkcjonalne
1. SuperAdmin może wysłać cold mail do listy adresów (wklejonych linia po linii)
2. Użytkownik podaje subject i body (dowolna treść)
3. Każdy wysłany mail do odbiorcy pojawia się w historii
4. Historia pokazuje adres odbiorcy (oraz sensowne meta: data, subject, status jeśli dostępne)
5. Lista historii ma filtr po adresie e-mail
6. Non-SuperAdmin nie ma dostępu (403 / redirect jak w module admina)

## Szkic API
| Method | Route | Cel |
|--------|-------|-----|
| `POST` | `/api/admin/cold-mails/send` | `{ emails, subject, body }` → wysyłka + zapis historii |
| `GET` | `/api/admin/cold-mails` | `?email=` → historia (opcjonalny filtr) |

## Szkic UI
- Wejście: karta na `/admin` → `/admin/cold-mails` (lub równoważny panel)
- Formularz wysyłki + tabela historii z filtrem

## Kryteria akceptacji
- [x] `POST /api/admin/cold-mails/send` wysyła maile i zapisuje historię (1 wiersz / odbiorca)
- [x] `GET /api/admin/cold-mails` zwraca historię; filtr `email` działa
- [x] Walidacja: poprawne e-maile + limit max adresów per request (50)
- [x] Non-SuperAdmin dostaje 403 (policy SuperAdminOnly + handler check)
- [x] UI: formularz (textarea maile + subject + body) + historia z filtrem
- [x] Build API i UI bez błędów
- [x] Testy handlerów / walidatorów (podstawowe) + AXE UI

## Poza zakresem (v1)
- Rate limiting / throttling
- Edytor WYSIWYG
- Szablony zapisane w DB / wielokrotne użycie szablonów
- Tracking otwarć / kliknięć
- Wysyłka do użytkowników z tabeli User (to jest welcome email)
