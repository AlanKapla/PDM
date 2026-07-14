import React, { useState, useEffect } from 'react';
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalCloseButton,
  VStack,
  HStack,
  Button,
  Text,
  Box,
} from '@chakra-ui/react';
import { Plus, Save } from 'lucide-react';
import { useToastNotification } from '../../../hooks/useToastNotification';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import { arrayMove, SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy } from '@dnd-kit/sortable';
import type { DragEndEvent } from '@dnd-kit/core';
import type { CostEstimateFieldSchemaWeb } from '../../../types/costEstimate.types.new';
import { FieldDefinitionList } from './FieldDefinitionList';
import { AddFieldModal } from './AddFieldModal';
import {
  reorderAdditionalFields,
  updateAdditionalField,
  deleteAdditionalField,
} from '../../../api/costEstimateApi';

interface SchemaManagerModalProps {
  isOpen: boolean;
  onClose: () => void;
  fieldSchemas: CostEstimateFieldSchemaWeb[];
  costEstimateId: string;
  tenantId: string;
  projectId: string;
  onSchemaUpdated: () => void;
  isReadOnly?: boolean;
}

export const SchemaManagerModal: React.FC<SchemaManagerModalProps> = ({
  isOpen,
  onClose,
  fieldSchemas,
  costEstimateId,
  tenantId,
  projectId,
  onSchemaUpdated,
  isReadOnly = false,
}) => {
  const { showSuccess, showApiError } = useToastNotification();
  const [isAddFieldOpen, setIsAddFieldOpen] = useState(false);
  const [localFields, setLocalFields] = useState<CostEstimateFieldSchemaWeb[]>(
    () => [...fieldSchemas].sort((a, b) => a.order - b.order)
  );
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setLocalFields([...fieldSchemas].sort((a, b) => a.order - b.order));
    }
  }, [isOpen, fieldSchemas]);

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const handleDragEnd = (event: DragEndEvent): void => {
    const { active, over } = event;

    if (!over || active.id === over.id) {
      return;
    }

    setLocalFields((fields) => {
      const oldIndex = fields.findIndex((f) => f.id === active.id);
      const newIndex = fields.findIndex((f) => f.id === over.id);
      return arrayMove(fields, oldIndex, newIndex);
    });
  };

  const handleRenameField = async (fieldId: string, newName: string): Promise<void> => {
    try {
      await updateAdditionalField(tenantId, projectId, costEstimateId, fieldId, {
        name: newName,
      });

      setLocalFields((fields) =>
        fields.map((f) => (f.id === fieldId ? { ...f, fieldName: newName } : f))
      );

      showSuccess('Nazwa zmieniona');
      onSchemaUpdated();
    } catch (error) {
      showApiError(error);
    }
  };

  const handleDeleteField = async (fieldId: string): Promise<void> => {
    try {
      await deleteAdditionalField(tenantId, projectId, costEstimateId, fieldId);

      setLocalFields((fields) => fields.filter((f) => f.id !== fieldId));

      showSuccess('Pole usunięte');
      onSchemaUpdated();
    } catch (error) {
      showApiError(error);
    }
  };

  const handleSaveOrder = async (): Promise<void> => {
    setIsSaving(true);
    try {
      const fieldIds = localFields.map((f) => f.id);
      await reorderAdditionalFields(tenantId, projectId, costEstimateId, fieldIds);

      showSuccess('Kolejność zapisana');
      onSchemaUpdated();
    } catch (error) {
      showApiError(error);
    } finally {
      setIsSaving(false);
    }
  };

  const handleFieldAdded = (): void => {
    setIsAddFieldOpen(false);
    onSchemaUpdated();
  };

  return (
    <>
      <Modal isOpen={isOpen} onClose={onClose} size="2xl" scrollBehavior="inside">
        <ModalOverlay />
        <ModalContent maxH="90vh">
          <ModalHeader pr={12}>
            Zarządzanie kolumnami kosztorysu
          </ModalHeader>
          <ModalCloseButton />
          <ModalBody pb={6}>
            {!isReadOnly && (
              <HStack spacing={2} justify="flex-end" mb={4}>
                <Button
                  leftIcon={<Plus size={16} />}
                  size="sm"
                  colorScheme="primary"
                  onClick={() => setIsAddFieldOpen(true)}
                >
                  Dodaj pole
                </Button>
                <Button
                  leftIcon={<Save size={16} />}
                  size="sm"
                  colorScheme="green"
                  onClick={handleSaveOrder}
                  isLoading={isSaving}
                >
                  Zapisz kolejność
                </Button>
              </HStack>
            )}
            <VStack spacing={4} align="stretch">
              {localFields.length === 0 ? (
                <Box py={6} textAlign="center">
                  <Text fontSize="sm" color="neutral.500" fontStyle="italic">
                    Brak zdefiniowanych kolumn. Kliknij &quot;Dodaj pole&quot; aby dodać własną kolumnę.
                  </Text>
                </Box>
              ) : (
                <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
                  <SortableContext items={localFields.map((f) => f.id)} strategy={verticalListSortingStrategy}>
                    <FieldDefinitionList
                      fields={localFields}
                      onRenameField={handleRenameField}
                      onDeleteField={handleDeleteField}
                      isReadOnly={isReadOnly}
                    />
                  </SortableContext>
                </DndContext>
              )}
            </VStack>
          </ModalBody>
        </ModalContent>
      </Modal>

      {isAddFieldOpen && (
        <AddFieldModal
          isOpen={isAddFieldOpen}
          onClose={() => setIsAddFieldOpen(false)}
          costEstimateId={costEstimateId}
          tenantId={tenantId}
          projectId={projectId}
          onFieldAdded={handleFieldAdded}
        />
      )}
    </>
  );
};
