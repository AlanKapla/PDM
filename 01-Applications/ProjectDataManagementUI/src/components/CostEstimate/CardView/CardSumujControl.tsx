import React from 'react';
import { Checkbox, Text, Tooltip, VStack } from '@chakra-ui/react';

interface CardSumujControlProps {
  isChecked: boolean;
  isDisabled: boolean;
  onChange: (checked: boolean) => void;
}

export function CardSumujControl({
  isChecked,
  isDisabled,
  onChange,
}: CardSumujControlProps): React.ReactElement {
  return (
    <VStack spacing={0.5} align="center" minW="44px">
      <Text
        fontSize="2xs"
        fontWeight="semibold"
        color="neutral.500"
        textTransform="uppercase"
        letterSpacing="0.04em"
        lineHeight="1"
      >
        Sumuj
      </Text>
      <Tooltip label={isChecked ? 'Wyłącz z sumy' : 'Uwzględnij w sumie'}>
        <Checkbox
          isChecked={isChecked}
          onChange={(e) => onChange(e.target.checked)}
          isDisabled={isDisabled}
          colorScheme="primary"
          size="sm"
          aria-label="Sumuj"
        />
      </Tooltip>
    </VStack>
  );
}
