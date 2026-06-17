import { Box, Flex } from '@chakra-ui/react';
import type { FlexProps, SystemStyleObject } from '@chakra-ui/react';
import React from 'react';

/** Opaque tints — rgba on white pre-composited so sticky cells mask horizontal scroll. */
const SURFACES = {
  groupL0: { bg: '#F5F8FE', hoverBg: '#EAF0FD' },
  groupL1: { bg: '#FAFBFF', hoverBg: '#F5F8FE' },
  itemL2: { bg: '#F5FBFA', hoverBg: '#ECF7F6' },
  itemL3: { bg: '#FAFDFD', hoverBg: '#F5FBFA' },
  itemL4: { bg: '#FAF8FD', hoverBg: '#F5F2FC' },
  default: { bg: '#FFFFFF', hoverBg: '#F1EFE8' },
  addRow: { bg: '#FFFFFF', hoverBg: '#F8F7F4' },
} as const;

/** Solid row surfaces — one token for the full-row bg layer and opaque sticky cells. */
export interface RowSurfaceColors {
  bg: string;
  hoverBg: string;
}

export function getGroupRowSurface(level: number): RowSurfaceColors {
  if (level === 0) {
    return SURFACES.groupL0;
  }
  return SURFACES.groupL1;
}

export function getItemRowSurface(itemLevel: number): RowSurfaceColors {
  switch (itemLevel) {
    case 2:
      return SURFACES.itemL2;
    case 3:
      return SURFACES.itemL3;
    case 4:
      return SURFACES.itemL4;
    default:
      return SURFACES.default;
  }
}

export const ADD_ROW_SURFACE: RowSurfaceColors = SURFACES.addRow;

export function treeViewRowHoverStyles(hoverBg: string): SystemStyleObject {
  return {
    '& [data-row-bg]': { bg: hoverBg },
    '& [data-sticky-cell]': { bg: hoverBg },
    '& [data-sticky-cell]::before': { bg: hoverBg },
  };
}

export interface TreeViewRowBackgroundProps {
  bg: string;
}

export const TreeViewRowBackground: React.FC<TreeViewRowBackgroundProps> = ({ bg }) =>
  React.createElement(Box, {
    'data-row-bg': true,
    position: 'absolute',
    inset: 0,
    bg,
    zIndex: 0,
    pointerEvents: 'none',
    'aria-hidden': true,
  });

export interface TreeViewStickyCellProps extends FlexProps {
  surfaceBg: string;
  left: string | number;
  width: string;
}

/** Sticky name/actions cell with a full-bleed opaque backdrop. */
export const TreeViewStickyCell: React.FC<TreeViewStickyCellProps> = ({
  surfaceBg,
  left,
  width,
  children,
  ...flexProps
}) =>
  React.createElement(
    Flex,
    {
      'data-sticky-cell': true,
      flex: '0 0 auto',
      w: width,
      position: 'sticky',
      left,
      zIndex: 5,
      alignSelf: 'stretch',
      alignItems: 'center',
      bg: surfaceBg,
      isolation: 'isolate',
      _before: {
        content: '""',
        position: 'absolute',
        inset: 0,
        bg: surfaceBg,
        zIndex: -1,
      },
      ...flexProps,
    },
    children
  );
