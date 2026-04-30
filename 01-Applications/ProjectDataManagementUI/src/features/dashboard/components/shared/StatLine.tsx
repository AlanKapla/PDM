import React from 'react';
import { Text } from '@chakra-ui/react';

export interface StatLineProps {
  label: string;
  value: string;
  color?: string;
}

/** Wiersz etykieta: wartość — inline, fontSize 11px. */
export function StatLine({ label, value, color }: StatLineProps): React.ReactElement {
  return (
    <Text fontSize="xs" lineHeight="shorter">
      <Text as="span" color="neutral.400">{label}: </Text>
      <Text as="span" color={color ?? "gray.800"} fontWeight="medium">{value}</Text>
    </Text>
  );
}

export default StatLine;
