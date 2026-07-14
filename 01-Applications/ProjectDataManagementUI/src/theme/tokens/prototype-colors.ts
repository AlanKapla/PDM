/**
 * Prototype color tokens - Modern Brickly Design System
 * Based on HTML prototype from .opencode/prototypes/
 */

export const prototypeColors = {
  // Base surfaces
  bg: '#F3F5F8',
  surface: '#FFFFFF',
  surface2: '#FBFCFE',
  surface3: '#F6F8FB',
  
  // Lines & borders
  line: '#E8EBF0',
  line2: '#EDF0F4',
  lineStrong: '#D9DEE6',
  
  // Text
  text: '#15212F',
  text2: '#566071',
  muted: '#8B95A6',
  faint: '#AEB7C5',
  
  // Brand
  brand: '#2F6CEC',
  brandInk: '#1E50C0',
  brandTint: '#EAF1FE',
  brandTint2: '#F4F8FF',
  
  // Hierarchy colors
  etap: '#2F6CEC',      // Stage
  podetap: '#6E59E6',   // Sub-stage
  pozycja: '#119D8C',   // Item
  komponent: '#C2792B', // Component
  
  // Status
  ok: '#1E9E63',
  okTint: '#E7F6EE',
  danger: '#E5544B',
  dangerTint: '#FCEEED',
  
  // Shadows
  shadowSm: '0 1px 2px rgba(20,33,47,.05), 0 1px 3px rgba(20,33,47,.04)',
  shadowMd: '0 4px 16px rgba(20,33,47,.08), 0 1px 3px rgba(20,33,47,.05)',
  shadowLg: '0 18px 50px rgba(20,33,47,.16), 0 4px 14px rgba(20,33,47,.08)',
  
  // Radius
  radius: '14px',
  radiusSm: '10px',
} as const;

/**
 * Hierarchy level to color mapping
 */
export const hierarchyColors = {
  group: prototypeColors.etap,
  subgroup: prototypeColors.podetap,
  item: prototypeColors.pozycja,
  component: prototypeColors.komponent,
  option: prototypeColors.muted,
} as const;

/**
 * Get hierarchy color by level depth
 */
export function getHierarchyColor(level: number): string {
  if (level === 0) return hierarchyColors.group;     // Etap
  if (level === 1) return hierarchyColors.subgroup;  // Podetap
  if (level === 2) return hierarchyColors.item;      // Pozycja
  return hierarchyColors.component;                   // Komponent
}

/**
 * Get hierarchy label by level
 */
export function getHierarchyLabel(level: number): string {
  if (level === 0) return 'ETAP';
  if (level === 1) return 'PODETAP';
  if (level === 2) return 'POZYCJA';
  return 'KOMPONENT';
}

/**
 * CSS variables for prototype design
 */
export const prototypeCssVars = {
  '--proto-bg': prototypeColors.bg,
  '--proto-surface': prototypeColors.surface,
  '--proto-surface-2': prototypeColors.surface2,
  '--proto-surface-3': prototypeColors.surface3,
  '--proto-line': prototypeColors.line,
  '--proto-line-2': prototypeColors.line2,
  '--proto-line-strong': prototypeColors.lineStrong,
  '--proto-text': prototypeColors.text,
  '--proto-text-2': prototypeColors.text2,
  '--proto-muted': prototypeColors.muted,
  '--proto-faint': prototypeColors.faint,
  '--proto-brand': prototypeColors.brand,
  '--proto-brand-ink': prototypeColors.brandInk,
  '--proto-brand-tint': prototypeColors.brandTint,
  '--proto-brand-tint-2': prototypeColors.brandTint2,
  '--proto-etap': prototypeColors.etap,
  '--proto-podetap': prototypeColors.podetap,
  '--proto-pozycja': prototypeColors.pozycja,
  '--proto-komponent': prototypeColors.komponent,
} as const;
