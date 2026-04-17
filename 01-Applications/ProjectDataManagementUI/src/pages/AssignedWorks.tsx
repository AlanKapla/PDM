import { memo, useCallback, useMemo, useState } from "react";
import {
  Alert,
  AlertIcon,
  Badge,
  Box,
  Button,
  Collapse,
  Flex,
  FormControl,
  HStack,
  Heading,
  SimpleGrid,
  Spinner,
  Switch,
  Text,
  Textarea,
  Tooltip,
  VStack,
  useColorModeValue,
} from "@chakra-ui/react";
import {
  Briefcase,
  ChevronDown,
  ChevronRight,
  ChevronsDown,
  ChevronsUp,
  FolderKanban,
  MessageSquare,
  RefreshCw,
} from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { useMyWorks } from "../hooks/useMyWorks";
import { flattenWorks } from "../utils/myWorksTree";
import type { FlatWork } from "../utils/myWorksTree";
import type {
  UserAssignedWorkWeb,
  WorkScheduleStageWorkPeriodWeb,
  WorkScheduleStageWorkCommentWeb,
} from "../types/workSchedule.types";
import { formatDateShort } from "../utils/formatters";

// --- Typy -----------------------------------------------------------------------

type FilterType = "today" | "upcoming" | "overdue" | "done" | "all";

// --- Komponent okresu pracy (bez zmian) -----------------------------------------

interface PeriodRowProps {
  period: WorkScheduleStageWorkPeriodWeb;
  isMutating: boolean;
  onToggle: (isClosed: boolean) => void;
}

const PeriodRow = memo(function PeriodRow({ period, isMutating, onToggle }: PeriodRowProps) {
  const borderColor = useColorModeValue("gray.100", "gray.700");
  const bgClosed = useColorModeValue("gray.50", "gray.750");

  return (
    <HStack
      px={3} py={1.5}
      bg={period.isClosed ? bgClosed : undefined}
      borderRadius="md"
      borderWidth="1px"
      borderColor={borderColor}
      spacing={3}
      opacity={period.isClosed ? 0.7 : 1}
    >
      <Switch
        size="sm"
        isChecked={period.isClosed}
        onChange={e => onToggle(e.target.checked)}
        isDisabled={isMutating}
        colorScheme="green"
      />
      <Text fontSize="xs" fontFamily="mono" whiteSpace="nowrap">
        {formatDateShort(period.startDate)} - {formatDateShort(period.endDate)}
      </Text>
      {period.isClosed && (
        <Badge colorScheme="green" fontSize="2xs">Zamkniety</Badge>
      )}
    </HStack>
  );
});

// --- Sekcja komentarzy (bez zmian) ----------------------------------------------

interface CommentsSectionProps {
  comments: WorkScheduleStageWorkCommentWeb[];
  isMutating: boolean;
  onAddComment: (content: string) => void;
}

const CommentsSection = memo(function CommentsSection({ comments, isMutating, onAddComment }: CommentsSectionProps) {
  const [inputValue, setInputValue] = useState("");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const commentBg = useColorModeValue("gray.50", "gray.750");
  const metaColor = useColorModeValue("gray.500", "gray.400");

  const handleSubmit = () => {
    const trimmed = inputValue.trim();
    if (!trimmed) return;
    onAddComment(trimmed);
    setInputValue("");
  };

  return (
    <Box>
      {comments.length > 0 && (
        <VStack align="stretch" spacing={1} mb={3}>
          {comments.map(comment => (
            <Box key={comment.id} bg={commentBg} borderRadius="md" px={3} py={2} borderWidth="1px" borderColor={borderColor}>
              <HStack spacing={2} mb={1}>
                <Text fontSize="2xs" color={metaColor} fontWeight="semibold">
                  {comment.createdByUserName}
                </Text>
                <Text fontSize="2xs" color={metaColor}>
                  {formatDateShort(comment.createdAt)}
                </Text>
              </HStack>
              <Text fontSize="sm" whiteSpace="pre-wrap">{comment.content}</Text>
            </Box>
          ))}
        </VStack>
      )}

      <FormControl>
        <Textarea
          placeholder="Dodaj komentarz..."
          size="sm"
          rows={2}
          value={inputValue}
          onChange={e => setInputValue(e.target.value)}
          resize="none"
          borderRadius="md"
        />
        <Button
          size="xs"
          colorScheme="primary"
          mt={1}
          isDisabled={!inputValue.trim() || isMutating}
          isLoading={isMutating}
          onClick={handleSubmit}
        >
          Wyslij
        </Button>
      </FormControl>
    </Box>
  );
});

// --- Wiersz zakresu pracy -------------------------------------------------------

interface WorkItemProps {
  work: UserAssignedWorkWeb;
  stageName?: string;
  isExpanded: boolean;
  isMutating: boolean;
  onToggleExpand: () => void;
  onToggleWork: (isClosed: boolean) => void;
  onTogglePeriod: (periodId: string, isClosed: boolean) => void;
  onAddComment: (content: string) => void;
}

const WorkItem = memo(function WorkItem({
  work, stageName, isExpanded, isMutating,
  onToggleExpand, onToggleWork, onTogglePeriod, onAddComment,
}: WorkItemProps) {
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const expandedBg = useColorModeValue("gray.50", "gray.800");
  const hoverBg = useColorModeValue("gray.50", "gray.750");
  const closedBg = useColorModeValue("gray.50", "gray.800");

  const closedPeriods = work.periods.filter(p => p.isClosed).length;
  const totalPeriods = work.periods.length;

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const isOverdue = !work.isClosed && totalPeriods > 0 && work.periods.every(p => new Date(p.endDate) < today);

  return (
    <Box borderWidth="1px" borderColor={borderColor} borderRadius="md" overflow="hidden">
      <HStack
        px={3} py={2} spacing={3} cursor="pointer"
        bg={work.isClosed ? closedBg : undefined}
        _hover={{ bg: hoverBg }}
        transition="background 0.12s"
        onClick={onToggleExpand}
        userSelect="none"
      >
        <Box
          w="10px" h="10px" borderRadius="full" flexShrink={0}
          bg={work.colorRgb || "gray.400"}
          opacity={work.isClosed ? 0.5 : 1}
        />
        <Box flex={1} minW={0}>
          <Text
            fontSize="sm" fontWeight="medium" noOfLines={1}
            textDecoration={work.isClosed ? "line-through" : undefined}
            color={work.isClosed ? "gray.500" : undefined}
          >
            {work.workName}
          </Text>
          {stageName && (
            <Text fontSize="xs" color="gray.400" noOfLines={1}>{stageName}</Text>
          )}
        </Box>
        {isOverdue && (
          <Badge colorScheme="red" fontSize="2xs">Przeterminowane</Badge>
        )}
        {totalPeriods > 0 && (
          <Tooltip label={`${closedPeriods}/${totalPeriods} okresow zamknietych`} openDelay={400}>
            <Badge
              colorScheme={closedPeriods === totalPeriods ? "green" : closedPeriods > 0 ? "yellow" : "gray"}
              fontSize="2xs" borderRadius="full" px={1.5}
            >
              {closedPeriods}/{totalPeriods}
            </Badge>
          </Tooltip>
        )}
        {work.comments.length > 0 && (
          <HStack spacing={0.5}>
            <MessageSquare size={12} />
            <Text fontSize="2xs">{work.comments.length}</Text>
          </HStack>
        )}
        <Tooltip label={work.isClosed ? "Otwórz zakres pracy" : "Zamknij zakres pracy"} openDelay={400}>
          <Box as="span" display="inline-flex" onClick={(e: React.MouseEvent) => e.stopPropagation()}>
            <Switch
              size="sm" colorScheme="green"
              isChecked={work.isClosed}
              isDisabled={isMutating}
              onChange={e => onToggleWork(e.target.checked)}
            />
          </Box>
        </Tooltip>
        {isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
      </HStack>

      <Collapse in={isExpanded} animateOpacity>
        <Box px={4} py={3} bg={expandedBg} borderTopWidth="1px" borderColor={borderColor}>
          {totalPeriods > 0 && (
            <Box mb={3}>
              <Text fontSize="xs" fontWeight="semibold" mb={2} color="gray.500">
                Okresy ({totalPeriods})
              </Text>
              <VStack align="stretch" spacing={1}>
                {work.periods.map(period => (
                  <PeriodRow
                    key={period.id}
                    period={period}
                    isMutating={isMutating}
                    onToggle={isClosed => onTogglePeriod(period.id, isClosed)}
                  />
                ))}
              </VStack>
            </Box>
          )}
          <Box>
            <HStack spacing={1} mb={2}>
              <MessageSquare size={13} />
              <Text fontSize="xs" fontWeight="semibold" color="gray.500">Komentarze</Text>
            </HStack>
            <CommentsSection
              comments={work.comments}
              isMutating={isMutating}
              onAddComment={onAddComment}
            />
          </Box>
        </Box>
      </Collapse>
    </Box>
  );
});

// --- Komponent strony -----------------------------------------------------------

export default function AssignedWorks() {
  const {
    data, loading, mutating, error, reload,
    setWorkIsClosed, setPeriodIsClosed, addComment,
  } = useMyWorks();

  const [activeFilter, setActiveFilter] = useState<FilterType>("today");
  const [expandedWorks, setExpandedWorks] = useState<Set<string>>(new Set());
  const [collapsedProjects, setCollapsedProjects] = useState<Set<string>>(new Set());

  const flatWorks = useMemo(() => flattenWorks(data), [data]);

  const findWork = useCallback((workId: string): FlatWork | undefined =>
    flatWorks.find(w => w.workId === workId),
  [flatWorks]);

  // --- Statystyki ---

  const stats = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const allPeriodsClosed = (w: FlatWork) => w.periods.length > 0 && w.periods.every(p => p.isClosed);

    const active = flatWorks.filter(w =>
      !w.isClosed && !allPeriodsClosed(w) &&
      w.periods.some(p => new Date(p.startDate) <= today && new Date(p.endDate) >= today)
    ).length;

    const upcoming = flatWorks.filter(w => {
      if (w.isClosed || allPeriodsClosed(w)) return false;
      return w.periods.some(p => {
        const diff = Math.ceil((new Date(p.startDate).getTime() - today.getTime()) / 86400000);
        return diff > 0 && diff <= 14;
      });
    }).length;

    const overdue = flatWorks.filter(w =>
      !w.isClosed && !allPeriodsClosed(w) &&
      w.periods.length > 0 &&
      w.periods.every(p => new Date(p.endDate) < today)
    ).length;

    const done = flatWorks.filter(w =>
      w.periods.length > 0 && w.periods.every(p => p.isClosed)
    ).length;

    return { all: flatWorks.length, active, upcoming, overdue, done };
  }, [flatWorks]);

  // --- Filtrowanie ---

  const filteredWorks = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const allPeriodsClosed = (w: FlatWork) => w.periods.length > 0 && w.periods.every(p => p.isClosed);

    switch (activeFilter) {
      case "today":
        return flatWorks.filter(w =>
          !w.isClosed && !allPeriodsClosed(w) &&
          w.periods.some(p => new Date(p.startDate) <= today && new Date(p.endDate) >= today)
        );
      case "upcoming":
        return flatWorks.filter(w => {
          if (w.isClosed || allPeriodsClosed(w)) return false;
          return w.periods.some(p => {
            const diff = Math.ceil((new Date(p.startDate).getTime() - today.getTime()) / 86400000);
            return diff > 0 && diff <= 14;
          });
        });
      case "overdue":
        return flatWorks.filter(w =>
          !w.isClosed && !allPeriodsClosed(w) &&
          w.periods.length > 0 &&
          w.periods.every(p => new Date(p.endDate) < today)
        );
      case "done":
        return flatWorks.filter(w =>
          w.periods.length > 0 && w.periods.every(p => p.isClosed)
        );
      default:
        return flatWorks;
    }
  }, [flatWorks, activeFilter]);

  // --- Grupowanie po projekcie ---

  const groupedByProject = useMemo(() => {
    const map = new Map<string, { projectId: string; projectName: string; tenantName: string; works: FlatWork[] }>();
    for (const work of filteredWorks) {
      if (!map.has(work.projectId)) {
        map.set(work.projectId, { projectId: work.projectId, projectName: work.projectName, tenantName: work.tenantName, works: [] });
      }
      map.get(work.projectId)!.works.push(work);
    }
    return Array.from(map.values());
  }, [filteredWorks]);

  // --- Handlery ---

  const toggleWork = useCallback((workId: string) => {
    setExpandedWorks(prev => {
      const next = new Set(prev);
      next.has(workId) ? next.delete(workId) : next.add(workId);
      return next;
    });
  }, []);

  const toggleProject = useCallback((projectId: string) => {
    setCollapsedProjects(prev => {
      const next = new Set(prev);
      next.has(projectId) ? next.delete(projectId) : next.add(projectId);
      return next;
    });
  }, []);

  const expandAll = useCallback(() => {
    setExpandedWorks(new Set(flatWorks.map(w => w.workId)));
    setCollapsedProjects(new Set());
  }, [flatWorks]);

  const collapseAll = useCallback(() => {
    setExpandedWorks(new Set());
    setCollapsedProjects(new Set(groupedByProject.map(g => g.projectId)));
  }, [groupedByProject]);

  const handleToggleWork = useCallback((workId: string, isClosed: boolean) => {
    const work = findWork(workId);
    if (!work) return;
    setWorkIsClosed(work.tenantId, work.projectId, work.scheduleId, work.stageId, workId, isClosed);
  }, [findWork, setWorkIsClosed]);

  const handleTogglePeriod = useCallback((workId: string, periodId: string, isClosed: boolean) => {
    const work = findWork(workId);
    if (!work) return;
    setPeriodIsClosed(work.tenantId, work.projectId, work.scheduleId, work.stageId, workId, periodId, isClosed);
  }, [findWork, setPeriodIsClosed]);

  const handleAddComment = useCallback((workId: string, content: string) => {
    const work = findWork(workId);
    if (!work) return;
    addComment(work.tenantId, work.projectId, work.scheduleId, work.stageId, workId, content);
  }, [findWork, addComment]);

  // --- Kolory ---

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const cardBg = useColorModeValue("white", "gray.800");

  // --- Render ---

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
        <Box p={{ base: 3, md: 8 }}>
          <Alert status="error"><AlertIcon />{error}</Alert>
        </Box>
      </MainLayout>
    );
  }

  const statTiles: { key: FilterType; label: string; value: number; color: string }[] = [
    { key: "today",    label: "Co robie dzisiaj",  value: stats.active,   color: "blue"   },
    { key: "upcoming", label: "Nadchodzace",        value: stats.upcoming, color: "orange" },
    { key: "overdue",  label: "Przeterminowane",    value: stats.overdue,  color: "red"    },
    { key: "done",     label: "Ukonczone",          value: stats.done,     color: "green"  },
    { key: "all",      label: "Wszystkie",          value: stats.all,      color: "gray"   },
  ];

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 8 }}>
        {/* Toolbar */}
        <Flex justify="space-between" align="center" mb={6} wrap="wrap" gap={3}>
          <HStack spacing={2}>
            <Briefcase size={24} color="orange" />
            <Heading size={{ base: "md", md: "lg" }}>Zaplanowane prace</Heading>
          </HStack>
          <HStack spacing={2}>
            <Button size="sm" variant="outline" leftIcon={<RefreshCw size={14} />} isLoading={loading} onClick={reload}>
              Odśwież
            </Button>
            <Button size="sm" variant="outline" leftIcon={<ChevronsDown size={14} />} onClick={expandAll}>
              Rozwń wszystko
            </Button>
            <Button size="sm" variant="outline" leftIcon={<ChevronsUp size={14} />} onClick={collapseAll}>
              Zwiń wszystko
            </Button>
          </HStack>
        </Flex>

        {/* Stats Bar — klikniecie przelaczy aktywny filtr */}
        <SimpleGrid columns={{ base: 2, sm: 3, md: 5 }} spacing={3} mb={6}>
          {statTiles.map(stat => (
            <Box
              key={stat.key}
              onClick={() => setActiveFilter(stat.key)}
              cursor="pointer"
              borderRadius="xl"
              borderWidth="2px"
              borderColor={activeFilter === stat.key ? `${stat.color}.400` : borderColor}
              p={4}
              bg={activeFilter === stat.key ? `${stat.color}.50` : cardBg}
              transition="all 0.15s"
              _hover={{ borderColor: `${stat.color}.300`, bg: `${stat.color}.50` }}
              userSelect="none"
            >
              <Text fontSize="3xl" fontWeight="bold" color={`${stat.color}.500`} lineHeight={1} mb={1}>
                {stat.value}
              </Text>
              <Text fontSize="xs" textTransform="uppercase" letterSpacing="wider" color={`${stat.color}.600`} fontWeight="semibold">
                {stat.label}
              </Text>
            </Box>
          ))}
        </SimpleGrid>

        {/* Empty state */}
        {groupedByProject.length === 0 && !loading && (
          <Box textAlign="center" py={16}>
            <Text fontSize="4xl" mb={3}>📋</Text>
            <Text fontWeight="semibold" color="gray.600" mb={1}>
              {activeFilter === "all" ? "Brak przypisanych prac" : "Brak prac w tej kategorii"}
            </Text>
            <Text fontSize="sm" color="gray.400">
              {activeFilter === "all"
                ? "Zostaniesz tu przypisany przez kierownika projektu"
                : "Zmien filtr aby zobaczyc inne prace"}
            </Text>
          </Box>
        )}

        {/* Sekcje z kartami prac */}
        {groupedByProject.map(group => {
          const isCollapsed = collapsedProjects.has(group.projectId);
          return (
            <Box key={group.projectId} mb={isCollapsed ? 2 : 6}>
              <HStack
                mb={isCollapsed ? 0 : 3}
                spacing={2}
                cursor="pointer"
                onClick={() => toggleProject(group.projectId)}
                userSelect="none"
                _hover={{ opacity: 0.8 }}
              >
                {isCollapsed ? <ChevronRight size={16} /> : <ChevronDown size={16} />}
                <FolderKanban size={16} />
                <Text fontWeight="bold" fontSize="md">{group.projectName}</Text>
                <Text fontSize="sm" color="gray.500">— {group.tenantName}</Text>
                <Badge colorScheme="gray" borderRadius="full">{group.works.length}</Badge>
              </HStack>

              {!isCollapsed && (
                <VStack spacing={2} align="stretch">
                  {group.works.map(work => (
                    <WorkItem
                      key={work.workId}
                      work={work}
                      stageName={work.stageName}
                      isExpanded={expandedWorks.has(work.workId)}
                      isMutating={mutating}
                      onToggleExpand={() => toggleWork(work.workId)}
                      onToggleWork={isClosed => handleToggleWork(work.workId, isClosed)}
                      onTogglePeriod={(periodId, isClosed) => handleTogglePeriod(work.workId, periodId, isClosed)}
                      onAddComment={content => handleAddComment(work.workId, content)}
                    />
                  ))}
                </VStack>
              )}
            </Box>
          );
        })}
      </Box>
    </MainLayout>
  );
}
