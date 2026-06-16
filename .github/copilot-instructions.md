# Copilot Instructions — Project Data Management (PDM)

## Struktura projektu

```
PDM/
├── 01-Applications/
│   └── ProjectDataManagementUI/     # React + TypeScript + Chakra UI
│       └── src/
│           ├── api/                 # Klienty API per domena (*Api.ts)
│           ├── components/
│           │   ├── ui/              # Bazowe elementy UI (AppModal, DeleteAlertDialog…)
│           │   ├── common/          # Komponenty pomocnicze (LoadingSpinner…)
│           │   ├── chat/            # Komponenty chatu AI
│           │   ├── CostEstimate/    # Komponenty kosztorysu
│           │   ├── CostTracker/     # Komponenty trackera kosztów
│           │   ├── ProjectParameters/ # Parametry projektu
│           │   └── gantt/           # Wykres Gantta
│           ├── config/              # Konfiguracja aplikacji
│           ├── constants/           # Stałe i enums
│           ├── context/             # React Contexts
│           ├── features/            # Moduły domenowe
│           ├── hooks/               # Hooki globalne
│           ├── i18n/                # Internacjonalizacja (i18next)
│           ├── layout/              # Layout aplikacji
│           ├── lib/                 # Biblioteki pomocnicze
│           ├── pages/               # Strony routowane
│           ├── routes/              # Konfiguracja routingu
│           ├── services/            # Serwisy (SignalR itp.)
│           ├── theme/tokens/        # Design tokens (colors.ts — jedyne źródło kolorów)
│           ├── types/               # Typy globalne per domena
│           └── utils/               # Funkcje pomocnicze (formatters itp.)
│
└── 02-ApplicationServices/
    └── ProductDataManagementWebAPI/
        └── src/
            ├── WebApi/              # Controllers, Middleware, Hubs
            ├── CQRS/                # Commands, Queries, Handlers, Behaviours
            ├── Business/            # Serwisy, interfejsy, web modele, wyjątki
            ├── Entities/            # EF Core — DbContext, encje, migracje
            └── Repositories/        # IRepository<T>, IReadRepository<T>
```

## Stack

| Warstwa | Technologie |
|---------|------------|
| API | .NET 10, MediatR 13, FluentValidation 12, EF Core 10, SignalR |
| UI | React 18, TypeScript 5.9, Chakra UI 2, Axios, React Router 7, TanStack React Query 5 |
| Auth | Azure AD B2C, MSAL (@azure/msal-browser 4, @azure/msal-react 3) |
| Infra | Azure Blob, Redis, SignalR (@microsoft/signalr 10) |

## Zasady ogólne — API

- Zakaz `var` — zawsze explicit type
- `is null` / `is not null` zamiast `== null`
- Klamry `{}` przy każdym bloku
- Max ~20 linii na metodę
- `IReadRepository<T>` dla odczytu, `IRepository<T>` dla zapisu
- Predykaty zawsze z `TenantId` i `ProjectId`

## Zasady ogólne — UI

- Zakaz `any` — zawsze explicit type
- Logika w hookach, komponenty tylko renderują
- Kolory przez Chakra tokens lub `appColors` z `theme/tokens/colors.ts`
- Zakaz inline styles
- `AppModal` zamiast własnych implementacji modala

## Skille — API

Przed implementacją użyj odpowiedniego skilla (wpisz `/` w chacie aby wybrać):

| Obszar | Skill |
|--------|-------|
| CQRS (Commands, Queries, Handlers) | `api-cqrs` |
| Walidatory (FluentValidation) | `api-validators` |
| Kontrolery (endpoints, routing) | `api-controllers` |
| Encje i konfiguracje EF Core | `api-entities` |
| Repozytoria | `api-repositories` |
| Serwisy domenowe | `api-services` |
| Testy jednostkowe | `api-unit-tests` |

## Skille — UI

| Obszar | Skill |
|--------|-------|
| Typy TypeScript | `ui-types` |
| Hooki (React Query, custom) | `ui-hooks` |
| Komponenty | `ui-components` |
| Formularze i modale | `ui-forms-modals` |
| Klienty API | `ui-api-client` |
| Theme i design tokens | `ui-theme` |
| Testy jednostkowe | `ui-unit-tests` |
| **Dostępność WCAG AA / AXE** | `ui-accessibility` |

## Agenty

Użyj `@` w chacie Copilota żeby wywołać specjalistycznego agenta:

| Agent | Kiedy używać |
|-------|-------------|
| `@feature-planner-agent` | Planowanie i koordynacja wdrożenia nowego feature |
| `@unit-test-orchestrator-agent` | Pisanie testów jednostkowych dla wielu warstw |
| `@handler-test-agent` | Testy dla CommandHandler / QueryHandler (CQRS.Tests) |
| `@validator-test-agent` | Testy dla walidatorów FluentValidation (CQRS.Tests) |
| `@service-test-agent` | Testy dla serwisów domenowych (Business.Tests) |
| `@api-audit-agent` | Audyt warstwy API przed implementacją zmian (read-only) |
| `@ui-audit-agent` | Audyt warstwy UI przed implementacją zmian (read-only) |
| `@refactor-agent` | Implementacja zmian w API wg promptu |
| `@ui-refactor-agent` | Implementacja zmian w UI wg promptu |
