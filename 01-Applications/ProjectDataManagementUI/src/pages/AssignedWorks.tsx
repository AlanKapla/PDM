import { useEffect, useMemo, useRef, useState } from "react";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Spinner,
  Alert,
  AlertIcon,
  useColorModeValue,
  Badge,
  Button,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Tooltip,
  IconButton,
  Divider,
  useMediaQuery,
} from "@chakra-ui/react";
import {
  Briefcase,
  AlertTriangle,
  ChevronDown,
  ChevronRight,
  Building2,
  FolderKanban,
  PanelLeftOpen,
  PanelLeftClose,
} from "lucide-react";
import MainLayout from "../layout/MainLayout";
import TimelineToolbar from "../components/TimelineToolbar";
import { projectApi } from "../api/projectApi";
import type { UserAssignedWorksGroupedWeb } from "../types/workSchedule.types";
import { useTimelineData } from "../hooks/useTimelineData";

/** Selekcja w panelu nawigacji: wszystko, tenant lub konkretny projekt */
type Selection =
  | { type: "all" }
  | { type: "tenant"; tenantId: string }
  | { type: "project"; tenantId: string; projectId: string };

/** Tenant z listą projektów pogrupowany z danych API */
interface TenantGroup {
  tenantId: string;
  tenantName: string;
  projects: UserAssignedWorksGroupedWeb[];
  /** Łączna liczba aktywnych (niezakończonych) prac */
  activeWorksCount: number;
}

export default function AssignedWorks() {
  const [assignedWorks, setAssignedWorks] = useState<UserAssignedWorksGroupedWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Panel nawigacji
  const [selection, setSelection] = useState<Selection>({ type: "all" });
  const [expandedTenants, setExpandedTenants] = useState<Set<string>>(new Set());

  // Timeline
  const [expandedProjects, setExpandedProjects] = useState<Set<string>>(new Set());
  const [expandedSchedules, setExpandedSchedules] = useState<Set<string>>(new Set());
  const [columnWidths] = useState({ stage: 200, work: 350 });

  const [isMobile] = useMediaQuery("(max-width: 768px)");
  const [sidebarOpen, setSidebarOpen] = useState(true);

  const {
    timeScale, setTimeScale,
    timeRangeMonths, setTimeRangeMonths,
    hideWeekends, toggleWeekends,
    dates, dateGroups,
    isToday, formatTimelineDate,
    isWorkInPeriod, getPeriodEnd,
    todayColumnRef, scrollContainerRef, scrollToToday,
  } = useTimelineData({ isMobile });

  // Na mobile sidebar domyślnie ukryty
  useEffect(() => {
    setSidebarOpen(!isMobile);
  }, [isMobile]);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const expiredBg = useColorModeValue("red.50", "red.900");
  const warningBg = useColorModeValue("yellow.50", "yellow.900");
  const completedBg = useColorModeValue("green.50", "green.900");
  const sidebarBg = useColorModeValue("gray.50", "gray.900");
  const selectedBg = useColorModeValue("purple.100", "purple.700");
  const selectedHoverBg = useColorModeValue("purple.200", "purple.600");
  const navHoverBg = useColorModeValue("gray.100", "gray.700");
  const theadBg = useColorModeValue("gray.50", "gray.700");
  const todayBg = useColorModeValue("blue.100", "blue.800");
  const projectRowBg = useColorModeValue("purple.100", "purple.800");
  const projectRowHoverBg = useColorModeValue("purple.200", "purple.700");
  const scheduleRowBg = useColorModeValue("cyan.50", "cyan.900");
  const scheduleRowHoverBg = useColorModeValue("cyan.100", "cyan.800");
  const stageRowBg = useColorModeValue("blue.50", "blue.900");

  useEffect(() => {
    const fetchAssignedWorks = async () => {
      try {
        setLoading(true);
        setError(null);
        const response = await projectApi.getMyAssignedWorks();
        setAssignedWorks(response.data);
      } catch (err: any) {
        setError(err.message || "Wystąpił błąd podczas pobierania zaplanowanych prac");
      } finally {
        setLoading(false);
      }
    };

    fetchAssignedWorks();
  }, []);

  // ─── Grupowanie po tenantach ────────────────────────────────────

  const tenantGroups = useMemo<TenantGroup[]>(() => {
    const map = new Map<string, TenantGroup>();
    for (const item of assignedWorks) {
      let group = map.get(item.tenantId);
      if (!group) {
        group = {
          tenantId: item.tenantId,
          tenantName: item.tenantName,
          projects: [],
          activeWorksCount: 0,
        };
        map.set(item.tenantId, group);
      }
      group.projects.push(item);
      for (const ws of item.workSchedules) {
        for (const stage of ws.stages) {
          for (const work of stage.works) {
            if (!work.isClosed) group.activeWorksCount++;
          }
        }
      }
    }
    return Array.from(map.values()).sort((a, b) => a.tenantName.localeCompare(b.tenantName));
  }, [assignedWorks]);

  // ─── Filtrowane dane wg selekcji ─────────────────────────────────

  const filteredWorks = useMemo<UserAssignedWorksGroupedWeb[]>(() => {
    if (selection.type === "all") return assignedWorks;
    if (selection.type === "tenant")
      return assignedWorks.filter((w) => w.tenantId === selection.tenantId);
    return assignedWorks.filter(
      (w) => w.tenantId === selection.tenantId && w.projectId === selection.projectId
    );
  }, [assignedWorks, selection]);

  // Auto-rozwijanie przy zmianie selekcji
  useEffect(() => {
    if (filteredWorks.length > 0) {
      setExpandedProjects(new Set(filteredWorks.map((p) => p.projectId)));
      setExpandedSchedules(
        new Set(filteredWorks.flatMap((p) => p.workSchedules.map((s) => s.workScheduleId)))
      );
    }
  }, [selection]); // eslint-disable-line react-hooks/exhaustive-deps

  // ─── Etykieta selekcji ──────────────────────────────────────────

  const selectionLabel = useMemo(() => {
    if (selection.type === "all") return "Wszystkie organizacje";
    if (selection.type === "tenant") {
      const t = tenantGroups.find((g) => g.tenantId === selection.tenantId);
      return t?.tenantName ?? "Organizacja";
    }
    const project = assignedWorks.find(
      (w) => w.tenantId === selection.tenantId && w.projectId === selection.projectId
    );
    return project ? `${project.tenantName} › ${project.projectName}` : "Projekt";
  }, [selection, tenantGroups, assignedWorks]);

  const getProjectActiveCount = (project: UserAssignedWorksGroupedWeb) => {
    let count = 0;
    for (const ws of project.workSchedules) {
      for (const stage of ws.stages) {
        for (const work of stage.works) {
          if (!work.isClosed) count++;
        }
      }
    }
    return count;
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString("pl-PL", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
  };

  const getWorkStatus = (periods: { startDate: string; endDate: string }[]) => {
    if (periods.length === 0) return 'none';
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    const latestPeriod = periods.reduce((latest, period) => {
      const endDate = new Date(period.endDate);
      const latestEndDate = new Date(latest.endDate);
      return endDate > latestEndDate ? period : latest;
    });
    
    const endDate = new Date(latestPeriod.endDate);
    endDate.setHours(0, 0, 0, 0);
    
    if (endDate < today) {
      return 'expired';
    }
    
    const fiveDaysFromNow = new Date(today);
    fiveDaysFromNow.setDate(today.getDate() + 5);
    
    if (endDate <= fiveDaysFromNow) {
      return 'warning';
    }
    
    return 'active';
  };

  // Auto-scroll do dzisiejszej daty po załadowaniu
  useEffect(() => {
    if (!loading && assignedWorks.length > 0) {
      setTimeout(scrollToToday, 100);
    }
  }, [loading, assignedWorks.length, scrollToToday]);

  const toggleNavTenant = (tenantId: string) => {
    setExpandedTenants((prev) => {
      const next = new Set(prev);
      if (next.has(tenantId)) next.delete(tenantId);
      else next.add(tenantId);
      return next;
    });
  };

  const toggleProject = (projectId: string) => {
    setExpandedProjects((prev) => {
      const next = new Set(prev);
      if (next.has(projectId)) {
        next.delete(projectId);
        filteredWorks
          .find((p) => p.projectId === projectId)
          ?.workSchedules.forEach((s) => {
            setExpandedSchedules((schedPrev) => {
              const schedNext = new Set(schedPrev);
              schedNext.delete(s.workScheduleId);
              return schedNext;
            });
          });
      } else {
        next.add(projectId);
      }
      return next;
    });
  };

  const toggleSchedule = (scheduleId: string) => {
    setExpandedSchedules((prev) => {
      const next = new Set(prev);
      if (next.has(scheduleId)) {
        next.delete(scheduleId);
      } else {
        next.add(scheduleId);
      }
      return next;
    });
  };

  const expandAll = () => {
    setExpandedProjects(new Set(filteredWorks.map((p) => p.projectId)));
    setExpandedSchedules(
      new Set(filteredWorks.flatMap((p) => p.workSchedules.map((s) => s.workScheduleId)))
    );
  };

  const collapseAll = () => {
    setExpandedProjects(new Set());
    setExpandedSchedules(new Set());
  };

  if (loading) {
    return (
      <MainLayout>
        <Box display="flex" justifyContent="center" alignItems="center" h="50vh">
          <Spinner size="xl" />
        </Box>
      </MainLayout>
    );
  }

  if (error) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 8 }}>
          <Alert status="error">
            <AlertIcon />
            {error}
          </Alert>
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 8 }}>
        <VStack align="stretch" spacing={{ base: 4, md: 6 }}>
          {/* Nagłówek strony */}
          <HStack spacing={{ base: 2, md: 4 }}>
            <Briefcase size={32} color="orange" />
            <Heading size={{ base: "md", md: "lg" }}>Zaplanowane prace</Heading>
          </HStack>

          {assignedWorks.length === 0 ? (
            <Alert status="info">
              <AlertIcon />
              Nie masz przypisanych żadnych prac
            </Alert>
          ) : (
            <Box position="relative" h="calc(100vh - 200px)">
              <HStack align="stretch" spacing={0} h="100%">
                {/* ─── Lewy panel: nawigacja tenant / projekt ─── */}
                {sidebarOpen && (
                  <>
                    {/* Overlay na mobile */}
                    {isMobile && (
                      <Box
                        position="fixed"
                        top={0}
                        left={0}
                        right={0}
                        bottom={0}
                        bg="blackAlpha.400"
                        zIndex={29}
                        onClick={() => setSidebarOpen(false)}
                      />
                    )}
                    <Box
                      w={isMobile ? "260px" : "280px"}
                      minW={isMobile ? "260px" : "280px"}
                      bg={sidebarBg}
                      borderWidth="1px"
                      borderColor={borderColor}
                      borderRadius="lg"
                      borderRightRadius={0}
                      overflowY="auto"
                      py={2}
                      {...(isMobile && {
                        position: "fixed" as const,
                        top: "60px",
                        left: "8px",
                        bottom: "8px",
                        zIndex: 30,
                        borderRadius: "lg",
                        shadow: "xl",
                      })}
                    >
                {/* Opcja "Wszystkie organizacje" */}
                <Box
                  px={3}
                  py={2}
                  cursor="pointer"
                  bg={selection.type === "all" ? selectedBg : undefined}
                  _hover={{ bg: selection.type === "all" ? selectedHoverBg : navHoverBg }}
                  onClick={() => {
                    setSelection({ type: "all" });
                    if (isMobile) setSidebarOpen(false);
                  }}
                  transition="background 0.15s"
                >
                  <HStack spacing={2}>
                    <Briefcase size={16} />
                    <Text fontSize="sm" fontWeight="semibold">
                      Wszystkie organizacje
                    </Text>
                  </HStack>
                </Box>

                <Divider my={1} />

                {/* Lista tenantów z projektami */}
                {tenantGroups.map((tenant) => {
                  const isTenantSelected =
                    selection.type === "tenant" && selection.tenantId === tenant.tenantId;
                  const isTenantExpanded = expandedTenants.has(tenant.tenantId);

                  return (
                    <Box key={tenant.tenantId}>
                      <HStack
                        px={3}
                        py={2}
                        cursor="pointer"
                        bg={isTenantSelected ? selectedBg : undefined}
                        _hover={{ bg: isTenantSelected ? selectedHoverBg : navHoverBg }}
                        transition="background 0.15s"
                        spacing={2}
                      >
                        <IconButton
                          aria-label="Rozwiń/zwiń"
                          icon={
                            isTenantExpanded ? (
                              <ChevronDown size={14} />
                            ) : (
                              <ChevronRight size={14} />
                            )
                          }
                          size="xs"
                          variant="ghost"
                          minW="20px"
                          h="20px"
                          onClick={(e) => {
                            e.stopPropagation();
                            toggleNavTenant(tenant.tenantId);
                          }}
                        />
                        <HStack
                          flex={1}
                          spacing={2}
                          onClick={() => {
                            setSelection({ type: "tenant", tenantId: tenant.tenantId });
                            if (!isTenantExpanded) toggleNavTenant(tenant.tenantId);
                            if (isMobile) setSidebarOpen(false);
                          }}
                        >
                          <Building2 size={15} />
                          <Tooltip label={tenant.tenantName} openDelay={500}>
                            <Text fontSize="sm" fontWeight="medium" noOfLines={1} flex={1}>
                              {tenant.tenantName}
                            </Text>
                          </Tooltip>
                          {tenant.activeWorksCount > 0 && (
                            <Tooltip label={`Aktywne prace: ${tenant.activeWorksCount}`}>
                              <Badge
                                colorScheme="purple"
                                fontSize="2xs"
                                borderRadius="full"
                                px={1.5}
                              >
                                {tenant.activeWorksCount}
                              </Badge>
                            </Tooltip>
                          )}
                        </HStack>
                      </HStack>

                      {/* Projekty tenanta */}
                      {isTenantExpanded &&
                        tenant.projects.map((project) => {
                          const isProjectSelected =
                            selection.type === "project" &&
                            selection.tenantId === tenant.tenantId &&
                            selection.projectId === project.projectId;
                          const activeCount = getProjectActiveCount(project);

                          return (
                            <HStack
                              key={project.projectId}
                              pl={10}
                              pr={3}
                              py={1.5}
                              cursor="pointer"
                              bg={isProjectSelected ? selectedBg : undefined}
                              _hover={{
                                bg: isProjectSelected ? selectedHoverBg : navHoverBg,
                              }}
                              transition="background 0.15s"
                              spacing={2}
                              onClick={() => {
                                setSelection({
                                  type: "project",
                                  tenantId: tenant.tenantId,
                                  projectId: project.projectId,
                                });
                                if (isMobile) setSidebarOpen(false);
                              }}
                            >
                              <FolderKanban size={14} />
                              <Tooltip label={project.projectName} openDelay={500}>
                                <Text fontSize="xs" noOfLines={1} flex={1}>
                                  {project.projectName}
                                </Text>
                              </Tooltip>
                              {activeCount > 0 && (
                                <Tooltip label={`Aktywne prace: ${activeCount}`}>
                                  <Badge
                                    colorScheme="orange"
                                    fontSize="2xs"
                                    borderRadius="full"
                                    px={1.5}
                                  >
                                    {activeCount}
                                  </Badge>
                                </Tooltip>
                              )}
                            </HStack>
                          );
                        })}
                    </Box>
                  );
                })}
                    </Box>
                  </>
                )}

              {/* ─── Prawy panel: timeline ─── */}
              <Box
                flex={1}
                borderWidth="1px"
                borderColor={borderColor}
                borderRadius="lg"
                borderLeftRadius={sidebarOpen && !isMobile ? 0 : "lg"}
                borderLeftWidth={sidebarOpen && !isMobile ? 0 : "1px"}
                bg={cardBg}
                display="flex"
                flexDirection="column"
                overflow="hidden"
              >
                {/* Nagłówek + kontrolki */}
                <Box
                  px={{ base: 3, md: 4 }}
                  py={3}
                  borderBottomWidth="1px"
                  borderColor={borderColor}
                >
                  <VStack spacing={3} align="stretch">
                    <HStack spacing={2} minW={0}>
                      <Tooltip label={sidebarOpen ? "Ukryj panel nawigacji" : "Pokaż panel nawigacji"}>
                        <IconButton
                          aria-label="Toggle sidebar"
                          icon={sidebarOpen ? <PanelLeftClose size={18} /> : <PanelLeftOpen size={18} />}
                          size="sm"
                          variant="ghost"
                          onClick={() => setSidebarOpen((p) => !p)}
                        />
                      </Tooltip>
                      <Text fontWeight="bold" fontSize={isMobile ? "xs" : "md"} noOfLines={1}>
                        {selectionLabel}
                      </Text>
                    </HStack>

                    <TimelineToolbar
                      timeScale={timeScale}
                      setTimeScale={setTimeScale}
                      timeRangeMonths={timeRangeMonths}
                      setTimeRangeMonths={setTimeRangeMonths}
                      hideWeekends={hideWeekends}
                      toggleWeekends={toggleWeekends}
                      scrollToToday={scrollToToday}
                      onExpandAll={expandAll}
                      onCollapseAll={collapseAll}
                      isMobile={isMobile}
                    />
                  </VStack>
                </Box>

                {/* Tabela timeline */}
                {filteredWorks.length === 0 ? (
                  <Box p={8} textAlign="center">
                    <Text color="gray.500">Brak prac dla wybranej selekcji</Text>
                  </Box>
                ) : (
                  <Box ref={scrollContainerRef} overflowX="auto" overflowY="auto" flex={1}>
                    <Table
                      variant="simple"
                      size="sm"
                      sx={{
                        borderCollapse: "collapse",
                        "& th, & td": {
                          borderWidth: "1px",
                          borderColor: borderColor,
                          borderStyle: "solid",
                        },
                      }}
                    >
                      <Thead bg={theadBg}>
                        {/* Nagłówki grup czasowych */}
                        {dateGroups && (
                          <Tr>
                            <Th
                              position="sticky"
                              left={0}
                              bg={theadBg}
                              zIndex={20}
                              top={0}
                            />
                            <Th
                              position="sticky"
                              left={`${columnWidths.stage}px`}
                              bg={theadBg}
                              zIndex={20}
                              top={0}
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
                        {/* Nagłówki kolumn */}
                        <Tr>
                          <Th
                            w={`${columnWidths.stage}px`}
                            minW={`${columnWidths.stage}px`}
                            maxW={`${columnWidths.stage}px`}
                            position="sticky"
                            left={0}
                            bg={theadBg}
                            zIndex={20}
                            top={0}
                            fontSize="xs"
                            py={2}
                            px={2}
                            fontWeight="bold"
                            textTransform="none"
                          >
                          </Th>
                          <Th
                            w={`${columnWidths.work}px`}
                            minW={`${columnWidths.work}px`}
                            maxW={`${columnWidths.work}px`}
                            position="sticky"
                            left={`${columnWidths.stage}px`}
                            bg={theadBg}
                            zIndex={20}
                            top={0}
                            fontSize="xs"
                            py={2}
                            px={2}
                            fontWeight="bold"
                            textTransform="none"
                          >
                            Zakres robót
                          </Th>
                          {dates.map((date, idx) => {
                            const isTodayCol = isToday(date);
                            return (
                              <Th
                                key={idx}
                                ref={isTodayCol ? todayColumnRef : undefined}
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
                                <Text fontSize="2xs" whiteSpace="pre-line">
                                  {formatTimelineDate(date)}
                                </Text>
                              </Th>
                            );
                          })}
                        </Tr>
                      </Thead>
                      <Tbody>
                        {filteredWorks.map((project) => (
                          <>
                            {/* Nagłówek projektu */}
                            <Tr
                              key={`project-${project.projectId}`}
                              bg={projectRowBg}
                              cursor="pointer"
                              onClick={() => toggleProject(project.projectId)}
                              _hover={{ bg: projectRowHoverBg }}
                            >
                              <Td
                                colSpan={2}
                                position="sticky"
                                left={0}
                                top="40px"
                                bg={projectRowBg}
                                zIndex={15}
                                py={2}
                                px={2}
                                fontWeight="bold"
                              >
                                <HStack spacing={2}>
                                  <IconButton
                                    aria-label="Toggle project"
                                    icon={
                                      expandedProjects.has(project.projectId) ? (
                                        <ChevronDown size={16} />
                                      ) : (
                                        <ChevronRight size={16} />
                                      )
                                    }
                                    size="xs"
                                    variant="ghost"
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      toggleProject(project.projectId);
                                    }}
                                  />
                                  <VStack align="flex-start" spacing={0}>
                                    <Text fontSize="md">{project.projectName}</Text>
                                    {selection.type === "all" && (
                                      <Text
                                        fontSize="2xs"
                                        color="gray.600"
                                        fontWeight="normal"
                                      >
                                        {project.tenantName}
                                      </Text>
                                    )}
                                  </VStack>
                                </HStack>
                              </Td>
                              <Td
                                colSpan={dates.length}
                                bg={projectRowBg}
                                py={2}
                                px={2}
                              />
                            </Tr>

                            {/* Harmonogramy */}
                            {expandedProjects.has(project.projectId) &&
                              project.workSchedules.map((schedule) => (
                                <>
                                  <Tr
                                    key={`schedule-${schedule.workScheduleId}`}
                                    bg={scheduleRowBg}
                                    cursor="pointer"
                                    onClick={() => toggleSchedule(schedule.workScheduleId)}
                                    _hover={{ bg: scheduleRowHoverBg }}
                                  >
                                    <Td
                                      colSpan={2}
                                      position="sticky"
                                      left={0}
                                      top="80px"
                                      bg={scheduleRowBg}
                                      zIndex={14}
                                      py={2}
                                      px={2}
                                      pl={8}
                                    >
                                      <HStack spacing={2}>
                                        <IconButton
                                          aria-label="Toggle schedule"
                                          icon={
                                            expandedSchedules.has(schedule.workScheduleId) ? (
                                              <ChevronDown size={16} />
                                            ) : (
                                              <ChevronRight size={16} />
                                            )
                                          }
                                          size="xs"
                                          variant="ghost"
                                          onClick={(e) => {
                                            e.stopPropagation();
                                            toggleSchedule(schedule.workScheduleId);
                                          }}
                                        />
                                        <VStack align="flex-start" spacing={0}>
                                          <Text fontWeight="semibold" fontSize="sm">
                                            {schedule.workScheduleName}
                                          </Text>
                                          <Text fontSize="2xs" color="gray.500">
                                            Utworzono: {formatDate(schedule.workScheduleCreatedAt)}
                                          </Text>
                                        </VStack>
                                      </HStack>
                                    </Td>
                                    <Td
                                      colSpan={dates.length}
                                      bg={scheduleRowBg}
                                      py={2}
                                      px={2}
                                    />
                                  </Tr>

                                  {/* Etapy i prace */}
                                  {expandedSchedules.has(schedule.workScheduleId) &&
                                    schedule.stages
                                      .sort((a, b) => a.stageOrder - b.stageOrder)
                                      .map((stage) => {
                                        const sortedWorks = stage.works.sort(
                                          (a, b) => a.workOrder - b.workOrder
                                        );
                                        return sortedWorks.map((work, workIdx) => {
                                          const workStatus = getWorkStatus(work.periods);

                                          let rowBg = undefined;
                                          if (work.isClosed) {
                                            rowBg = completedBg;
                                          } else if (workStatus === "expired") {
                                            rowBg = expiredBg;
                                          } else if (workStatus === "warning") {
                                            rowBg = warningBg;
                                          }

                                          const isFirstWorkInStage = workIdx === 0;

                                          return (
                                            <Tr
                                              key={work.workId}
                                              bg={rowBg}
                                              _hover={{ bg: hoverBg }}
                                            >
                                              {isFirstWorkInStage && (
                                                <Td
                                                  rowSpan={sortedWorks.length}
                                                  position="sticky"
                                                  left={0}
                                                  w={`${columnWidths.stage}px`}
                                                  minW={`${columnWidths.stage}px`}
                                                  maxW={`${columnWidths.stage}px`}
                                                  bg={stageRowBg}
                                                  zIndex={1}
                                                  py={2}
                                                  px={2}
                                                  pl={12}
                                                  verticalAlign="top"
                                                  borderRightWidth="2px"
                                                >
                                                  <Text fontWeight="bold" fontSize="sm">
                                                    {stage.stageName}
                                                  </Text>
                                                </Td>
                                              )}
                                              <Td
                                                position="sticky"
                                                left={`${columnWidths.stage}px`}
                                                w={`${columnWidths.work}px`}
                                                minW={`${columnWidths.work}px`}
                                                maxW={`${columnWidths.work}px`}
                                                bg={rowBg || cardBg}
                                                zIndex={1}
                                                py={2}
                                                px={2}
                                                borderRightWidth="2px"
                                              >
                                                <VStack align="flex-start" spacing={1}>
                                                  <HStack spacing={2}>
                                                    {work.isClosed ? (
                                                      <Badge
                                                        colorScheme="green"
                                                        fontSize="2xs"
                                                      >
                                                        Zakończone
                                                      </Badge>
                                                    ) : (
                                                      <>
                                                        {workStatus === "expired" && (
                                                          <Tooltip label="Praca przeterminowana">
                                                            <Box color="red.500">
                                                              <AlertTriangle size={12} />
                                                            </Box>
                                                          </Tooltip>
                                                        )}
                                                        {workStatus === "warning" && (
                                                          <Tooltip label="Zakończenie za 5 dni lub mniej">
                                                            <Box color="yellow.600">
                                                              <AlertTriangle size={12} />
                                                            </Box>
                                                          </Tooltip>
                                                        )}
                                                      </>
                                                    )}
                                                    <Text fontSize="sm">{work.workName}</Text>
                                                  </HStack>
                                                  {work.periods.length > 0 && (
                                                    <VStack
                                                      align="flex-start"
                                                      spacing={0.5}
                                                      fontSize="2xs"
                                                      color="gray.500"
                                                    >
                                                      {work.periods.map((period, pIdx) => (
                                                        <Text key={pIdx}>
                                                          {work.periods.length > 1
                                                            ? `${pIdx + 1}. `
                                                            : ""}
                                                          {formatDate(period.startDate)} –{" "}
                                                          {formatDate(period.endDate)}
                                                        </Text>
                                                      ))}
                                                    </VStack>
                                                  )}
                                                </VStack>
                                              </Td>
                                              {/* Komórki timeline */}
                                              {dates.map((periodStart, idx) => {
                                                const periodEnd = getPeriodEnd(periodStart);
                                                const isActive = work.periods.some(
                                                  (period) =>
                                                    isWorkInPeriod(
                                                      period.startDate,
                                                      period.endDate,
                                                      periodStart,
                                                      periodEnd
                                                    )
                                                );
                                                const isTodayCol = isToday(periodStart);

                                                return (
                                                  <Td
                                                    key={idx}
                                                    p={0}
                                                    bg={
                                                      isActive ? work.colorRgb : (isTodayCol ? todayBg : undefined)
                                                    }
                                                    position="relative"
                                                    borderLeftWidth={isTodayCol ? "2px" : undefined}
                                                    borderRightWidth={isTodayCol ? "2px" : undefined}
                                                    borderColor={isTodayCol ? "blue.500" : undefined}
                                                  >
                                                    {isActive && (
                                                      <Tooltip
                                                        label={`${work.workName}${
                                                          work.periods.length > 1
                                                            ? ` (${work.periods.length} okresów)`
                                                            : ""
                                                        }`}
                                                      >
                                                        <Box
                                                          h="100%"
                                                          minH="50px"
                                                          w="100%"
                                                          bg={work.colorRgb}
                                                          cursor="pointer"
                                                          transition="opacity 0.2s"
                                                          _hover={{ opacity: 0.8 }}
                                                        />
                                                      </Tooltip>
                                                    )}
                                                  </Td>
                                                );
                                              })}
                                            </Tr>
                                          );
                                        });
                                      })}
                                </>
                              ))}
                          </>
                        ))}
                      </Tbody>
                    </Table>
                  </Box>
                )}
              </Box>
            </HStack>
            </Box>
          )}
        </VStack>
      </Box>
    </MainLayout>
  );
}