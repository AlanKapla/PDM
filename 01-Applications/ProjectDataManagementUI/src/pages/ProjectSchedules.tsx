import React, { useEffect, useState, useContext, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Icon,
  Button,
  Badge,
  IconButton,
  useColorModeValue,
  useBreakpointValue,
  useDisclosure,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Tooltip,
} from "@chakra-ui/react";
import { Calendar, Clock, User, Trash2 } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import WorkScheduleFormModal from "../components/WorkScheduleFormModal";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner, EmptyState } from "../components/common";
import DeleteAlertDialog from "../components/ui/DeleteAlertDialog";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import { projectApi, ResourceScope } from "../api/projectApi";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { ResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useProjectDetails, useProjectMembers } from "../hooks/queries";
import type { WorkScheduleSummaryWeb } from "../types/workSchedule.types";
import type { ProjectDetailsWeb, ProjectMemberWeb } from "../types/project.types";

interface TabCacheResult<T> {
  data: T | null;
  loading: boolean;
  fetch: () => Promise<void>;
  setData: (data: T) => void;
  clear: () => void;
}

interface ScheduleTabProps {
  cache: TabCacheResult<WorkScheduleSummaryWeb[]>;
  renderSchedulesList: (schedules: WorkScheduleSummaryWeb[], canDelete: boolean) => JSX.Element;
  onOpen: () => void;
  resourcePerms: ResourcePermissions;
  canDelete: boolean;
  description: string;
  showCreate: boolean;
}

const ScheduleTab = React.memo<ScheduleTabProps>(({ cache, renderSchedulesList, onOpen, canDelete, description, showCreate }) => {
  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie harmonogramów..." />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between" flexWrap="wrap" gap={2}>
        <Text fontSize="sm" color="neutral.600">{description}</Text>
        {showCreate && (
          <Button
            leftIcon={<Calendar size={18} />}
            colorScheme="primary"
            onClick={onOpen}
          >
            Utwórz harmonogram
          </Button>
        )}
      </HStack>
      {renderSchedulesList(cache.data || [], canDelete)}
    </VStack>
  );
});

export default function ProjectSchedules() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showError } = useToastNotification();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();
  const [scheduleToDelete, setScheduleToDelete] = useState<WorkScheduleSummaryWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const isMobile = useBreakpointValue({ base: true, md: false });

  const [loading, setLoading] = useState(true);
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const hasFetchedProjectData = useRef(false);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const resourcePerms = useResourcePermissions(projectId, "schedule");

  // Tab cache dla Moje harmonogramy
  const mySchedulesCache = useTabCache<WorkScheduleSummaryWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getWorkSchedules(user.activeTenantId, projectId, ResourceScope.Mine);
      return res.data;
    },
    `schedules-mine-${projectId}`
  );

  // Tab cache dla Wszystkie harmonogramy
  const allSchedulesCache = useTabCache<WorkScheduleSummaryWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getWorkSchedules(user.activeTenantId, projectId, ResourceScope.All);
      return res.data;
    },
    `schedules-all-${projectId}`
  );

  // React Query — dane projektu i członkowie (współdzielony cache między stronami projektu)
  const { data: projectData } = useProjectDetails(
    user?.activeTenantId ?? undefined,
    projectId
  );
  const project: ProjectDetailsWeb | null = projectData ?? null;

  const { data: membersData } = useProjectMembers(
    user?.activeTenantId ?? undefined,
    projectId
  );
  const members: ProjectMemberWeb[] = membersData ?? [];

  useEffect(() => {
    if (resourcePerms.raw.loading) return;
    if (hasFetchedProjectData.current) return;
    
    hasFetchedProjectData.current = true;
    fetchProjectData();
  }, [projectId, resourcePerms.raw.loading]);

  const fetchProjectData = async () => {
    if (!user?.activeTenantId || !projectId) return;
    
    if (!resourcePerms.tabs.showMine && !resourcePerms.tabs.showAll) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      // Pobierz wszystkie zakładki równolegle według uprawnień
      const fetchPromises = [];
      if (resourcePerms.tabs.showAll) {
        fetchPromises.push(allSchedulesCache.fetch());
      }
      if (resourcePerms.tabs.showMine) {
        fetchPromises.push(mySchedulesCache.fetch());
      }
      
      await Promise.all(fetchPromises);
    } catch (error) {
      showError("Nie udało się pobrać danych");
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteClick = (schedule: WorkScheduleSummaryWeb, e: React.MouseEvent) => {
    e.stopPropagation();
    setScheduleToDelete(schedule);
    onDeleteOpen();
  };

  const handleDeleteConfirm = async () => {
    if (!scheduleToDelete || !user?.activeTenantId || !projectId) return;
    setIsDeleting(true);
    try {
      await projectApi.deleteWorkSchedule(user.activeTenantId, projectId, scheduleToDelete.id);
      onDeleteClose();
      setScheduleToDelete(null);
      refreshData();
    } catch {
      showError("Nie udało się usunąć harmonogramu");
    } finally {
      setIsDeleting(false);
    }
  };

  const refreshData = () => {
    mySchedulesCache.clear();
    allSchedulesCache.clear();
    hasFetchedProjectData.current = false;
    fetchProjectData();
  };

  // Oblicz indeksy tabów - zapobiega niepotrzebnemu wywoływaniu useEffect
  const allSchedulesTabIndex = resourcePerms.tabs.showAll ? 0 : -1;
  const mySchedulesTabIndex = 
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 1 : 
    !resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 0 : -1;

  const renderSchedulesList = (schedules: WorkScheduleSummaryWeb[], canDelete: boolean): JSX.Element => {
    if (schedules.length === 0) {
      return (
        <EmptyState
          icon={Calendar}
          title="Brak harmonogramów"
          description="Nie znaleziono żadnych harmonogramów"
        />
      );
    }

    if (isMobile) {
      return (
        <VStack spacing={3} align="stretch">
          {schedules.map((schedule) => (
            <Box
              key={schedule.id}
              bg="white"
              p={3}
              borderWidth="1px"
              borderColor="neutral.200"
              rounded="lg"
              cursor="pointer"
              shadow="sm"
              onClick={() => navigate(`/projects/${projectId}/schedules/${schedule.id}`)}
            >
              <HStack justify="space-between" align="flex-start">
                <VStack align="flex-start" spacing={1} flex={1} minW={0}>
                  <HStack spacing={2} align="center" flexWrap="wrap">
                    <Text fontWeight="semibold" fontSize="sm" noOfLines={2}>
                      {schedule.name}
                    </Text>
                    {schedule.costEstimateId && (
                      <Badge colorScheme="orange" fontSize="xs" flexShrink={0}>Kosztorys</Badge>
                    )}
                  </HStack>
                  <HStack spacing={3} fontSize="xs" color="neutral.500">
                    <HStack spacing={1}>
                      <Icon as={User} boxSize={3} />
                      <Text noOfLines={1}>{schedule.createdByUserName}</Text>
                    </HStack>
                    <HStack spacing={1}>
                      <Icon as={Clock} boxSize={3} />
                      <Text>{formatDate(schedule.createdAt)}</Text>
                    </HStack>
                  </HStack>
                </VStack>
                {canDelete && (
                  <IconButton
                    aria-label="Usuń harmonogram"
                    icon={<Trash2 size={14} />}
                    size="xs"
                    colorScheme="red"
                    variant="ghost"
                    flexShrink={0}
                    onClick={(e) => handleDeleteClick(schedule, e)}
                  />
                )}
              </HStack>
            </Box>
          ))}
        </VStack>
      );
    }

    return (
      <Box overflowX="auto" bg="white" rounded="lg" borderWidth="1px" borderColor="neutral.200">
        <Table size="sm" variant="simple">
          <Thead>
            <Tr>
              <Th>Nazwa</Th>
              <Th>Autor</Th>
              <Th>Data utworzenia</Th>
              {canDelete && <Th textAlign="center">Akcje</Th>}
            </Tr>
          </Thead>
          <Tbody>
            {schedules.map((schedule) => (
              <Tr
                key={schedule.id}
                _hover={{ bg: 'neutral.50' }}
                cursor="pointer"
                onClick={() => navigate(`/projects/${projectId}/schedules/${schedule.id}`)}
              >
                <Td>
                  <HStack spacing={2}>
                    <Text fontWeight="medium">{schedule.name}</Text>
                    {schedule.costEstimateId && (
                      <Badge colorScheme="orange" fontSize="xs">Kosztorys</Badge>
                    )}
                  </HStack>
                </Td>
                <Td>
                  <HStack spacing={1}>
                    <Icon as={User} boxSize={3} color="neutral.500" />
                    <Text fontSize="sm">{schedule.createdByUserName}</Text>
                  </HStack>
                </Td>
                <Td>
                  <HStack spacing={1}>
                    <Icon as={Clock} boxSize={3} color="neutral.500" />
                    <Text fontSize="sm">{formatDate(schedule.createdAt)}</Text>
                  </HStack>
                </Td>
                {canDelete && (
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <Tooltip label="Usuń">
                      <IconButton
                        aria-label="Usuń harmonogram"
                        icon={<Trash2 size={14} />}
                        size="xs"
                        colorScheme="red"
                        variant="ghost"
                        onClick={(e) => handleDeleteClick(schedule, e)}
                      />
                    </Tooltip>
                  </Td>
                )}
              </Tr>
            ))}
          </Tbody>
        </Table>
      </Box>
    );
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie harmonogramów..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={Calendar} boxSize={8} color="level2.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Harmonogramy prac</Heading>
              {project && (
                <Text fontSize="sm" color="neutral.600" noOfLines={1}>
                  {project.name}
                </Text>
              )}
            </VStack>
          </HStack>
        </HStack>

        {(!resourcePerms.tabs.showMine && !resourcePerms.tabs.showAll) ? (
          <Box p={{ base: 3, md: 8 }} textAlign="center">
            <EmptyState
              icon={Calendar}
              title="Brak dostępu"
              description="Harmonogramy są dostępne tylko dla edytorów i administratorów projektu"
            />
          </Box>
        ) : (
          <Tabs colorScheme="level2" variant="enclosed" onChange={setActiveTabIndex} isLazy>
            <TabList>
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Calendar} boxSize={4} />
                    <Text>Wszystkie harmonogramy</Text>
                    <Badge colorScheme="level2">{allSchedulesCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Calendar} boxSize={4} />
                    <Text>Moje harmonogramy</Text>
                    <Badge colorScheme="primary">{mySchedulesCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showAll && (
                <TabPanel p={{ base: 2, md: 4 }}>
                  <ScheduleTab
                    cache={allSchedulesCache}
                    renderSchedulesList={renderSchedulesList}
                    onOpen={onOpen}
                    resourcePerms={resourcePerms}
                    canDelete={resourcePerms.all.canEdit}
                    description="Wszystkie harmonogramy w projekcie (admin)"
                    showCreate={resourcePerms.all.canCreate}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showMine && (
                <TabPanel p={{ base: 2, md: 4 }}>
                  <ScheduleTab
                    cache={mySchedulesCache}
                    renderSchedulesList={renderSchedulesList}
                    onOpen={onOpen}
                    resourcePerms={resourcePerms}
                    canDelete={resourcePerms.mine.canEdit}
                    description="Twoje harmonogramy w projekcie"
                    showCreate={resourcePerms.mine.canCreate}
                  />
                </TabPanel>
              )}
            </TabPanels>
          </Tabs>
        )}

        <WorkScheduleFormModal
          mode="create"
          isOpen={isOpen}
          onClose={onClose}
          projectId={projectId || ""}
          tenantId={user?.activeTenantId || ""}
          projectName={project?.name || ""}
          members={members}
          onSuccess={refreshData}
        />

        <DeleteAlertDialog
          isOpen={isDeleteOpen}
          onClose={onDeleteClose}
          onConfirm={handleDeleteConfirm}
          itemName={scheduleToDelete?.name}
          isLoading={isDeleting}
        />
      </Box>
    </MainLayout>
  );
}
