import React, { useContext, useState, useEffect, useMemo, useCallback } from 'react';
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
  Kbd,
  Icon,
} from '@chakra-ui/react';
import {
  ArrowLeft,
  Save,
  RefreshCw,
  Eye,
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
import { costEstimateApiNew } from '../api/costEstimateApiNew';
import LoadingSpinner from '../components/common/LoadingSpinner';
import ConfirmDialog from '../components/common/ConfirmDialog';
import { useToastNotification } from '../hooks/useToastNotification';
import { recalculateCostEstimateDetails } from '../utils/recalculateCostEstimateDetails';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
} from '../types/costEstimate.types.new';
import {
  CostEstimateStatus,
  convertGroupWebToDto,
} from '../types/costEstimate.types.new';

// ---------------------------------------------------------------------------
// Helpery
// ---------------------------------------------------------------------------

/** Mapa statusów kosztorysu → etykieta PL + kolor Chakra */
const STATUS_MAP: Record<CostEstimateStatus, { label: string; color: string }> = {
  [CostEstimateStatus.Draft]: { label: 'Roboczy', color: 'gray' },
  [CostEstimateStatus.InProgress]: { label: 'W trakcie', color: 'blue' },
  [CostEstimateStatus.ReadyForReview]: { label: 'Do przeglądu', color: 'orange' },
  [CostEstimateStatus.Approved]: { label: 'Zatwierdzony', color: 'green' },
  [CostEstimateStatus.Rejected]: { label: 'Odrzucony', color: 'red' },
  [CostEstimateStatus.Archived]: { label: 'Zarchiwizowany', color: 'purple' },
};

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
  const { showSuccess, showError } = useToastNotification();

  // ---- Stan ----
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [details, setDetails] = useState<CostEstimateDetailsWeb | null>(null);
  const [hasChanges, setHasChanges] = useState(false);
  const [isEditMode, setIsEditMode] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [lastSavedAt, setLastSavedAt] = useState<Date | null>(null);

  // ---- Dialog usuwania grupy ----
  const [groupToDelete, setGroupToDelete] = useState<string | null>(null);

  // ---- Kolory (dark mode ready) ----
  const toolbarBg = useColorModeValue('white', 'gray.800');
  const toolbarBorder = useColorModeValue('gray.200', 'gray.700');
  const statBg = useColorModeValue('gray.50', 'gray.700');
  const pageBg = useColorModeValue('gray.50', 'gray.900');
  const segmentBg = useColorModeValue('gray.100', 'gray.700');

  // Ref do hasChanges — potrzebny w navigate guard (closure)
  const hasChangesRef = React.useRef(hasChanges);
  hasChangesRef.current = hasChanges;

  /** Nawigacja z potwierdzeniem gdy są niezapisane zmiany */
  const safeNavigate = useCallback(
    (to: string) => {
      if (
        hasChangesRef.current &&
        !window.confirm('Masz niezapisane zmiany. Czy na pewno chcesz opuścić stronę?')
      ) {
        return;
      }
      navigate(to);
    },
    [navigate],
  );

  // ========== BEFOREUNLOAD (zamykanie karty / odświeżanie) ==========

  useEffect(() => {
    if (!hasChanges) return;
    const handler = (e: BeforeUnloadEvent) => {
      e.preventDefault();
    };
    window.addEventListener('beforeunload', handler);
    return () => window.removeEventListener('beforeunload', handler);
  }, [hasChanges]);

  // ========== ŁADOWANIE DANYCH ==========

  useEffect(() => {
    if (user?.activeTenantId && projectId && estimateId) {
      loadCostEstimate();
    }
  }, [user?.activeTenantId, projectId, estimateId]);

  const loadCostEstimate = async () => {
    if (!user?.activeTenantId || !projectId || !estimateId) return;
    try {
      setLoading(true);
      const data = await costEstimateApiNew.getCostEstimateDetails(
        user.activeTenantId,
        projectId,
        estimateId,
      );
      const recalculated = recalculateCostEstimateDetails(data);
      setDetails(recalculated);
      setHasChanges(false);
    } catch (err) {
      showError(
        'Błąd ładowania',
        err instanceof Error ? err.message : 'Nie udało się załadować kosztorysu',
      );
    } finally {
      setLoading(false);
    }
  };

  // ========== ZAPIS ==========

  const handleSave = useCallback(async () => {
    if (!user?.activeTenantId || !projectId || !estimateId || !details) return;
    try {
      setSaving(true);
      const updateDto = {
        name: details.name,
        description: details.description,
        status: details.status,
        rootGroups: details.rootGroups.map((g) => convertGroupWebToDto(g)),
      };
      await costEstimateApiNew.updateCostEstimate(
        user.activeTenantId,
        projectId,
        estimateId,
        updateDto,
      );
      await loadCostEstimate();
      setHasChanges(false);
      setLastSavedAt(new Date());
      showSuccess('Zapisano', 'Kosztorys został zapisany pomyślnie');
    } catch (err) {
      showError(
        'Błąd zapisu',
        err instanceof Error ? err.message : 'Nie udało się zapisać kosztorysu',
      );
    } finally {
      setSaving(false);
    }
  }, [user?.activeTenantId, projectId, estimateId, details]);

  // ========== KEYBOARD SHORTCUTS ==========

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      // Ctrl+S / Cmd+S → Zapisz
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        if (hasChanges && !saving && isEditMode && details) {
          handleSave();
        }
      }
      // Esc → wyjście z fullscreen
      if (e.key === 'Escape' && isFullscreen) {
        setIsFullscreen(false);
      }
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [hasChanges, saving, isEditMode, details, isFullscreen, handleSave]);

  // ========== MUTACJE DANYCH ==========

  const handleDataChange = useCallback(
    (updated: CostEstimateDetailsWeb) => {
      const recalculated = recalculateCostEstimateDetails(updated);
      setDetails(recalculated);
      setHasChanges(true);
    },
    [],
  );

  const handleAddGroup = useCallback((): string | undefined => {
    if (!details) return undefined;
    const newGroupId = `temp_${Date.now()}`;
    const newGroup: CostEstimateGroupWeb = {
      id: newGroupId,
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
    setDetails({ ...details, rootGroups: [...details.rootGroups, newGroup] });
    setHasChanges(true);
    return newGroupId;
  }, [details]);

  const handleDeleteGroup = useCallback((groupId: string) => {
    setGroupToDelete(groupId);
  }, []);

  const confirmDeleteGroup = useCallback(() => {
    if (!details || !groupToDelete) return;
    const deleteRecursive = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
      groups
        .filter((g) => g.id !== groupToDelete)
        .map((g) => ({ ...g, childGroups: deleteRecursive(g.childGroups || []) }));

    setDetails({ ...details, rootGroups: deleteRecursive(details.rootGroups) });
    setHasChanges(true);
    setGroupToDelete(null);
  }, [details, groupToDelete]);

  const handleAddSubGroup = useCallback(
    (parentGroupId: string): string | undefined => {
      if (!details) return undefined;

      // Sprawdź limit zagnieżdżenia z szablonu
      const maxLevel = details.templateStructure?.maxGroupLevel;
      if (maxLevel != null) {
        const findGroupLevel = (groups: CostEstimateGroupWeb[]): number | undefined => {
          for (const g of groups) {
            if (g.id === parentGroupId) return g.level;
            const childResult = findGroupLevel(g.childGroups || []);
            if (childResult !== undefined) return childResult;
          }
          return undefined;
        };
        const parentLevel = findGroupLevel(details.rootGroups);
        if (parentLevel !== undefined && parentLevel >= maxLevel) {
          showError('Limit zagnieżdżenia', `Maksymalny poziom zagnieżdżenia etapów to ${maxLevel}`);
          return undefined;
        }
      }

      const newId = `temp_${Date.now()}`;
      const addSub = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
        groups.map((g) => {
          if (g.id === parentGroupId) {
            return {
              ...g,
              childGroups: [
                ...(g.childGroups || []),
                {
                  id: newId,
                  parentGroupId,
                  level: g.level + 1,
                  order: (g.childGroups || []).length,
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
          return { ...g, childGroups: addSub(g.childGroups || []) };
        });
      setDetails({ ...details, rootGroups: addSub(details.rootGroups) });
      setHasChanges(true);
      return newId;
    },
    [details],
  );

  const handleAddItem = useCallback(
    (groupId: string) => {
      if (!details) return;
      const addIt = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
        groups.map((g) => {
          if (g.id === groupId) {
            return {
              ...g,
              items: [
                ...(g.items || []),
                {
                  id: `temp_${Date.now()}`,
                  groupId,
                  parentItemId: undefined,
                  order: (g.items || []).length,
                  fieldValues: [],
                  options: [],
                  createdAt: new Date().toISOString(),
                  updatedAt: undefined,
                } as CostEstimateItemWeb,
              ],
            };
          }
          return { ...g, childGroups: addIt(g.childGroups || []) };
        });
      setDetails({ ...details, rootGroups: addIt(details.rootGroups) });
      setHasChanges(true);
    },
    [details],
  );

  const handleDeleteItem = useCallback(
    (groupId: string, itemId: string) => {
      if (!details) return;
      const del = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] =>
        groups.map((g) => {
          if (g.id === groupId) {
            return { ...g, items: (g.items || []).filter((i) => i.id !== itemId) };
          }
          return { ...g, childGroups: del(g.childGroups || []) };
        });
      setDetails({ ...details, rootGroups: del(details.rootGroups) });
      setHasChanges(true);
    },
    [details],
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

  const statusInfo = STATUS_MAP[details.status] || STATUS_MAP[CostEstimateStatus.Draft];

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
      {/* Wiersz 1: nawigacja, tytuł, akcje */}
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
            <Icon as={FileSpreadsheet} boxSize={5} color="blue.500" />
            <Text
              fontSize={{ base: 'md', md: 'lg' }}
              fontWeight="bold"
              isTruncated
              maxW={{ base: '200px', md: '400px' }}
            >
              {details.name}
            </Text>
            <Badge colorScheme={statusInfo.color} fontSize="xs" flexShrink={0}>
              {statusInfo.label}
            </Badge>
          </HStack>
        </HStack>

        {/* Prawa strona: akcje */}
        <HStack spacing={2} flexShrink={0}>
          {/* Indicator niezapisanych zmian */}
          {hasChanges && (
            <HStack
              spacing={1}
              bg="orange.50"
              color="orange.600"
              px={2}
              py={1}
              borderRadius="md"
              fontSize="xs"
              fontWeight="medium"
            >
              <AlertCircle size={12} />
              <Text>Niezapisane</Text>
            </HStack>
          )}

          {/* Ostatni zapis */}
          {lastSavedAt && !hasChanges && (
            <HStack spacing={1} color="green.500" fontSize="xs">
              <CheckCircle2 size={12} />
              <Text>Zapisano {formatTime(lastSavedAt)}</Text>
            </HStack>
          )}

          {/* Przycisk zapisu z hintem Ctrl+S */}
          <Tooltip
            label={
              <HStack spacing={1}>
                <Text>Zapisz zmiany</Text>
                <Kbd fontSize="2xs" bg="whiteAlpha.300">Ctrl+S</Kbd>
              </HStack>
            }
            hasArrow
          >
            <Button
              colorScheme="blue"
              size="sm"
              leftIcon={saving ? <Spinner size="xs" /> : <Save size={14} />}
              onClick={handleSave}
              isDisabled={!hasChanges || saving || !isEditMode}
              isLoading={saving}
              loadingText="Zapisuję…"
            >
              Zapisz
            </Button>
          </Tooltip>

          {/* Odśwież */}
          <Tooltip label="Odśwież dane z serwera">
            <IconButton
              aria-label="Odśwież"
              icon={<RefreshCw size={14} />}
              size="sm"
              variant="outline"
              onClick={loadCostEstimate}
              isDisabled={loading}
            />
          </Tooltip>

          {/* Segmented toggle Edycja / Podgląd */}
          <HStack spacing={0} bg={segmentBg} borderRadius="md" p="2px">
            <Button
              size="sm"
              h="28px"
              fontSize="xs"
              leftIcon={<Pencil size={12} />}
              variant={isEditMode ? 'solid' : 'ghost'}
              colorScheme={isEditMode ? 'blue' : 'gray'}
              onClick={() => setIsEditMode(true)}
              borderRadius="md"
            >
              Edycja
            </Button>
            <Button
              size="sm"
              h="28px"
              fontSize="xs"
              leftIcon={<Eye size={12} />}
              variant={!isEditMode ? 'solid' : 'ghost'}
              colorScheme={!isEditMode ? 'blue' : 'gray'}
              onClick={() => setIsEditMode(false)}
              borderRadius="md"
            >
              Podgląd
            </Button>
          </HStack>

          {/* Fullscreen toggle */}
          <Tooltip label={isFullscreen ? 'Zamknij pełny ekran (Esc)' : 'Pełny ekran'}>
            <IconButton
              aria-label="Pełny ekran"
              icon={isFullscreen ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
              size="sm"
              variant="outline"
              onClick={() => setIsFullscreen((v) => !v)}
            />
          </Tooltip>
        </HStack>
      </Flex>

      {/* Wiersz 2: karty podsumowań */}
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

  // ========== PROPS PRZEKAZYWANE DO TABELI ==========

  const tableProps = {
    details,
    editable: isEditMode,
    onDataChange: handleDataChange,
    onAddGroup: handleAddGroup,
    onDeleteGroup: handleDeleteGroup,
    onAddSubGroup: handleAddSubGroup,
    onAddItem: handleAddItem,
    onDeleteItem: handleDeleteItem,
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


    </MainLayout>
  );
};
