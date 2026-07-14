import React from 'react';
import { Input } from '@chakra-ui/react';

interface PrototypeDateInputProps extends Omit<React.ComponentProps<typeof Input>, 'variant'> {
  blendWithRow?: boolean;
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

/** Inline date input (natywny date picker) w stylu prototype. */
export const PrototypeDateInput: React.FC<PrototypeDateInputProps> = ({
  blendWithRow = false,
  showBorder = false,
  ...props
}) => {
  const interactionStyles = getInlineInputInteractionStyles(blendWithRow, showBorder);
  const focusSx = blendWithRow ? TREE_ROW_INPUT_FOCUS_SX : undefined;

  return (
    <Input
      type="date"
      {...props}
      variant={blendWithRow && !showBorder ? 'unstyled' : 'outline'}
      border="1px solid"
      borderColor={showBorder ? 'neutral.200' : 'transparent'}
      bg={showBorder ? 'white' : 'transparent'}
      borderRadius="8px"
      px="10px"
      py="7px"
      fontSize="sm"
      color="inherit"
      minW="60px"
      w="full"
      transition="all 0.1s"
      sx={focusSx}
      _hover={interactionStyles._hover}
      _focus={interactionStyles._focus}
      _focusVisible={interactionStyles._focusVisible}
    />
  );
};
