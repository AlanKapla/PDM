/**
 * Visual radio button for cost estimate option selection.
 */

import React from 'react';
import { Box } from '@chakra-ui/react';

interface OptionRadioButtonProps {
  isSelected: boolean;
  isDisabled?: boolean;
  onSelect: () => void;
  size?: 'sm' | 'md';
}

const SIZE_MAP = {
  sm: { outer: '16px', inner: '6px' },
  md: { outer: '18px', inner: '7px' },
} as const;

export const OptionRadioButton: React.FC<OptionRadioButtonProps> = ({
  isSelected,
  isDisabled = false,
  onSelect,
  size = 'md',
}) => {
  const dimensions = SIZE_MAP[size];

  return (
    <Box
      as="button"
      type="button"
      w={dimensions.outer}
      h={dimensions.outer}
      minW={dimensions.outer}
      borderRadius="50%"
      border="2px solid"
      borderColor={isSelected ? 'primary.500' : 'neutral.400'}
      bg={isSelected ? 'primary.500' : 'white'}
      display="flex"
      alignItems="center"
      justifyContent="center"
      flexShrink={0}
      onClick={(e: React.MouseEvent) => {
        e.stopPropagation();
        onSelect();
      }}
      disabled={isDisabled}
      aria-label="Wybierz opcję"
      aria-checked={isSelected}
      role="radio"
      _hover={isDisabled ? undefined : { borderColor: 'primary.500' }}
      _disabled={{ opacity: 0.6, cursor: 'not-allowed' }}
    >
      {isSelected && (
        <Box w={dimensions.inner} h={dimensions.inner} borderRadius="50%" bg="white" />
      )}
    </Box>
  );
};
