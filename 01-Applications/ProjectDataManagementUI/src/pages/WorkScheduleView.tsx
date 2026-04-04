import React, { useEffect, useState, useContext, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Badge,
  Spinner,
  Alert,
  AlertIcon,
  Button,
  useColorModeValue,
  IconButton,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Tooltip,
  useDisclosure,
  useToast,
  Textarea,
  Checkbox,
  useMediaQuery,
  Divider,
  Input,
  Wrap,
  WrapItem,
  Select,
  Grid,
  GridItem,
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionPanel,
  AccordionIcon,
} from "@chakra-ui/react";
import "./WorkScheduleView.css";
import { ArrowLeft, ArrowRight, Edit, Clock, User, AlertTriangle, ChevronDown, Plus, Trash2, RefreshCw, FileSpreadsheet, MessageSquare } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import WorkScheduleToolbar from "../components/WorkScheduleToolbar";
import ScheduleScaleToolbar from "../components/ScheduleScaleToolbar";
import { projectApi } from "../api/projectApi";
import WorkScheduleFormModal from "../components/WorkScheduleFormModal";
import WorkDetailsModal from "../components/WorkDetailsModal";
import { AuthContext } from "../context/AuthContext";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { WorkScheduleDetailsWeb, EditableComment, EditableWork, EditableStage, UpdateStageDto, UpdateWorkDto, WorkScheduleWorkDependencyWeb, WorkScheduleWorkDependencyDto } from "../types/workSchedule.types";
import {
  getWorkEffectiveDates,
  checkDependencyViolation,
  cascadeAutoAdjust,
  type GenericDependency,
} from "../utils/workScheduleDateConstraints";
import { useTimelineData, type TimeScale } from "../hooks/useTimelineData";

// Krótkie etykiety typów zależności do wyświetlenia na timeline
const DEP_TYPE_SHORT: Record<number, string> = { 0: 'FS', 1: 'SS', 2: 'FF', 3: 'SF' };
const DEP_TYPE_LABEL: Record<number, string> = {
  0: 'Koniec → Start',
  1: 'Start → Start',
  2: 'Koniec → Koniec',
  3: 'Start → Koniec',
};

// Spłaszcza drzewo etapów do płaskiej listy z informacją o głębokości
const flattenStagesToRows = (stages: EditableStage[], depth: number = 0): Array<{ stage: EditableStage; depth: number }> =>
  [...stages]
    .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
    .flatMap(s => [
      { stage: s, depth },
      ...flattenStagesToRows(s.childStages ?? [], depth + 1),
    ]);

// Zbiera rekurencyjnie wszystkie ID etapów (dla "rozwiń wszystko")
const getAllStageIds = (stages: EditableStage[]): string[] =>
  stages.flatMap(s => [s.id, ...getAllStageIds(s.childStages ?? [])]);

// Szuka pracy w drzewie etapów (mutuje znaleziony obiekt – działa na deep-copy)
const findWorkInStages = (stages: EditableStage[], workId: string): EditableWork | null => {
  for (const stage of stages) {
    const work = stage.works?.find((w) => w.id === workId);
    if (work) return work;
    const found = findWorkInStages(stage.childStages ?? [], workId);
    if (found) return found;
  }
  return null;
};

// Rekurencyjnie wywołuje mutację na etapie o podanym ID (działa na deep-copy)
const mutateStageInTree = (stages: EditableStage[], stageId: string, mutator: (s: EditableStage) => void): boolean => {
  for (const s of stages) {
    if (s.id === stageId) { mutator(s); return true; }
    if (mutateStageInTree(s.childStages ?? [], stageId, mutator)) return true;
  }
  return false;
};

// Rekurencyjnie usuwa etap po ID z drzewa
const removeStageFromViewTree = (stages: EditableStage[], stageId: string): EditableStage[] =>
  stages
    .filter(s => s.id !== stageId)
    .map(s => ({ ...s, childStages: removeStageFromViewTree(s.childStages ?? [], stageId) }));

// Rekurencyjnie usuwa pracę po ID ze wszystkich etapów (działa na deep-copy)
const removeWorkFromViewTree = (stages: EditableStage[], workId: string): void => {
  for (const s of stages) {
    const idx = (s.works ?? []).findIndex((w) => w.id === workId);
    if (idx >= 0) {
      s.works.splice(idx, 1);
      s.works.forEach((w, i) => { w.order = i; });
      return;
    }
    removeWorkFromViewTree(s.childStages ?? [], workId);
  }
};

// Walidacja drzewa etapów przed zapisem — ta sama logika co w WorkScheduleFormModal
function validateStagesTree(stages: EditableStage[]): string | null {
  for (const stage of stages) {
    if (!stage.name.trim()) return "Nazwa etapu jest wymagana dla wszystkich etapów";
    if (stage.name.length > 200) return `Nazwa etapu "${stage.name}" nie może przekraczać 200 znaków`;
    for (const work of stage.works ?? []) {
      if (!work.name.trim()) return `Nazwa zakresu robót jest wymagana w etapie "${stage.name}"`;
      if (work.name.length > 200) return `Nazwa zakresu robót "${work.name}" nie może przekraczać 200 znaków`;
      for (const comment of work.comments ?? []) {
        if (comment.content?.trim() && comment.content.length > 2000)
          return `Komentarz w zakresie robót "${work.name}" nie może przekraczać 2000 znaków`;
      }
    }
    const childError = validateStagesTree(stage.childStages ?? []);
    if (childError) return childError;
  }
  return null;
}

// Buduje payload zależności do API — filtruje niepodpane (puste ID lub ten sam zakres z obu stron).
// Działa zarówno dla istniejących (pierwsze ładowanie z DB) jak i nowo dodanych inline.
// Uwaga: pola predecessorDbId/successorDbId używane ponieważ selektory w panelu inline
// oferują tylko zakrsy z realnym DB ID.
const buildDependenciesPayload = (
  dependencies: WorkScheduleWorkDependencyWeb[] | undefined
): WorkScheduleWorkDependencyDto[] =>
  (dependencies ?? [])
    .filter(dep =>
      dep.predecessorWorkId &&
      dep.successorWorkId &&
      dep.predecessorWorkId !== dep.successorWorkId
    )
    .map(dep => {
      const isNewPred = String(dep.predecessorWorkId).startsWith('temp-');
      const isNewSucc = String(dep.successorWorkId).startsWith('temp-');
      return {
        // Nowe zakresy (temp-) → predecessorTempId/successorTempId (UUID po prefiksie "temp-")
        ...(isNewPred
          ? { predecessorTempId: dep.predecessorWorkId.slice(5) }
          : { predecessorDbId: dep.predecessorWorkId }),
        ...(isNewSucc
          ? { successorTempId: dep.successorWorkId.slice(5) }
          : { successorDbId: dep.successorWorkId }),
        dependencyType: dep.dependencyType,
        lagDays: dep.lagDays,
      };
    });

// Buduje polecenie aktualizacji etapu
const mapStageToUpdateCommand = (stage: EditableStage): UpdateStageDto => ({
  // Pomijaj tymczasowe ID nowo dodanych etapów/prac — backend sam je nada
  ...(stage.id && !String(stage.id).startsWith('temp-') ? { id: stage.id } : {}),
  name: stage.name,
  order: stage.order,
  works: (stage.works ?? []).map((work): UpdateWorkDto => ({
    ...(work.id && !String(work.id).startsWith('temp-') ? { id: work.id } : { tempId: work.id.slice(5) }),
    name: work.name,
    order: work.order,
    colorRgb: work.colorRgb,
    isClosed: work.isClosed,
    periods: (work.periods ?? []).map((period) => ({
      ...(period.id && !String(period.id).startsWith('temp-') ? { id: period.id } : {}),
      startDate: period.startDate,
      endDate: period.endDate,
      isClosed: period.isClosed,
    })),
    assignedUserIds: (work.assignees ?? []).map((a) => a.userId),
    comments: (work.comments ?? [])
      .filter((c) => c.content && c.content.trim())
      .map((c) => ({ ...(c.id && !String(c.id).startsWith('temp-') ? { id: c.id } : {}), content: c.content.trim() })),
  })),
  children: (stage.childStages ?? []).map(mapStageToUpdateCommand),
});

export default function WorkScheduleView() {
  const { projectId, workScheduleId } = useParams<{ projectId: string; workScheduleId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const toast = useToast();
  const permissions = useResourcePermissions(projectId);
  const { isOpen: isEditModalOpen, onOpen: onEditModalOpen, onClose: onEditModalClose } = useDisclosure();
  const { isOpen: isWorkDetailsOpen, onOpen: onWorkDetailsOpen, onClose: onWorkDetailsClose } = useDisclosure();
  const [isMobile] = useMediaQuery("(max-width: 768px)");

  const [schedule, setSchedule] = useState<WorkScheduleDetailsWeb | null>(null);
  const [members, setMembers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [columnWidths, setColumnWidths] = useState({
    stage: isMobile ? 90 : 200,
    description: isMobile ? window.innerWidth - 110 : 350,
  });
  const [expandedStages, setExpandedStages] = useState<Set<string>>(new Set());
  const [selectedWork, setSelectedWork] = useState<EditableWork | null>(null);
  const [showComments, setShowComments] = useState(false);
  const [editableSchedule, setEditableSchedule] = useState<WorkScheduleDetailsWeb | null>(null);
  const [isDirty, setIsDirty] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);
  const [scrollHintVisible, setScrollHintVisible] = useState(true);

  const {
    timeScale, setTimeScale,
    timeRangeMonths, setTimeRangeMonths,
    hideWeekends, toggleWeekends,
    dates, dateGroups,
    isToday, formatTimelineDate,
    isWorkInPeriod, getPeriodEnd,
    todayColumnRef, scrollContainerRef, scrollToToday,
  } = useTimelineData({ isMobile });

  // Mapa workId → nazwa zakresu (do wyświetlania etykiet zależności)
  const workNameMap = React.useMemo(() => {
    const map = new Map<string, string>();
    const traverse = (stages: EditableStage[]) => {
      for (const s of stages) {
        for (const w of s.works ?? []) {
          if (w.id && !String(w.id).startsWith('temp-')) map.set(w.id, w.name);
        }
        traverse(s.childStages ?? []);
      }
    };
    traverse(editableSchedule?.stages ?? []);
    return map;
  }, [editableSchedule?.stages]);

  // Płaska lista prac z realnym DB ID — do selectora zależności w trybie inline
  const allDbWorks = React.useMemo(() => {
    const works: Array<{ workId: string; label: string; isNew: boolean }> = [];
    const traverse = (stageList: EditableStage[], pathLabel: string = '') => {
      let idx = 0;
      for (const s of [...stageList].sort((a, b) => (a.order ?? 0) - (b.order ?? 0))) {
        const label = pathLabel ? `${pathLabel}.${idx + 1}` : `${idx + 1}`;
        const stageName = s.name || `Etap ${label}`;
        for (const w of (s.works ?? [])) {
          if (w.id) {
            const isNew = String(w.id).startsWith('temp-');
            const suffix = isNew ? ' (niezapisany)' : '';
            works.push({ workId: w.id, label: `${stageName} / ${w.name || '(bez nazwy)'}${suffix}`, isNew });
          }
        }
        traverse(s.childStages ?? [], label);
        idx++;
      }
    };
    traverse(editableSchedule?.stages ?? []);
    return works;
  }, [editableSchedule?.stages]);

  // Mapa: workId → efektywne daty (min start, max end) dla widoku inline
  const viewWorkDateRanges = React.useMemo(() => {
    const map = new Map<string, { startDate?: string; endDate?: string }>();
    const traverse = (stageList: EditableStage[]) => {
      for (const s of stageList) {
        for (const w of s.works ?? []) {
          if (w.id) map.set(w.id, getWorkEffectiveDates(w.periods ?? []));
        }
        traverse(s.childStages ?? []);
      }
    };
    traverse(editableSchedule?.stages ?? []);
    return map;
  }, [editableSchedule?.stages]);

  // Zależności w formacie generycznym (DB ID jako identyfikator)
  const viewGenericDependencies = React.useMemo<GenericDependency[]>(
    () =>
      (editableSchedule?.dependencies ?? [])
        .filter(d => d.predecessorWorkId && d.successorWorkId)
        .map(d => ({
          predecessorId: d.predecessorWorkId,
          successorId: d.successorWorkId,
          dependencyType: d.dependencyType,
          lagDays: d.lagDays,
        })),
    [editableSchedule?.dependencies]
  );

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const expiredBg = useColorModeValue("red.50", "red.900");
  const todayBg = useColorModeValue("blue.100", "blue.800");
  const depAltBg = useColorModeValue("gray.50", "gray.750");
  const depInvalidBg = useColorModeValue("orange.50", "orange.900");
  const colors = {
    theadBg: useColorModeValue("gray.50", "gray.700"),
    stageBg: useColorModeValue("blue.50", "blue.900"),
    stageHoverBg: useColorModeValue("blue.100", "blue.800"),
    warningBg: useColorModeValue("yellow.50", "yellow.900"),
    completedBg: useColorModeValue("green.50", "green.900"),
  };

  useEffect(() => {
    fetchSchedule();
    fetchMembers();
  }, [user?.activeTenantId, projectId, workScheduleId]);

  useEffect(() => {
    if (schedule) {
      setEditableSchedule(JSON.parse(JSON.stringify(schedule)));
      setIsDirty(false);
    }
  }, [schedule]);

  const fetchSchedule = async () => {
    if (!user?.activeTenantId || !projectId || !workScheduleId) return;

    setLoading(true);
    setError(null);

    try {
      // Pobierz szczegóły pojedynczego harmonogramu
      const response = await projectApi.getWorkSchedule(
        user.activeTenantId,
        projectId,
        workScheduleId
      );

      setSchedule(response.data);
    } catch (err) {
      setError("Błąd podczas pobierania harmonogramu");
    } finally {
      setLoading(false);
    }
  };

  const fetchMembers = async () => {
    if (!user?.activeTenantId || !projectId) return;

    try {
      const response = await projectApi.getProjectMembers(user.activeTenantId, projectId);
      setMembers(response.data);
    } catch (err) {
    }
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString("pl-PL", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  const formatDateTime = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString("pl-PL", {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const _isExpired = (endDate: string): boolean => {
    return new Date(endDate) < new Date();
  };

  const getWorkStatus = (periods: { startDate: string; endDate: string }[]): 'expired' | 'warning' | 'normal' => {
    if (periods.length === 0) return 'normal';

    // Get the last period (by endDate)
    const lastPeriod = periods.reduce((latest, period) => {
      return new Date(period.endDate) > new Date(latest.endDate) ? period : latest;
    }, periods[0]);

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const lastEndDate = new Date(lastPeriod.endDate);
    lastEndDate.setHours(0, 0, 0, 0);

    // Check if expired (ended before today - 1 day, so ended yesterday or earlier)
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (lastEndDate <= yesterday) {
      return 'expired';
    }

    // Check if warning (5 days or less until end)
    const daysUntilEnd = Math.ceil((lastEndDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

    if (daysUntilEnd <= 5 && daysUntilEnd >= 0) {
      return 'warning';
    }

    return 'normal';
  };

  const handleEditMode = () => {
    onEditModalOpen();
  };

  const handleToggleInlineEdit = () => {
    setIsEditing(prev => !prev);
  };

  const handleScheduleUpdated = () => {
    fetchSchedule();
  };

  const toggleWorkClosed = async (workId: string) => {
    if (!editableSchedule || !user?.activeTenantId || !projectId || !workScheduleId) return;

    const updatedSchedule = JSON.parse(JSON.stringify(editableSchedule));

    const work = findWorkInStages(updatedSchedule.stages, workId);
    if (work) {
      const newClosedState = !work.isClosed;
      work.isClosed = newClosedState;

      // Zaznaczenie zakresu pracy → zamknij wszystkie okresy
      // Odznaczenie zakresu pracy → otwórz wszystkie okresy
      work.periods.forEach((p: any) => p.isClosed = newClosedState);
    }

    setEditableSchedule(updatedSchedule);

    // W trybie edycji inline zapis następuje przez przycisk Zapisz
    if (isEditing) {
      setIsDirty(true);
      return;
    }

    // Automatyczny zapis
    try {
      const command = {
        name: updatedSchedule.name,
        stages: updatedSchedule.stages.map((stage: any) => mapStageToUpdateCommand(stage)),
        dependencies: buildDependenciesPayload(updatedSchedule.dependencies),
      };

      const response = await projectApi.updateWorkSchedule(user.activeTenantId, projectId, workScheduleId, command);

      // Użyj zwróconego modelu zamiast ponownego pobierania
      setSchedule(response.data);
      setEditableSchedule(response.data);

      toast({
        title: "Sukces",
        description: "Status pracy został zaktualizowany",
        status: "success",
        duration: 2000,
      });
    } catch (error) {
      toast({
        title: "Błąd",
        description: "Nie udało się zapisać zmian",
        status: "error",
        duration: 3000,
      });
    }
  };

  const togglePeriodClosed = async (workId: string, periodId: string | number) => {
    if (!editableSchedule || !user?.activeTenantId || !projectId || !workScheduleId) return;

    const updatedSchedule = JSON.parse(JSON.stringify(editableSchedule));

    const work = findWorkInStages(updatedSchedule.stages, workId);
    if (work) {
      let period;
      // Jeśli periodId to liczba (indeks), znajdź po indeksie
      if (typeof periodId === 'number') {
        period = work.periods[periodId];
      } else {
        // W przeciwnym razie znajdź po ID
        period = work.periods.find((p: any) => p.id === periodId);
      }

      if (period) {
        period.isClosed = !period.isClosed;

        // Sprawdź czy wszystkie okresy są zamknięte
        const allPeriodsClosed = work.periods.every((p: any) => p.isClosed);
        work.isClosed = allPeriodsClosed;
      }
    }

    setEditableSchedule(updatedSchedule);

    // W trybie edycji inline zapis następuje przez przycisk Zapisz
    if (isEditing) {
      setIsDirty(true);
      return;
    }

    // Automatyczny zapis
    try {
      const command = {
        name: updatedSchedule.name,
        stages: updatedSchedule.stages.map((stage: any) => mapStageToUpdateCommand(stage)),
        dependencies: buildDependenciesPayload(updatedSchedule.dependencies),
      };

      const response = await projectApi.updateWorkSchedule(user.activeTenantId, projectId, workScheduleId, command);

      // Użyj zwróconego modelu zamiast ponownego pobierania
      setSchedule(response.data);
      setEditableSchedule(response.data);

      toast({
        title: "Sukces",
        description: "Status zakresu został zaktualizowany",
        status: "success",
        duration: 2000,
      });
    } catch (error) {
      toast({
        title: "Błąd",
        description: "Nie udało się zapisać zmian",
        status: "error",
        duration: 3000,
      });
    }
  };

  const addWorkComment = (workId: string) => {
    if (!editableSchedule) return;

    const updatedSchedule = JSON.parse(JSON.stringify(editableSchedule));

    const work = findWorkInStages(updatedSchedule.stages, workId);
    if (work) {
      if (!work.comments) work.comments = [];
      work.comments.push({
        id: undefined,
        content: "",
        createdAt: new Date().toISOString(),
        createdByUserId: user?.id || "",
        createdByUserName: user?.firstName + " " + user?.lastName || "Użytkownik",
      });
    }

    setEditableSchedule(updatedSchedule);
    setIsDirty(true);
  };

  const updateWorkComment = (workId: string, commentId: string | undefined, content: string) => {
    if (!editableSchedule) return;

    const updatedSchedule = JSON.parse(JSON.stringify(editableSchedule));

    const work = findWorkInStages(updatedSchedule.stages, workId);
    if (work && work.comments) {
      const comment = work.comments.find((c: any) =>
        commentId ? c.id === commentId : c.id === undefined
      );
      if (comment) {
        comment.content = content;
      }
    }

    setEditableSchedule(updatedSchedule);
    setIsDirty(true);
  };

  const removeWorkComment = (workId: string, commentId: string | undefined) => {
    if (!editableSchedule) return;

    const updatedSchedule = JSON.parse(JSON.stringify(editableSchedule));

    const work = findWorkInStages(updatedSchedule.stages, workId);
    if (work && work.comments) {
      work.comments = work.comments.filter((c: any) =>
        commentId ? c.id !== commentId : c.id !== undefined
      );
    }

    setEditableSchedule(updatedSchedule);
    setIsDirty(true);
  };

  const handleSyncFromCostEstimate = async () => {
    if (!schedule || !user?.activeTenantId || !projectId || !workScheduleId) return;

    try {
      setIsSyncing(true);
      await projectApi.syncWorkScheduleWithEstimate(
        user.activeTenantId,
        projectId,
        workScheduleId
      );
      // Odśwież harmonogram po synchronizacji
      const refreshed = await projectApi.getWorkSchedule(user.activeTenantId, projectId, workScheduleId);
      setSchedule(refreshed.data);
      setEditableSchedule(refreshed.data);
      setIsDirty(false);
      toast({
        title: "Synchronizacja zakończona",
        description: "Struktura etapów została zaktualizowana wg kosztorysu",
        status: "success",
        duration: 4000,
      });
    } catch {
      toast({
        title: "Błąd synchronizacji",
        description: "Nie udało się zsynchronizować harmonogramu z kosztorysem",
        status: "error",
        duration: 3000,
      });
    } finally {
      setIsSyncing(false);
    }
  };

  // ——— Mutacje inline-edit editableSchedule ———

  const updateStageName = (stageId: string, name: string) => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    mutateStageInTree(updated.stages, stageId, (s) => { s.name = name; });
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const updateWorkName = (workId: string, name: string) => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    const work = findWorkInStages(updated.stages, workId);
    if (work) work.name = name;
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const addStage = () => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    updated.stages.push({
      id: `temp-${Date.now()}`,
      name: "",
      order: updated.stages.length,
      works: [],
      childStages: [],
      costEstimateGroupId: null,
    });
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const addWork = (stageId: string) => {
    if (!editableSchedule) return;
    const today = new Date().toISOString().split('T')[0];
    const tomorrow = new Date(Date.now() + 86400000).toISOString().split('T')[0];
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    mutateStageInTree(updated.stages, stageId, (s) => {
      s.works.push({
        id: `temp-${crypto.randomUUID()}`,
        name: "",
        order: s.works.length,
        colorRgb: "#3182CE",
        isClosed: false,
        periods: [{ id: `temp-period-${Date.now()}`, startDate: today, endDate: tomorrow, isClosed: false }],
        assignees: [],
        comments: [],
      });
    });
    setExpandedStages(prev => new Set([...prev, stageId]));
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const removeWork = (workId: string) => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    removeWorkFromViewTree(updated.stages, workId);
    // Usuń zależności odwołujące się do usuniętego zakresu
    if (updated.dependencies) {
      updated.dependencies = updated.dependencies.filter(
        (d: WorkScheduleWorkDependencyWeb) => d.predecessorWorkId !== workId && d.successorWorkId !== workId
      );
    }
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const removeStage = (stageId: string) => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    updated.stages = removeStageFromViewTree(updated.stages, stageId);
    updated.stages.forEach((s: any, i: number) => { s.order = i; });
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const handleSaveAndExitEdit = async () => {
    if (!editableSchedule) return;
    const validationError = validateStagesTree(editableSchedule.stages);
    if (validationError) {
      toast({ title: "Błąd walidacji", description: validationError, status: "error", duration: 4000 });
      return;
    }
    await handleSaveChanges();
    setIsEditing(false);
  };

  const handleCancelEdit = () => {
    if (schedule) setEditableSchedule(JSON.parse(JSON.stringify(schedule)));
    setIsDirty(false);
    setIsEditing(false);
  };

  // Przełącza dzień na timeline: klik w komórkę dodaje/usuwa dzień, a sąsiadujące dni są automatycznie scalane w jeden okres
  const toggleWorkPeriodAtDate = (workId: string, cellStart: Date, _cellEnd: Date) => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    const work = findWorkInStages(updated.stages, workId);
    if (!work) return;
    if (!work.periods) work.periods = [];

    // Lokalna konwersja — unika błędu strefowego (toISOString zwraca UTC)
    const toLocalStr = (d: Date) =>
      `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

    const clickedStr = toLocalStr(cellStart);

    // Zbierz wszystkie aktywne dni ze wszystkich okresów
    // slice(0,10) — obcina ewentualny czas z daty zwróconej przez API (np. "2026-04-05T00:00:00Z")
    // Mapa dzień → isClosed — zachowujemy stan wykonania przy przebudowie okresów
    const activeDays = new Set<string>();
    const dayClosedMap = new Map<string, boolean>();
    for (const p of work.periods) {
      const cur = new Date(p.startDate.slice(0, 10) + 'T00:00:00');
      const end = new Date(p.endDate.slice(0, 10) + 'T00:00:00');
      while (cur <= end) {
        const dayStr = toLocalStr(cur);
        activeDays.add(dayStr);
        dayClosedMap.set(dayStr, !!p.isClosed);
        cur.setDate(cur.getDate() + 1);
      }
    }

    // Toggle klikniętego dnia
    if (activeDays.has(clickedStr)) {
      activeDays.delete(clickedStr);
    } else {
      activeDays.add(clickedStr);
    }

    // Posortowane dni → scalaj sąsiadujące w okresy (odstęp ≥ 1 dzień = nowy okres)
    // isClosed okresu = wszystkie jego dni były w zamkniętym oryginalnym okresie
    const sortedDays = Array.from(activeDays).sort();
    const newPeriods: any[] = [];
    for (const day of sortedDays) {
      if (newPeriods.length === 0) {
        newPeriods.push({ id: `temp-period-${Date.now()}-0`, startDate: day, endDate: day, isClosed: dayClosedMap.get(day) ?? false });
      } else {
        const last = newPeriods[newPeriods.length - 1];
        const nextDay = new Date(last.endDate + 'T00:00:00');
        nextDay.setDate(nextDay.getDate() + 1);
        if (toLocalStr(nextDay) === day) {
          last.endDate = day; // Scal z poprzednim okresem
          // Nowe dni (nie istniejące w dayClosedMap) dziedziczą stan isClosed scalającego okresu.
          // Wyłącznie gdy dzień należał wcześniej do innego otwartego okresu (jawne false) → otwórz.
          if (dayClosedMap.has(day) && dayClosedMap.get(day) === false) {
            last.isClosed = false;
          }
        } else {
          newPeriods.push({ id: `temp-period-${Date.now()}-${newPeriods.length}`, startDate: day, endDate: day, isClosed: dayClosedMap.get(day) ?? false });
        }
      }
    }

    work.periods = newPeriods;

    // Walidacja ograniczeń dat wynikających z zależności
    // Sprawdzamy czy NOWY zakres dat (po toggle) nie narusza żadnej zależności
    if (viewGenericDependencies.length > 0) {
      const newEffective = getWorkEffectiveDates(newPeriods);
      // Tymczasowa mapa z nowymi datami tej pracy — reszta bez zmian
      const tempDateRanges = new Map(viewWorkDateRanges);
      tempDateRanges.set(workId, newEffective);

      // Sprawdź wszystkie zależności, w których bierze udział ta praca
      const relatedDeps = viewGenericDependencies.filter(
        d => d.predecessorId === workId || d.successorId === workId
      );
      for (const dep of relatedDeps) {
        const predName = workNameMap.get(dep.predecessorId) ?? 'Poprzednik';
        const succName = workNameMap.get(dep.successorId) ?? 'Następnik';
        const violation = checkDependencyViolation(dep, tempDateRanges, predName, succName);
        if (violation) {
          toast({
            title: 'Zablokowane — naruszenie zależności',
            description: violation,
            status: 'warning',
            duration: 5000,
            isClosable: true,
          });
          return; // Nie aktualizuj stanu — cofnij toggle
        }
      }
    }

    setEditableSchedule(updated);
    setIsDirty(true);
  };

  // Przypisuje / odpina członka od danej pracy
  const toggleWorkAssignee = (workId: string, userId: string, userName: string) => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    const work = findWorkInStages(updated.stages, workId);
    if (!work) return;
    if (!work.assignees) work.assignees = [];
    const idx = work.assignees.findIndex((a: any) => a.userId === userId);
    if (idx >= 0) {
      work.assignees.splice(idx, 1);
    } else {
      work.assignees.push({ userId, userName });
    }
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  // ——— Inline editing: zarządzanie zależnościami ———

  const addInlineDep = () => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    if (!updated.dependencies) updated.dependencies = [];
    updated.dependencies.push({
      id: `temp-dep-${Date.now()}`,
      predecessorWorkId: '',
      successorWorkId: '',
      dependencyType: 0,
      lagDays: 0,
    } as WorkScheduleWorkDependencyWeb);
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const updateInlineDep = (depId: string, changes: Partial<WorkScheduleWorkDependencyWeb>) => {
    if (!editableSchedule) return;
    const updated: typeof editableSchedule = JSON.parse(JSON.stringify(editableSchedule));
    const dep = (updated.dependencies ?? []).find((d: WorkScheduleWorkDependencyWeb) => d.id === depId);
    if (dep) Object.assign(dep, changes);

    // Auto-shift kaskadowy (Tryb A) gdy zależność jest kompletna
    const completeDep: WorkScheduleWorkDependencyWeb | undefined = dep;
    if (completeDep?.predecessorWorkId && completeDep?.successorWorkId) {
      // Zbuduj mapy potrzebne do kaskady na podstawie aktualnych stages (sprzed zapisu)
      const periodsMap = new Map<string, typeof updated.stages[0]['works'][0]['periods']>();
      const traversePeriods = (stageList: EditableStage[]) => {
        for (const s of stageList) {
          for (const w of s.works ?? []) {
            if (w.id) periodsMap.set(w.id, w.periods ?? []);
          }
          traversePeriods(s.childStages ?? []);
        }
      };
      traversePeriods(updated.stages);

      const allGenericDeps: GenericDependency[] = (updated.dependencies ?? [])
        .filter((d: WorkScheduleWorkDependencyWeb) => d.predecessorWorkId && d.successorWorkId)
        .map((d: WorkScheduleWorkDependencyWeb) => ({
          predecessorId: d.predecessorWorkId,
          successorId: d.successorWorkId,
          dependencyType: d.dependencyType,
          lagDays: d.lagDays,
        }));

      const shifts = cascadeAutoAdjust(
        [completeDep.predecessorWorkId],
        allGenericDeps,
        periodsMap,
        workNameMap
      );

      if (shifts.size > 0) {
        const applyShifts = (stageList: EditableStage[]): EditableStage[] =>
          stageList.map(s => ({
            ...s,
            works: (s.works ?? []).map(w => {
              if (!w.id) return w;
              const shift = shifts.get(w.id);
              return shift ? { ...w, periods: shift.periods as typeof w.periods } : w;
            }),
            childStages: applyShifts(s.childStages ?? []),
          }));

        updated.stages = applyShifts(updated.stages) as typeof updated.stages;

        const shiftLines = Array.from(shifts.entries())
          .map(([id, { shiftedBy }]) => `„${workNameMap.get(id) ?? id}" o ${shiftedBy} dni`)
          .join(', ');

        toast({
          title: 'Daty przesunięte kaskadowo',
          description: `Przesunięto: ${shiftLines}`,
          status: 'info',
          duration: 6000,
          isClosable: true,
        });
      }
    }

    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const removeInlineDep = (depId: string) => {
    if (!editableSchedule) return;
    const updated = JSON.parse(JSON.stringify(editableSchedule));
    updated.dependencies = (updated.dependencies ?? []).filter((d: WorkScheduleWorkDependencyWeb) => d.id !== depId);
    setEditableSchedule(updated);
    setIsDirty(true);
  };

  const handleSaveChanges = async () => {
    if (!editableSchedule || !user?.activeTenantId || !projectId || !workScheduleId) return;

    try {
      const command = {
        name: editableSchedule.name,
        stages: editableSchedule.stages.map((stage: any) => mapStageToUpdateCommand(stage)),
        dependencies: buildDependenciesPayload(editableSchedule.dependencies),
      };

      const response = await projectApi.updateWorkSchedule(user.activeTenantId, projectId, workScheduleId, command);

      // Użyj zwróconego modelu zamiast ponownego pobierania
      setSchedule(response.data);
      setEditableSchedule(response.data);

      toast({
        title: "Sukces",
        description: "Zmiany zostały zapisane",
        status: "success",
        duration: 3000,
      });

      setIsDirty(false);
    } catch (error) {
      toast({
        title: "Błąd",
        description: "Nie udało się zapisać zmian",
        status: "error",
        duration: 3000,
      });
    }
  };

  const handleResizeStart = (e: React.MouseEvent, column: 'stage' | 'description') => {
    e.preventDefault();
    const startX = e.clientX;
    const startWidth = columnWidths[column];

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const diff = moveEvent.clientX - startX;
      const newWidth = Math.max(50, startWidth + diff);
      setColumnWidths(prev => ({ ...prev, [column]: newWidth }));
    };

    const handleMouseUp = () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  const toggleStage = (stageId: string) => {
    setExpandedStages(prev => {
      const newSet = new Set(prev);
      if (newSet.has(stageId)) {
        newSet.delete(stageId);
      } else {
        newSet.add(stageId);
      }
      return newSet;
    });
  };

  const expandAllStages = () => {
    const allIds = getAllStageIds((editableSchedule || schedule)?.stages ?? []);
    setExpandedStages(new Set(allIds));
  };

  const collapseAllStages = () => {
    setExpandedStages(new Set());
  };

  const handleWorkClick = (work: EditableWork) => {
    setSelectedWork(work);
    onWorkDetailsOpen();
  };

  const getStageDataRange = (stage: any) => {
    let minDate: Date | null = null;
    let maxDate: Date | null = null;

    stage.works.forEach((work: any) => {
      work.periods.forEach((period: any) => {
        const start = new Date(period.startDate);
        const end = new Date(period.endDate);

        if (!minDate || start < minDate) minDate = start;
        if (!maxDate || end > maxDate) maxDate = end;
      });
    });

    return { minDate, maxDate };
  };

  // Scroll do dzisiejszej daty po załadowaniu
  useEffect(() => {
    if (!loading && schedule) {
      setTimeout(scrollToToday, 100);
    }
  }, [loading, schedule, scrollToToday]);

  const handleScroll = () => {
    if (scrollContainerRef.current) {
      const { scrollLeft } = scrollContainerRef.current;
      if (scrollLeft > 50) {
        setScrollHintVisible(false);
      }
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Box maxW="1400px" mx="auto" p={8}>
          <HStack justify="center" py={20}>
            <Spinner size="xl" />
          </HStack>
        </Box>
      </MainLayout>
    );
  }

  if (error || !schedule) {
    return (
      <MainLayout>
        <Box maxW="1400px" mx="auto" p={8}>
          <Alert status="error">
            <AlertIcon />
            {error || "Nie znaleziono harmonogramu"}
          </Alert>
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box maxW={isMobile ? "100%" : "1600px"} mx="auto" p={isMobile ? 2 : 8}>
        <VStack spacing={isMobile ? 2 : 6} align="stretch">
          {/* Header */}
          <HStack justify="space-between" flexDirection={isMobile ? "column" : "row"} align={isMobile ? "stretch" : "flex-start"}>
            <div className="workschedule-header">
              <HStack spacing={2} align="flex-start" flexDirection={isMobile ? "column" : "row"}>
                <VStack align="flex-start" spacing={0}>
                  <Heading size={isMobile ? "md" : "lg"}>{schedule?.name}</Heading>
                  <HStack spacing={isMobile ? 2 : 3} fontSize={isMobile ? "10px" : "sm"} color="gray.500" flexWrap="wrap">
                    <HStack spacing={1}>
                      <User size={14} />
                      <Text>{schedule?.createdByUserName}</Text>
                    </HStack>
                    <HStack spacing={1}>
                      <Clock size={14} />
                      <Text>{schedule && formatDateTime(schedule.createdAt)}</Text>
                    </HStack>
                    {schedule?.costEstimateId && (
                      <Badge colorScheme="gray" variant="subtle" fontSize="xs">Powiązany z kosztorysem</Badge>
                    )}
                  </HStack>
                </VStack>
              </HStack>
              <WorkScheduleToolbar
                hasCostEstimate={!!schedule?.costEstimateId}
                showComments={showComments}
                hideWeekends={hideWeekends}
                isEditing={isEditing}
                isDirty={isDirty}
                isSyncing={isSyncing}
                canEdit={permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit}
                onNavigateToCostEstimate={() => schedule?.costEstimateId && navigate(`/projects/${projectId}/cost-estimates/${schedule.costEstimateId}`)}
                onToggleComments={() => setShowComments((v) => !v)}
                onSyncFromCostEstimate={handleSyncFromCostEstimate}
                onAddStage={addStage}
                onEditMode={handleEditMode}
                onToggleInlineEdit={handleToggleInlineEdit}
                onSaveAndExitEdit={handleSaveAndExitEdit}
                onCancelEdit={handleCancelEdit}
                onToggleWeekends={toggleWeekends}
                onScrollToToday={scrollToToday}
                onExpandAll={expandAllStages}
                onCollapseAll={collapseAllStages}
              />
            </div>
          </HStack>
        </VStack>

        {/* Skala i zakres czasu */}
        <Box
          p={3}
          bg={cardBg}
          borderWidth="1px"
          borderColor={borderColor}
          borderRadius="lg"
        >
          <ScheduleScaleToolbar
            timeScale={timeScale}
            setTimeScale={setTimeScale}
            timeRangeMonths={timeRangeMonths}
            setTimeRangeMonths={setTimeRangeMonths}
          />
        </Box>

        {/* Timeline View - Mobile only shows stage and description */}
        <Box
          bg={cardBg}
          borderWidth="1px"
          borderColor={borderColor}
          borderRadius="lg"
          overflow="hidden"
          position="relative"
        >
          <div
            className="workschedule-timeline-scroll"
            ref={scrollContainerRef}
            onScroll={handleScroll}
            style={{
              overflowX: "auto",
              overflowY: "visible",
            }}
          >
            <Table
              className="workschedule-timeline-table"
              variant="simple"
              size={isMobile ? "xs" : "sm"}
              sx={{
                borderCollapse: "collapse",
                "& th, & td": {
                  borderWidth: "1px",
                  borderColor: borderColor,
                  borderStyle: "solid",
                }
              }}
            >
              <Thead bg={colors.theadBg}>
                {/* Group headers for weeks/months - desktop only */}
                {dateGroups && !isMobile && (
                  <Tr>
                    <Th
                      position="sticky"
                      left={0}
                      bg={colors.theadBg}
                      zIndex={3}
                    />
                    <Th
                      position="sticky"
                      left={`${columnWidths.stage}px`}
                      bg={colors.theadBg}
                      zIndex={3}
                    />
                    {dateGroups.map((group, idx) => (
                      <Th
                        key={idx}
                        colSpan={group.count}
                        textAlign="center"
                        py={2}
                        px={2}
                        fontSize="xs"
                        fontWeight="bold"
                        borderBottomWidth="2px"
                      >
                        {group.label}
                      </Th>
                    ))}
                  </Tr>
                )}
                {/* Day headers */}
                <Tr>
                  <Th
                    w={`${columnWidths.stage}px`}
                    minW={`${columnWidths.stage}px`}
                    maxW={`${columnWidths.stage}px`}
                    position="sticky"
                    left={0}
                    bg={colors.theadBg}
                    zIndex={3}
                    fontSize={isMobile ? "10px" : "xs"}
                    py={isMobile ? 1 : 2}
                    px={isMobile ? 1 : 2}
                    fontWeight="bold"
                    textTransform="none"
                  >
                    Etap
                    {!isMobile && (
                      <Box
                        position="absolute"
                        right={0}
                        top={0}
                        bottom={0}
                        w="4px"
                        cursor="col-resize"
                        bg="transparent"
                        _hover={{ bg: "blue.400" }}
                        onMouseDown={(e) => handleResizeStart(e, 'stage')}
                      />
                    )}
                  </Th>
                  <Th
                    w={`${columnWidths.description}px`}
                    minW={`${columnWidths.description}px`}
                    maxW={`${columnWidths.description}px`}
                    position="sticky"
                    left={`${columnWidths.stage}px`}
                    bg={colors.theadBg}
                    zIndex={3}
                    fontSize={isMobile ? "10px" : "xs"}
                    py={isMobile ? 1 : 2}
                    px={isMobile ? 1 : 2}
                    fontWeight="bold"
                    textTransform="none"
                  >
                    Zakres robót
                    {!isMobile && (
                      <Box
                        position="absolute"
                        right={0}
                        top={0}
                        bottom={0}
                        w="4px"
                        cursor="col-resize"
                        bg="transparent"
                        _hover={{ bg: "blue.400" }}
                        onMouseDown={(e) => handleResizeStart(e, 'description')}
                      />
                    )}
                  </Th>
                  {!isMobile && dates.map((date, idx) => {
                    const isTodayCol = isToday(date);
                    return (
                      <Th
                        key={idx}
                        textAlign="center"
                        minW="30px"
                        px={0.5}
                        py={1}
                        fontSize="2xs"
                        fontWeight={isTodayCol ? "bold" : "normal"}
                        textTransform="none"
                        bg={isTodayCol ? todayBg : undefined}
                        borderLeftWidth={isTodayCol ? "2px" : undefined}
                        borderRightWidth={isTodayCol ? "2px" : undefined}
                        borderColor={isTodayCol ? "blue.500" : undefined}
                        color={isTodayCol ? "blue.700" : undefined}
                      >
                        <Text fontSize="2xs" whiteSpace="pre-line" lineHeight="1">
                          {formatTimelineDate(date)}
                        </Text>
                      </Th>
                    );
                  })}
                </Tr>
              </Thead>
              <Tbody>
                {(() => {
                  const flatRows = flattenStagesToRows([...((editableSchedule || schedule)?.stages ?? [])]);
                  return flatRows.map(({ stage, depth }, flatIdx) => {
                    const sortedWorks = [...stage.works].sort((a, b) => a.order - b.order);
                    const { minDate, maxDate } = getStageDataRange(stage);
                    const isLastStage = flatIdx === flatRows.length - 1;
                    const isExpanded = expandedStages.has(stage.id);

                    // Jeśli etap nie ma prac, renderuj pusty wiersz nagłówka
                    if (sortedWorks.length === 0) {
                      return (
                        <Tr
                          key={stage.id}
                          borderBottomWidth={!isLastStage ? "4px" : undefined}
                          borderBottomColor={!isLastStage ? "purple.500" : undefined}
                        >
                          <Td
                            position="sticky"
                            left={0}
                            w={`${columnWidths.stage}px`}
                            minW={`${columnWidths.stage}px`}
                            maxW={`${columnWidths.stage}px`}
                            bg={colors.stageBg}
                            zIndex={1}
                            py={2}
                            px={2}
                            verticalAlign="top"
                            borderRightWidth="2px"
                            borderBottomWidth={!isLastStage ? "4px" : undefined}
                            borderBottomColor={!isLastStage ? "purple.500" : undefined}
                          >
                            <VStack align="flex-start" spacing={1} pl={depth * 4}>
                              {isEditing && !stage.costEstimateGroupId ? (
                                <Input
                                  size="xs"
                                  value={stage.name}
                                  fontWeight="bold"
                                  maxLength={200}
                                  isInvalid={!stage.name.trim()}
                                  onChange={(e) => updateStageName(stage.id, e.target.value)}
                                  onClick={(e) => e.stopPropagation()}
                                  placeholder="Nazwa etapu (wymagane, max 200)"
                                />
                              ) : (
                                <Text fontWeight="bold" fontSize="sm">{stage.name}</Text>
                              )}
                              {stage.costEstimateGroupId && (
                                <Badge colorScheme="gray" fontSize="2xs" variant="outline">Kosztorys</Badge>
                              )}
                              <Badge colorScheme="gray" fontSize="2xs">Brak prac</Badge>
                              {isEditing && (
                                <HStack spacing={1} className="ws-row-actions">
                                  <Tooltip label="Dodaj zakres robót">
                                    <IconButton
                                      size="xs" icon={<Plus size={10} />} colorScheme="green" variant="ghost"
                                      aria-label="Dodaj zakres robót"
                                      onClick={(e) => { e.stopPropagation(); addWork(stage.id); }}
                                    />
                                  </Tooltip>
                                  {!stage.costEstimateGroupId && (
                                    <Tooltip label="Usuń etap">
                                      <IconButton
                                        size="xs" icon={<Trash2 size={10} />} colorScheme="red" variant="ghost"
                                        aria-label="Usuń etap"
                                        onClick={(e) => { e.stopPropagation(); removeStage(stage.id); }}
                                      />
                                    </Tooltip>
                                  )}
                                </HStack>
                              )}
                            </VStack>
                          </Td>
                          <Td
                            position="sticky"
                            left={`${columnWidths.stage}px`}
                            w={`${columnWidths.description}px`}
                            minW={`${columnWidths.description}px`}
                            maxW={`${columnWidths.description}px`}
                            bg={cardBg}
                            zIndex={1}
                            py={2}
                            px={2}
                            borderRightWidth="2px"
                          >
                            <Text fontSize="sm" color="gray.500" fontStyle="italic">
                              Brak zakresu robót w tym etapie
                            </Text>
                            {isEditing && (
                              <Button
                                size="xs" leftIcon={<Plus size={10} />} colorScheme="green" variant="ghost"
                                mt={1}
                                onClick={() => addWork(stage.id)}
                              >
                                Dodaj zakres
                              </Button>
                            )}
                          </Td>
                          {!isMobile && dates.map((periodStart, idx) => {
                            const isTodayColumn = isToday(periodStart);
                            return (
                              <Td
                                key={idx}
                                ref={isTodayColumn ? todayColumnRef : undefined}
                                p={0}
                                bg={isTodayColumn ? colors.stageHoverBg : undefined}
                                borderLeftWidth={isTodayColumn ? "2px" : undefined}
                                borderRightWidth={isTodayColumn ? "2px" : undefined}
                                borderColor={isTodayColumn ? "blue.500" : undefined}
                              >
                                <Box h="100%" minH="50px" />
                              </Td>
                            );
                          })}
                        </Tr>
                      );
                    }

                    return sortedWorks.map((work, workIdx) => {
                      const workStatus = getWorkStatus(work.periods);
                      const warningBg = colors.warningBg;
                      const completedBg = colors.completedBg;
                      const isLastWork = workIdx === sortedWorks.length - 1;

                      let rowBg = undefined;
                      if (work.isClosed) {
                        // Completed work overrides all other statuses
                        rowBg = completedBg;
                      } else if (workStatus === 'expired') {
                        rowBg = expiredBg;
                      } else if (workStatus === 'warning') {
                        rowBg = warningBg;
                      }

                      if (!isExpanded && workIdx > 0) {
                        return null;
                      }

                      return (
                        <Tr
                          key={work.id}
                          bg={rowBg}
                          _hover={{ bg: hoverBg }}
                          borderTopWidth={workIdx > 0 ? "1px" : undefined}
                          borderTopColor={workIdx > 0 ? "gray.100" : undefined}
                          borderBottomWidth={!isLastStage && isLastWork ? "4px" : undefined}
                          borderBottomColor={!isLastStage && isLastWork ? "purple.500" : undefined}
                          display={!isExpanded && workIdx > 0 ? "none" : undefined}
                        >
                          {/* Stage name - merged vertically for all works */}
                          {workIdx === 0 && (
                            <Td
                              rowSpan={isExpanded ? sortedWorks.length : 1}
                              position="sticky"
                              left={0}
                              w={`${columnWidths.stage}px`}
                              minW={`${columnWidths.stage}px`}
                              maxW={`${columnWidths.stage}px`}
                              bg={colors.stageBg}
                              zIndex={1}
                              py={2}
                              px={2}
                              verticalAlign="top"
                              borderRightWidth="2px"
                              borderBottomWidth={!isLastStage && (isExpanded ? isLastWork : true) ? "4px" : undefined}
                              borderBottomColor={!isLastStage && (isExpanded ? isLastWork : true) ? "purple.500" : undefined}
                              cursor="pointer"
                              onClick={() => toggleStage(stage.id)}
                              _hover={{ bg: colors.stageHoverBg }}
                            >
                              <HStack spacing={2}>
                                <IconButton
                                  aria-label={isExpanded ? "Zwiń etap" : "Rozwiń etap"}
                                  icon={<ChevronDown size={16} style={{ transform: isExpanded ? "rotate(0deg)" : "rotate(-90deg)", transition: "transform 0.2s" }} />}
                                  size="xs"
                                  variant="ghost"
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    toggleStage(stage.id);
                                  }}
                                />
                                <VStack align="flex-start" spacing={1} flex={1} pl={depth * 4}>
                                  {isEditing && !stage.costEstimateGroupId ? (
                                    <Input
                                      size="xs"
                                      value={stage.name}
                                      fontWeight="bold"
                                      maxLength={200}
                                      isInvalid={!stage.name.trim()}
                                      onChange={(e) => updateStageName(stage.id, e.target.value)}
                                      onClick={(e) => e.stopPropagation()}
                                      placeholder="Nazwa etapu (wymagane, max 200)"
                                    />
                                  ) : (
                                    <Text fontWeight="bold" fontSize="sm">{stage.name}</Text>
                                  )}
                                  {stage.costEstimateGroupId && (
                                    <Badge colorScheme="gray" fontSize="2xs" variant="outline">Kosztorys</Badge>
                                  )}
                                  {minDate && maxDate && !isMobile && (
                                    <Text fontSize="2xs" color="gray.500">
                                      {formatDate(minDate)} - {formatDate(maxDate)}
                                    </Text>
                                  )}
                                  {!isExpanded && (
                                    <Badge colorScheme="gray" fontSize="2xs" variant="subtle">
                                      {sortedWorks.length} {sortedWorks.length === 1 ? 'praca' : sortedWorks.length < 5 ? 'prace' : 'prac'}
                                    </Badge>
                                  )}
                                  {isEditing && (
                                    <HStack spacing={1} className="ws-row-actions">
                                      <Tooltip label="Dodaj zakres robót">
                                        <IconButton
                                          size="xs" icon={<Plus size={10} />} colorScheme="green" variant="ghost"
                                          aria-label="Dodaj zakres robót"
                                          onClick={(e) => { e.stopPropagation(); addWork(stage.id); }}
                                        />
                                      </Tooltip>
                                      {!stage.costEstimateGroupId && (
                                        <Tooltip label="Usuń etap">
                                          <IconButton
                                            size="xs" icon={<Trash2 size={10} />} colorScheme="red" variant="ghost"
                                            aria-label="Usuń etap"
                                            onClick={(e) => { e.stopPropagation(); removeStage(stage.id); }}
                                          />
                                        </Tooltip>
                                      )}
                                    </HStack>
                                  )}
                                </VStack>
                              </HStack>
                            </Td>
                          )}
                          {/* Work details */}
                          <Td
                            position="sticky"
                            left={`${columnWidths.stage}px`}
                            w={`${columnWidths.description}px`}
                            minW={`${columnWidths.description}px`}
                            maxW={`${columnWidths.description}px`}
                            bg={rowBg || cardBg}
                            zIndex={1}
                            py={2}
                            px={2}
                            borderRightWidth="2px"
                          >
                            <VStack align="flex-start" spacing={1}>
                              <HStack spacing={2} width="100%" align="flex-start">
                                <Tooltip label={work.isClosed ? "Oznacz jako aktywny" : "Oznacz zakres jako zakończony"} hasArrow placement="top" closeOnClick>
                                  <Box as="span" display="inline-flex" mt={1}>
                                    <Checkbox
                                      size="sm"
                                      isChecked={work.isClosed}
                                      onChange={() => toggleWorkClosed(work.id)}
                                      colorScheme="green"
                                      isDisabled={!permissions.mine.canEdit && !permissions.all.canEdit && !permissions.shared.canEdit}
                                    />
                                  </Box>
                                </Tooltip>
                                <VStack align="flex-start" spacing={0} flex={1}>
                                  <HStack spacing={2}>
                                    {work.isClosed ? (
                                      <Badge colorScheme="green" fontSize="2xs">
                                        Zakończone
                                      </Badge>
                                    ) : (
                                      <>
                                        {workStatus === 'expired' && (
                                          <Tooltip label="Praca przeterminowana">
                                            <Box color="red.500">
                                              <AlertTriangle size={12} />
                                            </Box>
                                          </Tooltip>
                                        )}
                                        {workStatus === 'warning' && (
                                          <Tooltip label="Zakończenie za 5 dni lub mniej">
                                            <Box color="yellow.600">
                                              <AlertTriangle size={12} />
                                            </Box>
                                          </Tooltip>
                                        )}
                                      </>
                                    )}
                                    {isEditing ? (
                                      <Input
                                        size="xs"
                                        value={work.name}
                                        maxLength={200}
                                        isInvalid={!work.name.trim()}
                                        onChange={(e) => updateWorkName(work.id, e.target.value)}
                                        onClick={(e) => e.stopPropagation()}
                                        placeholder="Nazwa zakresu robót (wymagane, max 200)"
                                        fontWeight="medium"
                                        flex={1}
                                      />
                                    ) : (
                                      <Text fontSize={isMobile ? "xs" : "sm"} fontWeight="medium">{work.name}</Text>
                                    )}
                                  </HStack>

                                  {work.periods.length > 0 && (
                                    <VStack align="flex-start" spacing={0.5} fontSize="2xs" color="gray.500" width="100%">
                                      {work.periods.map((period: any, pIdx: number) => (
                                        <HStack key={period.id || pIdx} spacing={2} width="100%" justify="space-between">
                                          <Text>
                                            {work.periods.length > 1 ? `${pIdx + 1}. ` : ''}
                                            {formatDate(period.startDate)} - {formatDate(period.endDate)}
                                          </Text>
                                          <Tooltip label={period.isClosed ? "Oznacz okres jako aktywny" : "Oznacz okres jako zakończony"} hasArrow placement="top" closeOnClick>
                                            <Box as="span" display="inline-flex">
                                              <Checkbox
                                                size="sm"
                                                isChecked={period.isClosed}
                                                onChange={() => togglePeriodClosed(work.id, period.id || pIdx)}
                                                colorScheme="green"
                                                isDisabled={!permissions.mine.canEdit && !permissions.all.canEdit && !permissions.shared.canEdit}
                                              />
                                            </Box>
                                          </Tooltip>
                                        </HStack>
                                      ))}
                                    </VStack>
                                  )}

                                  {work.assignees.length > 0 && !isEditing && (
                                    <HStack spacing={1} flexWrap="wrap" mt={0.5}>
                                      {work.assignees.map((assignee: any) => (
                                        <Badge key={assignee.userId} colorScheme="gray" fontSize="2xs" variant="subtle">
                                          {assignee.userName}
                                        </Badge>
                                      ))}
                                    </HStack>
                                  )}

                                  {/* Przypisanie osób w trybie inline-edycji */}
                                  {isEditing && members.length > 0 && (
                                    <Wrap spacing={1} mt={1}>
                                      {members.map((member: any) => {
                                        const isAssigned = (work.assignees ?? []).some((a: any) => a.userId === member.userId);
                                        const displayName = [member.firstName, member.lastName].filter(Boolean).join(' ') || member.email;
                                        return (
                                          <WrapItem key={member.userId}>
                                            <Badge
                                              colorScheme={isAssigned ? "blue" : "gray"}
                                              variant={isAssigned ? "solid" : "outline"}
                                              cursor="pointer"
                                              fontSize="2xs"
                                              px={2} py={0.5}
                                              borderRadius="full"
                                              onClick={(e) => { e.stopPropagation(); toggleWorkAssignee(work.id, member.userId, displayName); }}
                                              _hover={{ opacity: 0.8 }}
                                            >
                                              {displayName}
                                            </Badge>
                                          </WrapItem>
                                        );
                                      })}
                                    </Wrap>
                                  )}

                                  {showComments && (
                                    <VStack align="flex-start" spacing={1} width="100%" mt={1} pt={1} borderTopWidth="1px" fontSize="2xs">
                                      {work.comments && work.comments.length > 0 ? (
                                        <>
                                          <Text fontWeight="bold" fontSize="2xs">Kom:</Text>
                                          {work.comments.map((comment: any, cIdx: number) => (
                                            <HStack key={comment.id || `new-${cIdx}`} spacing={1} align="flex-start" width="100%">
                                              {comment.id === undefined ? (
                                                <Textarea
                                                  value={comment.content}
                                                  onChange={(e) => updateWorkComment(work.id, comment.id, e.target.value)}
                                                  placeholder="Treść komentarza..."
                                                  size="xs"
                                                  fontSize="2xs"
                                                  minH="40px"
                                                  flex={1}
                                                  autoFocus
                                                  onClick={(e) => e.stopPropagation()}
                                                />
                                              ) : (
                                                <Text fontSize="2xs" flex={1} noOfLines={2}>
                                                  {comment.content}
                                                </Text>
                                              )}
                                              {(permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
                                                <IconButton
                                                  aria-label="Usuń komentarz"
                                                  icon={<Trash2 size={10} />}
                                                  size="xs"
                                                  colorScheme="red"
                                                  variant="ghost"
                                                  onClick={() => removeWorkComment(work.id, comment.id)}
                                                />
                                              )}
                                            </HStack>
                                          ))}
                                          {(permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
                                            <IconButton
                                              aria-label="Dodaj komentarz"
                                              icon={<Plus size={10} />}
                                              size="xs"
                                              variant="ghost"
                                              colorScheme="purple"
                                              onClick={() => addWorkComment(work.id)}
                                            />
                                          )}
                                        </>
                                      ) : (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
                                        <IconButton
                                          aria-label="Dodaj komentarz"
                                          icon={<Plus size={10} />}
                                          size="xs"
                                          variant="ghost"
                                          colorScheme="purple"
                                          onClick={() => addWorkComment(work.id)}
                                        />
                                      )}
                                    </VStack>
                                  )}

                                  {/* Zależności — poprzednicy i następniki tego zakresu */}
                                  {!isEditing && (() => {
                                    const deps = editableSchedule?.dependencies ?? [];
                                    const predecessors = deps.filter(d => d.successorWorkId === work.id);
                                    const successors = deps.filter(d => d.predecessorWorkId === work.id);
                                    if (predecessors.length === 0 && successors.length === 0) return null;
                                    return (
                                      <VStack align="flex-start" spacing={0.5} mt={0.5}>
                                        {predecessors.map(dep => (
                                          <Tooltip
                                            key={dep.id}
                                            label={`Zależy od: ${workNameMap.get(dep.predecessorWorkId) || dep.predecessorWorkId} — ${DEP_TYPE_LABEL[dep.dependencyType]}${dep.lagDays ? ` +${dep.lagDays}d` : ''}`}
                                          >
                                            <Badge colorScheme="gray" fontSize="2xs" variant="outline" cursor="default">
                                              ← {DEP_TYPE_SHORT[dep.dependencyType]} {workNameMap.get(dep.predecessorWorkId) || '?'}
                                              {dep.lagDays > 0 && ` +${dep.lagDays}d`}
                                            </Badge>
                                          </Tooltip>
                                        ))}
                                        {successors.map(dep => (
                                          <Tooltip
                                            key={dep.id}
                                            label={`Blokuje: ${workNameMap.get(dep.successorWorkId) || dep.successorWorkId} — ${DEP_TYPE_LABEL[dep.dependencyType]}${dep.lagDays ? ` +${dep.lagDays}d` : ''}`}
                                          >
                                            <Badge colorScheme="gray" fontSize="2xs" variant="outline" cursor="default">
                                              → {DEP_TYPE_SHORT[dep.dependencyType]} {workNameMap.get(dep.successorWorkId) || '?'}
                                              {dep.lagDays > 0 && ` +${dep.lagDays}d`}
                                            </Badge>
                                          </Tooltip>
                                        ))}
                                      </VStack>
                                    );
                                  })()}
                                </VStack>
                                {isEditing && (
                                  <Box className="ws-row-actions">
                                    <Tooltip label="Usuń zakres robót">
                                      <IconButton
                                        size="xs"
                                        icon={<Trash2 size={10} />}
                                        colorScheme="red"
                                        variant="ghost"
                                        aria-label="Usuń zakres robót"
                                        flexShrink={0}
                                        mt={0.5}
                                        onClick={(e) => { e.stopPropagation(); removeWork(work.id); }}
                                      />
                                    </Tooltip>
                                  </Box>
                                )}
                              </HStack>
                            </VStack>
                          </Td>
                          {/* Timeline cells - desktop only */}
                          {!isMobile && dates.map((periodStart, idx) => {
                            const periodEnd = getPeriodEnd(periodStart);
                            const isActive = work.periods.some((period: any) =>
                              isWorkInPeriod(period.startDate, period.endDate, periodStart, periodEnd)
                            );
                            const isTodayColumn = isToday(periodStart);

                            return (
                              <Td
                                key={idx}
                                ref={isTodayColumn ? todayColumnRef : undefined}
                                p={0}
                                position="relative"
                                cursor={isEditing ? "crosshair" : isActive ? "pointer" : "default"}
                                bg={isActive ? work.colorRgb : (isTodayColumn ? colors.stageHoverBg : undefined)}
                                borderLeftWidth={isTodayColumn ? "2px" : undefined}
                                borderRightWidth={isTodayColumn ? "2px" : undefined}
                                borderColor={isTodayColumn ? "blue.500" : undefined}
                                onClick={isEditing
                                  ? () => toggleWorkPeriodAtDate(work.id, periodStart, periodEnd)
                                  : isActive ? () => handleWorkClick(work) : undefined
                                }
                              >
                                {isActive ? (
                                  <Tooltip label={isEditing
                                    ? "Kliknij aby usunąć ten okres"
                                    : `${work.name}${work.periods.length > 1 ? ` (${work.periods.length} okresów)` : ''} \u2013 kliknij aby edytować`
                                  }>
                                    <Box
                                      h="100%"
                                      minH="50px"
                                      w="100%"
                                      bg={work.colorRgb}
                                      transition="opacity 0.1s"
                                      _hover={{ opacity: 0.7 }}
                                      borderLeftWidth={isTodayColumn ? "2px" : undefined}
                                      borderRightWidth={isTodayColumn ? "2px" : undefined}
                                      borderColor={isTodayColumn ? "blue.500" : undefined}
                                    />
                                  </Tooltip>
                                ) : (
                                  <Tooltip
                                    label={isEditing ? "Kliknij aby dodać okres" : undefined}
                                    isDisabled={!isEditing}
                                    openDelay={500}
                                  >
                                    <Box
                                      h="100%"
                                      minH="50px"
                                      w="100%"
                                      bg={isEditing ? work.colorRgb : undefined}
                                      opacity={isEditing ? 0.08 : undefined}
                                      transition="opacity 0.1s"
                                      _hover={isEditing ? { opacity: 0.3 } : undefined}
                                    />
                                  </Tooltip>
                                )}
                              </Td>
                            );
                          })}
                        </Tr>
                      );
                    });
                  });
                })()}
              </Tbody>
            </Table>
          </div>
        </Box>

        {/* Panel zależności — widoczny podczas edycji inline */}
        {isEditing && (
          <Box
            bg={cardBg}
            borderWidth="1px"
            borderColor={borderColor}
            borderRadius="lg"
            p={isMobile ? 3 : 4}
          >
            <HStack justify="space-between" mb={3} flexWrap="wrap" gap={2}>
              <VStack align="flex-start" spacing={0}>
                <Text fontWeight="bold" fontSize={isMobile ? "sm" : "md"}>Zależności między zakresami</Text>
                <Text fontSize="xs" color="gray.500">Określ, który zakres musi się zakończyć (lub rozpocząć), zanim inny może ruszyć</Text>
              </VStack>
            </HStack>

            {/* Legenda typów — zwijana */}
            <Accordion allowToggle mb={3} borderRadius="md" overflow="hidden">
              <AccordionItem border="1px solid" borderColor={useColorModeValue("gray.200", "gray.600")} borderRadius="md">
                <AccordionButton px={3} py={2} _expanded={{ bg: useColorModeValue("gray.50", "gray.700") }}>
                  <Box flex="1" textAlign="left">
                    <Text fontSize="xs" fontWeight="semibold" color="gray.500">Jak czytać typy zależności? (FS / SS / FF / SF)</Text>
                  </Box>
                  <AccordionIcon />
                </AccordionButton>
                <AccordionPanel pb={3} px={3} bg={useColorModeValue("gray.50", "gray.750")}>
                  <Text fontSize="xs" color="gray.600" mb={2}>
                    Zakres <strong>A</strong> (poprzednik) musi osiągnąć dany punkt, zanim zakres <strong>B</strong> (następnik) będzie mógł zacząć lub skończyć.
                    Typ zależności opisuje <em>który koniec A warunkuje który koniec B</em>.
                  </Text>
                  <HStack spacing={4} flexWrap="wrap">
                    <HStack spacing={1}><Text fontSize="xs" fontWeight="bold" color="teal.500">FS</Text><Text fontSize="xs" color="gray.600">— A musi się <strong>zakończyć</strong>, żeby B mogło się <strong>rozpocząć</strong> (najczęstsze)</Text></HStack>
                    <HStack spacing={1}><Text fontSize="xs" fontWeight="bold" color="teal.500">SS</Text><Text fontSize="xs" color="gray.600">— A musi się <strong>rozpocząć</strong>, żeby B mogło się <strong>rozpocząć</strong></Text></HStack>
                    <HStack spacing={1}><Text fontSize="xs" fontWeight="bold" color="teal.500">FF</Text><Text fontSize="xs" color="gray.600">— A musi się <strong>zakończyć</strong>, żeby B mogło się <strong>zakończyć</strong></Text></HStack>
                    <HStack spacing={1}><Text fontSize="xs" fontWeight="bold" color="teal.500">SF</Text><Text fontSize="xs" color="gray.600">— A musi się <strong>rozpocząć</strong>, żeby B mogło się <strong>zakończyć</strong> (rzadkie)</Text></HStack>
                  </HStack>
                </AccordionPanel>
              </AccordionItem>
            </Accordion>

            {allDbWorks.length < 2 ? (
              <Text fontSize="sm" color="gray.400" textAlign="center" py={2}>
                Zapisz co najmniej dwa zakresy robót, aby móc definiować zależności.
              </Text>
            ) : (editableSchedule?.dependencies ?? []).length === 0 ? (
              <Text fontSize="sm" color="gray.400" textAlign="center" py={2}>
                Brak zależności. Skorzystaj z przycisku poniżej, aby połączyć zakresy robót.
              </Text>
            ) : (
              <VStack spacing={3} align="stretch">
                {/* Nagłówek kolumn */}
                <Grid
                  templateColumns="2fr 3fr 90px 20px 2fr 36px"
                  gap={2}
                  px={3}
                  display={isMobile ? "none" : "grid"}
                >
                  <Text fontSize="xs" color="gray.500" textAlign="center">Poprzednik (A — zakres wcześniejszy)</Text>
                  <Text fontSize="xs" color="gray.500" textAlign="center">Typ zależności</Text>
                  <Text fontSize="xs" color="gray.500" textAlign="center">Opóźnienie</Text>
                  <Box />
                  <Text fontSize="xs" color="gray.500" textAlign="center">Następnik (B — zakres zależny)</Text>
                  <Box />
                </Grid>

                {(editableSchedule?.dependencies ?? []).map((dep: WorkScheduleWorkDependencyWeb, depIdx: number) => {
                  const depTypeLabel = DEP_TYPE_SHORT[dep.dependencyType] ?? 'FS';
                  const isValid = dep.predecessorWorkId && dep.successorWorkId && dep.predecessorWorkId !== dep.successorWorkId;
                  return (
                    <Box
                      key={dep.id}
                      p={isMobile ? 2 : 3}
                      borderWidth="1px"
                      borderRadius="md"
                      borderColor={isValid ? borderColor : "orange.300"}
                      bg={!isValid ? depInvalidBg : depIdx % 2 !== 0 ? depAltBg : undefined}
                    >
                      {/* Na mobile: etykiety nad polami */}
                      {isMobile && (
                        <VStack spacing={2} align="stretch">
                          <Box>
                            <Text fontSize="xs" color="gray.500" mb={1}>Poprzednik (A — zakres wcześniejszy)</Text>
                            <Select
                              size="sm"
                              placeholder="— wybierz zakres A —"
                              value={dep.predecessorWorkId}
                              onChange={(e) => updateInlineDep(dep.id, { predecessorWorkId: e.target.value })}
                              isInvalid={!dep.predecessorWorkId}
                            >
                              {allDbWorks
                                .filter(w => w.workId !== dep.successorWorkId)
                                .map(w => (
                                  <option key={w.workId} value={w.workId}>{w.label}</option>
                                ))
                              }
                            </Select>
                          </Box>
                          <HStack spacing={2}>
                            <Box flex={1}>
                              <Text fontSize="xs" color="gray.500" mb={1}>Typ ({depTypeLabel})</Text>
                              <Select
                                size="sm"
                                value={String(dep.dependencyType)}
                                onChange={(e) => updateInlineDep(dep.id, { dependencyType: parseInt(e.target.value, 10) })}
                              >
                                <option value="0">FS – A kończy → B startuje</option>
                                <option value="1">SS – A startuje → B startuje</option>
                                <option value="2">FF – A kończy → B kończy</option>
                                <option value="3">SF – A startuje → B kończy</option>
                              </Select>
                            </Box>
                            <Box>
                              <Text fontSize="xs" color="gray.500" mb={1}>Opóźnienie</Text>
                              <HStack spacing={1}>
                                <Input
                                  type="number"
                                  size="sm"
                                  defaultValue={dep.lagDays}
                                  key={`lag-m-${dep.id}`}
                                  onBlur={(e) => {
                                    const v = parseInt(e.target.value, 10);
                                    const next = isNaN(v) ? 0 : v;
                                    if (next !== dep.lagDays) updateInlineDep(dep.id, { lagDays: next });
                                    else e.target.value = String(dep.lagDays);
                                  }}
                                  w="60px"
                                />
                                <Text fontSize="xs" color="gray.500">dni</Text>
                              </HStack>
                            </Box>
                          </HStack>
                          <Box>
                            <Text fontSize="xs" color="gray.500" mb={1}>Następnik (B — zakres zależny)</Text>
                            <Select
                              size="sm"
                              placeholder="— wybierz zakres B —"
                              value={dep.successorWorkId}
                              onChange={(e) => updateInlineDep(dep.id, { successorWorkId: e.target.value })}
                              isInvalid={!dep.successorWorkId}
                            >
                              {allDbWorks
                                .filter(w => w.workId !== dep.predecessorWorkId)
                                .map(w => (
                                  <option key={w.workId} value={w.workId}>{w.label}</option>
                                ))
                              }
                            </Select>
                          </Box>
                          <HStack justify="space-between">
                            {!isValid && <Text fontSize="xs" color="orange.500">Uzupełnij oba zakresy</Text>}
                            <Tooltip label="Usuń zależność">
                              <IconButton
                                aria-label="Usuń zależność"
                                icon={<Trash2 size={14} />}
                                size="sm"
                                colorScheme="red"
                                variant="ghost"
                                ml="auto"
                                onClick={() => removeInlineDep(dep.id)}
                              />
                            </Tooltip>
                          </HStack>
                        </VStack>
                      )}

                      {/* Na desktop: grid z ustalonymi kolumnami – zapobiega rozjeżdżaniu layoutu */}
                      {!isMobile && (
                        <Grid
                          templateColumns="2fr 3fr 90px 20px 2fr 36px"
                          gap={2}
                          alignItems="center"
                        >
                          {/* Kolumna A – poprzednik */}
                          <GridItem overflow="hidden">
                            <Select
                              size="sm"
                              placeholder="— wybierz zakres A —"
                              value={dep.predecessorWorkId}
                              onChange={(e) => updateInlineDep(dep.id, { predecessorWorkId: e.target.value })}
                              isInvalid={!dep.predecessorWorkId}
                            >
                              {allDbWorks
                                .filter(w => w.workId !== dep.successorWorkId)
                                .map(w => (
                                  <option key={w.workId} value={w.workId}>{w.label}</option>
                                ))
                              }
                            </Select>
                          </GridItem>

                          {/* Kolumna: typ zależności */}
                          <GridItem>
                            <Tooltip
                              label={
                                dep.dependencyType === 0 ? "FS: A musi się zakończyć, żeby B mogło się rozpocząć" :
                                dep.dependencyType === 1 ? "SS: A musi się rozpocząć, żeby B mogło się rozpocząć" :
                                dep.dependencyType === 2 ? "FF: A musi się zakończyć, żeby B mogło się zakończyć" :
                                "SF: A musi się rozpocząć, żeby B mogło się zakończyć"
                              }
                              hasArrow
                            >
                              <Select
                                size="sm"
                                value={String(dep.dependencyType)}
                                onChange={(e) => updateInlineDep(dep.id, { dependencyType: parseInt(e.target.value, 10) })}
                              >
                                <option value="0">FS – kończy → startuje</option>
                                <option value="1">SS – startuje → startuje</option>
                                <option value="2">FF – kończy → kończy</option>
                                <option value="3">SF – startuje → kończy</option>
                              </Select>
                            </Tooltip>
                          </GridItem>

                          {/* Kolumna: opóźnienie */}
                          <GridItem>
                            <HStack spacing={1}>
                              <Input
                                type="number"
                                size="sm"
                                defaultValue={dep.lagDays}
                                key={`lag-d-${dep.id}`}
                                onBlur={(e) => {
                                  const v = parseInt(e.target.value, 10);
                                  const next = isNaN(v) ? 0 : v;
                                  if (next !== dep.lagDays) updateInlineDep(dep.id, { lagDays: next });
                                  else e.target.value = String(dep.lagDays);
                                }}
                                w="54px"
                                flexShrink={0}
                              />
                              <Text fontSize="xs" color="gray.500" flexShrink={0}>dni</Text>
                            </HStack>
                          </GridItem>

                          {/* Kolumna: strzałka */}
                          <GridItem display="flex" alignItems="center" justifyContent="center">
                            <ArrowRight size={16} color="gray" />
                          </GridItem>

                          {/* Kolumna B – następnik */}
                          <GridItem overflow="hidden">
                            <Select
                              size="sm"
                              placeholder="— wybierz zakres B —"
                              value={dep.successorWorkId}
                              onChange={(e) => updateInlineDep(dep.id, { successorWorkId: e.target.value })}
                              isInvalid={!dep.successorWorkId}
                            >
                              {allDbWorks
                                .filter(w => w.workId !== dep.predecessorWorkId)
                                .map(w => (
                                  <option key={w.workId} value={w.workId}>{w.label}</option>
                                ))
                              }
                            </Select>
                          </GridItem>

                          {/* Kolumna: usuń */}
                          <GridItem display="flex" alignItems="center" justifyContent="center">
                            <Tooltip label="Usuń zależność">
                              <IconButton
                                aria-label="Usuń zależność"
                                icon={<Trash2 size={14} />}
                                size="sm"
                                colorScheme="red"
                                variant="ghost"
                                onClick={() => removeInlineDep(dep.id)}
                              />
                            </Tooltip>
                          </GridItem>
                        </Grid>
                      )}
                    </Box>
                  );
                })}
              </VStack>
            )}

            {/* Dodaj zależność — zawsze widoczny na dole panelu */}
            {allDbWorks.length >= 2 && (
              <Box pt={3}>
                <Button
                  leftIcon={<Plus size={14} />}
                  colorScheme="teal"
                  variant="outline"
                  size={isMobile ? "xs" : "sm"}
                  onClick={addInlineDep}
                >
                  Dodaj zależność
                </Button>
              </Box>
            )}
          </Box>
        )}

        {/* Edit Modal */}
        {schedule && (
          <WorkScheduleFormModal
            mode="edit"
            isOpen={isEditModalOpen}
            onClose={onEditModalClose}
            tenantId={user?.activeTenantId || ""}
            projectId={projectId || ""}
            projectName=""
            schedule={schedule}
            members={members}
            onSuccess={handleScheduleUpdated}
          />
        )}

        {/* Work Details Modal */}
        <WorkDetailsModal
          isOpen={isWorkDetailsOpen}
          onClose={onWorkDetailsClose}
          tenantId={user?.activeTenantId || ""}
          projectId={projectId || ""}
          workScheduleId={workScheduleId || ""}
          work={selectedWork}
          members={members}
          dependencies={editableSchedule?.dependencies ?? []}
          allWorks={allDbWorks}
          workDateRanges={viewWorkDateRanges}
          onWorkUpdated={handleScheduleUpdated}
        />
      </Box>
    </MainLayout>
  );
}
