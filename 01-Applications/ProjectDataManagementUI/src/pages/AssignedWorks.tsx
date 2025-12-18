import { useEffect, useState } from "react";
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
  Slider,
  SliderTrack,
  SliderFilledTrack,
  SliderThumb,
  IconButton,
} from "@chakra-ui/react";
import { Briefcase, AlertTriangle, ChevronDown, ChevronRight } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { projectApi } from "../api/projectApi";
import { tenantApi } from "../api/tenantApi";
import type { UserAssignedWorksGroupedWeb } from "../types/workSchedule.types";

type TimeScale = "days" | "weeks" | "months";

export default function AssignedWorks() {
  const [assignedWorks, setAssignedWorks] = useState<UserAssignedWorksGroupedWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [timeScale, setTimeScale] = useState<TimeScale>("weeks");
  const [timeRangeMonths, setTimeRangeMonths] = useState(1);
  const [expandedProjects, setExpandedProjects] = useState<Set<string>>(new Set());
  const [expandedSchedules, setExpandedSchedules] = useState<Set<string>>(new Set());
  const [columnWidths] = useState({
    stage: 200,
    work: 350,
  });

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const expiredBg = useColorModeValue("red.50", "red.900");
  const warningBg = useColorModeValue("yellow.50", "yellow.900");
  const completedBg = useColorModeValue("green.50", "green.900");

  useEffect(() => {
    const fetchAssignedWorks = async () => {
      try {
        setLoading(true);
        setError(null);

        // Pobierz aktywny tenant
        const activeTenantResponse = await tenantApi.getActiveTenant();
        if (!activeTenantResponse.ok) {
          throw new Error("Nie udało się pobrać aktywnego tenanta");
        }

        const activeTenantData = await activeTenantResponse.json();
        if (!activeTenantData.activeTenantId || activeTenantData.activeTenantId === "00000000-0000-0000-0000-000000000000") {
          setError("Brak aktywnego tenanta. Wybierz organizację.");
          setLoading(false);
          return;
        }

        // Pobierz zaplanowane prace
        const response = await projectApi.getMyAssignedWorks(activeTenantData.activeTenantId);
        if (!response.ok) {
          throw new Error("Nie udało się pobrać zaplanowanych prac");
        }

        const data: UserAssignedWorksGroupedWeb[] = await response.json();
        setAssignedWorks(data);
      } catch (err: any) {
        console.error("Błąd pobierania zaplanowanych prac:", err);
        setError(err.message || "Wystąpił błąd podczas pobierania zaplanowanych prac");
      } finally {
        setLoading(false);
      }
    };

    fetchAssignedWorks();
  }, []);

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

  const getTimelineData = () => {
    const dates: Date[] = [];
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    const minDate = new Date(today);
    minDate.setMonth(minDate.getMonth() - timeRangeMonths);
    minDate.setDate(1);
    
    const maxDate = new Date(today);
    maxDate.setMonth(maxDate.getMonth() + timeRangeMonths);
    maxDate.setMonth(maxDate.getMonth() + 1);
    maxDate.setDate(0);
    
    const current = new Date(minDate);
    
    if (timeScale === "days") {
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
    } else if (timeScale === "weeks") {
      const dateGroups: { label: string; count: number; startIdx: number }[] = [];
      let groupStartIdx = 0;
      
      while (current <= maxDate) {
        const weekStart = new Date(current);
        const dayOfWeek = weekStart.getDay();
        const diff = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
        weekStart.setDate(weekStart.getDate() + diff);
        
        const weekEnd = new Date(weekStart);
        weekEnd.setDate(weekStart.getDate() + 6);
        
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
    return start < periodEnd && end >= periodStart;
  };

  const getPeriodEnd = (periodStart: Date): Date => {
    const end = new Date(periodStart);
    end.setDate(end.getDate() + 1);
    return end;
  };

  const formatTimelineDate = (date: Date): string => {
    if (timeScale === "days") {
      return `${date.getDate()}.${date.getMonth() + 1}`;
    } else if (timeScale === "weeks") {
      const dayNames = ["Nd", "Pn", "Wt", "Śr", "Cz", "Pt", "So"];
      return `${dayNames[date.getDay()]}\n${date.getDate()}.${date.getMonth() + 1}`;
    } else {
      return `${date.getDate()}`;
    }
  };

  const { dates, dateGroups } = getTimelineData();

  const toggleProject = (projectId: string) => {
    setExpandedProjects((prev) => {
      const next = new Set(prev);
      if (next.has(projectId)) {
        next.delete(projectId);
        // Also collapse all schedules in this project
        assignedWorks
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
    setExpandedProjects(new Set(assignedWorks.map((p) => p.projectId)));
    setExpandedSchedules(
      new Set(assignedWorks.flatMap((p) => p.workSchedules.map((s) => s.workScheduleId)))
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
        <Box p={8}>
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
      <Box p={8}>
        <VStack align="stretch" spacing={6}>
          {/* Header */}
          <HStack spacing={4} justify="space-between">
            <HStack spacing={4}>
              <Briefcase size={32} />
              <Heading size="lg">Zaplanowane prace</Heading>
            </HStack>
            <HStack spacing={2}>
              <Button size="sm" variant="outline" onClick={expandAll}>
                Rozwiń wszystkie
              </Button>
              <Button size="sm" variant="outline" onClick={collapseAll}>
                Zwiń wszystkie
              </Button>
            </HStack>
          </HStack>

          {assignedWorks.length === 0 ? (
            <Alert status="info">
              <AlertIcon />
              Nie masz przypisanych żadnych prac
            </Alert>
          ) : (
            <>
              {/* Controls */}
              <Box bg={cardBg} borderWidth="1px" borderColor={borderColor} borderRadius="lg" p={4}>
                <VStack spacing={4} align="stretch">
                  <HStack spacing={4} justify="space-between" flexWrap="wrap">
                    <HStack spacing={4}>
                      <Text fontWeight="medium" fontSize="sm">
                        Skala czasu:
                      </Text>
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
                  </HStack>

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
                      ±{timeRangeMonths}{" "}
                      {timeRangeMonths === 1
                        ? "miesiąc"
                        : timeRangeMonths < 5
                        ? "miesiące"
                        : "miesięcy"}
                    </Text>
                  </HStack>
                </VStack>
              </Box>

              {/* Timeline View */}
              <Box bg={cardBg} borderWidth="1px" borderColor={borderColor} borderRadius="lg" overflow="hidden">
                <Box overflowX="auto" overflowY="auto" maxH="calc(100vh - 350px)">
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
                    <Thead bg={useColorModeValue("gray.50", "gray.700")}>
                      {/* Group headers */}
                      {dateGroups && (
                        <Tr>
                          <Th position="sticky" left={0} bg={useColorModeValue("gray.50", "gray.700")} zIndex={20} top={0} />
                          <Th position="sticky" left={`${columnWidths.stage}px`} bg={useColorModeValue("gray.50", "gray.700")} zIndex={20} top={0} />
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
                          bg={useColorModeValue("gray.50", "gray.700")}
                          zIndex={20}
                          top={0}
                          fontSize="xs"
                          py={2}
                          px={2}
                          fontWeight="bold"
                          textTransform="none"
                        >
                          Etap
                        </Th>
                        <Th
                          w={`${columnWidths.work}px`}
                          minW={`${columnWidths.work}px`}
                          maxW={`${columnWidths.work}px`}
                          position="sticky"
                          left={`${columnWidths.stage}px`}
                          bg={useColorModeValue("gray.50", "gray.700")}
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
                        {dates.map((date, idx) => (
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
                            <Text fontSize="2xs" whiteSpace="pre-line">
                              {formatTimelineDate(date)}
                            </Text>
                          </Th>
                        ))}
                      </Tr>
                    </Thead>
                    <Tbody>
                      {assignedWorks.map((project) => (
                        <>
                          {/* Project header row */}
                          <Tr
                            key={`project-${project.projectId}`}
                            bg={useColorModeValue("purple.100", "purple.800")}
                            cursor="pointer"
                            onClick={() => toggleProject(project.projectId)}
                            _hover={{ bg: useColorModeValue("purple.200", "purple.700") }}
                          >
                            <Td
                              colSpan={2}
                              position="sticky"
                              left={0}
                              top={"40px"}
                              bg={useColorModeValue("purple.100", "purple.800")}
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
                                <Text fontSize="md">{project.projectName}</Text>
                              </HStack>
                            </Td>
                            <Td
                              colSpan={dates.length}
                              bg={useColorModeValue("purple.100", "purple.800")}
                              py={2}
                              px={2}
                            />
                          </Tr>

                          {/* Project schedules - only if expanded */}
                          {expandedProjects.has(project.projectId) &&
                            project.workSchedules.map((schedule) => (
                              <>
                                {/* Schedule header row */}
                                <Tr
                                  key={`schedule-${schedule.workScheduleId}`}
                                  bg={useColorModeValue("cyan.50", "cyan.900")}
                                  cursor="pointer"
                                  onClick={() => toggleSchedule(schedule.workScheduleId)}
                                  _hover={{ bg: useColorModeValue("cyan.100", "cyan.800") }}
                                >
                                  <Td
                                    colSpan={2}
                                    position="sticky"
                                    left={0}
                                    top={"80px"}
                                    bg={useColorModeValue("cyan.50", "cyan.900")}
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
                                    bg={useColorModeValue("cyan.50", "cyan.900")}
                                    py={2}
                                    px={2}
                                  />
                                </Tr>

                                {/* Schedule stages and works - only if expanded */}
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
                                          <Tr key={work.workId} bg={rowBg} _hover={{ bg: hoverBg }}>
                                            {/* Stage name */}
                                            {isFirstWorkInStage && (
                                              <Td
                                                rowSpan={sortedWorks.length}
                                                position="sticky"
                                                left={0}
                                                w={`${columnWidths.stage}px`}
                                                minW={`${columnWidths.stage}px`}
                                                maxW={`${columnWidths.stage}px`}
                                                bg={useColorModeValue("blue.50", "blue.900")}
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
                                            {/* Work details */}
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
                                                    <Badge colorScheme="green" fontSize="2xs">
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
                                                        {work.periods.length > 1 ? `${pIdx + 1}. ` : ""}
                                                        {formatDate(period.startDate)} -{" "}
                                                        {formatDate(period.endDate)}
                                                      </Text>
                                                    ))}
                                                  </VStack>
                                                )}
                                              </VStack>
                                            </Td>
                                            {/* Timeline cells */}
                                            {dates.map((periodStart, idx) => {
                                              const periodEnd = getPeriodEnd(periodStart);
                                              const isActive = work.periods.some((period) =>
                                                isWorkInPeriod(
                                                  period.startDate,
                                                  period.endDate,
                                                  periodStart,
                                                  periodEnd
                                                )
                                              );

                                              return (
                                                <Td
                                                  key={idx}
                                                  p={0}
                                                  bg={isActive ? work.colorRgb : undefined}
                                                  position="relative"
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
              </Box>
            </>
          )}
        </VStack>
      </Box>
    </MainLayout>
  );
}
