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

Przed implementacją przeczytaj odpowiedni skill:

| Obszar | Skill |
|--------|-------|
| CQRS (Commands, Queries, Handlers) | `.github/skills/api/skill-api-cqrs.md` |
| Walidatory (FluentValidation) | `.github/skills/api/skill-api-validators.md` |
| Kontrolery (endpoints, routing) | `.github/skills/api/skill-api-controllers.md` |
| Encje i konfiguracje EF Core | `.github/skills/api/skill-api-entities.md` |
| Repozytoria | `.github/skills/api/skill-api-repositories.md` |
| Serwisy domenowe | `.github/skills/api/skill-api-services.md` |
| Testy jednostkowe | `.github/skills/api/skill-api-unit-tests.md` |

## Skille — UI

| Obszar | Skill |
|--------|-------|
| Typy TypeScript | `.github/skills/ui/skill-ui-types.md` |
| Hooki (React Query, custom) | `.github/skills/ui/skill-ui-hooks.md` |
| Komponenty | `.github/skills/ui/skill-ui-components.md` |
| Formularze i modale | `.github/skills/ui/skill-ui-forms-modals.md` |
| Klienty API | `.github/skills/ui/skill-ui-api-client.md` |
| Theme i design tokens | `.github/skills/ui/skill-ui-theme.md` |
| Testy jednostkowe | `.github/skills/ui/skill-ui-unit-tests.md` |
