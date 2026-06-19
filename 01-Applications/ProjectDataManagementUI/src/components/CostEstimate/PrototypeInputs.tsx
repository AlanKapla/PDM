/**
 * Prototype-styled input components using Chakra UI
 * Inline editing with transparent → hover → focus states
 */

import React, { useState } from 'react';
import { Box, Input, type InputProps } from '@chakra-ui/react';
import {
  formatDecimalInput,
  parseNumericInput,
  resolveNumericInputDisplayValue,
  sanitizeNumericInput,
} from '../../utils/numericInputUtils';

interface PrototypeInputProps extends Omit<InputProps, 'variant'> {
  isGroup?: boolean;
  isStage?: boolean;
  /** W TreeView — bez białego tła przy hover/focus, spójnie z tłem wiersza */
  blendWithRow?: boolean;
  /** W modalach formularza — widoczna ramka i białe tło od razu */
  showBorder?: boolean;
}

const getInlineInputInteractionStyles = (blendWithRow: boolean, showBorder: boolean) => {
  if (showBorder) {
    return {
      _hover: { bg: 'white', borderColor: 'neutral.300', boxShadow: 'none' },
      _focus: {
        bg: 'white',
        borderColor: 'primary.500',
        boxShadow: '0 0 0 3px rgba(47, 108, 236, 0.12)',
        outline: 'none',
      },
      _focusVisible: {
        bg: 'white',
        borderColor: 'primary.500',
        boxShadow: '0 0 0 3px rgba(47, 108, 236, 0.12)',
        outline: 'none',
      },
    };
  }

  if (blendWithRow) {
    return {
      _hover: { bg: 'transparent', borderColor: 'neutral.200', boxShadow: 'none' },
      _focus: {
        bg: 'transparent',
        borderColor: 'neutral.200',
        boxShadow: 'none',
        outline: 'none',
      },
      _focusVisible: {
        bg: 'transparent',
        borderColor: 'neutral.200',
        boxShadow: 'none',
        outline: 'none',
      },
    };
  }

  return {
    _hover: { bg: 'white', borderColor: 'neutral.200', boxShadow: 'none' },
    _focus: {
      bg: 'white',
      borderColor: 'primary.500',
      boxShadow: '0 0 0 3px rgba(47, 108, 236, 0.12)',
      outline: 'none',
    },
    _focusVisible: {
      bg: 'white',
      borderColor: 'primary.500',
      boxShadow: '0 0 0 3px rgba(47, 108, 236, 0.12)',
      outline: 'none',
    },
  };
};

/** Wyłącza globalny outline/box-shadow — uzupełnienie reguły .ce-tree-view w index.css */
const TREE_ROW_INPUT_FOCUS_SX = {
  '&:focus': {
    outline: 'none !important',
    boxShadow: 'none !important',
  },
  '&:focus-visible': {
    outline: 'none !important',
    boxShadow: 'none !important',
  },
} as const;

/**
 * Inline text input with prototype styling
 * - Transparent background
 * - Border appears on hover
 * - Focus with thin border (tree) or brand shadow (modals)
 */
export const PrototypeTextInput: React.FC<PrototypeInputProps> = ({
  isGroup = false,
  isStage = false,
  blendWithRow = false,
  showBorder = false,
  minW,
  maxW,
  w,
  ...props
}) => {
  const interactionStyles = getInlineInputInteractionStyles(blendWithRow, showBorder);
  const resolvedMinW = minW ?? (blendWithRow ? 0 : '60px');
  const resolvedMaxW = maxW ?? (blendWithRow ? '100%' : '440px');
  const resolvedW = w ?? (blendWithRow ? 'full' : undefined);
  const focusSx = blendWithRow ? TREE_ROW_INPUT_FOCUS_SX : undefined;

  return (
    <Input
      {...props}
      variant={blendWithRow && !showBorder ? 'unstyled' : 'outline'}
      border="1px solid"
      borderColor={showBorder ? 'neutral.200' : 'transparent'}
      bg={showBorder ? 'white' : 'transparent'}
      borderRadius="8px"
      px="10px"
      py="7px"
      fontSize={isStage ? 'md' : 'sm'}
      fontWeight={isGroup || isStage ? 'bold' : 'normal'}
      color="inherit"
      minW={resolvedMinW}
      maxW={resolvedMaxW}
      w={resolvedW}
      transition="all 0.1s"
      sx={focusSx}
      _hover={interactionStyles._hover}
      _focus={interactionStyles._focus}
      _focusVisible={interactionStyles._focusVisible}
    />
  );
};

/**
 * Inline number input with tabular numerals for column alignment
 */
export const PrototypeNumberInput: React.FC<PrototypeInputProps> = ({
  blendWithRow = false,
  showBorder = false,
  sx,
  value,
  onChange,
  onFocus,
  onBlur,
  ...props
}) => {
  const interactionStyles = getInlineInputInteractionStyles(blendWithRow, showBorder);
  const focusSx = blendWithRow ? TREE_ROW_INPUT_FOCUS_SX : undefined;
  const [draft, setDraft] = useState<string | null>(null);

  const resolvedValue: string | number | undefined = Array.isArray(value)
    ? value[0]
    : value;

  const displayValue = draft !== null ? draft : resolveNumericInputDisplayValue(resolvedValue);

  const handleFocus = (event: React.FocusEvent<HTMLInputElement>) => {
    setDraft(resolveNumericInputDisplayValue(resolvedValue));
    onFocus?.(event);
  };

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const sanitized = sanitizeNumericInput(event.target.value);
    setDraft(sanitized);
    const syntheticEvent = {
      ...event,
      target: { ...event.target, value: sanitized },
      currentTarget: { ...event.currentTarget, value: sanitized },
    } as React.ChangeEvent<HTMLInputElement>;
    onChange?.(syntheticEvent);
  };

  const handleBlur = (event: React.FocusEvent<HTMLInputElement>) => {
    const raw = draft ?? resolveNumericInputDisplayValue(resolvedValue);
    const parsed = parseNumericInput(raw);
    if (parsed !== null) {
      const formatted = formatDecimalInput(parsed);
      if (formatted !== raw) {
        const syntheticEvent = {
          ...event,
          target: { ...event.target, value: formatted },
          currentTarget: { ...event.currentTarget, value: formatted },
        } as React.ChangeEvent<HTMLInputElement>;
        onChange?.(syntheticEvent);
      }
    } else if (raw.trim() === '' && resolvedValue !== '' && resolvedValue !== undefined && resolvedValue !== null) {
      const syntheticEvent = {
        ...event,
        target: { ...event.target, value: '' },
        currentTarget: { ...event.currentTarget, value: '' },
      } as React.ChangeEvent<HTMLInputElement>;
      onChange?.(syntheticEvent);
    }
    setDraft(null);
    onBlur?.(event);
  };

  return (
    <Input
      type="text"
      inputMode="decimal"
      {...props}
      value={displayValue}
      onChange={handleChange}
      onFocus={handleFocus}
      onBlur={handleBlur}
      variant={blendWithRow && !showBorder ? 'unstyled' : 'outline'}
      border="1px solid"
      borderColor={showBorder ? 'neutral.200' : 'transparent'}
      bg={showBorder ? 'white' : 'transparent'}
      borderRadius="8px"
      px="10px"
      py="7px"
      fontSize="sm"
      color="inherit"
      textAlign="right"
      w="full"
      transition="all 0.1s"
      sx={{ fontVariantNumeric: 'tabular-nums', ...focusSx, ...sx }}
      _hover={interactionStyles._hover}
      _focus={interactionStyles._focus}
      _focusVisible={interactionStyles._focusVisible}
    />
  );
};

/**
 * Inline date input — re-eksport z osobnego pliku (stabilniejszy HMR).
 */
export { PrototypeDateInput } from './PrototypeDateInput';

/**
 * Prototype-styled badge/tag for hierarchy levels
 * Uses Chakra color tokens: primary (blue), level2 (purple), action (teal), warning (orange)
 */
export const PrototypeTag: React.FC<{
  level: number;
  label?: string;
}> = ({ level, label }) => {
  // Map hierarchy levels to Chakra color tokens
  const colorTokens: Record<number, { bg: string; color: string }> = {
    0: { bg: 'primary.100', color: 'primary.600' },   // Etap (blue — dark parent)
    1: { bg: 'primary.50', color: 'primary.500' },     // Podetap (blue — light child)
    2: { bg: 'level1.100', color: 'level1.700' },      // Pozycja (green — dark parent)
    3: { bg: 'level1.50', color: 'level1.600' },       // Komponent (green — light child)
    4: { bg: 'level2.100', color: 'level2.600' },      // Opcja (purple)
  };

  const labels: Record<number, string> = { 0: 'ETAP', 1: 'PODETAP', 2: 'POZYCJA', 3: 'KOMPONENT', 4: 'OPCJA' };
  const style = colorTokens[level] || colorTokens[4];

  return (
    <Box
      as="span"
      display="inline-flex"
      alignItems="center"
      justifyContent="center"
      h="21px"
      px={2}
      borderRadius="6px"
      fontSize="2xs"
      fontWeight="bold"
      textTransform="uppercase"
      letterSpacing="0.04em"
      bg={style.bg}
      color={style.color}
      whiteSpace="nowrap"
    >
      {label || labels[level] || 'INNE'}
    </Box>
  );
};

/**
 * Colored dot for hierarchy visualization
 * Uses Chakra color tokens
 */
export const PrototypeDot: React.FC<{
  level: number;
  size?: number;
}> = ({ level, size = 8 }) => {
  const colorTokens: Record<number, string> = {
    0: 'primary.600',   // Etap (blue — dark parent)
    1: 'primary.500',   // Podetap (blue — light child)
    2: 'level1.700',    // Pozycja (green — dark parent)
    3: 'level1.600',    // Komponent (green — light child)
    4: 'level2.600',    // Opcja (purple)
  };

  const color = colorTokens[level] || colorTokens[4];

  return (
    <span
      style={{
        width: `${size}px`,
        height: `${size}px`,
        borderRadius: level === 3 ? '2px' : '50%',
        background: `var(--chakra-colors-${color.replace('.', '-')})`,
        flex: `0 0 ${size}px`,
      }}
    />
  );
};
