import React from 'react';
import { Text, type TextProps } from '@chakra-ui/react';
import { getFieldLabelByKey } from '../../../utils/costEstimateFieldSchema';
import type { ColumnDef } from '../TreeView/costEstimateColumnTypes';

interface CardNameTextProps extends TextProps {
  name: string | null | undefined;
  schemaColumns: ColumnDef[];
}

/** Nazwa na karcie — pusta wartość pokazuje etykietę pola ze schematu (jak placeholder w Tree View). */
export function CardNameText({
  name,
  schemaColumns,
  ...textProps
}: CardNameTextProps): React.ReactElement {
  const displayName: string | undefined = name?.trim() || undefined;
  const fieldLabel: string = getFieldLabelByKey(schemaColumns, 'name');

  return (
    <Text
      color={displayName ? undefined : 'neutral.500'}
      fontStyle={displayName ? undefined : 'italic'}
      {...textProps}
    >
      {displayName ?? fieldLabel}
    </Text>
  );
}
