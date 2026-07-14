/** Kolory wykresów zgodne z tokenami Chakra UI projektu. */
export const CHART_COLORS = {
  primary: '#3182CE',
  primaryLight: '#EBF8FF',
  level1: '#38A169',
  level1Dark: '#276749',
  level2: '#805AD5',
  level2Light: '#FAF5FF',
  orange: '#DD6B20',
  orangeDark: '#C05621',
  red: '#E53E3E',
  redLight: '#FFF5F5',
  amber: '#D69E2E',
  neutral: '#A0AEC0',
  neutralDark: '#4A5568',
  neutralLight: '#EDF2F7',
  action: '#319795',
} as const;

export const CHART_PALETTE: string[] = [
  CHART_COLORS.primary,
  CHART_COLORS.level1,
  CHART_COLORS.level2,
  CHART_COLORS.orange,
  CHART_COLORS.amber,
  CHART_COLORS.action,
  CHART_COLORS.neutral,
];

export const CHART_HEIGHT = 260;

export const CHART_MARGIN = { top: 8, right: 16, left: 8, bottom: 8 };
