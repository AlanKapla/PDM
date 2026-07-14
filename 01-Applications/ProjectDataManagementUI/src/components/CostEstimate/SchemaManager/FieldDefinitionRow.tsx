import React, { useState } from 'react';
import {
  Box,
  HStack,
  Text,
  IconButton,
  Input,
  Tooltip,
  Badge,
  useDisclosure,
} from '@chakra-ui/react';
import { GripVertical, Edit2, Trash2, Check, X } from 'lucide-react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { CostEstimateFieldSchemaWeb } from '../../../types/costEstimate.types.new';
import { AdditionalFieldType, CostEstimateFieldType } from '../../../types/costEstimate.types.new';
import DeleteAlertDialog from '../../ui/DeleteAlertDialog';

interface FieldDefinitionRowProps {
  field: CostEstimateFieldSchemaWeb;
  onRenameField: (fieldId: string, newName: string) => void;
  onDeleteField: (fieldId: string) => void;
  isReadOnly: boolean;
}

function getFieldTypeLabel(field: CostEstimateFieldSchemaWeb): string {
  if (field.isBasicField) {
    return 'Podstawowe';
  }

  const fieldType = field.fieldType as number;
  switch (fieldType) {
    case AdditionalFieldType.String:
    case CostEstimateFieldType.Text:
      return 'Tekst';
    case AdditionalFieldType.Decimal:
    case CostEstimateFieldType.Number:
      return 'Liczba';
    case AdditionalFieldType.Boolean:
    case CostEstimateFieldType.Boolean:
      return 'Tak/Nie';
    case AdditionalFieldType.DateTime:
    case CostEstimateFieldType.Date:
      return 'Data';
    default:
      return 'Dodatkowe';
  }
}

function getFieldTypeColor(field: CostEstimateFieldSchemaWeb): string {
  if (field.isBasicField) {
    return 'gray';
  }

  const fieldType = field.fieldType as number;
  switch (fieldType) {
    case AdditionalFieldType.Decimal:
    case CostEstimateFieldType.Number:
      return 'green';
    case AdditionalFieldType.Boolean:
    case CostEstimateFieldType.Boolean:
      return 'orange';
    case AdditionalFieldType.DateTime:
    case CostEstimateFieldType.Date:
      return 'purple';
    default:
      return 'blue';
  }
}

export const FieldDefinitionRow: React.FC<FieldDefinitionRowProps> = ({
  field,
  onRenameField,
  onDeleteField,
  isReadOnly,
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editedName, setEditedName] = useState(field.fieldName);
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: field.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  const handleSaveEdit = (): void => {
    if (editedName.trim() && editedName !== field.fieldName) {
      onRenameField(field.id, editedName.trim());
    }
    setIsEditing(false);
  };

  const handleCancelEdit = (): void => {
    setEditedName(field.fieldName);
    setIsEditing(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent): void => {
    if (e.key === 'Enter') {
      handleSaveEdit();
    } else if (e.key === 'Escape') {
      handleCancelEdit();
    }
  };

  const fieldTypeLabel = getFieldTypeLabel(field);
  const fieldTypeColor = getFieldTypeColor(field);
  const canDelete = !field.isBasicField && !isReadOnly;

  return (
    <>
      <Box
        ref={setNodeRef}
        style={style}
        bg="white"
        border="1px solid"
        borderColor="neutral.200"
        borderRadius="md"
        p={3}
        _hover={{ borderColor: 'primary.300', boxShadow: 'sm' }}
      >
        <HStack spacing={3}>
          {!isReadOnly && (
            <Box
              {...attributes}
              {...listeners}
              cursor="grab"
              _active={{ cursor: 'grabbing' }}
              color="neutral.400"
              _hover={{ color: 'neutral.600' }}
              aria-label="Przeciągnij aby zmienić kolejność"
            >
              <GripVertical size={20} />
            </Box>
          )}

          <Badge colorScheme={fieldTypeColor} fontSize="xs" flexShrink={0}>
            {fieldTypeLabel}
          </Badge>

          <Box flex={1}>
            {isEditing ? (
              <HStack>
                <Input
                  value={editedName}
                  onChange={(e) => setEditedName(e.target.value)}
                  onKeyDown={handleKeyDown}
                  size="sm"
                  autoFocus
                />
                <IconButton
                  aria-label="Zapisz"
                  icon={<Check size={16} />}
                  size="sm"
                  colorScheme="green"
                  onClick={handleSaveEdit}
                />
                <IconButton
                  aria-label="Anuluj"
                  icon={<X size={16} />}
                  size="sm"
                  variant="ghost"
                  onClick={handleCancelEdit}
                />
              </HStack>
            ) : (
              <Text fontWeight="medium" fontSize="sm">
                {field.fieldName}
              </Text>
            )}
          </Box>

          <Text fontSize="xs" color="neutral.400" flexShrink={0}>
            #{field.order + 1}
          </Text>

          {!isReadOnly && (
            <HStack spacing={1}>
              {!isEditing && (
                <Tooltip label="Zmień nazwę">
                  <IconButton
                    aria-label="Zmień nazwę pola"
                    icon={<Edit2 size={16} />}
                    size="sm"
                    variant="ghost"
                    colorScheme="blue"
                    onClick={() => setIsEditing(true)}
                  />
                </Tooltip>
              )}

              {canDelete && (
                <Tooltip label="Usuń pole">
                  <IconButton
                    aria-label="Usuń pole"
                    icon={<Trash2 size={16} />}
                    size="sm"
                    variant="ghost"
                    colorScheme="red"
                    onClick={onDeleteOpen}
                  />
                </Tooltip>
              )}
            </HStack>
          )}
        </HStack>
      </Box>

      <DeleteAlertDialog
        isOpen={isDeleteOpen}
        onClose={onDeleteClose}
        onConfirm={() => {
          onDeleteField(field.id);
          onDeleteClose();
        }}
        itemName={field.fieldName}
      />
    </>
  );
};
