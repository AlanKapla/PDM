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
- Moduły AI automatyzujące pracę: rozpoznawanie dokumentów kosztowych i generowanie kosztorysów

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
| `ai-cost-import.png`            | AI — automatyczne rozpoznawanie faktur i paragonów |
| `ai-estimate-generate.png`      | AI — generowanie kosztorysu na podstawie opisu     |

## Procedura modyfikacji komponentu

1. Przeczytaj istniejący `.tsx` i `.css` komponentu
2. Zidentyfikuj sekcje do zmiany
3. Zastosuj zasady językowe (bezosobowe, profesjonalne)
4. Zachowaj strukturę BEM w CSS
5. Użyj tokenów kolorów z tabeli powyżej (zmienne CSS)
6. Nie dodawaj inline styles
7. Zweryfikuj brak błędów TypeScript
8. Spełnij wymagania dostępności (patrz sekcja poniżej)

## Wymagania dostępności (WCAG AA / AXE)

**Obowiązkowe przy każdej zmianie komponentu:**

### Ikony dekoracyjne
- Każda ikona Lucide w roli dekoracyjnej (obok tekstu, wewnątrz przycisku z etykietą) musi mieć `aria-hidden="true"`:
  ```tsx
  <ArrowRight size={18} aria-hidden="true" />
  ```
- Ikony stanowiące jedyną treść elementu interaktywnego muszą mieć `aria-label` na rodzicu.

### Elementy dekoracyjne (spany z CSS dot, divider itp.)
```tsx
<span className="feature-row__dot" aria-hidden="true" />
```

### Przyciski i linki
- Każdy `<button>` bez widocznego tekstu musi mieć `aria-label`.
- `aria-expanded` na hamburgerze i podobnych toggleach.
- Przyciski nawigacyjne w sliderach: `aria-label="Poprzedni zrzut ekranu"` (nie tylko "Poprzedni").

### Fokus i klawiatura
- Nie usuwaj `:focus-visible` z `index.css` — jedyne źródło stylów fokusowych w projekcie.
- `div` z `onClick` musi mieć `role="button"`, `tabIndex={0}` i `onKeyDown` obsługujący `Enter`/`Space`.
- Modale (lightbox) wymagają:
  - `role="dialog"`, `aria-modal="true"`, `aria-label`
  - focus na pierwszym fokusowanym elemencie po otwarciu
  - powrót focusu do triggera po zamknięciu
  - obsługę `Escape`

### Nawigacja
- `<nav>` musi mieć `aria-label` gdy jest więcej niż jeden `<nav>` na stronie.
- Skip link (`<a href="#main-content" className="skip-link">`) jest zdefiniowany w `App.tsx` — nie usuwaj.
- `<main id="main-content" tabIndex={-1}>` — wymagany cel skip linka.

### Obrazy
- Logo w linkach: `alt=""` + `aria-label` na `<a>` (logo jest dekoracyjne gdy link ma własną etykietę).
- Screenshoty funkcjonalności: `alt` musi opisywać co widać na ekranie.

### Kontrast (WCAG AA minimalne wymagania)
- Tekst ≥ 18px normalny lub ≥ 14px bold: min. **3:1**
- Pozostały tekst: min. **4.5:1**
- Zatwierdzone pary (zweryfikowane): `#111111` / `#FFF5EE`, `#1B4FD8` / `#FFF5EE`, `#FFFFFF` / `#1B4FD8`
- **Nie używaj** `--color-text-muted` (`#555555`) na jasnym tle dla małego tekstu bez weryfikacji kontrastu.

### Testy
- Uruchom AXE w DevTools (zakładka Accessibility) po każdej zmianie.
- Zero naruszeń krytycznych i poważnych przed commitem.
