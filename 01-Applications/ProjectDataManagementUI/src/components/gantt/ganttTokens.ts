/** Tokeny designowe i stałe wymiarowe Ganttu */
export const G = {
  // ─── Kolory ────────────────────────────────────────────────────────────────
  bg:           "#f5f4f1",
  surface:      "#ffffff",
  surface2:     "#f9f8f6",
  border:       "#e8e6e1",
  borderStrong: "#d4d0c8",
  text:         "#1a1917",
  text2:        "#6b6860",
  text3:        "#9b9790",
  accent:       "#2d5be3",
  accentLight:  "#eef1fd",
  green:        "#1a7a4a",
  greenLight:   "#e8f5ee",
  greenMid:     "#34c472",
  amber:        "#b45309",
  amberLight:   "#fef3c7",
  today:        "#5b8def",
  todayBg:      "rgba(91,141,239,0.08)",
  stageBg:      "#f0ede8",
  closedBg:     "#f0fdf4",

  // ─── Wymiary ───────────────────────────────────────────────────────────────
  LEFT_W:         420,
  STAGE_ROW_H:    48,   // wiersz nagłówka etapu
  ROW_H:          44,   // wiersz zakresu pracy
  PERIOD_ROW_H:   36,   // wiersz okresu
  STAGE_DETAIL_H: 28,   // wiersz "Ukończone: X/Y"
  ADD_WORK_H:     36,   // wiersz "+ zakres"
  HEADER_H:       84,
  HEADER_WEEKS:   56,
  HEADER_DAYS:    28,
  DEPTH_INDENT:   16,

  /** Szerokość pojedynczej kolumny (= 1 dzień) dla danej skali */
  colW: (scale: string): number =>
    ({ days: 34, weeks: 26, months: 18 } as Record<string, number>)[scale] ?? 34,
} as const;
