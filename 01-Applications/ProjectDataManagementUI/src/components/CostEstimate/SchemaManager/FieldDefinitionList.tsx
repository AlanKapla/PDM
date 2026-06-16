import React from 'react';
import { VStack } from '@chakra-ui/react';
import type { CostEstimateFieldSchemaWeb } from '../../../types/costEstimate.types.new';
import { FieldDefinitionRow } from './FieldDefinitionRow';

interface FieldDefinitionListProps {
  fields: CostEstimateFieldSchemaWeb[];
  onRenameField: (fieldId: string, newName: string) => void;
  onDeleteField: (fieldId: string) => void;
  isReadOnly: boolean;
}

export const FieldDefinitionList: React.FC<FieldDefinitionListProps> = ({
  fields,
  onRenameField,
  onDeleteField,
  isReadOnly,
}) => {
  return (
    <VStack spacing={2} align="stretch">
      {fields.map((field) => (
        <FieldDefinitionRow
          key={field.id}
          field={field}
          onRenameField={onRenameField}
          onDeleteField={onDeleteField}
          isReadOnly={isReadOnly}
        />
      ))}
    </VStack>
  );
};
