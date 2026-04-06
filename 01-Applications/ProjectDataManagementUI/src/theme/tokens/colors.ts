/**
 * Centralny plik tokenów kolorów aplikacji Brickly / PDM.
 *
 * WZORZEC: kolorystyka z widoków kosztorysu i harmonogramu
 *   – niebieski jako kolor przewodni (nagłówki, CTA, aktywne stany)
 *   – zielony dla hierarchii poziom 1 (komponenty, zakresy robót, sukces)
 *   – fioletowy dla hierarchii poziom 2 (opcje, sumowania, obliczenia)
 *   – teal dla akcji drugorzędnych (zapis, udostępnij)
 *   – szary/neutralny dla powierzchni i tekstu pomocniczego
 *
 * UŻYCIE w komponentach:
 *   import { appColors } from "@/theme/tokens/colors";
 *   bg={appColors.primary[600]}
 *   colorScheme="primary"  (po rejestracji w extendTheme)
 *
 * UŻYCIE jako tokeny Chakry (po rejestracji w theme.ts):
 *   bg="primary.600"
 *   colorScheme="primary"
 */

// ---------------------------------------------------------------------------
// Kolor przewodni – niebieski
// Nagłówki tabel, przyciski CTA, focus ring, aktywne stany, linki, paginacja,
// ikony sekcji, avatar, notification badge, toolbar aktywny tryb podglądu,
// Today-column w harmonogramie, nagłówek thead kosztorysu, etap główny harmonogramu
// ---------------------------------------------------------------------------
export const primaryColors = {
  50: "#EBF8FF",   // tło info-box, unread notification bg, selected row bg
  100: "#BEE3F8",  // hover aktywny, today column bg, etap główny hover, etap drag
  200: "#90CDF4",  // obramowanie etapu głównego
  300: "#63B3ED",  // border info-box
  400: "#4299E1",  // icon logo, Spinner loading
  500: "#3182CE",  // domyślny kolor zakresów Gantta, link, badge outline
  600: "#2B6CB0",  // avatar, icon header, nagłówek thead kosztorysu, bg "Etap" badge level-0
  700: "#2C5282",  // ikona FileSpreadsheet toolbar
  800: "#2A4365",  // dark mode today bg
  900: "#1A365D",  // dark mode etap bg
} as const;

// ---------------------------------------------------------------------------
// Hierarchia poziom 1 – zielony
// Komponenty w kosztorysie (SortableComponentRow bg), zakresy robót
// zakończone/closed w harmonogramie (completedBg), pola obliczeniowe
// w szablonie, status „Zatwierdzony", przycisk Dodaj/Nowy
// ---------------------------------------------------------------------------
export const level1Colors = {
  50: "#F0FFF4",   // tło wiersza komponentu (kosztorys), completedBg harmonogram
  100: "#C6F6D5",  // hover komponentu
  200: "#9AE6B4",  // obramowanie sekcji
  300: "#68D391",  // badge count pola obliczeniowego
  400: "#48BB78",  // animacja aktywna
  500: "#38A169",  // ikona/badge "Zatwierdzony", preset kolor zakresu, indicator "Zapisano"
  600: "#2F855A",  // wartość totalGross w tabeli kosztorysów, bg badge Zatwierdzonego, ikona Dodaj pliki
  700: "#276749",  // tekst pogrubiony w obliczeniach
  800: "#22543D",
  900: "#1C4532",
} as const;

// ---------------------------------------------------------------------------
// Hierarchia poziom 2 – fioletowy
// Opcje w kosztorysie (SortableOptionRow bg), wiersz SUMA, pola generyczne
// w szablonie, zależności harmonogramu (badge/przycisk), status „Zarchiwizowany",
// kopiuj kosztorys, shared count badge
// ---------------------------------------------------------------------------
export const level2Colors = {
  50: "#FAF5FF",   // tło wiersza opcji (kosztorys)
  100: "#E9D8FD",  // hover opcji
  200: "#D6BCFA",  // obramowanie wiersza SUMA (borderTop)
  300: "#B794F4",  // badge count pola generycznego
  400: "#9F7AEA",  // icon org/tenant switcher
  500: "#805AD5",  // preset kolor zakresu, badge "Zarchiwizowany", shared count
  600: "#6B46C1",  // ikona sekcji (Calendar, FileText), tekst SUMA bold
  700: "#553C9A",  // tekst w SUMA row
  800: "#44337A",
  900: "#322659",
} as const;

// ---------------------------------------------------------------------------
// Akcje drugorzędne – teal
// Przycisk Zapisz (kosztorys toolbar), Udostępnij, aktywna kropka mobile toolbar,
// aktywny element menu, shared-with-me badge, etykiety zależności (FS/SS...)
// ---------------------------------------------------------------------------
export const actionColors = {
  50: "#E6FFFA",
  100: "#B2F5EA",
  200: "#81E6D9",
  300: "#4FD1C5",
  400: "#38B2AC",  // aktywna kropka mobile toolbar (bg), notification dot sidebar
  500: "#319795",  // preset kolor zakresu, badge "Udostępniony", etykiety FS/SS
  600: "#2C7A7B",  // active menu item color, icon Share2
  700: "#285E61",
  800: "#234E52",
  900: "#1D4044",
} as const;

// ---------------------------------------------------------------------------
// Stany semantyczne
// ---------------------------------------------------------------------------

// Błąd / Niebezpieczeństwo – czerwony
// Usuwanie, przeterminowane zakresy (expiredBg), status Odrzucony, ikona pliku błędu
export const dangerColors = {
  50: "#FFF5F5",   // expiredBg harmonogram (przeterminowany zakres)
  100: "#FED7D7",
  200: "#FEB2B2",
  300: "#FC8181",
  400: "#F56565",
  500: "#E53E3E",  // icon błędu pliku, tekst "Brak dostępu", preset kolor zakresu
  600: "#C53030",  // ikony delete/trash, red text
  700: "#9B2C2C",
  800: "#822727",
  900: "#63171B",
} as const;

// Ostrzeżenie – pomarańczowy
// Zagrożone zakresy (warningBg ≤5 dni), status Do przeglądu,
// przycisk Harmonogram (CostEstimateToolbar), tooltip niepoprawna zależność
export const warningColors = {
  50: "#FFFAF0",   // warningBg harmonogram (zagrożony termin)
  100: "#FEEBC8",
  200: "#FBD38D",
  300: "#F6AD55",
  400: "#ED8936",
  500: "#DD6B20",  // preset kolor zakresu, orange icon w modalu szablonu
  600: "#C05621",  // icon "Zaplanowane prace" dashboard
  700: "#9C4221",
  800: "#7B341E",
  900: "#652B19",
} as const;

// Sukces / Zakończono – zielony
// Używa tych samych wartości co level1 (green), wyeksportowany jako alias
// dla czytelności kodu (semantyka vs hierarchia)
export const successColors = level1Colors;

// ---------------------------------------------------------------------------
// Powierzchnie i tła
// ---------------------------------------------------------------------------
export const surfaceColors = {
  // Strona i layout
  pageBg: {
    light: "gray.50",   // #F7FAFC — tło strony (ProjectDetails, Home, Profile)
    dark: "gray.900",
  },
  // Karty, modale, toolbary, nagłówki
  cardBg: {
    light: "white",
    dark: "gray.800",
  },
  // Nagłówek thead (harmonogram)
  theadBg: {
    light: "gray.50",
    dark: "gray.700",
  },
  // Wiersze naprzemienne / lekkie tło w formach
  subtleBg: {
    light: "gray.50",
    dark: "gray.700",
  },
  // Hover na wierszach (pozycja, zakres normalny)
  hoverBg: {
    light: "gray.50",
    dark: "gray.700",
  },
} as const;

// ---------------------------------------------------------------------------
// Obramowania
// ---------------------------------------------------------------------------
export const borderColors = {
  default: {
    light: "gray.200",
    dark: "gray.700",
  },
  strong: {
    light: "gray.300",
    dark: "gray.600",
  },
} as const;

// ---------------------------------------------------------------------------
// Tekst
// ---------------------------------------------------------------------------
export const textColors = {
  heading: {
    light: "gray.800",
    dark: "gray.100",
  },
  body: {
    light: "gray.700",
    dark: "gray.300",
  },
  muted: {
    light: "gray.500",
    dark: "gray.400",
  },
  placeholder: {
    light: "gray.400",
    dark: "gray.500",
  },
  onColor: "white",           // tekst na kolorowych tłach (badge, avatar, thead)
  onSubtle: {
    light: "gray.600",
    dark: "gray.400",
  },
  link: primaryColors[500],
  linkHover: primaryColors[600],
} as const;

// ---------------------------------------------------------------------------
// Eksport zbiorczy (do użycia w theme.ts lub bezpośrednio)
// ---------------------------------------------------------------------------
export const appColors = {
  primary: primaryColors,
  level1: level1Colors,
  level2: level2Colors,
  action: actionColors,
  danger: dangerColors,
  warning: warningColors,
  success: successColors,
  surface: surfaceColors,
  border: borderColors,
  text: textColors,
} as const;

// ---------------------------------------------------------------------------
// Mapowanie na token-strings Chakry (do użycia w colorScheme / bg={} propach)
// ---------------------------------------------------------------------------
export const CHAKRA_TOKENS = {
  // Kolor przewodni → primary (rejestrujemy jako "primary" w theme)
  primary: "primary",
  // Hierarchia
  level1: "level1",
  level2: "level2",
  // Akcje drugorzędne
  action: "action",
  // Semantyczne
  danger: "red",     // standardowy Chakra token
  warning: "orange", // standardowy Chakra token
  success: "green",  // standardowy Chakra token
  // Neutralny
  neutral: "gray",
} as const;

// ---------------------------------------------------------------------------
// Paleta kolorów zakresów robót (Gantt) – wzorzec z WorkScheduleFormModal
// Te wartości są DANYMI domenowymi (zapisywane w bazie jako colorRgb),
// ale spójne z tokenami projektu.
// ---------------------------------------------------------------------------
export const WORK_SCOPE_COLORS = [
  { hex: "#3182CE", label: "Niebieski",   token: "primary.500"  },
  { hex: "#38A169", label: "Zielony",     token: "level1.500"   },
  { hex: "#DD6B20", label: "Pomarańczowy",token: "warning.500"  },
  { hex: "#E53E3E", label: "Czerwony",    token: "danger.500"   },
  { hex: "#805AD5", label: "Fioletowy",   token: "level2.500"   },
  { hex: "#D69E2E", label: "Żółty",       token: "yellow.500"   },
  { hex: "#00B5D8", label: "Cyan",        token: "cyan.400"     },
  { hex: "#D53F8C", label: "Różowy",      token: "pink.500"     },
  { hex: "#319795", label: "Teal",        token: "action.500"   },
  { hex: "#718096", label: "Szary",       token: "gray.500"     },
] as const;
