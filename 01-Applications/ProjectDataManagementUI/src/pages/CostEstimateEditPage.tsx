import React, { useContext, useState, useEffect, useMemo, useCallback, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Spinner,
  Text,
  IconButton,
  HStack,
  Tooltip,
  Badge,
  Flex,
  Stat,
  StatLabel,
  StatNumber,
  StatGroup,
  useColorModeValue,
  Icon,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  FormControl,
  FormLabel,
  FormErrorMessage,
  Input,
  Textarea,
  useDisclosure,
  VStack,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  Alert,
  AlertIcon,
  Image,
  Divider,
  Popover,
  PopoverTrigger,
  PopoverContent,
  PopoverHeader,
  PopoverBody,
  Checkbox,
} from '@chakra-ui/react';
import {
  ArrowLeft,
  Pencil,
  Maximize2,
  Minimize2,
  CheckCircle2,
  AlertCircle,
  FileSpreadsheet,
  Upload,
  FileText,
  Download,
  Eye,
  Trash2,
  Paperclip,
  ImageIcon,
  Columns3,
  Lock,
} from 'lucide-react';
import { AuthContext } from '../context/AuthContext';
import MainLayout from '../layout/MainLayout';
import {
  CostEstimateModernView,
  type CostEstimateModernViewHandle,
  type CostEstimateViewMode,
} from '../components/CostEstimate/CostEstimateModernView';
import CostEstimateToolbar from '../components/CostEstimateToolbar';
import { BASE_COLUMNS, isAlwaysVisibleColumn, loadVisibleCols, saveVisibleCols, VISIBLE_COLS_KEY } from '../components/CostEstimate/TreeView/CostEstimateTreeView';
import { resolveTreeViewSchemaColumns } from '../utils/costEstimateFieldSchema';
import { SchemaManagerModal } from '../components/CostEstimate/SchemaManager/SchemaManagerModal';
import { costEstimateApi } from '../api/costEstimateApi';
import { projectApi } from '../api/projectApi';
import WorkScheduleFormModal from '../components/WorkScheduleFormModal';
import LoadingSpinner from '../components/common/LoadingSpinner';
import ConfirmDialog from '../components/common/ConfirmDialog';
import ShareCostEstimateModal from '../components/ShareCostEstimateModal';
import { useToastNotification } from '../hooks/useToastNotification';
import { useIsMobile } from '../hooks/useIsMobile';
import { useFieldAutosave } from '../hooks/useFieldAutosave';
import { useResourcePermissions } from '../hooks/useResourcePermissions';
import {
  useCostEstimateDetails,
  useReorderCostEstimateItems,
  useReorderCostEstimateItemChildren,
  useReorderCostEstimateGroups,
  costEstimateKeys,
} from '../hooks/queries';
import { recalculateCostEstimateDetails } from '../utils/recalculateCostEstimateDetails';
import { removeItemFromCostEstimateTree } from '../utils/costEstimateUtils';
import { upsertAdditionalFieldValue, resolveAdditionalFieldType, cloneAdditionalFieldValues } from '../utils/additionalFieldHelpers';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
  CostEstimateItemFileWeb,
  CostEstimateFieldValueWeb,
  CostEstimateAdditionalFieldValueWeb,
  CostEstimateAdditionalFieldWeb,
  AddGroupRequestDto,
  AddItemRequestDto,
} from '../types/costEstimate.types.new';
import {
  CostEstimateStatus,
  CostEstimateAccessLevel,
  AdditionalFieldType,
  convertGroupWebToDto,
} from '../types/costEstimate.types.new';
import { addAdditionalField, updateAdditionalField, uploadItemFiles, deleteItemFile, setItemIsSelected } from '../api/costEstimateApi';
import { getFieldDefByType, FieldType } from '../utils/schemaHelpers';

// ---------------------------------------------------------------------------
// Helpery
// ---------------------------------------------------------------------------

/** Formatuj kwotę z polskim separatorem tysięcy i walutą */
const formatCurrency = (value: number | undefined, symbol: string): string => {
  if (value === undefined || value === null) return `0,00 ${symbol}`;
  return `${value.toLocaleString('pl-PL', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} ${symbol}`;
};

/** Formatuj godzinę z daty */
const formatTime = (date: Date): string =>
  date.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' });

/**
 * Tworzy domyślne fieldValues dla nowo dodanej pozycji/komponentu/opcji.
 * Odzwierciedla logikę backendową (CostEstimateService.CreateDefaultItemFieldValues).
 *
 * ItemSystemSelected (104):
 *   - pozycja (relationType=0) i komponent (relationType=2): boolValue = true  (zaznaczone)
 *   - opcja (relationType=1):                                boolValue = false (odznaczone)
 */
const FIELD_SCOPE_ITEM_SYSTEM = 1; // FieldScope.ItemSystem

function buildDefaultItemFieldValues(
  schema: CostEstimateDetailsWeb['schema'],
  relationType: 0 | 1 | 2
): CostEstimateFieldValueWeb[] {
  if (!schema) return [];
  const selectedFieldDef = getFieldDefByType(schema, FieldType.ItemSystemSelected);

  if (!selectedFieldDef) return [];

  return [{
    id: `temp_default_sel_${selectedFieldDef.id}`,
    fieldDefinitionId: selectedFieldDef.id,
    fieldType: FieldType.ItemSystemSelected,
    fieldScope: FIELD_SCOPE_ITEM_SYSTEM,
    boolValue: relationType !== 1, // opcje domyślnie odznaczone; reszta zaznaczona
  }];
}

/** Formatuj rozmiar pliku */
const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

/** Rekurencyjnie znajdź pozycję (item/option/component) po ID w drzewie grup */
function findItemInTree(
  groups: CostEstimateGroupWeb[],
  itemId: string
): CostEstimateItemWeb | null {
  for (const group of groups) {
    for (const item of group.items) {
      const found = findItemRecursive(item, itemId);
      if (found) return found;
    }
    if (group.childGroups?.length) {
      const found = findItemInTree(group.childGroups, itemId);
      if (found) return found;
    }
  }
  return null;
}

function findItemRecursive(
  item: CostEstimateItemWeb,
  itemId: string
): CostEstimateItemWeb | null {
  if (item.id === itemId) return item;

  for (const children of [item.options ?? [], item.components ?? []]) {
    for (const child of children) {
      if (child.id === itemId) return child;
      // Sprawdź głębiej (option → component, component → option)
      for (const deeper of [child.options ?? [], child.components ?? []]) {
        for (const d of deeper) {
          if (d.id === itemId) return d;
        }
      }
    }
  }
  return null;
}

// ---------------------------------------------------------------------------
// Popover widoczności kolumn (w toolbarze, zawsze dostępny)
// ---------------------------------------------------------------------------

interface ColumnVisibilityPopoverProps {
  visibleColIds: Set<string>;
  onToggleColVisibility: (fieldId: string) => void;
  fieldSchemas: CostEstimateDetailsWeb['fieldSchemas'];
  additionalFields: CostEstimateAdditionalFieldWeb[];
}

const ColumnVisibilityPopover: React.FC<ColumnVisibilityPopoverProps> = ({
  visibleColIds,
  onToggleColVisibility,
  fieldSchemas,
  additionalFields,
}) => {
  const [isOpen, setIsOpen] = useState(false);

  const schemaColumns = useMemo(
    () => resolveTreeViewSchemaColumns({ fieldSchemas, additionalFields }, BASE_COLUMNS),
    [fieldSchemas, additionalFields]
  );

  return (
    <Popover isOpen={isOpen} onClose={() => setIsOpen(false)} placement="bottom-end" closeOnBlur>
      <PopoverTrigger>
        <Button
          leftIcon={<Columns3 size={14} />}
          size="sm"
          variant="outline"
          colorScheme="gray"
          fontWeight="600"
          fontSize="12.5px"
          onClick={() => setIsOpen(!isOpen)}
          aria-label="Widoczność kolumn"
        >
          Kolumny
        </Button>
      </PopoverTrigger>
      <PopoverContent
        w="260px"
        borderRadius="14px"
        boxShadow="0 18px 50px rgba(20,33,47,0.16), 0 4px 14px rgba(20,33,47,0.08)"
        border="1px solid"
        borderColor="neutral.200"
        maxH="420px"
        overflowY="auto"
      >
        <PopoverHeader borderBottom="none" pt={4} pb={2}>
          <Text fontSize="15px" fontWeight="800" letterSpacing="-0.01em">
            Widoczność kolumn
          </Text>
          <Text fontSize="12.5px" color="neutral.500" mt={1}>
            Pokaż lub ukryj kolumny w widoku drzewa
          </Text>
        </PopoverHeader>
        <PopoverBody pb={4}>
          <VStack align="stretch" spacing={0.5}>
            {schemaColumns.map((col) => {
              const isAlwaysVisible = isAlwaysVisibleColumn(col);
              const isVisible = visibleColIds.has(col.id);
              return (
                <HStack
                  key={col.id}
                  spacing={2}
                  px={1.5}
                  py={1.5}
                  borderRadius="8px"
                  _hover={{ bg: 'neutral.50' }}
                  opacity={isAlwaysVisible ? 0.6 : 1}
                >
                  <Checkbox
                    isChecked={isVisible}
                    isDisabled={isAlwaysVisible}
                    onChange={() => {
                      if (!isAlwaysVisible) {
                        onToggleColVisibility(col.id);
                      }
                    }}
                    size="sm"
                    colorScheme="primary"
                    aria-label={`Pokaż/ukryj kolumnę ${col.label}`}
                  />
                  <Text flex={1} fontSize="13px" fontWeight="500" noOfLines={1}>
                    {col.label}
                  </Text>
                  {col.isAdditional && (
                    <Badge colorScheme="blue" fontSize="9px">
                      dodatkowe
                    </Badge>
                  )}
                  {isAlwaysVisible && (
                    <Box as="span" aria-label="Zawsze widoczne" title="Kolumna zawsze widoczna">
                      <Lock size={11} color="var(--chakra-colors-neutral-400)" aria-hidden="true" />
                    </Box>
                  )}
                </HStack>
              );
            })}
          </VStack>
        </PopoverBody>
      </PopoverContent>
    </Popover>
  );
};

// ---------------------------------------------------------------------------
// Komponent strony
// ---------------------------------------------------------------------------

export const CostEstimateEditPage: React.FC = () => {
  const { projectId, estimateId } = useParams<{
    projectId: string;
    estimateId: string;
  }>();

  const { user } = useContext(AuthContext);
  const userId = user?.id ?? 'anonymous';
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showSuccess, showError, showApiSuccess } = useToastNotification();

  // ---- Uprawnienia do zasobu ----
  const resourcePerms = useResourcePermissions(projectId, "estimates");

  // ---- Mutacje reorder ----
  const reorderItemsMutation = useReorderCostEstimateItems(
    user?.activeTenantId ?? '',
    projectId ?? '',
    estimateId ?? ''
  );
  const reorderItemChildrenMutation = useReorderCostEstimateItemChildren(
    user?.activeTenantId ?? '',
    projectId ?? '',
    estimateId ?? ''
  );
  const reorderGroupsMutation = useReorderCostEstimateGroups(
    user?.activeTenantId ?? '',
    projectId ?? '',
    estimateId ?? ''
  );

  // ---- React Query: pobieranie szczegółów kosztorysu ----
  const { data: fetchedDetails, isLoading, refetch } = useCostEstimateDetails(
    user?.activeTenantId ?? undefined,
    projectId,
    estimateId
  );

  // ---- Stan ----
  const [isRecalculating, setIsRecalculating] = useState(false);
  const [details, setDetails] = useState<CostEstimateDetailsWeb | null>(null);
  const [hasChanges, setHasChanges] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [viewMode, setViewMode] = useState<CostEstimateViewMode>('tree');
  const [searchQuery, setSearchQuery] = useState('');
  const [visibleColIds, setVisibleColIds] = useState<Set<string>>(
    () => new Set(BASE_COLUMNS.map((c) => c.id))
  );

  const editPermissions = useMemo(() => {
    if (!details) {
      return {
        canFullEdit: false,
        canRestrictedEdit: false,
        canAnyEdit: false,
      };
    }
    const canFullEdit =
      details.accessLevel === CostEstimateAccessLevel.Full &&
      (resourcePerms.mine.canEdit || resourcePerms.all.canEdit);
    const canRestrictedEdit =
      details.accessLevel === CostEstimateAccessLevel.Restricted &&
      resourcePerms.shared.canEdit;
    return {
      canFullEdit,
      canRestrictedEdit,
      canAnyEdit: canFullEdit || canRestrictedEdit,
    };
  }, [details, resourcePerms]);

  const { canFullEdit, canAnyEdit } = editPermissions;

  const handleToggleColVisibility = useCallback((fieldId: string) => {
    const schemaColumnsForToggle = resolveTreeViewSchemaColumns(
      details ?? { fieldSchemas: [], additionalFields: [] },
      BASE_COLUMNS
    );
    const col = schemaColumnsForToggle.find((c) => c.id === fieldId);
    if (col && isAlwaysVisibleColumn(col)) {
      return;
    }
    setVisibleColIds((prev) => {
      const next = new Set(prev);
      if (next.has(fieldId)) next.delete(fieldId);
      else next.add(fieldId);
      if (estimateId) {
        saveVisibleCols(userId, estimateId, next);
      }
      return next;
    });
  }, [userId, estimateId, details]);

  // Przetwarzaj dane z React Query (recalculacja + backup) gdy się zmienią
  useEffect(() => {
    if (fetchedDetails) {
      prePopulateBackups(fetchedDetails);
      const recalculated = recalculateCostEstimateDetails(fetchedDetails);
      setDetails(recalculated);
      // Synchronizuj widoczność kolumn z sessionStorage
      if (estimateId) {
        const columns = resolveTreeViewSchemaColumns(recalculated, BASE_COLUMNS);
        const saved = loadVisibleCols(userId, estimateId, columns);
        if (saved.size > 0) {
          setVisibleColIds(saved);
        }
      }
      setHasChanges(false);
      // Członkowie projektu potrzebni do modala tworzenia harmonogramu (tylko Full)
      if (fetchedDetails.accessLevel === CostEstimateAccessLevel.Full) {
        fetchProjectMembers();
      }
    }
  }, [fetchedDetails, estimateId, userId]);

  // Ref do timeout auto-recalculate (2s po ostatnim zapisie)
  const autoRecalcTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const modernViewRef = useRef<CostEstimateModernViewHandle>(null);
  const isMobile = useIsMobile();
  const [lastSavedAt, setLastSavedAt] = useState<Date | null>(null);

  // Backup oryginalnych wartości pozycji nadrzędnej przed wyborem opcji
  // Klucz: itemId pozycji nadrzędnej, wartość: pola finansowe przed nadpisaniem
  const parentValuesBackupRef = useRef<Map<string, {
    quantity?: number;
    unit?: string;
    unitPriceNet?: number;
    vatRate?: number;
    unitPriceGross?: number;
    netValue?: number;
    grossValue?: number;
    vatValue?: number;
    additionalFieldValues?: CostEstimateAdditionalFieldValueWeb[];
  }>>(new Map());

  // ---- Modal harmonogramu ----
  const { isOpen: isScheduleModalOpen, onOpen: onScheduleModalOpen, onClose: onScheduleModalClose } = useDisclosure();
  const [scheduleModalMode, setScheduleModalMode] = useState<'create' | 'edit'>('create');
  const [isSyncing, setIsSyncing] = useState(false);

  const [projectMembers, setProjectMembers] = useState<any[]>([]);

  // ---- Modal udostępniania ----
  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();

  // ---- Modal zarządzania schematem ----
  const { isOpen: isSchemaModalOpen, onOpen: onSchemaModalOpen, onClose: onSchemaModalClose } = useDisclosure();

  // ---- Dialog usuwania grupy ----
  const [groupToDelete, setGroupToDelete] = useState<string | null>(null);
  const [itemToDelete, setItemToDelete] =
    useState<{ groupId: string; itemId: string } | null>(null);
  const [optionToDelete, setOptionToDelete] =
    useState<{ groupId: string; itemId: string; optionId: string } | null>(null);
  const [componentToDelete, setComponentToDelete] =
    useState<{ groupId: string; itemId: string; componentId: string } | null>(null);

  // ---- File upload modal ----
  const [uploadItemId, setUploadItemId] = useState<string | null>(null);
  const { isOpen: isUploadModalOpen, onOpen: onUploadModalOpen, onClose: onUploadModalClose } = useDisclosure();
  const { isOpen: isPreviewOpen, onOpen: onPreviewOpen, onClose: onPreviewClose } = useDisclosure();
  const [previewFile, setPreviewFile] = useState<CostEstimateItemFileWeb | null>(null);

  // ---- Modal edycji nazwy/opisu ----
  const { isOpen: isEditMetaOpen, onOpen: onEditMetaOpen, onClose: onEditMetaClose } = useDisclosure();
  const [editName, setEditName] = useState('');
  const [editNameError, setEditNameError] = useState<string>('');

  // ---- Modal ostrzeżenia o niezapisanych zmianach ----
  const { isOpen: isUnsavedOpen, onOpen: onUnsavedOpen, onClose: onUnsavedClose } = useDisclosure();
  const unsavedCancelRef = useRef<HTMLButtonElement>(null);
  const [pendingNavigation, setPendingNavigation] = useState<string | null>(null);
  const [isBackNavigation, setIsBackNavigation] = useState(false);
  const [editDescription, setEditDescription] = useState('');

  // ---- Kolory (dark mode ready) ----
  const toolbarBg = useColorModeValue('white', 'gray.800');
  const toolbarBorder = useColorModeValue('gray.200', 'gray.700');
  const statBg = useColorModeValue('gray.50', 'gray.700');
  const pageBg = useColorModeValue('gray.50', 'gray.900');

  // Ref do hasChanges — potrzebny w navigate guard (closure)
  const hasChangesRef = React.useRef(hasChanges);
  useEffect(() => {
    hasChangesRef.current = hasChanges;
  }, [hasChanges]);

  // ========== RĘCZNA OBSŁUGA NAWIGACJI Z OSTRZEŻENIEM ==========
  const handleConfirmLeave = useCallback(() => {
    onUnsavedClose();
    setHasChanges(false);
    hasChangesRef.current = false;
    
    if (isBackNavigation) {
      // Nawigacja przyciskiem wstecz - użyj history.back()
      setIsBackNavigation(false);
      setPendingNavigation(null);
      window.history.back();
    } else if (pendingNavigation) {
      // Normalna nawigacja przez safeNavigate
      navigate(pendingNavigation);
      setPendingNavigation(null);
    }
  }, [pendingNavigation, isBackNavigation, navigate, onUnsavedClose]);

  const handleCancelLeave = useCallback(() => {
    onUnsavedClose();
    setPendingNavigation(null);
    setIsBackNavigation(false);
  }, [onUnsavedClose]);

  /** Bezpieczna nawigacja - pokazuje modal jeśli są niezapisane zmiany */
  const safeNavigate = useCallback((to: string) => {
    if (hasChangesRef.current) {
      setPendingNavigation(to);
      onUnsavedOpen();
    } else {
      navigate(to);
    }
  }, [navigate, onUnsavedOpen]);

  /** Otwiera modal edycji z aktualnymi wartościami */
  const handleOpenEditMeta = useCallback(() => {
    if (details) {
      setEditName(details.name);
      setEditDescription(details.description || '');
      setEditNameError('');
      onEditMetaOpen();
    }
  }, [details, onEditMetaOpen]);

  /** Zapisuje zmiany nazwy/opisu */
  const handleSaveMetaChanges = useCallback(() => {
    if (!details) return;
    const trimmedName = editName.trim();
    if (!trimmedName) {
      setEditNameError('Nazwa kosztorysu nie może być pusta');
      return;
    }
    setEditNameError('');
    setDetails({
      ...details,
      name: trimmedName,
      description: editDescription.trim() || undefined,
    });
    setHasChanges(true);
    showApiSuccess('nameUpdated');
    onEditMetaClose();
  }, [details, editName, editDescription, onEditMetaClose, showSuccess]);

  // ========== POPSTATE (przycisk wstecz/dalej przeglądarki) ==========
  useEffect(() => {
    // Zapisz aktualną pozycję w historii
    const currentPath = window.location.pathname + window.location.search;
    
    const handlePopState = () => {
      if (hasChangesRef.current) {
        // Przywróć poprzedni URL (anuluj nawigację)
        window.history.pushState(null, '', currentPath);
        // Oznacz jako nawigację wstecz i pokaż modal
        setIsBackNavigation(true);
        onUnsavedOpen();
      }
    };
    
    // Dodaj wpis do historii, aby móc przechwycić przycisk wstecz
    window.history.pushState(null, '', currentPath);
    window.addEventListener('popstate', handlePopState);
    
    return () => {
      window.removeEventListener('popstate', handlePopState);
    };
  }, [onUnsavedOpen]);

  // ========== ŁADOWANIE DANYCH ==========

  useEffect(() => {
    if (user?.activeTenantId && projectId && estimateId) {
      loadCostEstimate();
    }
  }, [user?.activeTenantId, projectId, estimateId]);

  const fetchProjectMembers = async () => {
    if (!user?.activeTenantId || !projectId) return;
    try {
      const membersRes = await projectApi.getProjectMembers(user.activeTenantId, projectId);
      setProjectMembers(membersRes.data ?? []);
    } catch {
      // ignoruj błąd pobierania członków projektu
    }
  };

  /**
   * Przy ładowaniu danych z API, pre-populuje parentValuesBackupRef dla pozycji,
   * które mają już zaznaczoną opcję. Dzięki temu odznaczenie opcji (pierwszy raz w sesji)
   * przywróci oryginalne wartości pozycji zamiast używać wartości z opcji.
   */
  const prePopulateBackups = useCallback((details: CostEstimateDetailsWeb) => {
    const traverseGroups = (groups: CostEstimateGroupWeb[]) => {
      for (const group of groups) {
        for (const item of group.items) {
          if (item.options?.some((o) => o.isSelected) && !parentValuesBackupRef.current.has(item.id)) {
            parentValuesBackupRef.current.set(item.id, {
              quantity: item.quantity,
              unit: item.unit,
              unitPriceNet: item.unitPriceNet,
              vatRate: item.vatRate,
              unitPriceGross: item.unitPriceGross,
              netValue: item.netValue,
              grossValue: item.grossValue,
              vatValue: item.vatValue,
              additionalFieldValues: cloneAdditionalFieldValues(item.additionalFieldValues),
            });
          }
        }
        traverseGroups(group.childGroups);
      }
    };
    traverseGroups(details.rootGroups);
  }, []);

  const loadCostEstimate = useCallback(async () => {
    await refetch();
  }, [refetch]);

  // ========== AUTO-RECALCULATE W TLE ==========
  
  const AUTO_RECALC_DELAY_MS = 2000; // 2s po ostatnim zapisie
  
  /**
   * Uruchamia przeliczanie kosztorysu w tle.
   * Wywoływane automatycznie po zapisie pola (z debounce 2s).
   * Nie pobiera danych - UI ma aktualne dane lokalnie, chodzi tylko o synchronizację backendu.
   */
  const runAutoRecalculate = useCallback(async () => {
    if (!user?.activeTenantId || !projectId || !estimateId) return;
    
    try {
      setIsRecalculating(true);
      
      // Przelicz kosztorys na backendzie (bez pobierania - UI ma aktualne dane)
      await costEstimateApi.recalculate(
        user.activeTenantId,
        projectId,
        estimateId,
      );
      
      setHasChanges(false);
      setLastSavedAt(new Date());
    } catch (err) {
      // Nie pokazuj błędu użytkownikowi - przeliczanie w tle
    } finally {
      setIsRecalculating(false);
    }
  }, [user?.activeTenantId, projectId, estimateId]);
  
  /**
   * Planuje auto-recalculate z debounce.
   * Każdy kolejny zapis resetuje timer.
   */
  const scheduleAutoRecalculate = useCallback(() => {
    // Anuluj poprzedni timeout
    if (autoRecalcTimeoutRef.current) {
      clearTimeout(autoRecalcTimeoutRef.current);
    }
    
    // Ustaw nowy timeout
    autoRecalcTimeoutRef.current = setTimeout(() => {
      runAutoRecalculate();
      autoRecalcTimeoutRef.current = null;
    }, AUTO_RECALC_DELAY_MS);
  }, [runAutoRecalculate]);
  
  // Cleanup timeout przy unmount
  useEffect(() => {
    return () => {
      if (autoRecalcTimeoutRef.current) {
        clearTimeout(autoRecalcTimeoutRef.current);
      }
    };
  }, []);

  // ========== AUTOSAVE PÓL Z DEBOUNCE ==========

  const { scheduleFieldSave, flushPendingChanges } = useFieldAutosave({
    params: user?.activeTenantId && projectId && estimateId 
      ? { tenantId: user.activeTenantId, projectId, costEstimateId: estimateId }
      : null,
    onSaveSuccess: (fieldInfo, savedFieldValueId, savedValue) => {
      // Gdy pole dodatkowe było nowe (fieldValueId === null) i mamy ID z backendu,
      // zaktualizuj lokalny stan zastępując optimistic ID prawdziwym ID z API.
      if (
        fieldInfo.fieldType === 'additional' &&
        fieldInfo.fieldValueId === null &&
        savedFieldValueId &&
        fieldInfo.additionalFieldId
      ) {
        const additionalFieldId = fieldInfo.additionalFieldId;
        setDetails(prev => {
          if (!prev) return prev;

          const updateAdditionalValues = (
            values: CostEstimateAdditionalFieldValueWeb[],
          ) =>
            values.map(fv =>
              fv.additionalFieldId === additionalFieldId && fv.id.startsWith('temp_')
                ? { ...fv, id: savedFieldValueId }
                : fv,
            );

          if (fieldInfo.entityType === 'group') {
            const updateGroups = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
              groups.map(g => {
                if (g.id === fieldInfo.entityId) {
                  return { ...g, additionalFieldValues: updateAdditionalValues(g.additionalFieldValues) };
                }
                return { ...g, childGroups: updateGroups(g.childGroups || []) };
              });
            return { ...prev, rootGroups: updateGroups(prev.rootGroups) };
          } else {
            const updateItems = (items: CostEstimateItemWeb[]): CostEstimateItemWeb[] =>
              items.map(item => {
                if (item.id === fieldInfo.entityId) {
                  return { ...item, additionalFieldValues: updateAdditionalValues(item.additionalFieldValues) };
                }
                return {
                  ...item,
                  options: item.options ? updateItems(item.options) : item.options,
                  components: item.components ? updateItems(item.components) : item.components,
                };
              });
            const updateGroups = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
              groups.map(g => ({
                ...g,
                items: updateItems(g.items || []),
                childGroups: updateGroups(g.childGroups || []),
              }));
            return { ...prev, rootGroups: updateGroups(prev.rootGroups) };
          }
        });
      }

      // Supresuj unused variable warning — savedValue może być potrzebne w przyszłości
      void savedValue;

      // Po udanym zapisie pola - zaplanuj auto-recalculate
      scheduleAutoRecalculate();
    },
    onSaveError: (_fieldInfo, error) => {
      showError('Błąd zapisu', 'Nie udało się zapisać zmiany pola');
    },
    enabled: canAnyEdit,
  });

  /**
   * Synchronizuje powiązany harmonogram prac ze strukturą kosztorysu.
   */
  const handleSyncSchedule = async () => {
    if (!user?.activeTenantId || !projectId || !details?.workScheduleId) return;
    try {
      setIsSyncing(true);
      await projectApi.syncWorkScheduleWithEstimate(user.activeTenantId, projectId, details.workScheduleId);
      showApiSuccess('syncDone');
    } catch {
      showError('Błąd synchronizacji', 'Nie udało się zsynchronizować harmonogramu');
    } finally {
      setIsSyncing(false);
    }
  };

  /**
   * Przelicza kosztorys na backendzie i pobiera aktualne dane.
   * Używane przez przycisk "Odśwież" i Ctrl+S.
   */
  const handleRefresh = useCallback(async () => {
    if (!user?.activeTenantId || !projectId || !estimateId) return;
    
    try {
      setIsRecalculating(true);
      
      // Flush pending changes
      await flushPendingChanges();
      
      // Przelicz kosztorys na backendzie
      await costEstimateApi.recalculate(
        user.activeTenantId,
        projectId,
        estimateId,
      );
      
      // Pobierz aktualne dane z bazy
      await loadCostEstimate();
      setHasChanges(false);
      setLastSavedAt(new Date());
    } catch (err) {
      showError(
        'Błąd przeliczania',
        'Nie udało się przeliczyć kosztorysu. Spróbuj ponownie.'
      );
    } finally {
      setIsRecalculating(false);
    }
  }, [user?.activeTenantId, projectId, estimateId, flushPendingChanges]);

  // Handler dla autosave wywoływany z tabeli
  const handleFieldAutosave = useCallback((params: {
    entityType: 'group' | 'item';
    entityId: string;
    fieldValueId?: string | null;
    /** @deprecated Używaj additionalFieldId */
    fieldDefinitionId?: string;
    /** @deprecated Nie używany w nowym API */
    fieldType?: number;
    /** Dla pól dodatkowych: ID definicji pola dodatkowego */
    additionalFieldId?: string;
    /** Dla pól bazowych: nazwa pola (name, quantity, unit, unitPriceNet, vatRate) */
    fieldName?: string;
    /** 'base' = pole systemowe, 'additional' = pole dodatkowe */
    fieldKind?: 'base' | 'additional';
    valueType: 'string' | 'numeric' | 'boolean' | 'date';
    value: string | undefined;
  }) => {
    const fieldKind = params.fieldKind ?? 'additional';
    // Obsługa backward compat: stare komponenty wysyłają fieldDefinitionId zamiast additionalFieldId
    const resolvedAdditionalFieldId =
      params.additionalFieldId ?? params.fieldDefinitionId ?? '';
    const resolvedFieldName =
      params.fieldName ?? params.additionalFieldId ?? params.fieldDefinitionId ?? '';

    scheduleFieldSave(
      {
        entityType: params.entityType,
        entityId: params.entityId,
        fieldType: fieldKind,
        name: fieldKind === 'base' ? resolvedFieldName : resolvedAdditionalFieldId,
        additionalFieldId: fieldKind === 'additional' ? resolvedAdditionalFieldId : undefined,
        fieldValueId: params.fieldValueId ?? null,
        valueType: params.valueType,
      },
      params.value
    );
  }, [scheduleFieldSave]);

  // ========== KEYBOARD SHORTCUTS ==========

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      // Ctrl+S / Cmd+S → Przelicz i pobierz aktualne dane
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        if (canAnyEdit && details && !isRecalculating) {
          // Anuluj zaplanowane auto-recalculate
          if (autoRecalcTimeoutRef.current) {
            clearTimeout(autoRecalcTimeoutRef.current);
            autoRecalcTimeoutRef.current = null;
          }
          handleRefresh();
        }
      }
      // Esc → wyjście z fullscreen
      if (e.key === 'Escape' && isFullscreen) {
        setIsFullscreen(false);
      }
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [canAnyEdit, details, isFullscreen, isRecalculating, handleRefresh]);

  // ========== MUTACJE DANYCH ==========

  const handleDataChange = useCallback(
    (updated: CostEstimateDetailsWeb) => {
      const recalculated = recalculateCostEstimateDetails(updated);
      setDetails(recalculated);
      setHasChanges(true);
    },
    [],
  );

  const handleAddGroup = useCallback(async (): Promise<string | undefined> => {
    if (!user?.activeTenantId || !projectId || !estimateId || !details) return undefined;
    
    // Optimistic update - dodaj tymczasową grupę od razu
    const tempId = `temp_group_${Date.now()}`;
    const tempGroup: CostEstimateGroupWeb = {
      id: tempId,
      parentGroupId: undefined,
      level: 0,
      order: details.rootGroups.length,
      name: '',
      additionalFieldValues: [],
      fieldValues: [],
      totalNet: 0,
      totalGross: 0,
      totalVat: 0,
      lastCalculatedAt: undefined,
      childGroups: [],
      items: [],
      createdAt: new Date().toISOString(),
      updatedAt: undefined,
    };
    
    // Od razu pokaż nową grupę w UI
    setDetails(prev => prev ? { ...prev, rootGroups: [...prev.rootGroups, tempGroup] } : prev);
    
    try {
      const request: AddGroupRequestDto = {
        parentGroupId: null,
        order: details.rootGroups.length,
      };
      
      const newGroupId = await costEstimateApi.addGroup(
        user.activeTenantId,
        projectId,
        estimateId,
        request
      );
      
      // Zamień tymczasową grupę na prawdziwą z API (fieldValues puste — tworzone przez autosave)
      setDetails(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          rootGroups: prev.rootGroups.map(g => 
            g.id === tempId 
              ? { ...g, id: newGroupId, fieldValues: [] }
              : g
          ),
        };
      });
      return newGroupId;
    } catch (err) {
      // Usuń tymczasową grupę przy błędzie
      setDetails(prev => prev ? { ...prev, rootGroups: prev.rootGroups.filter(g => g.id !== tempId) } : prev);
      showError('Błąd', err instanceof Error ? err.message : 'Nie udało się dodać etapu');
      return undefined;
    }
  }, [user?.activeTenantId, projectId, estimateId, details, showError]);

  const handleDeleteGroup = useCallback((groupId: string) => {
    setGroupToDelete(groupId);
  }, []);

  const confirmDeleteGroup = useCallback(async () => {
    if (!user?.activeTenantId || !projectId || !estimateId || !details || !groupToDelete) return;

    const idToDelete = groupToDelete;

    // Optimistic: usuń grupę natychmiast z UI, request idzie w tle
    const deleteRecursive = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
      groups
        .filter((g) => g.id !== idToDelete)
        .map((g) => ({ ...g, childGroups: deleteRecursive(g.childGroups || []) }));

    const prevDetails = details;
    setDetails(prev => prev ? { ...prev, rootGroups: deleteRecursive(prev.rootGroups) } : prev);
    setGroupToDelete(null);

    try {
      await costEstimateApi.deleteGroup(
        user.activeTenantId,
        projectId,
        estimateId,
        idToDelete
      );
    } catch (err) {
      // Przywróć stan przed usunięciem gdy API zwróci błąd
      setDetails(prevDetails);
      showError('Błąd', err instanceof Error ? err.message : 'Nie udało się usunąć etapu');
    }
  }, [user?.activeTenantId, projectId, estimateId, details, groupToDelete, showError]);

  const handleAddSubGroup = useCallback(
    async (parentGroupId: string): Promise<string | undefined> => {
      if (!user?.activeTenantId || !projectId || !estimateId || !details) return undefined;

      // Sprawdź limit zagnieżdżenia (currently not enforced in schema-based structure)
      // Note: maxGroupLevel is no longer part of schema - removed from backend
      const maxLevel = undefined; // Schema-based structure doesn't enforce max level
      let parentLevel: number | undefined;
      if (maxLevel != null) {
        const findGroupLevel = (groups: CostEstimateGroupWeb[]): number | undefined => {
          for (const g of groups) {
            if (g.id === parentGroupId) return g.level;
            const childResult = findGroupLevel(g.childGroups || []);
            if (childResult !== undefined) return childResult;
          }
          return undefined;
        };
        parentLevel = findGroupLevel(details.rootGroups);
        if (parentLevel !== undefined && parentLevel >= maxLevel) {
          showError('Limit zagnieżdżenia', `Maksymalny poziom zagnieżdżenia etapów to ${maxLevel}`);
          return undefined;
        }
      }

      // Oblicz order dla nowej grupy
      const findParentGroup = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb | undefined => {
        for (const g of groups) {
          if (g.id === parentGroupId) return g;
          const found = findParentGroup(g.childGroups || []);
          if (found) return found;
        }
        return undefined;
      };
      const parentGroup = findParentGroup(details.rootGroups);
      const childOrder = parentGroup ? (parentGroup.childGroups || []).length : 0;
      const newLevel = (parentGroup?.level ?? 0) + 1;

      // Optimistic update - dodaj tymczasową podgrupę od razu
      const tempId = `temp_subgroup_${Date.now()}`;
      const addTempSub = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
        groups.map((g) => {
          if (g.id === parentGroupId) {
            return {
              ...g,
              childGroups: [
                ...(g.childGroups || []),
                {
                  id: tempId,
                  parentGroupId,
                  level: newLevel,
                  order: childOrder,
                  name: '',
                  additionalFieldValues: [],
                  fieldValues: [],
                  totalNet: 0,
                  totalGross: 0,
                  totalVat: 0,
                  lastCalculatedAt: undefined,
                  childGroups: [],
                  items: [],
                  createdAt: new Date().toISOString(),
                  updatedAt: undefined,
                } as CostEstimateGroupWeb,
              ],
            };
          }
          return { ...g, childGroups: addTempSub(g.childGroups || []) };
        });
      
      // Od razu pokaż nową podgrupę w UI
      setDetails(prev => prev ? { ...prev, rootGroups: addTempSub(prev.rootGroups) } : prev);

      try {
        const request: AddGroupRequestDto = {
          parentGroupId,
          order: childOrder,
        };
        
        const newSubGroupId = await costEstimateApi.addGroup(
          user.activeTenantId,
          projectId,
          estimateId,
          request
        );

        // Zamień tymczasową grupę na prawdziwą z API (fieldValues puste — tworzone przez autosave)
        const replaceTempWithReal = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
          groups.map((g) => {
            if (g.id === tempId) {
              return { ...g, id: newSubGroupId, fieldValues: [] };
            }
            return { ...g, childGroups: replaceTempWithReal(g.childGroups || []) };
          });
        
        setDetails(prev => prev ? { ...prev, rootGroups: replaceTempWithReal(prev.rootGroups) } : prev);
        return newSubGroupId;
      } catch (err) {
        // Usuń tymczasową grupę przy błędzie
        const removeTempGroup = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
          groups
            .filter(g => g.id !== tempId)
            .map(g => ({ ...g, childGroups: removeTempGroup(g.childGroups || []) }));
        
        setDetails(prev => prev ? { ...prev, rootGroups: removeTempGroup(prev.rootGroups) } : prev);
        showError('Błąd', err instanceof Error ? err.message : 'Nie udało się dodać podetapu');
        return undefined;
      }
    },
    [user?.activeTenantId, projectId, estimateId, details, showError],
  );

  const handleAddItem = useCallback(
    async (groupId: string): Promise<string | undefined> => {
      if (!user?.activeTenantId || !projectId || !estimateId || !details) return undefined;

      // Oblicz order dla nowego itemu
      const findGroup = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb | undefined => {
        for (const g of groups) {
          if (g.id === groupId) return g;
          const found = findGroup(g.childGroups || []);
          if (found) return found;
        }
        return undefined;
      };
      const group = findGroup(details.rootGroups);
      const itemOrder = group ? (group.items || []).length : 0;

      // Optimistic update - dodaj tymczasową pozycję od razu
      const tempId = `temp_item_${Date.now()}`;
      const addTempItem = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
        groups.map((g) => {
          if (g.id === groupId) {
            return {
              ...g,
              items: [
                ...(g.items || []),
                {
                  id: tempId,
                  groupId,
                  parentItemId: undefined,
                  relationType: 0,
                  order: itemOrder,
                  name: '',
                  isSelected: true,
                  isStageWork: false,
                  additionalFieldValues: [],
                  fieldValues: buildDefaultItemFieldValues(details.schema, 0),
                  options: [],
                  createdAt: new Date().toISOString(),
                  updatedAt: undefined,
                } as CostEstimateItemWeb,
              ],
            };
          }
          return { ...g, childGroups: addTempItem(g.childGroups || []) };
        });
      
      // Od razu pokaż nową pozycję w UI
      setDetails(prev => prev ? { ...prev, rootGroups: addTempItem(prev.rootGroups) } : prev);

      try {
        const request: AddItemRequestDto = {
          groupId,
          order: itemOrder,
          relationType: 0, // None - zwykły item
        };
        
        const newItemId = await costEstimateApi.addItem(
          user.activeTenantId,
          projectId,
          estimateId,
          request
        );

        // Zamień tymczasową pozycję na prawdziwą z API (fieldValues puste — tworzone przez autosave)
        const replaceTempWithReal = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
          groups.map((g) => {
            if (g.id === groupId) {
              return {
                ...g,
                items: (g.items || []).map(item =>
                  item.id === tempId
                    // Zachowaj domyślne fieldValues z optimistic update, tylko nadaj prawdziwe ID
                    ? { ...item, id: newItemId }
                    : item
                ),
              };
            }
            return { ...g, childGroups: replaceTempWithReal(g.childGroups || []) };
          });
        
        setDetails(prev => prev ? { ...prev, rootGroups: replaceTempWithReal(prev.rootGroups) } : prev);
        
        return newItemId;
      } catch (err) {
        // Usuń tymczasową pozycję przy błędzie
        const removeTempItem = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
          groups.map((g) => {
            if (g.id === groupId) {
              return { ...g, items: (g.items || []).filter(i => i.id !== tempId) };
            }
            return { ...g, childGroups: removeTempItem(g.childGroups || []) };
          });
        
        setDetails(prev => prev ? { ...prev, rootGroups: removeTempItem(prev.rootGroups) } : prev);
        showError('Błąd', err instanceof Error ? err.message : 'Nie udało się dodać pozycji');
        return undefined;
      }
    },
    [user?.activeTenantId, projectId, estimateId, details, showError],
  );

  const handleDeleteItem = useCallback(
    async (groupId: string, itemId: string) => {
      if (!user?.activeTenantId || !projectId || !estimateId) return;

      const applyOptimisticDelete = (
        prev: CostEstimateDetailsWeb,
      ): CostEstimateDetailsWeb =>
        recalculateCostEstimateDetails({
          ...prev,
          rootGroups: removeItemFromCostEstimateTree(prev.rootGroups, itemId),
        });

      let prevDetails: CostEstimateDetailsWeb | null = null;
      setDetails((prev) => {
        if (!prev) {
          return prev;
        }
        prevDetails = prev;
        return applyOptimisticDelete(prev);
      });

      if (prevDetails && user.activeTenantId && projectId && estimateId) {
        queryClient.setQueryData<CostEstimateDetailsWeb>(
          costEstimateKeys.detail(user.activeTenantId, projectId, estimateId),
          (cached) => (cached ? applyOptimisticDelete(cached) : cached),
        );
      }

      try {
        await costEstimateApi.deleteItem(
          user.activeTenantId,
          projectId,
          estimateId,
          itemId
        );
      } catch (err) {
        if (prevDetails) {
          setDetails(prevDetails);
          if (user.activeTenantId && projectId && estimateId) {
            queryClient.setQueryData<CostEstimateDetailsWeb>(
              costEstimateKeys.detail(user.activeTenantId, projectId, estimateId),
              prevDetails,
            );
          }
        }
        showError('Błąd', err instanceof Error ? err.message : 'Nie udało się usunąć pozycji');
      }
    },
    [user?.activeTenantId, projectId, estimateId, queryClient, showError],
  );

  const confirmDeleteItem = useCallback(async () => {
    if (!itemToDelete) return;
    await handleDeleteItem(itemToDelete.groupId, itemToDelete.itemId);
    setItemToDelete(null);
  }, [itemToDelete, handleDeleteItem]);

  const confirmDeleteOption = useCallback(async () => {
    if (!optionToDelete) return;
    await handleDeleteItem(optionToDelete.groupId, optionToDelete.optionId);
    setOptionToDelete(null);
  }, [optionToDelete, handleDeleteItem]);

  const confirmDeleteComponent = useCallback(async () => {
    if (!componentToDelete) return;
    await handleDeleteItem(componentToDelete.groupId, componentToDelete.componentId);
    setComponentToDelete(null);
  }, [componentToDelete, handleDeleteItem]);

  /**
   * Dodaje opcję (relationType=1) lub komponent (relationType=2) do pozycji
   * Wywołuje API POST /items z parentItemId
   */
  const handleAddChildItem = useCallback(
    async (
      groupId: string,
      parentItemId: string,
      relationType: 1 | 2 // 1=Option, 2=Component
    ): Promise<string | undefined> => {
      if (!user?.activeTenantId || !projectId || !estimateId || !details) return undefined;

      // Znajdź parent item i oblicz order dla nowego child
      const findItem = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb | undefined => {
        for (const g of groups) {
          for (const item of g.items || []) {
            if (item.id === parentItemId) return item;
            // Sprawdź też komponenty
            for (const comp of item.components || []) {
              if (comp.id === parentItemId) return comp;
            }
          }
          const found = findItem(g.childGroups || []);
          if (found) return found;
        }
        return undefined;
      };
      const parentItem = findItem(details.rootGroups);
      const childCollection = relationType === 1 
        ? (parentItem?.options || []) 
        : (parentItem?.components || []);
      const childOrder = childCollection.length;

      // Optimistic update - dodaj tymczasowy element od razu
      const tempId = relationType === 1 ? `temp_opt_${Date.now()}` : `temp_comp_${Date.now()}`;
      const tempChild: CostEstimateItemWeb = {
        id: tempId,
        groupId,
        parentItemId,
        relationType,
        order: childOrder,
        name: '',
        isSelected: relationType !== 1, // opcje domyślnie odznaczone
        isStageWork: false,
        additionalFieldValues: [],
        fieldValues: buildDefaultItemFieldValues(details.schema, relationType),
        options: relationType === 1 ? undefined : [],
        components: undefined,
        createdAt: new Date().toISOString(),
        updatedAt: undefined,
      };

      const addTempChild = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
        groups.map((g) => {
          const updateItems = (items: CostEstimateItemWeb[]): CostEstimateItemWeb[] =>
            items.map((item) => {
              if (item.id === parentItemId) {
                if (relationType === 1) {
                  return { ...item, options: [...(item.options || []), tempChild] };
                } else {
                  return { ...item, components: [...(item.components || []), tempChild] };
                }
              }
              // Sprawdź komponenty (opcje mogą być dodawane do komponentów)
              if (item.components?.some(c => c.id === parentItemId)) {
                return {
                  ...item,
                  components: item.components.map(comp => {
                    if (comp.id === parentItemId && relationType === 1) {
                      return { ...comp, options: [...(comp.options || []), tempChild] };
                    }
                    return comp;
                  }),
                };
              }
              return item;
            });

          return {
            ...g,
            items: updateItems(g.items || []),
            childGroups: addTempChild(g.childGroups || []),
          };
        });

      // Od razu pokaż w UI
      setDetails(prev => prev ? { ...prev, rootGroups: addTempChild(prev.rootGroups) } : prev);

      try {
        const request: AddItemRequestDto = {
          groupId,
          order: childOrder,
          relationType,
          parentItemId,
        };
        
        const newChildItemId = await costEstimateApi.addItem(
          user.activeTenantId,
          projectId,
          estimateId,
          request
        );

        // Zamień tymczasowy element na prawdziwy z API — zachowaj domyślne fieldValues z optimistic update
        const replaceTempWithReal = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
          groups.map((g) => {
            const updateItems = (items: CostEstimateItemWeb[]): CostEstimateItemWeb[] =>
              items.map((item) => {
                if (item.id === parentItemId) {
                  if (relationType === 1) {
                    return {
                      ...item,
                      options: (item.options || []).map(opt =>
                        opt.id === tempId
                          ? { ...opt, id: newChildItemId }
                          : opt
                      ),
                    };
                  } else {
                    return {
                      ...item,
                      components: (item.components || []).map(comp =>
                        comp.id === tempId
                          ? { ...comp, id: newChildItemId }
                          : comp
                      ),
                    };
                  }
                }
                // Sprawdź komponenty (opcje mogą być dodawane do komponentów)
                if (item.components?.some(c => c.id === parentItemId)) {
                  return {
                    ...item,
                    components: item.components.map(comp => {
                      if (comp.id === parentItemId && relationType === 1) {
                        return {
                          ...comp,
                          options: (comp.options || []).map(opt =>
                            opt.id === tempId
                              ? { ...opt, id: newChildItemId }
                              : opt
                          ),
                        };
                      }
                      return comp;
                    }),
                  };
                }
                return item;
              });

            return {
              ...g,
              items: updateItems(g.items || []),
              childGroups: replaceTempWithReal(g.childGroups || []),
            };
          });
        
        setDetails(prev => prev ? { ...prev, rootGroups: replaceTempWithReal(prev.rootGroups) } : prev);
        return newChildItemId;
      } catch (err) {
        // Usuń tymczasowy element przy błędzie
        const removeTempChild = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
          groups.map((g) => {
            const updateItems = (items: CostEstimateItemWeb[]): CostEstimateItemWeb[] =>
              items.map((item) => {
                if (item.id === parentItemId) {
                  if (relationType === 1) {
                    return { ...item, options: (item.options || []).filter(opt => opt.id !== tempId) };
                  } else {
                    return { ...item, components: (item.components || []).filter(comp => comp.id !== tempId) };
                  }
                }
                // Sprawdź komponenty
                if (item.components?.some(c => c.id === parentItemId)) {
                  return {
                    ...item,
                    components: item.components.map(comp => {
                      if (comp.id === parentItemId && relationType === 1) {
                        return { ...comp, options: (comp.options || []).filter(opt => opt.id !== tempId) };
                      }
                      return comp;
                    }),
                  };
                }
                return item;
              });

            return {
              ...g,
              items: updateItems(g.items || []),
              childGroups: removeTempChild(g.childGroups || []),
            };
          });
        
        setDetails(prev => prev ? { ...prev, rootGroups: removeTempChild(prev.rootGroups) } : prev);
        showError('Błąd', err instanceof Error ? err.message : 'Nie udało się dodać elementu');
        return undefined;
      }
    },
    [user?.activeTenantId, projectId, estimateId, details, showError],
  );

  // ========== PODSUMOWANIA Z SZABLONU ==========

  const summaryStats = useMemo(() => {
    if (!details) return [];
    
    // Get calculated fields from schema that should be shown in summary
    // Note: sumInTotal property is not yet part of schema - showing main calculated fields
    const calculatedFields = (details.schema?.fieldDefinitions ?? [])
      .filter((f) => f.fieldScope === 2) // ItemCalculated = 2
      .filter((f) => [
        FieldType.ItemCalculatedValueNet as number,
        FieldType.ItemCalculatedValueGross as number,
        FieldType.ItemCalculatedTotalVat as number
      ].includes(f.fieldType))
      .sort((a, b) => a.order - b.order);

    const currency =
      details.selectedCurrencySymbol || details.selectedCurrencyCode || '';

    return calculatedFields.map((f) => {
      let value: number | undefined;
      if (f.fieldType === FieldType.ItemCalculatedValueNet) value = details.totalNet;
      else if (f.fieldType === FieldType.ItemCalculatedValueGross) value = details.totalGross;
      else if (f.fieldType === FieldType.ItemCalculatedTotalVat) value = details.totalVat;
      else value = (details as any).summaryValues?.[f.id];

      return {
        id: f.id,
        label: f.label || f.fieldName,
        value: formatCurrency(value, currency),
      };
    });
  }, [details]);

  // ========== PROPS PRZEKAZYWANE DO TABELI (hooki muszą być przed guards!) ==========

  /**
   * Reorder pozycji w grupie — wywołuje API PUT /{id}/groups/{groupId}/items/reorder
   * Używa useMutation z invalidacją cache'u po sukcesie.
   */
  const handleReorderItems = useCallback(async (
    groupId: string,
    itemOrders: Array<{ itemId: string; order: number }>
  ): Promise<void> => {
    await reorderItemsMutation.mutateAsync({ groupId, items: itemOrders });
  }, [reorderItemsMutation]);

  /**
   * Reorder elementów potomnych (opcji/komponentów) w pozycji nadrzędnej
   * Wywołuje API PUT /{id}/items/{parentItemId}/children/reorder
   * Używa useMutation z invalidacją cache'u po sukcesie.
   */
  const handleReorderItemChildren = useCallback(async (
    parentItemId: string,
    itemOrders: Array<{ itemId: string; order: number }>
  ): Promise<void> => {
    await reorderItemChildrenMutation.mutateAsync({ parentItemId, items: itemOrders });
  }, [reorderItemChildrenMutation]);

  /**
   * Reorder grup — wywołuje API PUT /{id}/groups/reorder
   * Obsługuje też przenoszenie grup między parentami (parentGroupId)
   * Używa useMutation z invalidacją cache'u po sukcesie.
   */
  const handleReorderGroups = useCallback(async (
    groupOrders: Array<{ groupId: string; parentGroupId: string | null; order: number }>
  ): Promise<void> => {
    await reorderGroupsMutation.mutateAsync(groupOrders);
  }, [reorderGroupsMutation]);

  /**
   * Przeniesienie pozycji między grupami — wywołuje API PATCH /{id}/items/{itemId}/move
   */
  const handleMoveItem = useCallback(async (
    itemId: string,
    targetGroupId: string
  ): Promise<void> => {
    if (!user?.activeTenantId || !projectId || !estimateId) {
      throw new Error('Brak wymaganych parametrów');
    }
    await costEstimateApi.moveItem(
      user.activeTenantId,
      projectId,
      estimateId,
      itemId,
      {
        costEstimateId: estimateId,
        itemId,
        targetGroupId,
      }
    );
  }, [user?.activeTenantId, projectId, estimateId]);

  const handleUploadFiles = useCallback(async (itemId: string, _fieldDefinitionId: string, files: File[]): Promise<string[]> => {
    if (!user?.activeTenantId || !projectId || !estimateId) {
      throw new Error('Brak wymaganych parametrów');
    }
    return uploadItemFiles(
      user.activeTenantId,
      projectId,
      estimateId,
      itemId,
      files
    );
  }, [user?.activeTenantId, projectId, estimateId]);

  const handleUploadSuccess = useCallback(() => {
    loadCostEstimate();
  }, [loadCostEstimate]);

  /**
   * Otwiera modal uploadu plików dla danej pozycji
   */
  const handleOpenFileUpload = useCallback((itemId: string) => {
    setUploadItemId(itemId);
    onUploadModalOpen();
  }, [onUploadModalOpen]);

  /**
   * Obsługuje wybór opcji (radio button) - zaznacza opcję i kopiuje wartości finansowe do pozycji.
   * - Przy zaznaczeniu: zapisuje kopię oryginalnych wartości rodzica, następnie nadpisuje wartościami opcji
   * - Przy odznaczeniu: przywraca oryginalne wartości rodzica (bez trwałego czyszczenia)
   * - Po każdej zmianie: przelicza sumy etapów
   * - Wysyła request do API, robi optimistic update localnego stanu
   */
  const handleSelectOption = useCallback(
    async (groupId: string, itemId: string, optionId: string) => {
      if (!details || !user?.activeTenantId || !projectId || !estimateId) return;

      // Znajdź opcję i pozycję nadrzędną w drzewie
      let selectedOption: CostEstimateItemWeb | undefined;
      let parentItem: CostEstimateItemWeb | undefined;
      let newIsSelected = false;

      const findItems = (groups: CostEstimateGroupWeb[]): boolean => {
        for (const group of groups) {
          for (const item of group.items || []) {
            if (item.id === itemId) {
              parentItem = item;
              selectedOption = item.options?.find((o) => o.id === optionId);
              if (selectedOption) {
                newIsSelected = !selectedOption.isSelected;
                return true;
              }
            }
            for (const comp of item.components || []) {
              if (comp.id === itemId) {
                parentItem = comp;
                selectedOption = comp.options?.find((o) => o.id === optionId);
                if (selectedOption) {
                  newIsSelected = !selectedOption.isSelected;
                  return true;
                }
              }
            }
          }
          if (findItems(group.childGroups || [])) return true;
        }
        return false;
      };

      if (!findItems(details.rootGroups) || !selectedOption || !parentItem) return;

      // Zachowaj poprzedni stan do rollbacku
      const prevDetails = details;

      // Przy zaznaczaniu: zapisz oryginalne wartości rodzica (jeśli jeszcze nie są zapisane)
      if (newIsSelected && !parentValuesBackupRef.current.has(itemId)) {
        parentValuesBackupRef.current.set(itemId, {
          quantity: parentItem.quantity,
          unit: parentItem.unit,
          unitPriceNet: parentItem.unitPriceNet,
          vatRate: parentItem.vatRate,
          unitPriceGross: parentItem.unitPriceGross,
          netValue: parentItem.netValue,
          grossValue: parentItem.grossValue,
          vatValue: parentItem.vatValue,
          additionalFieldValues: cloneAdditionalFieldValues(parentItem.additionalFieldValues),
        });
      }

      // Optimistic update — zaktualizuj isSelected i wartości finansowe
      setDetails((prev) => {
        if (!prev) return prev;

        const updateItemInTree = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
          return groups.map((group) => ({
            ...group,
            items: group.items.map((item) => {
              if (item.id === itemId) {
                const updatedOptions = item.options?.map((opt) => ({
                  ...opt,
                  isSelected: opt.id === optionId ? newIsSelected : false,
                }));

                // Przywróć oryginalne wartości lub nadpisz z opcji
                const backup = parentValuesBackupRef.current.get(itemId);
                const parentOverride = newIsSelected
                  ? {
                      quantity: selectedOption!.quantity,
                      unit: selectedOption!.unit,
                      unitPriceNet: selectedOption!.unitPriceNet,
                      vatRate: selectedOption!.vatRate,
                      unitPriceGross: selectedOption!.unitPriceGross,
                      netValue: selectedOption!.netValue,
                      grossValue: selectedOption!.grossValue,
                      vatValue: selectedOption!.vatValue,
                      additionalFieldValues: cloneAdditionalFieldValues(selectedOption!.additionalFieldValues),
                    }
                  : backup
                    ? {
                        quantity: backup.quantity,
                        unit: backup.unit,
                        unitPriceNet: backup.unitPriceNet,
                        vatRate: backup.vatRate,
                        unitPriceGross: backup.unitPriceGross,
                        netValue: backup.netValue,
                        grossValue: backup.grossValue,
                        vatValue: backup.vatValue,
                        additionalFieldValues: cloneAdditionalFieldValues(backup.additionalFieldValues),
                      }
                    : {
                        quantity: item.quantity,
                        unit: item.unit,
                        unitPriceNet: item.unitPriceNet,
                        vatRate: item.vatRate,
                        unitPriceGross: item.unitPriceGross,
                        netValue: item.netValue,
                        grossValue: item.grossValue,
                        vatValue: item.vatValue,
                        additionalFieldValues: cloneAdditionalFieldValues(item.additionalFieldValues),
                      };

                return {
                  ...item,
                  options: updatedOptions,
                  ...parentOverride,
                };
              }

              // Szukaj w komponentach
              return {
                ...item,
                components: item.components?.map((comp) => {
                  if (comp.id === itemId) {
                    const updatedOptions = comp.options?.map((opt) => ({
                      ...opt,
                      isSelected: opt.id === optionId ? newIsSelected : false,
                    }));

                    const backup = parentValuesBackupRef.current.get(itemId);
                    const compOverride = newIsSelected
                      ? {
                          quantity: selectedOption!.quantity,
                          unit: selectedOption!.unit,
                          unitPriceNet: selectedOption!.unitPriceNet,
                          vatRate: selectedOption!.vatRate,
                          unitPriceGross: selectedOption!.unitPriceGross,
                          netValue: selectedOption!.netValue,
                          grossValue: selectedOption!.grossValue,
                          vatValue: selectedOption!.vatValue,
                          additionalFieldValues: cloneAdditionalFieldValues(selectedOption!.additionalFieldValues),
                        }
                      : backup
                        ? {
                            quantity: backup.quantity,
                            unit: backup.unit,
                            unitPriceNet: backup.unitPriceNet,
                            vatRate: backup.vatRate,
                            unitPriceGross: backup.unitPriceGross,
                            netValue: backup.netValue,
                            grossValue: backup.grossValue,
                            vatValue: backup.vatValue,
                            additionalFieldValues: cloneAdditionalFieldValues(backup.additionalFieldValues),
                          }
                        : {
                            quantity: comp.quantity,
                            unit: comp.unit,
                            unitPriceNet: comp.unitPriceNet,
                            vatRate: comp.vatRate,
                            unitPriceGross: comp.unitPriceGross,
                            netValue: comp.netValue,
                            grossValue: comp.grossValue,
                            vatValue: comp.vatValue,
                            additionalFieldValues: cloneAdditionalFieldValues(comp.additionalFieldValues),
                          };

                    return {
                      ...comp,
                      options: updatedOptions,
                      ...compOverride,
                    };
                  }
                  return comp;
                }),
              };
            }),
            childGroups: updateItemInTree(group.childGroups || []),
          }));
        };

        const updated = {
          ...prev,
          rootGroups: updateItemInTree(prev.rootGroups),
          lastCalculatedAt: undefined,
        };

        // Przelicz sumy etapów
        return recalculateCostEstimateDetails(updated);
      });

      setHasChanges(true);

      // Wywołaj API, aby zapisać zmianę po stronie serwera
      try {
        await setItemIsSelected(
          user.activeTenantId,
          projectId,
          estimateId,
          optionId,
          newIsSelected,
        );
      } catch {
        // Rollback przy błędzie
        setDetails(prevDetails);
        showError('Błąd zapisu', 'Nie udało się zapisać wyboru opcji');
      }
    },
    [details, user?.activeTenantId, projectId, estimateId, showError]
  );

  /**
   * Przełącza widoczność kolumny (hide/show)
   */
  const handleToggleFieldVisibility = useCallback(
    async (fieldId: string) => {
      if (!user?.activeTenantId || !projectId || !estimateId || !details) return;

      // Szukamy w additionalFields (nowa architektura)
      const additionalField = (details.additionalFields ?? []).find((f) => f.id === fieldId);
      if (!additionalField) return;

      try {
        await updateAdditionalField(user.activeTenantId, projectId, estimateId, fieldId, {
          name: additionalField.name,
        });

        // Odśwież dane
        loadCostEstimate();
      } catch (err) {
        showError('Błąd', 'Nie udało się zmienić widoczności kolumny');
      }
    },
    [user?.activeTenantId, projectId, estimateId, details, loadCostEstimate, showError]
  );

  /**
   * Dodaje nową kolumnę/pole do schematu kosztorysu
   */
  const handleAddField = useCallback(
    async (label: string, fieldScope: number, fieldType: number) => {
      if (!user?.activeTenantId || !projectId || !estimateId) return;

      try {
        await addAdditionalField(user.activeTenantId, projectId, estimateId, {
          name: label,
          fieldType: fieldType as AdditionalFieldType,
        });

        // Odśwież dane
        loadCostEstimate();
      } catch (err) {
        showError('Błąd', 'Nie udało się dodać kolumny');
      }
    },
    [user?.activeTenantId, projectId, estimateId, loadCostEstimate, showError]
  );

  // ========== MODERN VIEW HANDLERS ==========

  // Field change handler for modern view
  // Obsługuje hierarchię: rootGroups → childGroups (rekurencyjnie) → items
  //
  // Nowa architektura: pola bazowe (name, quantity, unit, unitPriceNet, vatRate, isSelected, isStageWork)
  // są bezpośrednimi właściwościami encji, a pola dodatkowe są w additionalFieldValues.
  const handleFieldChangeModern = useCallback(
    (groupId: string, itemId: string | null, fieldId: string, value: string | number | boolean | null) => {
      setDetails((prev) => {
        if (!prev) {
          return prev;
        }
        // Deep clone to avoid mutations
        const updated = JSON.parse(JSON.stringify(prev)) as CostEstimateDetailsWeb;
        
        // Rekurencyjne wyszukiwanie grupy w hierarchii
        const findGroup = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb | undefined => {
          for (const g of groups) {
            if (g.id === groupId) return g;
            if (g.childGroups?.length) {
              const found = findGroup(g.childGroups);
              if (found) return found;
            }
          }
          return undefined;
        };

        // Rekurencyjne wyszukiwanie pozycji (item) — przeszukuje items, options i components w hierarchii
        const findItem = (groups: CostEstimateGroupWeb[]): CostEstimateItemWeb | undefined => {
          for (const g of groups) {
            for (const item of g.items ?? []) {
              // Sprawdź sam item
              if (item.id === itemId) return item;
              // Sprawdź w opcjach itemu
              const inOptions = item.options?.find((o) => o.id === itemId);
              if (inOptions) return inOptions;
              // Sprawdź w komponentach itemu
              const inComponents = item.components?.find((c) => c.id === itemId);
              if (inComponents) return inComponents;
              // Sprawdź w opcjach komponentów
              for (const comp of item.components ?? []) {
                const inCompOptions = comp.options?.find((o) => o.id === itemId);
                if (inCompOptions) return inCompOptions;
              }
            }
            if (g.childGroups?.length) {
              const found = findItem(g.childGroups);
              if (found) return found;
            }
          }
          return undefined;
        };

        // Bazowe pola encji — bezpośrednie właściwości item/group
        const BASE_ITEM_FIELDS = ['name', 'quantity', 'unit', 'unitPriceNet', 'vatRate', 'isSelected', 'isStageWork', 'netValue', 'vatValue', 'grossValue', 'unitPriceGross'] as const;
        const BASE_GROUP_FIELDS = ['name'] as const;
        type BaseItemField = typeof BASE_ITEM_FIELDS[number];
        type BaseGroupField = typeof BASE_GROUP_FIELDS[number];

        // Sprawdź czy pole jest bazowe (bezpośrednia właściwość encji)
        const isBaseItemField = (id: string): id is BaseItemField =>
          (BASE_ITEM_FIELDS as readonly string[]).includes(id);
        const isBaseGroupField = (id: string): id is BaseGroupField =>
          (BASE_GROUP_FIELDS as readonly string[]).includes(id);

        // Resolve additional field ID — komponenty mogą wysyłać "additional_{id}" lub samo id
        const resolveAdditionalFieldId = (id: string): string =>
          id.startsWith('additional_') ? id.substring('additional_'.length) : id;

        // Parsowanie wartości bazowej z uwzględnieniem typu
        const parseBaseValue = (field: string, raw: string | number | boolean | null): string | number | boolean | null | undefined => {
          if (raw === null || raw === '') {
            if (field === 'isSelected' || field === 'isStageWork') {
              if (typeof raw === 'boolean') {
                return raw;
              }
              return false;
            }
            if (
              field === 'quantity'
              || field === 'unitPriceNet'
              || field === 'vatRate'
              || field === 'netValue'
              || field === 'vatValue'
              || field === 'grossValue'
              || field === 'unitPriceGross'
            ) {
              return null;
            }
            return '';
          }
          // Obsługa pól numerycznych — parsuj string → number
          if (field === 'quantity' || field === 'unitPriceNet' || field === 'vatRate' || field === 'netValue' || field === 'vatValue' || field === 'grossValue' || field === 'unitPriceGross') {
            if (typeof raw === 'string') {
              const parsed = parseFloat(raw.replace(',', '.'));
              return isNaN(parsed) ? null : parsed;
            }
            return raw;
          }
          // Obsługa pól boolean
          if (field === 'isSelected' || field === 'isStageWork') {
            if (typeof raw === 'string') return raw === 'true';
            return raw;
          }
          // Pozostałe pola (name, unit) — string
          return raw;
        };

        if (itemId === null) {
          // ========== GROUP FIELD ==========
          const group = findGroup(updated.rootGroups);
          if (!group) return prev;

          if (isBaseGroupField(fieldId)) {
            const parsed = parseBaseValue(fieldId, value);
            if (fieldId === 'name') {
              group.name = parsed === null ? '' : String(parsed);
            }
          } else {
            // Pole dodatkowe grupy — aktualizuj additionalFieldValues
            const additionalFieldId = resolveAdditionalFieldId(fieldId);
            const fieldType = resolveAdditionalFieldType(updated, additionalFieldId);
            group.additionalFieldValues = upsertAdditionalFieldValue(
              group.additionalFieldValues ?? [],
              additionalFieldId,
              fieldType,
              value,
            );
          }
        } else {
          // ========== ITEM FIELD ==========
          const item = findItem(updated.rootGroups);
          if (!item) return prev;

          if (isBaseItemField(fieldId)) {
            // Bazowe pole pozycji — aktualizuj bezpośrednią właściwość
            const parsed = parseBaseValue(fieldId, value);
            const isClear = parsed === null || parsed === undefined;
            switch (fieldId) {
              case 'name': item.name = parsed === null ? '' : String(parsed); break;
              case 'quantity':
                item.quantity = parsed as number | undefined ?? undefined;
                if (isClear) {
                  // quantity wyczyszczone → czyść wszystkie zależne pola kalkulowane
                  item.netValue = undefined;
                  item.vatValue = undefined;
                  item.grossValue = undefined;
                  item.unitPriceGross = undefined;
                }
                break;
              case 'unit':
                item.unit = parsed === null || parsed === '' ? undefined : String(parsed);
                break;
              case 'unitPriceNet':
                item.unitPriceNet = parsed as number | undefined ?? undefined;
                if (isClear) {
                  // unitPriceNet wyczyszczone → czyść wszystkie zależne pola kalkulowane
                  item.netValue = undefined;
                  item.vatValue = undefined;
                  item.grossValue = undefined;
                  item.unitPriceGross = undefined;
                }
                break;
              case 'vatRate':
                item.vatRate = parsed as number | undefined ?? undefined;
                if (isClear) {
                  // vatRate wyczyszczone → czyść wszystkie zależne pola kalkulowane
                  item.vatValue = undefined;
                  item.grossValue = undefined;
                  item.unitPriceGross = undefined;
                }
                break;
              case 'isSelected': item.isSelected = parsed as boolean; break;
              case 'isStageWork': item.isStageWork = parsed as boolean; break;
              case 'netValue': item.netValue = parsed as number | undefined ?? undefined; break;
              case 'vatValue': item.vatValue = parsed as number | undefined ?? undefined; break;
              case 'grossValue': item.grossValue = parsed as number | undefined ?? undefined; break;
              case 'unitPriceGross': item.unitPriceGross = parsed as number | undefined ?? undefined; break;
            }
          } else {
            // Pole dodatkowe pozycji — aktualizuj additionalFieldValues
            const additionalFieldId = resolveAdditionalFieldId(fieldId);
            const fieldType = resolveAdditionalFieldType(updated, additionalFieldId);
            item.additionalFieldValues = upsertAdditionalFieldValue(
              item.additionalFieldValues ?? [],
              additionalFieldId,
              fieldType,
              value,
            );
          }
        }
        
        const recalculated = recalculateCostEstimateDetails(updated);
        setHasChanges(true);
        return recalculated;
      });
    },
    []
  );

  // Reorder groups wrapper (simplified API)
  const handleReorderGroupsModern = useCallback(
    async (groupIds: string[]) => {
      const groupOrders = groupIds.map((groupId, index) => ({
        groupId,
        parentGroupId: null,
        order: index,
      }));
      await handleReorderGroups(groupOrders);
    },
    [handleReorderGroups]
  );

  // Reorder items wrapper (accepts itemOrders directly)
  const handleReorderItemsModern = useCallback(
    async (groupId: string, itemOrders: Array<{ itemId: string; order: number }>) => {
      await handleReorderItems(groupId, itemOrders);
    },
    [handleReorderItems]
  );

  // ========== GUARDS ==========

  if (!user?.activeTenantId || !projectId || !estimateId) {
    return (
      <MainLayout>
        <Box p={10} textAlign="center">
          <Text color="gray.500">Brak wymaganych parametrów</Text>
        </Box>
      </MainLayout>
    );
  }

  if (isLoading) {
    return (
      <MainLayout>
        <LoadingSpinner message="Ładowanie kosztorysu…" fullScreen />
      </MainLayout>
    );
  }

  if (!details) {
    return (
      <MainLayout>
        <Box p={10} textAlign="center">
          <Text color="gray.500">Nie znaleziono kosztorysu</Text>
          <Button mt={4} onClick={loadCostEstimate}>
            Spróbuj ponownie
          </Button>
        </Box>
      </MainLayout>
    );
  }

  const canShareResource =
    canFullEdit && (resourcePerms.mine.canShare || resourcePerms.all.canShare);
  const canSchedule = resourcePerms.raw.canViewSchedule;

  // ========== SHARED TOOLBAR (normalny + fullscreen) ==========

  const toolbar = (
    <Box
      bg={toolbarBg}
      borderBottomWidth="1px"
      borderColor={toolbarBorder}
      px={{ base: 3, md: 5 }}
      py={3}
      position="sticky"
      top={0}
      zIndex={20}
      shadow="sm"
    >
      {/* Wiersz 1: nawigacja, tytuł */}
      <Flex align="center" justify="space-between" gap={3} wrap="wrap">
        {/* Lewa strona: powrót + tytuł + status */}
        <HStack spacing={3} minW={0} flex={1}>
          <Tooltip label="Powrót do listy kosztorysów">
            <IconButton
              aria-label="Powrót"
              icon={<ArrowLeft size={18} />}
              variant="ghost"
              size="sm"
              onClick={() => safeNavigate(`/projects/${projectId}/cost-estimates`)}
            />
          </Tooltip>

          <HStack spacing={2} minW={0}>
            <Icon as={FileSpreadsheet} boxSize={5} color="primary.500" />
            <HStack spacing={1} minW={0}>
              <Text
                fontSize={{ base: 'md', md: 'lg' }}
                fontWeight="bold"
                isTruncated
                maxW={{ base: '200px', md: '400px' }}
              >
                {details.name}
              </Text>
              {hasChanges && (
                <Badge
                  colorScheme="orange"
                  variant="subtle"
                  fontSize="xs"
                  verticalAlign="middle"
                  flexShrink={0}
                >
                  Niezapisane zmiany
                </Badge>
              )}
              {canFullEdit && (
                <Tooltip label="Edytuj nazwę i opis">
                  <IconButton
                    aria-label="Edytuj nazwę i opis"
                    icon={<Pencil size={14} />}
                    size="xs"
                    variant="ghost"
                    onClick={handleOpenEditMeta}
                  />
                </Tooltip>
              )}
            </HStack>
          </HStack>
        </HStack>

        {/* Prawa strona: wskaźniki + fullscreen */}
        <HStack spacing={2} flexShrink={0}>
          {isRecalculating && (
            <HStack
              spacing={1}
              bg="primary.50"
              color="primary.600"
              px={2}
              py={1}
              borderRadius="md"
              fontSize="xs"
              fontWeight="medium"
            >
              <Spinner size="xs" />
              <Text>Przeliczam...</Text>
            </HStack>
          )}
          {lastSavedAt && !isRecalculating && (
            <HStack spacing={1} color="green.500" fontSize="xs">
              <CheckCircle2 size={12} />
              <Text>Przeliczono {formatTime(lastSavedAt)}</Text>
            </HStack>
          )}
          <Box display={{ base: 'none', md: 'block' }}>
            <Tooltip label={isFullscreen ? 'Zamknij pełny ekran (Esc)' : 'Pełny ekran'}>
              <IconButton
                aria-label="Pełny ekran"
                icon={isFullscreen ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
                size="sm"
                variant="outline"
                onClick={() => setIsFullscreen((v) => !v)}
              />
            </Tooltip>
          </Box>
        </HStack>
      </Flex>

      {/* Wiersz 2: podsumowanie finansowe */}
      {summaryStats.length > 0 && (
        <>
          <Divider mt={3} mb={2} borderColor={toolbarBorder} />
          <StatGroup gap={3} flexWrap="wrap" justifyContent="flex-start">
            {summaryStats.map((stat) => (
              <Stat
                key={stat.id}
                bg={statBg}
                px={4}
                py={2}
                borderRadius="lg"
                minW="140px"
                maxW="220px"
                flex="0 1 auto"
              >
                <StatLabel fontSize="xs" color="gray.500" isTruncated>
                  {stat.label}
                </StatLabel>
                <StatNumber fontSize="md" fontWeight="bold" isTruncated>
                  {stat.value}
                </StatNumber>
              </Stat>
            ))}
          </StatGroup>
        </>
      )}

      {/* Wiersz 3: toolbar (dokument + widok) */}
      <Box mt={summaryStats.length > 0 ? 2 : 3}>
        <CostEstimateToolbar
          viewMode={viewMode}
          onViewModeChange={setViewMode}
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
          columnVisibility={
            <ColumnVisibilityPopover
              visibleColIds={visibleColIds}
              onToggleColVisibility={handleToggleColVisibility}
              fieldSchemas={details?.fieldSchemas ?? []}
              additionalFields={details?.additionalFields ?? []}
            />
          }
          canEdit={canAnyEdit}
          canShare={canShareResource}
          canSchedule={canSchedule}
          hasSchedule={!!details.workScheduleId}
          isSyncing={isSyncing}
          isRecalculating={isRecalculating}
          onExpandAll={() => modernViewRef.current?.expandAll()}
          onCollapseAll={() => modernViewRef.current?.collapseAll()}
          onOpenSchema={onSchemaModalOpen}
          onRefresh={() => {
            if (autoRecalcTimeoutRef.current) {
              clearTimeout(autoRecalcTimeoutRef.current);
              autoRecalcTimeoutRef.current = null;
            }
            handleRefresh();
          }}
          onNavigateToSchedule={() => safeNavigate(`/projects/${projectId}/schedules/${details.workScheduleId}`)}
          onCreateSchedule={() => {
            setScheduleModalMode('create');
            onScheduleModalOpen();
          }}
          onSyncSchedule={handleSyncSchedule}
          onShare={onShareModalOpen}
        />
      </Box>
    </Box>
  );

  // ========== FULLSCREEN ==========

  if (isFullscreen) {
    return (
      <>
        <Box
          position="fixed"
          top={0}
          left={0}
          right={0}
          bottom={0}
          bg={pageBg}
          zIndex={9999}
          display="flex"
          flexDirection="column"
        >
          {toolbar}

          <Box
            flex={1}
            minH={0}
            overflow="hidden"
            display="flex"
            flexDirection="column"
            p={{ base: 2, md: 4 }}
          >
            <CostEstimateModernView
              ref={modernViewRef}
              fillHeight
              details={details}
              isEditMode={canAnyEdit}
              tenantId={user.activeTenantId}
              projectId={projectId ?? ''}
              searchQuery={searchQuery}
              onSearchChange={setSearchQuery}
              visibleColIds={visibleColIds}
              onToggleColVisibility={handleToggleColVisibility}
              onFieldChange={handleFieldChangeModern}
              onFieldAutosave={handleFieldAutosave}
              onAddGroup={handleAddGroup}
              onAddSubGroup={(parentGroupId) => handleAddSubGroup(parentGroupId)}
              onAddItem={(groupId) => handleAddItem(groupId)}
              onAddComponent={(groupId, itemId) => handleAddChildItem(groupId, itemId, 2)}
              onAddOption={(groupId, itemId) => handleAddChildItem(groupId, itemId, 1)}
              onDeleteGroup={handleDeleteGroup}
              onDeleteItem={handleDeleteItem}
              onSelectOption={handleSelectOption}
              onUploadFiles={handleOpenFileUpload}
              onReorderGroups={handleReorderGroupsModern}
              onReorderItems={handleReorderItemsModern}
              onReorderItemChildren={handleReorderItemChildren}
              onToggleFieldVisibility={handleToggleFieldVisibility}
              onAddField={handleAddField}
              viewMode={viewMode}
            />
          </Box>
        </Box>

        {/* Dialogi renderowane poza fullscreen overlay */}
        <ConfirmDialog
          isOpen={!!groupToDelete}
          onClose={() => setGroupToDelete(null)}
          onConfirm={confirmDeleteGroup}
          title="Usuń etap"
          message="Czy na pewno chcesz usunąć ten etap? Wszystkie podetapy i pozycje zostaną trwale usunięte. Tej operacji nie można cofnąć."
          confirmText="Usuń etap"
          colorScheme="red"
        />
      </>
    );
  }

  // ========== NORMALNY WIDOK ==========

  return (
    <MainLayout>
      <Box bg={pageBg} minH="100vh">
        {toolbar}

        {/* Kosztorys — widok drzewa (desktop) / karty (mobile) */}
        <Box px={{ base: 1, md: 4 }} py={{ base: 2, md: 3 }}>
          <CostEstimateModernView
            ref={modernViewRef}
            details={details}
            isEditMode={canAnyEdit}
            tenantId={user.activeTenantId}
            projectId={projectId ?? ''}
            searchQuery={searchQuery}
            onSearchChange={setSearchQuery}
            visibleColIds={visibleColIds}
            onToggleColVisibility={handleToggleColVisibility}
            onFieldChange={handleFieldChangeModern}
            onFieldAutosave={handleFieldAutosave}
            onAddGroup={handleAddGroup}
            onAddSubGroup={(parentGroupId) => handleAddSubGroup(parentGroupId)}
            onAddItem={(groupId) => handleAddItem(groupId)}
            onAddComponent={(groupId, itemId) => handleAddChildItem(groupId, itemId, 2)}
            onAddOption={(groupId, itemId) => handleAddChildItem(groupId, itemId, 1)}
            onDeleteGroup={handleDeleteGroup}
            onDeleteItem={handleDeleteItem}
            onSelectOption={handleSelectOption}
            onUploadFiles={handleOpenFileUpload}
            onReorderGroups={handleReorderGroupsModern}
            onReorderItems={handleReorderItemsModern}
            onReorderItemChildren={handleReorderItemChildren}
            onToggleFieldVisibility={handleToggleFieldVisibility}
            onAddField={handleAddField}
            viewMode={viewMode}
          />
        </Box>
      </Box>

      {/* Modal udostępniania kosztorysu */}
      {canShareResource && (
        <ShareCostEstimateModal
          isOpen={isShareModalOpen}
          onClose={onShareModalClose}
          tenantId={user.activeTenantId}
          projectId={projectId}
          costEstimateId={estimateId}
          costEstimateName={details.name}
          ownerId={details.ownerId}
          currentUserId={user.id ?? ''}
          currentSharedUsers={details.sharedWithUsers ?? []}
          onShareUpdated={loadCostEstimate}
        />
      )}

      {/* Modal zarządzania plikami pozycji */}
      {isUploadModalOpen && uploadItemId && details && (
        <>
          <Modal isOpen={isUploadModalOpen} onClose={onUploadModalClose} size="lg">
            <ModalOverlay />
            <ModalContent>
              <ModalHeader>
                <HStack spacing={2}>
                  <Paperclip size={18} />
                  <Text>Załączniki pozycji</Text>
                </HStack>
              </ModalHeader>
              <ModalCloseButton />
              <ModalBody pb={6}>
                <VStack spacing={4} align="stretch">
                  {/* Lista istniejących plików */}
                  {(() => {
                    const currentItem = findItemInTree(details.rootGroups, uploadItemId);
                    const itemFiles = currentItem?.files ?? [];
                    if (itemFiles.length === 0) {
                      return (
                        <Flex
                          direction="column"
                          align="center"
                          justify="center"
                          py={6}
                          color="neutral.500"
                          borderRadius="md"
                          border="2px dashed"
                          borderColor="neutral.200"
                        >
                          <Paperclip size={28} />
                          <Text mt={2} fontSize="sm">Brak załączników</Text>
                          <Text fontSize="xs">Dodaj pliki używając przycisku poniżej</Text>
                        </Flex>
                      );
                    }
                    return (
                      <VStack spacing={2} align="stretch" maxH="300px" overflowY="auto">
                        <Text fontSize="sm" fontWeight="medium" color="neutral.600">
                          Pliki ({itemFiles.length})
                        </Text>
                        {itemFiles.map((file) => (
                          <Flex
                            key={file.id}
                            p={2}
                            borderRadius="md"
                            border="1px solid"
                            borderColor="gray.200"
                            align="center"
                            gap={3}
                          >
                            {/* Miniaturka */}
                            <Box
                              w="40px"
                              h="40px"
                              borderRadius="md"
                              overflow="hidden"
                              bg="neutral.50"
                              display="flex"
                              alignItems="center"
                              justifyContent="center"
                              flexShrink={0}
                              cursor="pointer"
                              onClick={() => {
                                setPreviewFile(file);
                                onPreviewOpen();
                              }}
                            >
                              {file.contentType.startsWith('image/') && file.sasUriPreview ? (
                                <Image
                                  src={file.sasUriPreview}
                                  alt={file.originalFileName}
                                  objectFit="cover"
                                  w="100%"
                                  h="100%"
                                />
                              ) : (
                                <FileText
                                  size={20}
                                  color={file.contentType === 'application/pdf' ? '#E53E3E' : '#718096'}
                                />
                              )}
                            </Box>

                            {/* Nazwa i info */}
                            <VStack align="start" spacing={0} flex={1} minW={0}>
                              <Text fontSize="sm" fontWeight="medium" noOfLines={1} title={file.originalFileName}>
                                {file.originalFileName}
                              </Text>
                              <HStack spacing={2}>
                                <Text fontSize="xs" color="neutral.500">
                                  {formatFileSize(file.fileSize)}
                                </Text>
                                <Badge
                                  size="sm"
                                  colorScheme={file.contentType.startsWith('image/') ? 'green' : 'red'}
                                  fontSize="2xs"
                                >
                                  {file.contentType.startsWith('image/') ? 'JPG' : 'PDF'}
                                </Badge>
                              </HStack>
                            </VStack>

                            {/* Akcje */}
                            <HStack spacing={1}>
                              <Tooltip label="Podgląd">
                                <IconButton
                                  aria-label="Podgląd"
                                  icon={<Eye size={14} />}
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => {
                                    setPreviewFile(file);
                                    onPreviewOpen();
                                  }}
                                />
                              </Tooltip>
                              <Tooltip label="Pobierz">
                                <IconButton
                                  aria-label="Pobierz"
                                  icon={<Download size={14} />}
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => {
                                    if (file.sasUriDownload) {
                                      window.open(file.sasUriDownload, '_blank');
                                    }
                                  }}
                                  isDisabled={!file.sasUriDownload}
                                />
                              </Tooltip>
                              {canAnyEdit && (
                                <Tooltip label="Usuń">
                                  <IconButton
                                    aria-label="Usuń"
                                    icon={<Trash2 size={14} />}
                                    size="sm"
                                    variant="ghost"
                                    colorScheme="red"
                                    onClick={async () => {
                                      const tenantId = user.activeTenantId;
                                      if (!tenantId || !projectId || !estimateId) return;
                                      try {
                                        await deleteItemFile(
                                          tenantId,
                                          projectId,
                                          estimateId,
                                          uploadItemId,
                                          file.id
                                        );
                                        showApiSuccess('deleted');
                                        loadCostEstimate();
                                      } catch (err) {
                                        showError('Błąd usuwania', 'Nie udało się usunąć pliku');
                                      }
                                    }}
                                  />
                                </Tooltip>
                              )}
                            </HStack>
                          </Flex>
                        ))}
                      </VStack>
                    );
                  })()}

                  <Divider />

                  {/* Upload */}
                  <Alert status="info" borderRadius="md">
                    <AlertIcon />
                    <Text fontSize="sm">
                      Dozwolone formaty: PDF, JPG (max 50MB na plik).
                    </Text>
                  </Alert>
                  <Button
                    as="label"
                    htmlFor="file-upload"
                    leftIcon={<Upload size={16} />}
                    colorScheme="primary"
                    variant="outline"
                    cursor="pointer"
                    size="sm"
                  >
                    Dodaj pliki
                    <Input
                      id="file-upload"
                      type="file"
                      multiple
                      hidden
                      accept=".pdf,.jpg,.jpeg"
                      onChange={async (e) => {
                        const files = e.target.files;
                        if (files && files.length > 0) {
                          try {
                            const tenantId = user.activeTenantId;
                            if (!tenantId || !projectId || !estimateId) {
                              return;
                            }
                            const fileArray = Array.from(files);
                            if (uploadItemId) {
                              await uploadItemFiles(
                                tenantId,
                                projectId,
                                estimateId,
                                uploadItemId,
                                fileArray
                              );
                              showApiSuccess('filesUploaded');
                              loadCostEstimate();
                            }
                          } catch (err) {
                            showError('Błąd uploadu', 'Nie udało się przesłać plików');
                          }
                        }
                        e.target.value = '';
                      }}
                    />
                  </Button>
                </VStack>
              </ModalBody>
              <ModalFooter>
                <Button variant="ghost" onClick={onUploadModalClose}>
                  Zamknij
                </Button>
              </ModalFooter>
            </ModalContent>
          </Modal>

          {/* Modal podglądu pliku */}
          <Modal isOpen={isPreviewOpen} onClose={onPreviewClose} size={{ base: "full", md: "4xl" }}>
            <ModalOverlay />
            <ModalContent>
              <ModalHeader>
                <HStack>
                  {previewFile?.contentType.startsWith('image/') ? <ImageIcon size={20} /> : <FileText size={20} />}
                  <Text noOfLines={1}>{previewFile?.originalFileName ?? ''}</Text>
                </HStack>
              </ModalHeader>
              <ModalCloseButton />
              <ModalBody pb={6}>
                {previewFile?.contentType.startsWith('image/') && previewFile?.sasUriPreview ? (
                  <Image
                    src={previewFile.sasUriPreview}
                    alt={previewFile.originalFileName}
                    maxH="70vh"
                    mx="auto"
                  />
                ) : previewFile?.sasUriPreview ? (
                  <Box
                    as="iframe"
                    src={previewFile.sasUriPreview}
                    w="100%"
                    h="70vh"
                    border="none"
                    borderRadius="md"
                  />
                ) : (
                  <Flex direction="column" align="center" justify="center" py={12} color="neutral.500">
                    <FileText size={64} />
                    <Text mt={4}>Podgląd niedostępny</Text>
                  </Flex>
                )}
              </ModalBody>
              <ModalFooter>
                <Button variant="ghost" onClick={onPreviewClose}>
                  Zamknij
                </Button>
              </ModalFooter>
            </ModalContent>
          </Modal>
        </>
      )}

      {/* Modal zarządzania polami dodatkowymi */}
      {canAnyEdit && (
        <SchemaManagerModal
          isOpen={isSchemaModalOpen}
          onClose={onSchemaModalClose}
          fieldSchemas={details.fieldSchemas ?? []}
          costEstimateId={estimateId}
          tenantId={user.activeTenantId}
          projectId={projectId}
          onSchemaUpdated={loadCostEstimate}
          isReadOnly={!canAnyEdit}
        />
      )}

      {/* Modal harmonogramu powiązanego z kosztorysem */}
      <WorkScheduleFormModal
        mode={scheduleModalMode}
        isOpen={isScheduleModalOpen}
        onClose={onScheduleModalClose}
        tenantId={user.activeTenantId}
        projectId={projectId}
        projectName=""
        members={projectMembers}
        initialCostEstimateId={scheduleModalMode === 'create' ? estimateId : undefined}
        initialCostEstimateName={scheduleModalMode === 'create' ? details.name : undefined}
        onSuccess={() => {
          loadCostEstimate();
        }}
      />

      {/* Dialog usuwania grupy */}
      <ConfirmDialog
        isOpen={!!groupToDelete}
        onClose={() => setGroupToDelete(null)}
        onConfirm={confirmDeleteGroup}
        title="Usuń etap"
        message="Czy na pewno chcesz usunąć ten etap? Wszystkie podetapy i pozycje zostaną trwale usunięte. Tej operacji nie można cofnąć."
        confirmText="Usuń etap"
        colorScheme="red"
      />
      <ConfirmDialog
        isOpen={!!itemToDelete}
        onClose={() => setItemToDelete(null)}
        onConfirm={confirmDeleteItem}
        title="Usuń pozycję"
        message="Czy na pewno chcesz usunąć tę pozycję? Operacja jest nieodwracalna."
        confirmText="Usuń pozycję"
        colorScheme="red"
      />
      <ConfirmDialog
        isOpen={!!optionToDelete}
        onClose={() => setOptionToDelete(null)}
        onConfirm={confirmDeleteOption}
        title="Usuń opcję"
        message="Czy na pewno chcesz usunąć tę opcję?"
        confirmText="Usuń opcję"
        colorScheme="red"
      />
      <ConfirmDialog
        isOpen={!!componentToDelete}
        onClose={() => setComponentToDelete(null)}
        onConfirm={confirmDeleteComponent}
        title="Usuń komponent"
        message="Czy na pewno chcesz usunąć ten komponent?"
        confirmText="Usuń komponent"
        colorScheme="red"
      />

      {/* Modal edycji nazwy i opisu */}
      <Modal isOpen={isEditMetaOpen} onClose={onEditMetaClose} isCentered size={{ base: "full", md: "lg" }}>
        <ModalOverlay />
        <ModalContent sx={{ "input, textarea, select": { fontSize: "16px" } }}>
          <ModalHeader>Edytuj kosztorys</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack spacing={4}>
              <FormControl isRequired isInvalid={!!editNameError}>
                <FormLabel>Nazwa</FormLabel>
                <Input
                  value={editName}
                  onChange={(e) => {
                    setEditName(e.target.value);
                    if (editNameError) setEditNameError('');
                  }}
                  placeholder="Nazwa kosztorysu"
                  autoFocus
                />
                <FormErrorMessage>{editNameError}</FormErrorMessage>
              </FormControl>
              <FormControl>
                <FormLabel>Opis</FormLabel>
                <Textarea
                  value={editDescription}
                  onChange={(e) => setEditDescription(e.target.value)}
                  placeholder="Opcjonalny opis kosztorysu"
                  rows={3}
                />
              </FormControl>
            </VStack>
          </ModalBody>
          <ModalFooter>
            <Button variant="ghost" mr={3} onClick={onEditMetaClose}>
              Anuluj
            </Button>
            <Button colorScheme="primary" onClick={handleSaveMetaChanges}>
              Zapisz
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>

      {/* Modal ostrzeżenia o niezapisanych zmianach */}
      <AlertDialog
        isOpen={isUnsavedOpen}
        leastDestructiveRef={unsavedCancelRef}
        onClose={handleCancelLeave}
        isCentered
      >
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader fontSize="lg" fontWeight="bold">
              <HStack spacing={2}>
                <AlertCircle size={24} color="orange" />
                <Text>Niezapisane zmiany</Text>
              </HStack>
            </AlertDialogHeader>

            <AlertDialogBody>
              <Text>
                Masz niezapisane zmiany w kosztorysie. Czy na pewno chcesz opuścić tę stronę?
              </Text>
              <Text mt={2} color="gray.600" fontSize="sm">
                Wszystkie niezapisane zmiany zostaną utracone.
              </Text>
            </AlertDialogBody>

            <AlertDialogFooter>
              <Button ref={unsavedCancelRef} onClick={handleCancelLeave}>
                Zostań na stronie
              </Button>
              <Button colorScheme="red" onClick={handleConfirmLeave} ml={3}>
                Opuść bez zapisywania
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </MainLayout>
  );
};

export default CostEstimateEditPage;