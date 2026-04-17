# PDM — Instrukcje dla GitHub Copilot

## Kontekst projektu

**PDM (Project Data Management)** — platforma do zarządzania projektami budowlanymi/inżynieryjnymi (marka: Brickly).

| Warstwa | Technologia |
|---------|-------------|
| Frontend | React 18 SPA + TypeScript + Vite (`01-Applications/ProjectDataManagementUI`) |
| Backend | .NET 10 Web API, CQRS + MediatR (`02-ApplicationServices/ProductDataManagementWebAPI`) |
| Real-time | SignalR (chat, powiadomienia) |
| Autentykacja | Microsoft Entra External ID / Azure AD B2C (MSAL) — JWT Bearer |
| Baza danych | SQL Server + EF Core (code-first, migrations) |
| Cache | Redis |
| Storage | Azure Blob Storage + Queue Storage |
| Deploy | Docker Compose + nginx reverse proxy |

---

## Architektura systemu

```
[Przeglądarka] → nginx (port 8085)
                   ├─→ /              → React SPA (port 80 kontenera)
                   └─→ /api/*         → .NET Web API (port 8080 kontenera)
                        └─→ /api/hubs/* → SignalR WebSockets
```

Frontend komunikuje się z backendem wyłącznie przez REST API i SignalR.  
Token JWT (MSAL `acquireTokenSilent`) wysyłany w headerze `Authorization: Bearer <token>`.

---

## Uruchomienie lokalne

```bash
# Z katalogu 03-Deployment
docker-compose -f docker-compose.development.yml up
# Aplikacja dostępna na: http://localhost:8085
```

Backend wymaga zmiennych środowiskowych w `03-Deployment/.env.development` — wzorzec kluczy w `appsettings.json`.

---

## Kontrakt API — konwencja typów

- Backend zwraca **web modele** z sufiksem `Web` w C# (np. `ProjectDetailsWeb`, `CostEstimateGroupWeb`)
- Frontend używa identycznych nazw interfejsów TypeScript (np. `ProjectDetailsWeb`, `CostEstimateGroupWeb`)
- Nigdy nie eksportuj encji EF Core (`Project`, `Tenant`, `User`) bezpośrednio do JSON
- Format odpowiedzi błędu: `{ error: string, message: string, objectType?: string, objectId?: string }`

---

## Ogólne zasady kodu

### Czytelność
- Nazwy klas, metod i zmiennych muszą wskazywać **co to jest** i **po co istnieje**
- Unikaj skrótów: `svc`, `mgr`, `obj` — pisz pełnymi nazwami
- Preferuj prosty, jednoznaczny kod ponad sprytne one-linery

### DRY
- Reaguj na zduplikowaną logikę — wydzielaj do metod pomocniczych
- Nie proponuj nadmiernej abstrakcji przy małej skali powtórzeń

### Obsługa błędów
- Nigdy nie połykaj wyjątków w pustym `catch`
- Każdy `catch` musi albo logować albo propagować błąd
- Nie zwracaj `200 OK` z treścią błędu

### Komentarze
- Dobre komentarze tłumaczą **dlaczego** (decyzja architektoniczna), nie **co** (kod)
- Jeśli komentarz opisuje co kod robi — uprość kod zamiast pisać komentarz
- Copilot pisze komentarze **po polsku**, podając powód i proponując gotową poprawkę

### Bezpieczeństwo
- Nigdy nie loguj tokenów JWT, haseł ani danych wrażliwych
- Waliduj dane na wejściu do API (FluentValidation w backendzie, `fieldValidation.ts` na frontendzie)
- Uprawnienia sprawdzaj przez dedykowane mechanizmy — nie hardkoduj ról w logice biznesowej
