import { useEffect, useState, useContext } from "react";
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
} from "@chakra-ui/react";
import { ArrowLeft, Edit, Clock, User, AlertTriangle } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { projectApi } from "../api/projectApi";
import EditWorkScheduleModal from "../components/EditWorkScheduleModal";
import { AuthContext } from "../context/AuthContext";
import type { WorkScheduleDetailsWeb } from "../types/workSchedule.types";

type TimeScale = "days" | "weeks" | "months";

export default function WorkScheduleView() {
  const { projectId, workScheduleId } = useParams<{ projectId: string; workScheduleId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { isOpen: isEditModalOpen, onOpen: onEditModalOpen, onClose: onEditModalClose } = useDisclosure();

  const [schedule, setSchedule] = useState<WorkScheduleDetailsWeb | null>(null);
  const [members, setMembers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [timeScale, setTimeScale] = useState<TimeScale>("weeks");
  const [error, setError] = useState<string | null>(null);
  const [timeRangeMonths, setTimeRangeMonths] = useState(1); // ±1 month by default
  const [columnWidths, setColumnWidths] = useState({
    stage: 200,
    description: 350,
  });

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const expiredBg = useColorModeValue("red.50", "red.900");

  useEffect(() => {
    fetchSchedule();
    fetchMembers();
  }, [user?.activeTenantId, projectId, workScheduleId]);

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
      return `${date.getDate()}.${date.getMonth() + 1}`;
    } else if (timeScale === "weeks") {
      const dayNames = ["Nd", "Pn", "Wt", "Śr", "Cz", "Pt", "So"];
      return `${dayNames[date.getDay()]}\n${date.getDate()}.${date.getMonth() + 1}`;
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

  const { dates, dateGroups } = getTimelineData();

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
          <Button
            leftIcon={<ArrowLeft size={18} />}
            mt={4}
            onClick={() => navigate(`/projects/${projectId}`)}
          >
            Powrót do projektu
          </Button>
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box maxW="1600px" mx="auto" p={8}>
        <VStack spacing={6} align="stretch">
          {/* Header */}
          <HStack justify="space-between">
            <HStack spacing={4}>
              <IconButton
                aria-label="Powrót"
                icon={<ArrowLeft size={20} />}
                onClick={() => navigate(`/projects/${projectId}`)}
              />
              <VStack align="flex-start" spacing={0}>
                <Heading size="lg">{schedule.name}</Heading>
                <HStack spacing={3} fontSize="sm" color="gray.500">
                  <HStack spacing={1}>
                    <User size={14} />
                    <Text>{schedule.createdByUserName}</Text>
                  </HStack>
                  <HStack spacing={1}>
                    <Clock size={14} />
                    <Text>{formatDateTime(schedule.createdAt)}</Text>
                  </HStack>
                </HStack>
              </VStack>
            </HStack>

            <HStack spacing={3}>
              <Button
                leftIcon={<Edit size={18} />}
                colorScheme="purple"
                onClick={handleEditMode}
              >
                Edytuj
              </Button>
            </HStack>
          </HStack>

          {/* Controls */}
          <Box
            p={4}
            bg={cardBg}
            borderWidth="1px"
            borderColor={borderColor}
            borderRadius="lg"
          >
            <VStack spacing={4} align="stretch">
              <HStack spacing={6} justify="space-between">
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

          {/* Timeline View - Format tabelaryczny */}
          <Box
              bg={cardBg}
              borderWidth="1px"
              borderColor={borderColor}
              borderRadius="lg"
              overflow="hidden"
            >
              <Box overflowX="auto">
                <Table 
                  variant="simple" 
                  size="sm" 
                  sx={{
                    borderCollapse: "collapse",
                    "& th, & td": {
                      borderWidth: "1px",
                      borderColor: borderColor,
                      borderStyle: "solid",
                    }
                  }}
                >
                  <Thead bg={useColorModeValue("gray.50", "gray.700")}>
                    {/* Group headers for weeks/months */}
                    {dateGroups && (
                      <Tr>
                        <Th 
                          position="sticky"
                          left={0}
                          bg={useColorModeValue("gray.50", "gray.700")}
                          zIndex={3}
                        />
                        <Th 
                          position="sticky"
                          left={`${columnWidths.stage}px`}
                          bg={useColorModeValue("gray.50", "gray.700")}
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
                        bg={useColorModeValue("gray.50", "gray.700")}
                        zIndex={3}
                        fontSize="xs"
                        py={2}
                        px={2}
                        fontWeight="bold"
                        textTransform="none"
                      >
                        Etap
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
                      </Th>
                      <Th 
                        w={`${columnWidths.description}px`}
                        minW={`${columnWidths.description}px`}
                        maxW={`${columnWidths.description}px`}
                        position="sticky"
                        left={`${columnWidths.stage}px`}
                        bg={useColorModeValue("gray.50", "gray.700")}
                        zIndex={3}
                        fontSize="xs"
                        py={2}
                        px={2}
                        fontWeight="bold"
                        textTransform="none"
                      >
                        Zakres robót
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
                    {schedule.stages
                      .sort((a, b) => a.order - b.order)
                      .map((stage) => {
                        const sortedWorks = stage.works.sort((a, b) => a.order - b.order);
                        return sortedWorks.map((work, workIdx) => {
                          const workStatus = getWorkStatus(work.periods);
                          const warningBg = useColorModeValue("yellow.50", "yellow.900");
                          const completedBg = useColorModeValue("green.50", "green.900");
                          
                          let rowBg = undefined;
                          if (work.isClosed) {
                            // Completed work overrides all other statuses
                            rowBg = completedBg;
                          } else if (workStatus === 'expired') {
                            rowBg = expiredBg;
                          } else if (workStatus === 'warning') {
                            rowBg = warningBg;
                          }
                          
                          return (
                            <Tr 
                              key={work.id}
                              bg={rowBg}
                              _hover={{ bg: hoverBg }}
                            >
                              {/* Stage name - merged vertically for all works */}
                              {workIdx === 0 && (
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
                                  verticalAlign="top"
                                  borderRightWidth="2px"
                                >
                                  <Text fontWeight="bold" fontSize="sm">{stage.name}</Text>
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
                                    <Text fontSize="sm">{work.name}</Text>
                                  </HStack>
                                  {work.periods.length > 0 && (
                                    <VStack align="flex-start" spacing={0.5} fontSize="2xs" color="gray.500">
                                      {work.periods.map((period, pIdx) => (
                                        <Text key={pIdx}>
                                          {work.periods.length > 1 ? `${pIdx + 1}. ` : ''}
                                          {formatDate(period.startDate)} - {formatDate(period.endDate)}
                                        </Text>
                                      ))}
                                    </VStack>
                                  )}
                                  {work.assignees.length > 0 && (
                                    <HStack spacing={1} flexWrap="wrap" mt={1}>
                                      {work.assignees.map((assignee) => (
                                        <Badge key={assignee.userId} colorScheme="purple" fontSize="2xs">
                                          {assignee.userName}
                                        </Badge>
                                      ))}
                                    </HStack>
                                  )}
                                </VStack>
                              </Td>
                              {/* Timeline cells */}
                              {dates.map((periodStart, idx) => {
                                const periodEnd = getPeriodEnd(periodStart);
                                // Check if any period of this work is active in this cell
                                const isActive = work.periods.some(period => 
                                  isWorkInPeriod(period.startDate, period.endDate, periodStart, periodEnd)
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
                                        label={`${work.name}${work.periods.length > 1 ? ` (${work.periods.length} okresów)` : ''}`}
                                      >
                                        <Box
                                          h="100%"
                                          minH="50px"
                                          w="100%"
                                          bg={work.colorRgb}
                                          cursor="pointer"
                                          transition="opacity 0.2s"
                                          _hover={{
                                            opacity: 0.85,
                                          }}
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
              </Box>
            </Box>

          {/* Edit Modal */}
          {schedule && (
            <EditWorkScheduleModal
              isOpen={isEditModalOpen}
              onClose={onEditModalClose}
              tenantId={user?.activeTenantId || ""}
              projectId={projectId || ""}
              schedule={schedule}
              members={members}
              onScheduleUpdated={handleScheduleUpdated}
            />
          )}
        </VStack>
      </Box>
    </MainLayout>
  );
}
