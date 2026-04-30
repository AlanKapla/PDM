import React, { useContext, useState, useEffect, useMemo, useCallback, useRef } from 'react';
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
} from '@chakra-ui/react';
import {
  ArrowLeft,
  Pencil,
  Maximize2,
  Minimize2,
  CheckCircle2,
  AlertCircle,
  FileSpreadsheet,
} from 'lucide-react';
import { AuthContext } from '../context/AuthContext';
import MainLayout from '../layout/MainLayout';
import { CostEstimateTableView } from '../components/CostEstimate/CostEstimateTableView';
import type { CostEstimateTableHandle } from '../components/CostEstimate/CostEstimateTableView';
import CostEstimateToolbar from '../components/CostEstimateToolbar';
import { costEstimateApi } from '../api/costEstimateApi';
import { projectApi } from '../api/projectApi';
import WorkScheduleFormModal from '../components/WorkScheduleFormModal';
import LoadingSpinner from '../components/common/LoadingSpinner';
import ConfirmDialog from '../components/common/ConfirmDialog';
import ShareCostEstimateModal from '../components/ShareCostEstimateModal';
import { useToastNotification } from '../hooks/useToastNotification';
import { useFieldAutosave } from '../hooks/useFieldAutosave';
import { useResourcePermissions } from '../hooks/useResourcePermissions';
import { recalculateCostEstimateDetails } from '../utils/recalculateCostEstimateDetails';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
  CostEstimateFieldValueWeb,
  AddGroupRequestDto,
  AddItemRequestDto,
} from '../types/costEstimate.types.new';
import {
  CostEstimateStatus,
  CostEstimateAccessLevel,
  convertGroupWebToDto,
} from '../types/costEstimate.types.new';

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
const FIELD_TYPE_SELECTED = 104; // FieldType.ItemSystemSelected
const FIELD_SCOPE_ITEM_SYSTEM = 1; // FieldScope.ItemSystem

function buildDefaultItemFieldValues(
  templateStructure: CostEstimateDetailsWeb['templateStructure'],
  relationType: 0 | 1 | 2
): CostEstimateFieldValueWeb[] {
  const selectedFieldDef = (templateStructure?.systemFields as any[])?.find(
    (f: any) => (f.fieldType ?? f.fieldTypeConfig?.fieldType) === FIELD_TYPE_SELECTED
  );

  if (!selectedFieldDef) return [];

  return [{
    id: `temp_default_sel_${selectedFieldDef.id}`,
    fieldDefinitionId: selectedFieldDef.id,
    fieldType: FIELD_TYPE_SELECTED,
    fieldScope: FIELD_SCOPE_ITEM_SYSTEM,
    boolValue: relationType !== 1, // opcje domyślnie odznaczone; reszta zaznaczona
  }];
}

// ---------------------------------------------------------------------------
// Komponent strony
// ---------------------------------------------------------------------------

export const CostEstimateEditPage: React.FC = () => {
  const { projectId, estimateId } = useParams<{
    projectId: string;
    estimateId: string;
  }>();

  const { user } = useContext(AuthContext);
  const navigate = useNavigate();
  const { showSuccess, showError, showApiSuccess } = useToastNotification();

  // ---- Uprawnienia do zasobu ----
  const resourcePerms = useResourcePermissions(projectId);

  // ---- Stan ----
  const [loading, setLoading] = useState(true);
  const [isRecalculating, setIsRecalculating] = useState(false);
  const [details, setDetails] = useState<CostEstimateDetailsWeb | null>(null);
  const [hasChanges, setHasChanges] = useState(false);
  const [isEditMode, setIsEditMode] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  
  // Ref do timeout auto-recalculate (2s po ostatnim zapisie)
  const autoRecalcTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [lastSavedAt, setLastSavedAt] = useState<Date | null>(null);

  // ---- Modal harmonogramu ----
  const { isOpen: isScheduleModalOpen, onOpen: onScheduleModalOpen, onClose: onScheduleModalClose } = useDisclosure();
  const [scheduleModalMode, setScheduleModalMode] = useState<'create' | 'edit'>('create');
  const [isSyncing, setIsSyncing] = useState(false);

  const [projectMembers, setProjectMembers] = useState<any[]>([]);

  // ---- Modal udostępniania ----
  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();

  // ---- Dialog usuwania grupy ----
  const [groupToDelete, setGroupToDelete] = useState<string | null>(null);
  const [itemToDelete, setItemToDelete] =
    useState<{ groupId: string; itemId: string } | null>(null);
  const [optionToDelete, setOptionToDelete] =
    useState<{ groupId: string; itemId: string; optionId: string } | null>(null);
  const [componentToDelete, setComponentToDelete] =
    useState<{ groupId: string; itemId: string; componentId: string } | null>(null);

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

  // ---- Ref do kontroli expand/collapse tabeli ----
  const tableControlsRef = useRef<CostEstimateTableHandle | null>(null);

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

  // ========== BEFOREUNLOAD (zamykanie karty / odświeżanie) ==========
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (hasChangesRef.current) {
        e.preventDefault();
      }
    };
    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, []);

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

  const loadCostEstimate = async () => {
    if (!user?.activeTenantId || !projectId || !estimateId) return;
    try {
      setLoading(true);
      const data = await costEstimateApi.getCostEstimateDetails(
        user.activeTenantId,
        projectId,
        estimateId,
      );
      const recalculated = recalculateCostEstimateDetails(data);
      setDetails(recalculated);
      setHasChanges(false);
      // Członkowie projektu potrzebni do modala tworzenia harmonogramu (tylko Full)
      if (data.accessLevel === CostEstimateAccessLevel.Full) {
        fetchProjectMembers();
      }
    } catch (err) {
      showError(
        'Błąd ładowania',
        err instanceof Error ? err.message : 'Nie udało się załadować kosztorysu',
      );
    } finally {
      setLoading(false);
    }
  };

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
      // Gdy pole było nowe (fieldValueId === null), zaktualizuj lokalny stan o nowe ID oraz wartość.
      // Optimistic update tworzy wpis z id: 'temp_XXXXX'. Musimy go zastąpić prawdziwym ID.
      // Gdybyśmy dodali nowy wpis, w tablicy byłyby dwa wpisy dla tego samego fieldDefinitionId,
      // a kolejna edycja trafiałaby na temp_ → wysyłała fieldValueId: null → wyjątek z backendu.
      if (fieldInfo.fieldValueId === null) {
        // Zbuduj entry z właściwą wartością (ten sam mapping co createUpsertDto w hooku)
        const buildEntry = (): Omit<CostEstimateFieldValueWeb, 'id'> => ({
          fieldDefinitionId: fieldInfo.fieldDefinitionId,
          fieldType: fieldInfo.fieldType,
          fieldScope: 0,
          ...(savedValue !== undefined && savedValue !== '' && {
            ...(fieldInfo.valueType === 'numeric'
              ? { decimalValue: parseFloat(savedValue.replace(',', '.')) || undefined }
              : fieldInfo.valueType === 'boolean'
              ? { boolValue: savedValue === 'true' || savedValue === '1' }
              : fieldInfo.valueType === 'date'
              ? { dateTimeValue: savedValue }
              : { stringValue: savedValue }),
          }),
        });

        // Zastąp istniejący temp entry prawdziwym ID, lub dodaj jeśli nie istnieje
        const upsertFieldValue = (fieldValues: CostEstimateFieldValueWeb[]): CostEstimateFieldValueWeb[] => {
          const idx = fieldValues.findIndex(fv => fv.fieldDefinitionId === fieldInfo.fieldDefinitionId);
          if (idx >= 0) {
            // Zastąp istniejący (temp lub stary) — zachowaj dane pola, nadpisz id i wartość
            const updated = [...fieldValues];
            updated[idx] = { ...updated[idx], ...buildEntry(), id: savedFieldValueId };
            return updated;
          }
          return [...fieldValues, { ...buildEntry(), id: savedFieldValueId }];
        };

        setDetails(prev => {
          if (!prev) return prev;

          if (fieldInfo.entityType === 'group') {
            const updateGroups = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
              groups.map(g => {
                if (g.id === fieldInfo.entityId) {
                  return { ...g, fieldValues: upsertFieldValue(g.fieldValues) };
                }
                return { ...g, childGroups: updateGroups(g.childGroups || []) };
              });
            return { ...prev, rootGroups: updateGroups(prev.rootGroups) };
          } else {
            // item – może być pozycją, opcją lub komponentem (dowolna głębokość)
            const updateItems = (items: CostEstimateItemWeb[]): CostEstimateItemWeb[] =>
              items.map(item => {
                if (item.id === fieldInfo.entityId) {
                  return { ...item, fieldValues: upsertFieldValue(item.fieldValues) };
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

      // Po udanym zapisie pola - zaplanuj auto-recalculate
      scheduleAutoRecalculate();
    },
    onSaveError: (_fieldInfo, error) => {
      showError('Błąd zapisu', 'Nie udało się zapisać zmiany pola');
    },
    enabled: isEditMode,
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
    fieldValueId: string | null;
    fieldDefinitionId: string;
    fieldType: number;
    valueType: 'string' | 'numeric' | 'boolean' | 'date';
    value: string | undefined;
  }) => {
    scheduleFieldSave(
      {
        entityType: params.entityType,
        entityId: params.entityId,
        fieldValueId: params.fieldValueId,
        fieldDefinitionId: params.fieldDefinitionId,
        fieldType: params.fieldType,
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
        if (isEditMode && details && !isRecalculating) {
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
  }, [isEditMode, details, isFullscreen, isRecalculating, handleRefresh]);

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

      // Sprawdź limit zagnieżdżenia z szablonu
      const maxLevel = details.templateStructure?.maxGroupLevel;
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
                  fieldValues: [],
                  totalNet: 0,
                  totalGross: 0,
                  totalVat: 0,
                  lastCalculatedAt: undefined,
                  childGroups: [],
                  items: [],
                  createdAt: new Date().toISOString(),
                  updatedAt: undefined,
                },
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
                  order: itemOrder,
                  fieldValues: buildDefaultItemFieldValues(details.templateStructure, 0),
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
      if (!user?.activeTenantId || !projectId || !estimateId || !details) return;

      // Optimistic: usuń pozycję natychmiast z UI (działa też dla opcji/komponentów wywołanych z TableView).
      // Dla opcji/komponentów filtr na g.items nic nie znajdzie (są zagnieżdżone głębiej),
      // więc UI-remove był już wykonany przez removeOptionFromItem/removeComponentFromItem.
      const del = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
        groups.map((g) => {
          if (g.id === groupId) {
            return { ...g, items: (g.items || []).filter((i) => i.id !== itemId) };
          }
          return { ...g, childGroups: del(g.childGroups || []) };
        });

      const prevDetails = details;
      setDetails(prev => prev ? { ...prev, rootGroups: del(prev.rootGroups) } : prev);

      try {
        await costEstimateApi.deleteItem(
          user.activeTenantId,
          projectId,
          estimateId,
          itemId
        );
      } catch (err) {
        // Przywróć stan przed usunięciem gdy API zwróci błąd
        setDetails(prevDetails);
        showError('Błąd', err instanceof Error ? err.message : 'Nie udało się usunąć pozycji');
      }
    },
    [user?.activeTenantId, projectId, estimateId, details, showError],
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
        fieldValues: buildDefaultItemFieldValues(details.templateStructure, relationType),
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
    const sumFields = (details.templateStructure.calculatedFields || [])
      .filter((f) => f.sumInTotal === true)
      .sort((a, b) => a.order - b.order);

    const currency =
      details.selectedCurrencySymbol || details.selectedCurrencyCode || '';
    const getFieldType = (f: any) =>
      f.fieldType ?? f.fieldTypeConfig?.fieldType;

    return sumFields.map((f) => {
      let value: number | undefined;
      const ft = getFieldType(f);
      if (f.fieldName === 'valueNet' || ft === 203) value = details.totalNet;
      else if (f.fieldName === 'valueGross' || ft === 204) value = details.totalGross;
      else if (f.fieldName === 'totalVat' || ft === 206) value = details.totalVat;
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
   */
  const handleReorderItems = useCallback(async (
    groupId: string,
    itemOrders: Array<{ itemId: string; order: number }>
  ): Promise<void> => {
    if (!user?.activeTenantId || !projectId || !estimateId) {
      throw new Error('Brak wymaganych parametrów');
    }
    await costEstimateApi.reorderItems(
      user.activeTenantId,
      projectId,
      estimateId,
      groupId,
      {
        costEstimateId: estimateId,
        items: itemOrders,
      }
    );
  }, [user?.activeTenantId, projectId, estimateId]);

  /**
   * Reorder grup — wywołuje API PUT /{id}/groups/reorder
   * Obsługuje też przenoszenie grup między parentami (parentGroupId)
   */
  const handleReorderGroups = useCallback(async (
    groupOrders: Array<{ groupId: string; parentGroupId: string | null; order: number }>
  ): Promise<void> => {
    if (!user?.activeTenantId || !projectId || !estimateId) {
      throw new Error('Brak wymaganych parametrów');
    }
    await costEstimateApi.reorderGroups(
      user.activeTenantId,
      projectId,
      estimateId,
      {
        costEstimateId: estimateId,
        groups: groupOrders,
      }
    );
  }, [user?.activeTenantId, projectId, estimateId]);

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

  const handleUploadFiles = useCallback(async (itemId: string, fieldDefinitionId: string, files: File[]): Promise<string[]> => {
    if (!user?.activeTenantId || !projectId || !estimateId) {
      throw new Error('Brak wymaganych parametrów');
    }
    return costEstimateApi.uploadCostEstimateItemFiles(
      user.activeTenantId,
      projectId,
      estimateId,
      itemId,
      fieldDefinitionId,
      files
    );
  }, [user?.activeTenantId, projectId, estimateId]);

  const handleUploadSuccess = useCallback(() => {
    // Po uploadzie odśwież dane kosztorysu, aby uzyskać nowe SAS URI
    loadCostEstimate();
  }, [loadCostEstimate]);

  /** Anuluje tryb edycji i odświeża dane z serwera */
  const handleCancelEdit = useCallback(() => {
    setIsEditMode(false);
    setHasChanges(false);
    loadCostEstimate();
  }, [loadCostEstimate]);

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

  if (loading) {
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

  // Uprawnienia do edycji wynikające z access level kosztorysu i uprawnień w projekcie
  // Full (3) — właściciel lub admin: może edytować wszystko
  const canFullEdit =
    details.accessLevel === CostEstimateAccessLevel.Full &&
    (resourcePerms.mine.canEdit || resourcePerms.all.canEdit);
  // Restricted (2) — udostępniony: może edytować tylko pola nieoznaczone isReadonly
  const canRestrictedEdit =
    details.accessLevel === CostEstimateAccessLevel.Restricted &&
    resourcePerms.shared.canEdit;

  const canAnyEdit = canFullEdit || canRestrictedEdit;
  const canShareResource =
    canFullEdit && (resourcePerms.mine.canShare || resourcePerms.all.canShare);

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
              {isEditMode && canFullEdit && (
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

      {/* Wiersz 2: toolbar akcji – identyczny wzorzec co harmonogram */}
      <Box mt={2}>
        <CostEstimateToolbar
          isEditMode={isEditMode}
          hasChanges={hasChanges}
          canEdit={canAnyEdit}
          canShare={canShareResource}
          hasSchedule={!!details.workScheduleId}
          isSyncing={isSyncing}
          isRecalculating={isRecalculating}
          onExpandAll={() => tableControlsRef.current?.expandAll()}
          onCollapseAll={() => tableControlsRef.current?.collapseAll()}
          onSetViewMode={() => setIsEditMode(false)}
          onSetEditMode={() => setIsEditMode(true)}
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

      {/* Wiersz 3: karty podsumowań */}
      {summaryStats.length > 0 && (
        <StatGroup mt={3} gap={3} flexWrap="wrap" justifyContent="flex-start">
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
      )}
    </Box>
  );

  // ========== TABLE PROPS ==========

  const tableProps = {
    details,
    editable: isEditMode && canAnyEdit,
    accessLevel: details.accessLevel,
    controlsRef: tableControlsRef,
    onDataChange: handleDataChange,
    onAddGroup: handleAddGroup,
    onDeleteGroup: handleDeleteGroup,
    onAddSubGroup: handleAddSubGroup,
    onAddItem: handleAddItem,
    onDeleteItem: handleDeleteItem,
    onRequestDeleteItem: (groupId: string, itemId: string) => setItemToDelete({ groupId, itemId }),
    onRequestDeleteOption: (groupId: string, itemId: string, optionId: string) => setOptionToDelete({ groupId, itemId, optionId }),
    onRequestDeleteComponent: (groupId: string, itemId: string, componentId: string) => setComponentToDelete({ groupId, itemId, componentId }),
    onAddChildItem: handleAddChildItem,
    onUploadFiles: handleUploadFiles,
    onUploadSuccess: handleUploadSuccess,
    onFieldAutosave: handleFieldAutosave,
    onReorderItems: handleReorderItems,
    onReorderGroups: handleReorderGroups,
    onMoveItem: handleMoveItem,
  };

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

          <Box flex={1} overflow="hidden" p={2}>
            <CostEstimateTableView
              {...tableProps}
              maxTableHeight="calc(100vh - 140px)"
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

        {/* Tabela kosztorysu */}
        <Box px={{ base: 2, md: 4 }} py={3}>
          <CostEstimateTableView {...tableProps} />
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