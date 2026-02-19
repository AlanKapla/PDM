import React, { useState, useEffect, useRef } from 'react';
import {
  Box,
  Text,
  Button,
  Alert,
  AlertIcon,
  Spinner,
  Divider,
  Stack,
  Badge,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  Tooltip,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useDisclosure,
} from '@chakra-ui/react';
import {
  Save,
  X,
  Plus,
  RefreshCw,
  Trash2,
  ChevronRight,
  ChevronDown,
} from 'lucide-react';
import { type WorkScopeFieldDefinition } from './WorkScopeItemRow';
import { FieldValueInput, getFieldInputType } from './FieldValueInput';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupDto,
  CostEstimateItemDto,
  CostEstimateFieldValueDto,
  CostEstimateGroupFieldValueDto,
  UpdateCostEstimateDto,
} from '../../types/costEstimate.types.new';

/**
 * Field definition for group header
 */
export interface GroupHeaderFieldDefinition {
  id: string;
  label: string;
  fieldType: string;
  isRequired: boolean;
  allowedValues?: string[];
  order: number;
  helpText?: string;
  fieldTypeConfig?: import('../../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb;
}
import { CostEstimateStatus } from '../../types/costEstimate.types.new';
import { convertDetailsWebToUpdateDto, convertGroupWebToDto, createEmptyGroup } from '../../types/costEstimate.types.new';
import { costEstimateApiNew } from '../../api/costEstimateApiNew';

/**
 * Props for CostEstimateEditor component
 */
export interface CostEstimateEditorProps {
  /** Tenant ID */
  tenantId: string;
  /** Project ID */
  projectId: string;
  /** Cost estimate ID */
  costEstimateId: string;
  /** Whether editor is in readonly mode */
  readonly?: boolean;
  /** Callback when save is successful */
  onSaveSuccess?: () => void;
  /** Callback when cancel is clicked */
  onCancel?: () => void;
}

/**
 * CostEstimateEditor - Main editor component for cost estimate hierarchy
 * Manages state, loads data, validates, and saves changes
 */
export const CostEstimateEditor: React.FC<CostEstimateEditorProps> = ({
  tenantId,
  projectId,
  costEstimateId,
  readonly = false,
  onSaveSuccess,
  onCancel,
}) => {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [details, setDetails] = useState<CostEstimateDetailsWeb | null>(null);
  const [editedData, setEditedData] = useState<UpdateCostEstimateDto | null>(null);
  const [hasChanges, setHasChanges] = useState(false);
  
  // Modal usuwania grupy
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();
  const [groupToDelete, setGroupToDelete] = useState<number | null>(null);
  const cancelRef = useRef<HTMLButtonElement>(null);

  // Derive field definitions from template structure
  const headerFieldDefinitions: GroupHeaderFieldDefinition[] = details?.templateStructure?.groupHeaderFields?.map(f => ({
    id: f.id,
    label: f.customLabel || f.fieldName,
    fieldType: f.fieldTypeConfig?.valueTypeName?.toLowerCase() || 'string',
    isRequired: f.isRequired,
    order: f.order,
    allowedValues: f.allowedValues,
    fieldTypeConfig: f.fieldTypeConfig,
  })) || [];

  const workScopeFieldDefinitions: WorkScopeFieldDefinition[] = [
    ...(details?.templateStructure?.systemFields?.map(f => ({
      id: f.id,
      label: f.label,
      fieldType: f.fieldTypeConfig?.valueTypeName?.toLowerCase() || 'string',
      isRequired: f.isRequired,
      isReadOnly: false,
      order: f.order,
      valueType: 'system' as const,
      fieldTypeConfig: f.fieldTypeConfig,
    })) || []),
    ...(details?.templateStructure?.calculatedFields?.map(f => ({
      id: f.id,
      label: f.label,
      fieldType: f.fieldTypeConfig?.valueTypeName?.toLowerCase() || 'decimal',
      isRequired: f.isRequired,
      isReadOnly: f.isReadOnly,
      order: f.order,
      valueType: 'calculated' as const,
      unit: f.unit,
      fieldTypeConfig: f.fieldTypeConfig,
    })) || []),
    ...(details?.templateStructure?.genericFields?.map(f => ({
      id: f.id,
      label: f.label,
      fieldType: f.fieldTypeConfig?.valueTypeName?.toLowerCase() || 'string',
      isRequired: f.isRequired,
      isReadOnly: false,
      order: f.order,
      valueType: 'generic' as const,
      min: f.minValue,
      max: f.maxValue,
      allowedValues: f.allowedValues,
      fieldTypeConfig: f.fieldTypeConfig,
    })) || []),
  ].sort((a, b) => a.order - b.order);

  // Load cost estimate details
  useEffect(() => {
    loadDetails();
  }, [costEstimateId]);

  const loadDetails = async () => {
    try {
      setLoading(true);
      setError(null);

      const data = await costEstimateApiNew.getCostEstimateDetails(tenantId, projectId, costEstimateId);
      setDetails(data);

      // Initialize edited data
      const dto = convertDetailsWebToUpdateDto(data);
      setEditedData(dto);
      setHasChanges(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas ładowania kosztorysu');
      console.error('Error loading cost estimate details:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!editedData) return;

    try {
      setSaving(true);
      setError(null);

      await costEstimateApiNew.updateCostEstimate(tenantId, projectId, costEstimateId, editedData);

      // Reload details to get calculated values
      await loadDetails();
      
      setHasChanges(false);
      
      if (onSaveSuccess) {
        onSaveSuccess();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas zapisywania kosztorysu');
      console.error('Error saving cost estimate:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    if (hasChanges) {
      const confirm = window.confirm('Masz niezapisane zmiany. Czy na pewno chcesz anulować?');
      if (!confirm) return;
    }

    if (details) {
      // Reset to original data
      const dto = convertDetailsWebToUpdateDto(details);
      setEditedData(dto);
      setHasChanges(false);
    }

    if (onCancel) {
      onCancel();
    }
  };

  const handleAddRootGroup = () => {
    if (!editedData) return;

    const newGroup = createEmptyGroup(0, editedData.rootGroups.length);
    
    setEditedData({
      ...editedData,
      rootGroups: [...editedData.rootGroups, newGroup],
    });
    setHasChanges(true);
  };

  const handleUpdateGroup = (groupIndex: number, updatedGroup: CostEstimateGroupDto) => {
    if (!editedData) return;

    const newGroups = [...editedData.rootGroups];
    newGroups[groupIndex] = updatedGroup;
    
    setEditedData({
      ...editedData,
      rootGroups: newGroups,
    });
    setHasChanges(true);
  };

  const handleDeleteGroup = (groupIndex: number) => {
    if (!editedData) return;
    setGroupToDelete(groupIndex);
    onDeleteOpen();
  };

  const confirmDeleteGroup = () => {
    if (!editedData || groupToDelete === null) return;

    const newGroups = editedData.rootGroups.filter((_, idx) => idx !== groupToDelete);
    
    setEditedData({
      ...editedData,
      rootGroups: newGroups,
    });
    setHasChanges(true);
    setGroupToDelete(null);
    onDeleteClose();
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight={400}>
        <Spinner size="xl" />
      </Box>
    );
  }

  if (error && !details) {
    return (
      <Alert status="error" mb={2}>
        <AlertIcon />
        {error}
        <Button onClick={loadDetails} ml={2}>
          Spróbuj ponownie
        </Button>
      </Alert>
    );
  }

  if (!details || !editedData) {
    return (
      <Alert status="warning">
        <AlertIcon />
        Nie znaleziono kosztorysu
      </Alert>
    );
  }

  // Template config could be loaded from template structure if needed

  // Helper component for flat table rows
  interface GroupRowFlatProps {
    group: CostEstimateGroupDto;
    groupIndex: number;
    level: number;
    headerFieldDefinitions: GroupHeaderFieldDefinition[];
    workScopeFieldDefinitions: WorkScopeFieldDefinition[];
    onChange: (updated: CostEstimateGroupDto) => void;
    onDelete: () => void;
    readonly: boolean;
  }

  const GroupRowFlat: React.FC<GroupRowFlatProps> = ({
    group,
    groupIndex,
    level,
    headerFieldDefinitions,
    workScopeFieldDefinitions,
    onChange,
    onDelete,
    readonly,
  }) => {
    const [collapsed, setCollapsed] = useState(false);
    const indent = level * 24;

    const getGroupFieldValue = (fieldId: string): string | undefined => {
      const fieldValue = group.fieldValues.find((fv) => fv.fieldDefinitionId === fieldId);
      return fieldValue?.value;
    };

    const updateGroupFieldValue = (fieldId: string, value: string | undefined) => {
      const existingIndex = group.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
      const newFieldValues = [...group.fieldValues];

      if (existingIndex >= 0) {
        if (value === undefined || value === '') {
          newFieldValues.splice(existingIndex, 1);
        } else {
          newFieldValues[existingIndex] = {
            ...newFieldValues[existingIndex],
            value,
          };
        }
      } else if (value !== undefined && value !== '') {
        const newFieldValue: CostEstimateGroupFieldValueDto = {
          fieldDefinitionId: fieldId,
          value,
        };
        newFieldValues.push(newFieldValue);
      }

      onChange({
        ...group,
        fieldValues: newFieldValues,
      });
    };

    const getGroupName = (): string => {
      const nameField = headerFieldDefinitions.find(
        (f) => f.label.toLowerCase().includes('nazwa') || f.label.toLowerCase().includes('name')
      );
      const value = nameField ? getGroupFieldValue(nameField.id) : undefined;
      return value || `Grupa ${groupIndex + 1}`;
    };

    const handleAddWorkScopeItem = () => {
      const newItem: CostEstimateItemDto = {
        id: undefined,
        order: (group.items || []).length,
        relationType: 0,
        fieldValues: [],
      };
      onChange({
        ...group,
        items: [...(group.items || []), newItem],
      });
    };

    const handleUpdateWorkScopeItem = (itemIndex: number, updatedItem: CostEstimateItemDto) => {
      const newItems = [...(group.items || [])];
      newItems[itemIndex] = updatedItem;
      onChange({
        ...group,
        items: newItems,
      });
    };

    const handleDeleteWorkScopeItem = (itemIndex: number) => {
      const newItems = (group.items || []).filter((_: CostEstimateItemDto, idx: number) => idx !== itemIndex);
      onChange({
        ...group,
        items: newItems,
      });
    };

    const handleAddChildGroup = () => {
      const newChildGroup = createEmptyGroup(group.level + 1, group.childGroups.length, group.id);
      onChange({
        ...group,
        childGroups: [...group.childGroups, newChildGroup],
      });
    };

    const handleUpdateChildGroup = (childIndex: number, updatedChild: CostEstimateGroupDto) => {
      const newChildren = [...group.childGroups];
      newChildren[childIndex] = updatedChild;
      onChange({
        ...group,
        childGroups: newChildren,
      });
    };

    const handleDeleteChildGroup = (childIndex: number) => {
      const newChildren = group.childGroups.filter((_, idx) => idx !== childIndex);
      onChange({
        ...group,
        childGroups: newChildren,
      });
    };

    const getItemFieldValue = (
      item: CostEstimateItemDto,
      fieldDef: WorkScopeFieldDefinition
    ): string | undefined => {
      const fieldValue = item.fieldValues.find((fv: CostEstimateFieldValueDto) => {
        return fv.fieldDefinitionId === fieldDef.id;
      });
      return fieldValue?.value;
    };

    const updateItemFieldValue = (
      item: CostEstimateItemDto,
      fieldDef: WorkScopeFieldDefinition,
      value: string | undefined
    ): CostEstimateItemDto => {
      const values = [...item.fieldValues];
      const idx = values.findIndex((fv: CostEstimateFieldValueDto) => {
        return fv.fieldDefinitionId === fieldDef.id;
      });

      if (idx >= 0) {
        if (!value) {
          values.splice(idx, 1);
        } else {
          values[idx] = { ...values[idx], value } as CostEstimateFieldValueDto;
        }
      } else if (value) {
        const newValue: CostEstimateFieldValueDto = { 
          fieldDefinitionId: fieldDef.id,
          value 
        };
        values.push(newValue);
      }

      return { ...item, fieldValues: values };
    };

    return (
      <>
        <Tr _hover={{ bg: 'gray.50' }}>
          {/* Left actions */}
          {!readonly && (
            <Td p={1}>
              <Stack direction="row" spacing={1}>
                <Tooltip label="Dodaj pozycję">
                  <IconButton
                    size="xs"
                    variant="ghost"
                    colorScheme="green"
                    onClick={handleAddWorkScopeItem}
                    aria-label="Dodaj pozycję"
                    icon={<Plus size={14} />}
                  />
                </Tooltip>
                <Tooltip label="Dodaj podgrupę">
                  <IconButton
                    size="xs"
                    variant="ghost"
                    colorScheme="blue"
                    onClick={handleAddChildGroup}
                    aria-label="Dodaj podgrupę"
                    icon={<Plus size={14} />}
                  />
                </Tooltip>
              </Stack>
            </Td>
          )}
          {/* Collapse */}
          <Td p={1} pl={`${indent + 8}px`}>
            <Tooltip label={collapsed ? 'Rozwiń grupę' : 'Zwiń grupę'}>
              <IconButton
                size="xs"
                variant="ghost"
                onClick={() => setCollapsed(!collapsed)}
                aria-label={collapsed ? 'Rozwiń' : 'Zwiń'}
                icon={collapsed ? <ChevronRight size={14} /> : <ChevronDown size={14} />}
              />
            </Tooltip>
          </Td>

          {/* Header fields */}
          {headerFieldDefinitions.map((fieldDef) => {
            const value = getGroupFieldValue(fieldDef.id);
            const inputType = getFieldInputType(
              fieldDef.fieldTypeConfig || fieldDef.fieldType,
              fieldDef.allowedValues
            );
            return (
              <Td key={fieldDef.id} p={1}>
                <FieldValueInput
                  label=""
                  value={value}
                  type={inputType}
                  onChange={(newValue) => updateGroupFieldValue(fieldDef.id, newValue)}
                  required={fieldDef.isRequired}
                  disabled={readonly}
                  allowedValues={fieldDef.allowedValues}
                  helpText={fieldDef.helpText}
                  size="small"
                />
              </Td>
            );
          })}

          {/* Right actions */}
          {!readonly && (
            <Td>
              <Stack direction="row" spacing={1}>
                <Tooltip label="Usuń grupę">
                  <IconButton
                    size="xs"
                    variant="ghost"
                    colorScheme="red"
                    onClick={onDelete}
                    aria-label="Usuń grupę"
                    icon={<Trash2 size={14} />}
                  />
                </Tooltip>
              </Stack>
            </Td>
          )}
        </Tr>

        {/* Work Scope Items */}
        {!collapsed &&
          (group.items || []).map((item: CostEstimateItemDto, itemIndex: number) => (
            <Tr key={`item-${itemIndex}`} _hover={{ bg: 'gray.50' }}>
              {/* Index */}
              <Td p={1} pl={`${indent + 32}px`} width="40px">
                <Text fontSize="xs" color="gray.500">{itemIndex + 1}</Text>
              </Td>
              {/* Placeholder for label */}
              <Td p={1}>
                <Text fontSize="sm" color="gray.700">Pozycja {itemIndex + 1}</Text>
              </Td>
              {/* Empty cells for header fields */}
              {headerFieldDefinitions.map((fieldDef) => (
                <Td key={`empty-${fieldDef.id}`} p={1} bg="gray.50">
                  <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">
                    —
                  </Text>
                </Td>
              ))}
              {/* Work scope fields */}
              {workScopeFieldDefinitions.map((fieldDef) => {
                const value = getItemFieldValue(item, fieldDef);
                const inputType = getFieldInputType(
                  fieldDef.fieldTypeConfig || fieldDef.fieldType,
                  fieldDef.allowedValues
                );
                return (
                  <Td key={fieldDef.id} p={1}>
                    <FieldValueInput
                      label=""
                      value={value}
                      type={inputType}
                      onChange={(newValue) => {
                        const updated = updateItemFieldValue(item, fieldDef, newValue);
                        handleUpdateWorkScopeItem(itemIndex, updated);
                      }}
                      required={fieldDef.isRequired}
                      disabled={readonly || fieldDef.isReadOnly}
                      allowedValues={fieldDef.allowedValues}
                      min={fieldDef.min}
                      max={fieldDef.max}
                      unit={fieldDef.unit}
                      helpText={fieldDef.helpText}
                      size="small"
                    />
                  </Td>
                );
              })}
              {/* Item actions */}
              {!readonly && (
                <Td p={1}>
                  <Tooltip label="Usuń pozycję">
                    <IconButton
                      size="xs"
                      variant="ghost"
                      colorScheme="red"
                      onClick={() => handleDeleteWorkScopeItem(itemIndex)}
                      aria-label="Usuń pozycję"
                      icon={<Trash2 size={14} />}
                    />
                  </Tooltip>
                </Td>
              )}
            </Tr>
          ))}

        {/* Child groups */}
        {!collapsed &&
          group.childGroups.map((childGroup, childIndex) => (
            <GroupRowFlat
              key={`child-${childIndex}`}
              group={childGroup}
              groupIndex={childIndex}
              level={level + 1}
              headerFieldDefinitions={headerFieldDefinitions}
              workScopeFieldDefinitions={workScopeFieldDefinitions}
              onChange={(updated) => handleUpdateChildGroup(childIndex, updated)}
              onDelete={() => handleDeleteChildGroup(childIndex)}
              readonly={readonly}
            />
          ))}
      </>
    );
  };

  return (
    <Box>
      {/* Header */}
      <Box p={4} mb={4} bg="white" borderRadius="md" shadow="sm">
        <Stack direction="row" spacing={2} alignItems="center" mb={2}>
          <Box flex={1}>
            <Text fontSize="xl" fontWeight="bold" mb={2}>
              {details.name}
            </Text>
            <Text fontSize="sm" color="gray.600">
              Szablon: {details.templateName} • Wersja: {details.templateVersionNumber}
            </Text>
          </Box>
          
          <Badge 
            colorScheme={details.status === CostEstimateStatus.Draft ? 'gray' : 'blue'}
          >
            Status: {details.status}
          </Badge>
        </Stack>

        <Divider my={2} />

        {/* Summary */}
        <Stack direction="row" spacing={4}>
          <Box>
            <Text fontSize="xs" color="gray.600">
              Wartość netto
            </Text>
            <Text fontSize="xl" fontWeight="semibold">
              {details.totalNet?.toFixed(2) || '0.00'} {details.selectedCurrencyCode}
            </Text>
          </Box>
          <Box>
            <Text fontSize="xs" color="gray.600">
              VAT
            </Text>
            <Text fontSize="xl" fontWeight="semibold">
              {details.totalVat?.toFixed(2) || '0.00'} {details.selectedCurrencyCode}
            </Text>
          </Box>
          <Box>
            <Text fontSize="xs" color="gray.600">
              Wartość brutto
            </Text>
            <Text fontSize="xl" fontWeight="semibold" color="blue.600">
              {details.totalGross?.toFixed(2) || '0.00'} {details.selectedCurrencyCode}
            </Text>
          </Box>
        </Stack>

        {/* Action buttons */}
        {!readonly && (
          <>
            <Divider my={2} />
            
            <Stack direction="row" spacing={2}>
              <Button
                colorScheme="blue"
                leftIcon={saving ? <Spinner size="sm" /> : <Save size={16} />}
                onClick={handleSave}
                isDisabled={!hasChanges || saving}
              >
                {saving ? 'Zapisywanie...' : 'Zapisz zmiany'}
              </Button>
              
              <Button
                variant="outline"
                leftIcon={<X size={16} />}
                onClick={handleCancel}
                isDisabled={saving}
              >
                Anuluj
              </Button>
              
              <Button
                variant="outline"
                leftIcon={<RefreshCw size={16} />}
                onClick={loadDetails}
                isDisabled={saving}
              >
                Odśwież
              </Button>
              
              <Box flex={1} />
              
              <Button
                variant="outline"
                colorScheme="blue"
                leftIcon={<Plus size={16} />}
                onClick={handleAddRootGroup}
                isDisabled={saving}
              >
                Dodaj grupę główną
              </Button>
            </Stack>
          </>
        )}

        {hasChanges && !readonly && (
          <Alert status="info" mt={2}>
            <AlertIcon />
            Masz niezapisane zmiany. Kliknij "Zapisz zmiany" aby zapisać.
          </Alert>
        )}

        {error && (
          <Alert status="error" mt={2}>
            <AlertIcon />
            {error}
          </Alert>
        )}
      </Box>

      {/* Groups and Work Scope Items in Excel-style Table */}
      <Box>
        {editedData.rootGroups.length === 0 ? (
          <Box p={4} textAlign="center" bg="white" borderRadius="md" shadow="sm">
            <Text fontSize="lg" fontWeight="semibold" color="gray.600" mb={2}>
              Brak grup w kosztorysie
            </Text>
            <Text fontSize="sm" color="gray.600" mb={4}>
              Kliknij "Dodaj grupę główną" aby utworzyć pierwszą grupę.
            </Text>
            {!readonly && (
              <Button
                colorScheme="blue"
                leftIcon={<Plus size={16} />}
                onClick={handleAddRootGroup}
              >
                Dodaj grupę główną
              </Button>
            )}
          </Box>
        ) : (
          <Box bg="white" borderRadius="md" shadow="sm" overflowX="auto">
            <Table size="sm" variant="simple">
              <Thead bg="gray.50">
                <Tr>
                  {!readonly && <Th width="100px">Akcje</Th>}
                  <Th width="40px"></Th>
                  {headerFieldDefinitions.map((fieldDef) => (
                    <Th key={fieldDef.id} minW="150px">
                      {fieldDef.label}
                      {fieldDef.isRequired && <Text as="span" color="red.500"> *</Text>}
                    </Th>
                  ))}
                  {workScopeFieldDefinitions.map((fieldDef) => (
                    <Th key={fieldDef.id} minW="150px">
                      {fieldDef.label}
                      {fieldDef.isRequired && <Text as="span" color="red.500"> *</Text>}
                      {fieldDef.unit && <Text as="span" fontSize="xs" color="gray.500"> ({fieldDef.unit})</Text>}
                    </Th>
                  ))}
                  {!readonly && <Th width="100px">Akcje</Th>}
                </Tr>
              </Thead>
              <Tbody>
                {editedData.rootGroups.map((group, groupIndex) => (
                  <React.Fragment key={groupIndex}>
                    <GroupRowFlat
                      group={group}
                      groupIndex={groupIndex}
                      level={0}
                      headerFieldDefinitions={headerFieldDefinitions}
                      workScopeFieldDefinitions={workScopeFieldDefinitions}
                      onChange={(updated) => handleUpdateGroup(groupIndex, updated)}
                      onDelete={() => handleDeleteGroup(groupIndex)}
                      readonly={readonly}
                    />
                  </React.Fragment>
                ))}
              </Tbody>
            </Table>
          </Box>
        )}
      </Box>

      {/* Modal potwierdzenia usunięcia grupy */}
      <AlertDialog
        isOpen={isDeleteOpen}
        leastDestructiveRef={cancelRef}
        onClose={onDeleteClose}
        isCentered
      >
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader fontSize="lg" fontWeight="bold" display="flex" alignItems="center" gap={2}>
              <Trash2 size={20} color="var(--chakra-colors-red-500)" />
              Usuń grupę
            </AlertDialogHeader>

            <AlertDialogBody>
              <Text mb={2}>
                Czy na pewno chcesz usunąć tę grupę?
              </Text>
              <Text fontSize="sm" color="gray.600">
                Wszystkie podgrupy i pozycje w tej grupie zostaną trwale usunięte. 
                Tej operacji nie można cofnąć.
              </Text>
            </AlertDialogBody>

            <AlertDialogFooter gap={3}>
              <Button ref={cancelRef} onClick={onDeleteClose}>
                Anuluj
              </Button>
              <Button colorScheme="red" onClick={confirmDeleteGroup} leftIcon={<Trash2 size={16} />}>
                Usuń grupę
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </Box>
  );
};
