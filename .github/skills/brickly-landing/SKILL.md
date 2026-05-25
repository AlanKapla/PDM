---
name: brickly-landing
description: "Skill do pracy z landing page Brickly. Użyj gdy: modyfikujesz treść, układ, kolory lub komponenty BricklyLandingPage (React + Vite + czysty CSS). Zawiera konwencje projektowe, paletę kolorów, zasady językowe i słownik screenshotów."
argument-hint: "Co chcesz zmienić na landing page Brickly?"
---

# Brickly Landing Page — Skill

## Lokalizacja projektu

```
01-Applications/BricklyLandingPage/
├── src/
│   ├── components/          # Wszystkie sekcje strony
│   │   ├── Hero.tsx / Hero.css
│   │   ├── About.tsx / About.css
│   │   ├── Modules.tsx / Modules.css
│   │   ├── TargetUsers.tsx / TargetUsers.css
│   │   ├── CallToAction.tsx / CallToAction.css
│   │   ├── Navbar.tsx / Navbar.css
│   │   ├── Footer.tsx / Footer.css
│   │   └── BrowserMockup.tsx / BrowserMockup.css
│   ├── hooks/
│   └── config/
└── public/
    └── screenshots/         # Screenshoty funkcjonalności (patrz słownik poniżej)
```

## Stack

- React 18 + TypeScript 5
- Vite
- Czysty CSS (bez frameworka UI)
- Lucide React (ikony)

## Paleta kolorów

| Token CSS           | Wartość          | Zastosowanie                        |
|---------------------|------------------|-------------------------------------|
| `--color-bg`        | `#FFF5EE`        | Tło strony (jasna brzoskwinia)       |
| `--color-bg-alt`    | `#FFF0E6`        | Tło sekcji alternatywnej            |
| `--color-primary`   | `#1B4FD8`        | Kolor główny cobalt (tytuły, CTA)   |
| `--color-primary-dark` | `#163DB8`    | Hover na cobalt                     |
| `--color-text`      | `#111111`        | Kolor opisów (czarny)               |
| `--color-text-muted`| `#555555`        | Tekst drugorzędny                   |
| `--color-accent`    | `#E07B39`        | Akcenty (dot, badge, highlight)     |

## Zasady językowe

- Język **bezosobowy** — nie mówimy do użytkownika na "Ty"
- **Profesjonalny**, bez luźnych fraz, bez humoru
- Zachęcający, prezentujący wartość platformy
- Przykład złego: „Wiesz ile kosztuje ta budowa?"
- Przykład dobrego: „Kompleksowe zarządzanie kosztami każdego projektu"

## Grupy docelowe

1. **Deweloperzy** — zarządzający wieloma inwestycjami jednocześnie
2. **Inwestorzy zastępczy** — działający w imieniu inwestorów, potrzebujący dokumentacji decyzyjnej
3. **Inwestorzy prywatni** — oczekujący wglądu w projekt bez angażowania zespołu
4. **Architekci** — prowadzący nadzór autorski i koordynację dokumentacji

## Oferta platformy

- Bezpłatna
- Otwarta na integracje z zewnętrznymi systemami (ERP, księgowość, platformy zakupowe)
- Możliwość dodawania spersonalizowanych modułów dla klienta
- Moduły AI automatyzujące pracę (w przyszłości)

## Słownik screenshotów

Screenshoty umieszczane są w: `public/screenshots/`

| Klucz (nazwa pliku)             | Funkcjonalność                                      |
|---------------------------------|-----------------------------------------------------|
| `doc-versioning.png`            | Dokumentacja projektowa — wersjonowanie             |
| `doc-comments.png`              | Dokumentacja projektowa — komentarze                |
| `doc-sharing.png`               | Dokumentacja projektowa — udostępnianie             |
| `cost-expenses.png`             | Dokumentacja kosztowa — wydatki członków projektu   |
| `cost-approval.png`             | Dokumentacja kosztowa — akceptacja wydatków         |
| `cost-add.png`                  | Dokumentacja kosztowa — dodawanie kosztów           |
| `estimate-templates.png`        | Kosztorysy — szablony spersonalizowane              |
| `estimate-variants.png`         | Kosztorysy — warianty pozycji                       |
| `estimate-components.png`       | Kosztorysy — budowanie z komponentów                |
| `schedule-periods.png`          | Harmonogram — podział na okresy realizacji          |
| `schedule-completion.png`       | Harmonogram — zaznaczanie wykonania                 |
| `schedule-dependencies.png`     | Harmonogram — zależności między zakresami           |
| `sync-stages.png`               | Synchronizacja kosztorys–harmonogram — etapy        |
| `sync-substages.png`            | Synchronizacja kosztorys–harmonogram — podetapy     |
| `dashboard-costs.png`           | Dashboard — dodawanie kosztów                       |
| `dashboard-alerts.png`          | Dashboard — alerty o przekroczeniach               |
| `dashboard-analysis.png`        | Dashboard — analiza kosztowo-czasowa                |
| `communication-module.png`      | Moduł komunikacji między członkami projektu         |
| `tasks-module.png`              | Zaplanowane prace dla członków projektu             |
| `contractors-module.png`        | Kontrahenci organizacji                             |
| `parameters-module.png`         | Parametryzacja projektu (np. waluta)               |

## Procedura modyfikacji komponentu

1. Przeczytaj istniejący `.tsx` i `.css` komponentu
2. Zidentyfikuj sekcje do zmiany
3. Zastosuj zasady językowe (bezosobowe, profesjonalne)
4. Zachowaj strukturę BEM w CSS
5. Użyj tokenów kolorów z tabeli powyżej (zmienne CSS)
6. Nie dodawaj inline styles
7. Zweryfikuj brak błędów TypeScript
