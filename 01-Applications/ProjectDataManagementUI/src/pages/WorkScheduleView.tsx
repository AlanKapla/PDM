import { useEffect, useState, useContext, useRef } from "react";
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
} from "@chakra-ui/react";
import "./WorkScheduleView.css";
import { ArrowLeft, Edit, Clock, User, AlertTriangle, ChevronDown, Plus, Trash2, RefreshCw, FileSpreadsheet, MessageSquare } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import TimelineToolbar from "../components/TimelineToolbar";
import { projectApi } from "../api/projectApi";
import WorkScheduleFormModal from "../components/WorkScheduleFormModal";
import WorkDetailsModal from "../components/WorkDetailsModal";
import { AuthContext } from "../context/AuthContext";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { WorkScheduleDetailsWeb, WorkScheduleStageWorkWeb } from "../types/workSchedule.types";
import { useTimelineData, type TimeScale } from "../hooks/useTimelineData";

// Spłaszcza drzewo etapów do płaskiej listy z informacją o głębokości
const flattenStagesToRows = (stages: any[], depth: number = 0): Array<{ stage: any; depth: number }> =>
  [...stages]
    .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
    .flatMap(s => [
      { stage: s, depth },
      ...flattenStagesToRows(s.childStages ?? [], depth + 1),
    ]);

// Zbiera rekurencyjnie wszystkie ID etapów (dla "rozwiń wszystko")
const getAllStageIds = (stages: any[]): string[] =>
  stages.flatMap(s => [s.id, ...getAllStageIds(s.childStages ?? [])]);

// Szuka pracy w drzewie etapów (mutuje znaleziony obiekt – działa na deep-copy)
const findWorkInStages = (stages: any[], workId: string): any | null => {
  for (const stage of stages) {
    const work = stage.works?.find((w: any) => w.id === workId);
    if (work) return work;
    const found = findWorkInStages(stage.childStages ?? [], workId);
    if (found) return found;
  }
  return null;
};

// Rekurencyjnie wywołuje mutację na etapie o podanym ID (działa na deep-copy)
const mutateStageInTree = (stages: any[], stageId: string, mutator: (s: any) => void): boolean => {
  for (const s of stages) {
    if (s.id === stageId) { mutator(s); return true; }
    if (mutateStageInTree(s.childStages ?? [], stageId, mutator)) return true;
  }
  return false;
};

// Rekurencyjnie usuwa etap po ID z drzewa
const removeStageFromViewTree = (stages: any[], stageId: string): any[] =>
  stages
    .filter(s => s.id !== stageId)
    .map(s => ({ ...s, childStages: removeStageFromViewTree(s.childStages ?? [], stageId) }));

// Rekurencyjnie usuwa pracę po ID ze wszystkich etapów (działa na deep-copy)
const removeWorkFromViewTree = (stages: any[], workId: string): void => {
  for (const s of stages) {
    const idx = (s.works ?? []).findIndex((w: any) => w.id === workId);
    if (idx >= 0) {
      s.works.splice(idx, 1);
      s.works.forEach((w: any, i: number) => { w.order = i; });
      return;
    }
    removeWorkFromViewTree(s.childStages ?? [], workId);
  }
};

// Walidacja drzewa etapów przed zapisem — ta sama logika co w WorkScheduleFormModal
function validateStagesTree(stages: any[]): string | null {
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

// Buduje rekurencyjne polecenie aktualizacji etapu
const mapStageToUpdateCommand = (stage: any): any => ({
  // Pomijaj tymczasowe ID nowo dodanych etapów/prac — backend sam je nada
  ...(stage.id && !String(stage.id).startsWith('temp-') ? { id: stage.id } : {}),
  name: stage.name,
  order: stage.order,
  works: (stage.works ?? []).map((work: any) => ({
    ...(work.id && !String(work.id).startsWith('temp-') ? { id: work.id } : {}),
    name: work.name,
    order: work.order,
    colorRgb: work.colorRgb,
    isClosed: work.isClosed,
    periods: (work.periods ?? []).map((period: any) => ({
      ...(period.id && !String(period.id).startsWith('temp-') ? { id: period.id } : {}),
      startDate: period.startDate,
      endDate: period.endDate,
      isClosed: period.isClosed,
    })),
    assignedUserIds: (work.assignees ?? []).map((a: any) => a.userId),
    comments: (work.comments ?? [])
      .filter((c: any) => c.content && c.content.trim())
      .map((c: any) => ({ id: c.id, content: c.content.trim() })),
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
  const [selectedWork, setSelectedWork] = useState<WorkScheduleStageWorkWeb | null>(null);
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

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const expiredBg = useColorModeValue("red.50", "red.900");
  const todayBg = useColorModeValue("blue.100", "blue.800");
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
      };

      const response = await projectApi.updateWorkSchedule(user.activeTenantId, projectId, workScheduleId, command);

      // Użyj zwróconego modelu zamiast ponownego pobierania
      setSchedule(response.data);
      setEditableSchedule(response.data);

      toast({
        title: "Sukces",
        description: "Status okresu został zaktualizowany",
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
      const response = await projectApi.updateWorkSchedule(
        user.activeTenantId,
        projectId,
        workScheduleId,
        { name: schedule.name }
      );
      setSchedule(response.data);
      setEditableSchedule(response.data);
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
        id: `temp-work-${Date.now()}`,
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

  const handleSaveChanges = async () => {
    if (!editableSchedule || !user?.activeTenantId || !projectId || !workScheduleId) return;

    try {
      const command = {
        name: editableSchedule.name,
        stages: editableSchedule.stages.map((stage: any) => mapStageToUpdateCommand(stage)),
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

  const handleWorkClick = (work: WorkScheduleStageWorkWeb) => {
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
                      <Badge colorScheme="orange" fontSize="xs">Powiązany z kosztorysem</Badge>
                    )}
                  </HStack>
                </VStack>
              </HStack>
              <div className="workschedule-actions">

                {/* === NAWIGACJA: linki do powiązanych zasobów === */}
                {schedule?.costEstimateId && (
                  <Tooltip label="Przejdź do powiązanego kosztorysu">
                    <Button
                      leftIcon={<FileSpreadsheet size={isMobile ? 12 : 14} />}
                      colorScheme="orange"
                      variant="ghost"
                      size={isMobile ? "xs" : "sm"}
                      onClick={() => navigate(`/projects/${projectId}/cost-estimates/${schedule.costEstimateId}`)}
                    >
                      Kosztorys
                    </Button>
                  </Tooltip>
                )}

                {!isMobile && schedule?.costEstimateId && (
                  <Divider orientation="vertical" height="20px" alignSelf="center" />
                )}

                {/* === WIDOK: przełączniki widoczności === */}
                <Tooltip label={showComments ? "Ukryj komentarze do prac" : "Pokaż komentarze do prac"}>
                  <Button
                    leftIcon={<MessageSquare size={isMobile ? 12 : 14} />}
                    size={isMobile ? "xs" : "sm"}
                    variant={showComments ? "solid" : "ghost"}
                    colorScheme="purple"
                    onClick={() => setShowComments(!showComments)}
                  >
                    Komentarze
                  </Button>
                </Tooltip>

                {!isMobile && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
                  <Divider orientation="vertical" height="20px" alignSelf="center" />
                )}

                {/* === EDYCJA: akcje modyfikujące harmonogram === */}
                {!isEditing && schedule?.costEstimateId && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
                  <Tooltip label="Aktualizuje strukturę etapów na podstawie aktualnych grup w kosztorysie">
                    <IconButton
                      aria-label="Synchronizuj z kosztorysem"
                      icon={<RefreshCw size={isMobile ? 12 : 14} />}
                      size={isMobile ? "xs" : "sm"}
                      colorScheme="orange"
                      variant="outline"
                      onClick={handleSyncFromCostEstimate}
                      isLoading={isSyncing}
                    />
                  </Tooltip>
                )}

                {/* Tryb edycji inline — dodawanie etapów */}
                {isEditing && (
                  <Button
                    leftIcon={<Plus size={isMobile ? 12 : 14} />}
                    colorScheme="blue"
                    variant="outline"
                    size={isMobile ? "xs" : "sm"}
                    onClick={addStage}
                  >
                    Dodaj etap
                  </Button>
                )}

                {(permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
                  <>
                    {!isEditing && (
                      <Button
                        leftIcon={<Edit size={isMobile ? 12 : 14} />}
                        colorScheme="blue"
                        variant="outline"
                        size={isMobile ? "xs" : "sm"}
                        onClick={handleEditMode}
                      >
                        Edytuj
                      </Button>
                    )}
                    <Tooltip label={isEditing ? "Kliknij aby wyjść z trybu edycji inline" : "Włącz edycję inline — klikaj kafelki, edytuj nazwy, dodawaj etapy i zakresy"}>
                      <Button
                        leftIcon={<Edit size={isMobile ? 12 : 14} />}
                        aria-label="Edycja inline"
                        size={isMobile ? "xs" : "sm"}
                        colorScheme={isEditing ? "blue" : "gray"}
                        variant="outline"
                        onClick={handleToggleInlineEdit}
                      >
                        {isEditing ? "Edycja inline ✓" : "Edycja inline"}
                      </Button>
                    </Tooltip>
                  </>
                )}

                {/* === ZAPIS: widoczny gdy są niezapisane zmiany LUB trwa edycja inline === */}
                {(isDirty || isEditing) && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
                  <>
                    {!isMobile && <Divider orientation="vertical" height="20px" alignSelf="center" />}
                    <Button
                      colorScheme="green"
                      size={isMobile ? "xs" : "sm"}
                      onClick={handleSaveAndExitEdit}
                    >
                      Zapisz
                    </Button>
                    <Button
                      variant="ghost"
                      colorScheme="gray"
                      size={isMobile ? "xs" : "sm"}
                      onClick={handleCancelEdit}
                    >
                      Anuluj
                    </Button>
                  </>
                )}

              </div>
            </div>
          </HStack>
        </VStack>

        {/* Controls */}
        {!isMobile && (
          <Box
            p={4}
            bg={cardBg}
            borderWidth="1px"
            borderColor={borderColor}
            borderRadius="lg"
          >
            <TimelineToolbar
              timeScale={timeScale}
              setTimeScale={setTimeScale}
              timeRangeMonths={timeRangeMonths}
              setTimeRangeMonths={setTimeRangeMonths}
              hideWeekends={hideWeekends}
              toggleWeekends={toggleWeekends}
              scrollToToday={scrollToToday}
              onExpandAll={expandAllStages}
              onCollapseAll={collapseAllStages}
            />
          </Box>
        )}

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
                                <Badge colorScheme="orange" fontSize="2xs" variant="subtle">Kosztorys</Badge>
                              )}
                              <Badge colorScheme="gray" fontSize="2xs">Brak prac</Badge>
                              {isEditing && (
                                <HStack spacing={1}>
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
                                    <Badge colorScheme="orange" fontSize="2xs" variant="subtle">Kosztorys</Badge>
                                  )}
                                  {minDate && maxDate && !isMobile && (
                                    <Text fontSize="2xs" color="gray.500">
                                      {formatDate(minDate)} - {formatDate(maxDate)}
                                    </Text>
                                  )}
                                  {!isExpanded && (
                                    <Badge colorScheme="purple" fontSize="2xs">
                                      {sortedWorks.length} {sortedWorks.length === 1 ? 'praca' : sortedWorks.length < 5 ? 'prace' : 'prac'}
                                    </Badge>
                                  )}
                                  {isEditing && (
                                    <HStack spacing={1}>
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
                                <Checkbox
                                  size="sm"
                                  isChecked={work.isClosed}
                                  onChange={() => toggleWorkClosed(work.id)}
                                  colorScheme="green"
                                  mt={1}
                                  isDisabled={!permissions.mine.canEdit && !permissions.all.canEdit && !permissions.shared.canEdit}
                                />
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
                                          <Checkbox
                                            size="sm"
                                            isChecked={period.isClosed}
                                            onChange={() => togglePeriodClosed(work.id, period.id || pIdx)}
                                            colorScheme="green"
                                            isDisabled={!permissions.mine.canEdit && !permissions.all.canEdit && !permissions.shared.canEdit}
                                          />
                                        </HStack>
                                      ))}
                                    </VStack>
                                  )}

                                  {work.assignees.length > 0 && !isEditing && (
                                    <HStack spacing={1} flexWrap="wrap" mt={0.5}>
                                      {work.assignees.map((assignee: any) => (
                                        <Badge key={assignee.userId} colorScheme="purple" fontSize="2xs">
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
                                </VStack>
                                {isEditing && (
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
          onWorkUpdated={handleScheduleUpdated}
        />
      </Box>
    </MainLayout>
  );
}
