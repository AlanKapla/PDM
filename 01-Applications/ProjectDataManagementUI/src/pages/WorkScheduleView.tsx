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
  Slider,
  SliderTrack,
  SliderFilledTrack,
  SliderThumb,
  useToast,
  Textarea,
  Checkbox,
  useMediaQuery,
} from "@chakra-ui/react";
import "./WorkScheduleView.css";
import { ArrowLeft, Edit, Clock, User, AlertTriangle, CalendarDays, ChevronDown, Plus, Trash2 } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { projectApi } from "../api/projectApi";
import WorkScheduleFormModal from "../components/WorkScheduleFormModal";
import WorkDetailsModal from "../components/WorkDetailsModal";
import { AuthContext } from "../context/AuthContext";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { WorkScheduleDetailsWeb, WorkScheduleStageWorkWeb } from "../types/workSchedule.types";

type TimeScale = "days" | "weeks" | "months";

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
  const [timeScale, setTimeScale] = useState<TimeScale>("weeks");
  const [error, setError] = useState<string | null>(null);
  const [timeRangeMonths, setTimeRangeMonths] = useState(1); // ±1 month by default
  const [columnWidths, setColumnWidths] = useState({
    stage: isMobile ? 90 : 200,
    description: isMobile ? window.innerWidth - 110 : 350,
  });
  const [expandedStages, setExpandedStages] = useState<Set<string>>(new Set());
  const [selectedWork, setSelectedWork] = useState<WorkScheduleStageWorkWeb | null>(null);
  const [showComments, setShowComments] = useState(false);
  const [editableSchedule, setEditableSchedule] = useState<WorkScheduleDetailsWeb | null>(null);
  const [isDirty, setIsDirty] = useState(false);
  const [scrollHintVisible, setScrollHintVisible] = useState(true);

  const todayColumnRef = useRef<HTMLTableCellElement>(null);
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const expiredBg = useColorModeValue("red.50", "red.900");
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
      console.error("Błąd pobierania harmonogramu:", err);
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
      console.error("Błąd pobierania członków:", err);
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

  const getTimelineData = () => {
    if (!schedule) return { dates: [], minDate: null, maxDate: null, dateGroups: null };

    const today = new Date();
    const minDate = new Date(today);
    minDate.setMonth(today.getMonth() - timeRangeMonths);
    minDate.setDate(1);
    minDate.setHours(0, 0, 0, 0);

    const maxDate = new Date(today);
    maxDate.setMonth(today.getMonth() + timeRangeMonths);
    maxDate.setHours(0, 0, 0, 0);

    const dates: Date[] = [];
    const current = new Date(minDate);

    if (timeScale === "days") {
      // Simple day view
      while (current <= maxDate) {
        dates.push(new Date(current));
        current.setDate(current.getDate() + 1);
      }
      return { dates, minDate, maxDate, dateGroups: null };
    } else if (timeScale === "weeks") {
      // Weeks view - each week divided into 7 day tiles
      const day = current.getDay();
      current.setDate(current.getDate() - (day === 0 ? 6 : day - 1)); // Start from Monday

      const dateGroups: { label: string; count: number; startIdx: number }[] = [];
      let groupStartIdx = 0;

      while (current <= maxDate) {
        const weekStart = new Date(current);
        const weekEnd = new Date(current);
        weekEnd.setDate(weekEnd.getDate() + 6);

        // Add 7 days for this week
        for (let i = 0; i < 7; i++) {
          dates.push(new Date(current));
          current.setDate(current.getDate() + 1);
        }

        dateGroups.push({
          label: `${weekStart.getDate()}.${weekStart.getMonth() + 1} - ${weekEnd.getDate()}.${weekEnd.getMonth() + 1}`,
          count: 7,
          startIdx: groupStartIdx,
        });
        groupStartIdx += 7;
      }

      return { dates, minDate, maxDate, dateGroups };
    } else {
      // Months view - each month divided into all days in the month
      const dateGroups: { label: string; count: number; startIdx: number }[] = [];
      let groupStartIdx = 0;

      while (current <= maxDate) {
        const monthStart = new Date(current);
        const daysInMonth = new Date(current.getFullYear(), current.getMonth() + 1, 0).getDate();

        for (let day = 1; day <= daysInMonth; day++) {
          const dayDate = new Date(current.getFullYear(), current.getMonth(), day);
          dates.push(dayDate);
        }

        dateGroups.push({
          label: monthStart.toLocaleDateString("pl-PL", { month: "long", year: "numeric" }),
          count: daysInMonth,
          startIdx: groupStartIdx,
        });
        groupStartIdx += daysInMonth;

        current.setMonth(current.getMonth() + 1);
      }

      return { dates, minDate, maxDate, dateGroups };
    }
  };

  const isWorkInPeriod = (workStart: string, workEnd: string, periodStart: Date, periodEnd: Date): boolean => {
    const start = new Date(workStart);
    const end = new Date(workEnd);

    // Praca nachodzi na okres, jeśli:
    // - zaczyna się przed końcem okresu
    // - kończy się po początku okresu
    return start < periodEnd && end >= periodStart;
  };

  const getPeriodEnd = (periodStart: Date): Date => {
    const end = new Date(periodStart);
    end.setDate(end.getDate() + 1); // Always one day per cell
    return end;
  };

  const formatTimelineDate = (date: Date): string => {
    if (timeScale === "days") {
      return isMobile ? `${date.getDate()}` : `${date.getDate()}.${date.getMonth() + 1}`;
    } else if (timeScale === "weeks") {
      const dayNames = ["Nd", "Pn", "Wt", "Śr", "Cz", "Pt", "So"];
      return isMobile ? dayNames[date.getDay()] : `${dayNames[date.getDay()]}\n${date.getDate()}.${date.getMonth() + 1}`;
    } else {
      return `${date.getDate()}`;
    }
  };

  const handleEditMode = () => {
    onEditModalOpen();
  };

  const handleScheduleUpdated = () => {
    fetchSchedule();
  };

  const toggleWorkClosed = async (workId: string) => {
    if (!editableSchedule || !user?.activeTenantId || !projectId || !workScheduleId) return;

    const updatedSchedule = JSON.parse(JSON.stringify(editableSchedule));

    for (const stage of updatedSchedule.stages) {
      const work = stage.works.find((w: any) => w.id === workId);
      if (work) {
        const newClosedState = !work.isClosed;
        work.isClosed = newClosedState;

        // Zaznaczenie zakresu pracy → zamknij wszystkie okresy
        // Odznaczenie zakresu pracy → otwórz wszystkie okresy
        work.periods.forEach((p: any) => p.isClosed = newClosedState);
        break;
      }
    }

    setEditableSchedule(updatedSchedule);

    // Automatyczny zapis
    try {
      const command = {
        name: updatedSchedule.name,
        stages: updatedSchedule.stages.map((stage: any) => ({
          id: stage.id,
          name: stage.name,
          order: stage.order,
          works: stage.works.map((work: any) => ({
            id: work.id,
            name: work.name,
            order: work.order,
            colorRgb: work.colorRgb,
            isClosed: work.isClosed,
            periods: work.periods.map((period: any) => ({
              id: period.id,
              startDate: period.startDate,
              endDate: period.endDate,
              isClosed: period.isClosed,
            })),
            assignedUserIds: work.assignees.map((a: any) => a.userId),
            comments: (work.comments || [])
              .filter((c: any) => c.content && c.content.trim())
              .map((c: any) => ({
                id: c.id,
                content: c.content.trim(),
              })),
          })),
        })),
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
      console.error("Błąd zapisywania zmian:", error);
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

    for (const stage of updatedSchedule.stages) {
      const work = stage.works.find((w: any) => w.id === workId);
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
        break;
      }
    }

    setEditableSchedule(updatedSchedule);

    // Automatyczny zapis
    try {
      const command = {
        name: updatedSchedule.name,
        stages: updatedSchedule.stages.map((stage: any) => ({
          id: stage.id,
          name: stage.name,
          order: stage.order,
          works: stage.works.map((work: any) => ({
            id: work.id,
            name: work.name,
            order: work.order,
            colorRgb: work.colorRgb,
            isClosed: work.isClosed,
            periods: work.periods.map((period: any) => ({
              id: period.id,
              startDate: period.startDate,
              endDate: period.endDate,
              isClosed: period.isClosed,
            })),
            assignedUserIds: work.assignees.map((a: any) => a.userId),
            comments: (work.comments || [])
              .filter((c: any) => c.content && c.content.trim())
              .map((c: any) => ({
                id: c.id,
                content: c.content.trim(),
              })),
          })),
        })),
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
      console.error("Błąd zapisywania zmian:", error);
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

    for (const stage of updatedSchedule.stages) {
      const work = stage.works.find((w: any) => w.id === workId);
      if (work) {
        if (!work.comments) work.comments = [];
        work.comments.push({
          id: undefined,
          content: "",
          createdAt: new Date().toISOString(),
          createdByUserId: user?.id || "",
          createdByUserName: user?.firstName + " " + user?.lastName || "Użytkownik",
        });
        break;
      }
    }

    setEditableSchedule(updatedSchedule);
    setIsDirty(true);
  };

  const updateWorkComment = (workId: string, commentId: string | undefined, content: string) => {
    if (!editableSchedule) return;

    const updatedSchedule = JSON.parse(JSON.stringify(editableSchedule));

    for (const stage of updatedSchedule.stages) {
      const work = stage.works.find((w: any) => w.id === workId);
      if (work && work.comments) {
        const comment = work.comments.find((c: any) =>
          commentId ? c.id === commentId : c.id === undefined
        );
        if (comment) {
          comment.content = content;
        }
        break;
      }
    }

    setEditableSchedule(updatedSchedule);
    setIsDirty(true);
  };

  const removeWorkComment = (workId: string, commentId: string | undefined) => {
    if (!editableSchedule) return;

    const updatedSchedule = JSON.parse(JSON.stringify(editableSchedule));

    for (const stage of updatedSchedule.stages) {
      const work = stage.works.find((w: any) => w.id === workId);
      if (work && work.comments) {
        work.comments = work.comments.filter((c: any) =>
          commentId ? c.id !== commentId : c.id !== undefined
        );
        break;
      }
    }

    setEditableSchedule(updatedSchedule);
    setIsDirty(true);
  };

  const handleSaveChanges = async () => {
    if (!editableSchedule || !user?.activeTenantId || !projectId || !workScheduleId) return;

    try {
      const command = {
        name: editableSchedule.name,
        stages: editableSchedule.stages.map((stage: any) => ({
          id: stage.id,
          name: stage.name,
          order: stage.order,
          works: stage.works.map((work: any) => ({
            id: work.id,
            name: work.name,
            order: work.order,
            colorRgb: work.colorRgb,
            isClosed: work.isClosed,
            periods: work.periods.map((period: any) => ({
              id: period.id,
              startDate: period.startDate,
              endDate: period.endDate,
              isClosed: period.isClosed,
            })),
            assignedUserIds: work.assignees.map((a: any) => a.userId),
            comments: (work.comments || [])
              .filter((c: any) => c.content && c.content.trim())
              .map((c: any) => ({
                id: c.id,
                content: c.content.trim(),
              })),
          })),
        })),
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
      console.error("Błąd zapisywania zmian:", error);
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

  const scrollToToday = () => {
    if (todayColumnRef.current) {
      todayColumnRef.current.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest',
        inline: 'center'
      });
    }
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

  const isToday = (date: Date): boolean => {
    const today = new Date();
    return date.getDate() === today.getDate() &&
      date.getMonth() === today.getMonth() &&
      date.getFullYear() === today.getFullYear();
  };

  const { dates, dateGroups } = getTimelineData();

  // Scroll do dzisiejszej daty po załadowaniu
  useEffect(() => {
    if (!loading && schedule) {
      setTimeout(scrollToToday, 100);
    }
  }, [loading, schedule]);

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
                  </HStack>
                </VStack>
              </HStack>
              <div className="workschedule-actions">
                <Button
                  size={isMobile ? "xs" : "sm"}
                  variant={showComments ? "solid" : "outline"}
                  colorScheme="purple"
                  onClick={() => setShowComments(!showComments)}
                  width={isMobile ? "100%" : "auto"}
                >
                  {showComments ? "Ukryj komentarze" : "Pokaż komentarze"}
                </Button>
                {isDirty && (permissions.mine.canEdit || permissions.all.canEdit || permissions.shared.canEdit) && (
                  <>
                    <Button
                      colorScheme="green"
                      onClick={handleSaveChanges}
                      size={isMobile ? "xs" : "sm"}
                      width={isMobile ? "100%" : "auto"}
                    >
                      Zapisz
                    </Button>
                    <Button
                      colorScheme="gray"
                      onClick={() => {
                        setEditableSchedule(JSON.parse(JSON.stringify(schedule)));
                        setIsDirty(false);
                      }}
                      size={isMobile ? "xs" : "sm"}
                      width={isMobile ? "100%" : "auto"}
                    >
                      Anuluj
                    </Button>
                  </>
                )}
                <Button
                  leftIcon={<Edit size={18} />}
                  colorScheme="purple"
                  onClick={handleEditMode}
                  size={isMobile ? "xs" : "sm"}
                  isDisabled={!permissions.mine.canEdit && !permissions.all.canEdit && !permissions.shared.canEdit}
                  width={isMobile ? "100%" : "auto"}
                >
                  Edytuj
                </Button>
              </div>
            </div>
          </HStack>
        </VStack>

        {/* Controls - Hidden on mobile */}
        {!isMobile && (
          <Box
            p={4}
            bg={cardBg}
            borderWidth="1px"
            borderColor={borderColor}
            borderRadius="lg"
          >
            <VStack spacing={4} align="stretch">
              <HStack spacing={6} justify="space-between" flexWrap="wrap">
                <HStack spacing={4}>
                  <Text fontWeight="medium">Skala czasu:</Text>
                  <HStack spacing={2}>
                    <Button
                      size="sm"
                      variant={timeScale === "days" ? "solid" : "outline"}
                      colorScheme="purple"
                      onClick={() => setTimeScale("days")}
                    >
                      Dni
                    </Button>
                    <Button
                      size="sm"
                      variant={timeScale === "weeks" ? "solid" : "outline"}
                      colorScheme="purple"
                      onClick={() => setTimeScale("weeks")}
                    >
                      Tygodnie
                    </Button>
                    <Button
                      size="sm"
                      variant={timeScale === "months" ? "solid" : "outline"}
                      colorScheme="purple"
                      onClick={() => setTimeScale("months")}
                    >
                      Miesiące
                    </Button>
                  </HStack>
                </HStack>
                <Button
                  size="sm"
                  leftIcon={<CalendarDays size={16} />}
                  colorScheme="blue"
                  onClick={scrollToToday}
                >
                  Wróć do dzisiejszej daty
                </Button>
              </HStack>

              {/* Time Range Slider */}
              <HStack spacing={4}>
                <Text fontWeight="medium" minW="120px" fontSize="sm">
                  Zakres czasu:
                </Text>
                <Slider
                  value={timeRangeMonths}
                  onChange={setTimeRangeMonths}
                  min={1}
                  max={24}
                  step={1}
                  colorScheme="purple"
                  flex={1}
                  maxW="500px"
                >
                  <SliderTrack>
                    <SliderFilledTrack />
                  </SliderTrack>
                  <SliderThumb boxSize={6}>
                    <Box color="purple.500" fontSize="2xs" fontWeight="bold">
                      {timeRangeMonths}
                    </Box>
                  </SliderThumb>
                </Slider>
                <Text fontSize="sm" color="gray.600" minW="180px">
                  ±{timeRangeMonths} {timeRangeMonths === 1 ? "miesiąc" : timeRangeMonths < 5 ? "miesiące" : "miesięcy"}
                </Text>
              </HStack>
            </VStack>
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
                  {!isMobile && dates.map((date, idx) => (
                    <Th
                      key={idx}
                      textAlign="center"
                      minW="30px"
                      px={0.5}
                      py={1}
                      fontSize="2xs"
                      fontWeight="normal"
                      textTransform="none"
                    >
                      <Text fontSize="2xs" whiteSpace="pre-line" lineHeight="1">
                        {formatTimelineDate(date)}
                      </Text>
                    </Th>
                  ))}
                </Tr>
              </Thead>
              <Tbody>
                {(editableSchedule || schedule)?.stages
                  .sort((a, b) => a.order - b.order)
                  .map((stage, stageIdx) => {
                    const sortedWorks = stage.works.sort((a, b) => a.order - b.order);
                    const { minDate, maxDate } = getStageDataRange(stage);
                    const isLastStage = stageIdx === (editableSchedule || schedule)!.stages.length - 1;
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
                            <VStack align="flex-start" spacing={1}>
                              <Text fontWeight="bold" fontSize="sm">{stage.name}</Text>
                              <Badge colorScheme="gray" fontSize="2xs">Brak prac</Badge>
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
                                <VStack align="flex-start" spacing={1} flex={1}>
                                  <Text fontWeight="bold" fontSize="sm">{stage.name}</Text>
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
                              <HStack spacing={2} width="100%">
                                <Checkbox
                                  size="sm"
                                  isChecked={work.isClosed}
                                  onChange={() => toggleWorkClosed(work.id)}
                                  colorScheme="green"
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
                                    <Text fontSize={isMobile ? "xs" : "sm"} fontWeight="medium">{work.name}</Text>
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

                                  {work.assignees.length > 0 && (
                                    <HStack spacing={1} flexWrap="wrap" mt={0.5}>
                                      {work.assignees.map((assignee: any) => (
                                        <Badge key={assignee.userId} colorScheme="purple" fontSize="2xs">
                                          {assignee.userName}
                                        </Badge>
                                      ))}
                                    </HStack>
                                  )}

                                  {showComments && work.comments && work.comments.length > 0 && (
                                    <VStack align="flex-start" spacing={1} width="100%" mt={1} pt={1} borderTopWidth="1px" fontSize="2xs">
                                      <Text fontWeight="bold" fontSize="2xs">Kom:</Text>
                                      {work.comments.map((comment: any, cIdx: number) => (
                                        <HStack key={comment.id || cIdx} spacing={1} align="flex-start" width="100%">
                                          <Text fontSize="2xs" flex={1} noOfLines={2}>
                                            {comment.content}
                                          </Text>
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
                                    </VStack>
                                  )}
                                </VStack>
                              </HStack>
                            </VStack>
                          </Td>
                          {/* Timeline cells - desktop only */}
                          {!isMobile && dates.map((periodStart, idx) => {
                            const periodEnd = getPeriodEnd(periodStart);
                            const isActive = work.periods.some(period =>
                              isWorkInPeriod(period.startDate, period.endDate, periodStart, periodEnd)
                            );
                            const isTodayColumn = isToday(periodStart);

                            return (
                              <Td
                                key={idx}
                                ref={isTodayColumn ? todayColumnRef : undefined}
                                p={0}
                                bg={isActive ? work.colorRgb : (isTodayColumn ? colors.stageHoverBg : undefined)}
                                position="relative"
                                borderLeftWidth={isTodayColumn ? "2px" : undefined}
                                borderRightWidth={isTodayColumn ? "2px" : undefined}
                                borderColor={isTodayColumn ? "blue.500" : undefined}
                              >
                                {isActive && (
                                  <Tooltip
                                    label={`${work.name}${work.periods.length > 1 ? ` (${work.periods.length} okresów)` : ''} - Kliknij aby edytować`}
                                  >
                                    <Box
                                      h="100%"
                                      minH="50px"
                                      w="100%"
                                      bg={work.colorRgb}
                                      cursor="pointer"
                                      transition="opacity 0.2s"
                                      onClick={() => handleWorkClick(work)}
                                      _hover={{
                                        opacity: 0.85,
                                      }}
                                      borderLeftWidth={isTodayColumn ? "2px" : undefined}
                                      borderRightWidth={isTodayColumn ? "2px" : undefined}
                                      borderColor={isTodayColumn ? "blue.500" : undefined}
                                    />
                                  </Tooltip>
                                )}
                                {!isActive && <Box h="100%" minH="50px" />}
                              </Td>
                            );
                          })}
                        </Tr>
                      );
                    });
                  })}
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
          onWorkUpdated={handleScheduleUpdated}
        />
      </Box>
    </MainLayout>
  );
}
