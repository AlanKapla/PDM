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
  useColorModeValue,
  useDisclosure,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  useMediaQuery,
} from "@chakra-ui/react";
import { Calendar, Clock, User } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import WorkScheduleFormModal from "../components/WorkScheduleFormModal";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import { projectApi, ResourceScope } from "../api/projectApi";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { ResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useGlobalCache } from "../hooks/useGlobalCache";
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
  renderSchedulesList: (schedules: WorkScheduleSummaryWeb[]) => JSX.Element;
  onOpen: () => void;
  resourcePerms: ResourcePermissions;
  isMobile: boolean;
}

// Komponent dla tabu "Moje harmonogramy"
const MySchedulesTab = React.memo<ScheduleTabProps>(({ cache, renderSchedulesList, onOpen, resourcePerms, isMobile }) => {
  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie harmonogramów..." />;
  }

  return (
    <VStack spacing={isMobile ? 3 : 4} align="stretch">
      <HStack justify="space-between" flexWrap="wrap" gap={2}>
        <Text fontSize={isMobile ? "xs" : "sm"} color="gray.600">
          Twoje harmonogramy w projekcie
        </Text>
        {resourcePerms.mine.canCreate && (
          <Button
            leftIcon={<Calendar size={18} />}
            colorScheme="purple"
            onClick={onOpen}
            size={isMobile ? "sm" : "md"}
            whiteSpace="normal"
            height="auto"
            py={2}
          >
            Utwórz harmonogram
          </Button>
        )}
      </HStack>
      {renderSchedulesList(cache.data || [])}
    </VStack>
  );
});

// Komponent dla tabu "Wszystkie harmonogramy"
const AllSchedulesTab = React.memo<ScheduleTabProps>(({ cache, renderSchedulesList, onOpen, resourcePerms, isMobile }) => {
  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie harmonogramów..." />;
  }

  return (
    <VStack spacing={isMobile ? 3 : 4} align="stretch">
      <HStack justify="space-between" flexWrap="wrap" gap={2}>
        <Text fontSize={isMobile ? "xs" : "sm"} color="gray.600">
          Wszystkie harmonogramy w projekcie (admin)
        </Text>
        {resourcePerms.all.canCreate && (
          <Button
            leftIcon={<Calendar size={18} />}
            colorScheme="purple"
            onClick={onOpen}
            size={isMobile ? "sm" : "md"}
            whiteSpace="normal"
            height="auto"
            py={2}
          >
            Utwórz harmonogram
          </Button>
        )}
      </HStack>
      {renderSchedulesList(cache.data || [])}
    </VStack>
  );
});

export default function ProjectSchedules() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showError } = useToastNotification();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [isMobile] = useMediaQuery("(max-width: 768px)");

  const [loading, setLoading] = useState(true);
  const [project, setProject] = useState<ProjectDetailsWeb | null>(null);
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const hasFetchedProjectData = useRef(false);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const resourcePerms = useResourcePermissions(projectId);

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

  // Globalny cache dla project details (współdzielony między stronami projektu)
  const projectDetailsCache = useGlobalCache(
    `project-details-${projectId}`,
    async () => {
      if (!user?.activeTenantId || !projectId) throw new Error('Missing tenant or project ID');
      const res = await projectApi.getProjectDetails(user.activeTenantId, projectId);
      return res.data;
    }
  );

  // Globalny cache dla członków projektu
  const membersCache = useGlobalCache(
    `project-members-${projectId}`,
    async () => {
      if (!user?.activeTenantId || !projectId) throw new Error('Missing tenant or project ID');
      const res = await projectApi.getProjectMembers(user.activeTenantId, projectId);
      return res.data;
    }
  );

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
      const projectData = await projectDetailsCache.fetch();
      setProject(projectData);

      // Pobierz członków projektu
      const membersData = await membersCache.fetch();
      setMembers(membersData);

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

  const refreshData = () => {
    mySchedulesCache.clear();
    allSchedulesCache.clear();
    projectDetailsCache.clear();
    membersCache.clear();
    hasFetchedProjectData.current = false;
    fetchProjectData();
  };

  // Oblicz indeksy tabów - zapobiega niepotrzebnemu wywoływaniu useEffect
  const allSchedulesTabIndex = resourcePerms.tabs.showAll ? 0 : -1;
  const mySchedulesTabIndex = 
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 1 : 
    !resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 0 : -1;

  const renderSchedulesList = (schedules: WorkScheduleSummaryWeb[]) => {
    if (schedules.length === 0) {
      return (
        <EmptyState
          icon={Calendar}
          title="Brak harmonogramów"
          description="Nie znaleziono żadnych harmonogramów"
        />
      );
    }

    return (
      <VStack spacing={isMobile ? 2 : 4} align="stretch">
        {schedules.map((schedule) => (
          <Box
            key={schedule.id}
            bg={cardBg}
            p={isMobile ? 3 : 6}
            borderWidth="1px"
            borderColor={borderColor}
            rounded="lg"
            _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
            transition="all 0.2s"
            cursor="pointer"
            onClick={() => navigate(`/projects/${projectId}/schedules/${schedule.id}`)}
            shadow="sm"
          >
            <VStack align="flex-start" spacing={isMobile ? 1.5 : 3}>
              <Text fontWeight="bold" fontSize={isMobile ? "md" : "xl"} noOfLines={2}>
                {schedule.name}
              </Text>
              <HStack spacing={isMobile ? 3 : 6} fontSize={isMobile ? "9px" : "sm"} color="gray.600" flexWrap="wrap">
                <HStack spacing={1} minW="0">
                  <Icon as={User} boxSize={isMobile ? 3 : 4} flexShrink={0} />
                  <Text noOfLines={1} fontSize={isMobile ? "9px" : "sm"}>
                    {schedule.createdByUserName}
                  </Text>
                </HStack>
                <HStack spacing={1}>
                  <Icon as={Clock} boxSize={isMobile ? 3 : 4} flexShrink={0} />
                  <Text fontSize={isMobile ? "9px" : "sm"}>{formatDate(schedule.createdAt)}</Text>
                </HStack>
              </HStack>
            </VStack>
          </Box>
        ))}
      </VStack>
    );
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={isMobile ? 2 : 10} minH="100vh">
          <LoadingSpinner message="Ładowanie harmonogramów..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={isMobile ? 2 : 10} minH="100vh">
        <HStack 
          justify="space-between" 
          mb={isMobile ? 4 : 8} 
          flexDirection={isMobile ? "column" : "row"}
          align={isMobile ? "flex-start" : "center"}
          spacing={isMobile ? 2 : 4}
          width="100%"
        >
          <HStack spacing={isMobile ? 2 : 3} align="flex-start">
            <Icon as={Calendar} boxSize={isMobile ? 6 : 8} color="purple.600" flexShrink={0} />
            <VStack align="flex-start" spacing={isMobile ? 0.5 : 1}>
              <Heading size={isMobile ? "md" : "lg"}>Harmonogramy prac</Heading>
              {project && (
                <Text fontSize={isMobile ? "9px" : "sm"} color="gray.600" noOfLines={1}>
                  {project.name}
                </Text>
              )}
            </VStack>
          </HStack>
        </HStack>

        {(!resourcePerms.tabs.showMine && !resourcePerms.tabs.showAll) ? (
          <Box p={isMobile ? 2 : 8} textAlign="center">
            <EmptyState
              icon={Calendar}
              title="Brak dostępu"
              description="Harmonogramy są dostępne tylko dla edytorów i administratorów projektu"
            />
          </Box>
        ) : (
          <Tabs colorScheme="purple" variant="enclosed" onChange={setActiveTabIndex} isLazy>
            <TabList overflowX={isMobile ? "auto" : "visible"} pb={isMobile ? 2 : 0}>
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold" fontSize={isMobile ? "xs" : "md"} px={isMobile ? 2 : 4} py={isMobile ? 2 : 4}>
                  <HStack spacing={isMobile ? 1 : 2} minW="0">
                    <Icon as={Calendar} boxSize={isMobile ? 3 : 4} flexShrink={0} />
                    <Text whiteSpace="nowrap" fontSize={isMobile ? "xs" : "md"}>
                      {isMobile ? "Wszystkie" : "Wszystkie harmonogramy"}
                    </Text>
                    <Badge colorScheme="purple" fontSize={isMobile ? "7px" : "xs"}>
                      {allSchedulesCache.data?.length || 0}
                    </Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold" fontSize={isMobile ? "xs" : "md"} px={isMobile ? 2 : 4} py={isMobile ? 2 : 4}>
                  <HStack spacing={isMobile ? 1 : 2} minW="0">
                    <Icon as={Calendar} boxSize={isMobile ? 3 : 4} flexShrink={0} />
                    <Text whiteSpace="nowrap" fontSize={isMobile ? "xs" : "md"}>
                      {isMobile ? "Moje" : "Moje harmonogramy"}
                    </Text>
                    <Badge colorScheme="blue" fontSize={isMobile ? "7px" : "xs"}>
                      {mySchedulesCache.data?.length || 0}
                    </Badge>
                  </HStack>
                </Tab>
              )}
            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showAll && (
                <TabPanel p={isMobile ? 2 : 4}>
                  <AllSchedulesTab
                    cache={allSchedulesCache}
                    renderSchedulesList={renderSchedulesList}
                    onOpen={onOpen}
                    resourcePerms={resourcePerms}
                    isMobile={isMobile}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showMine && (
                <TabPanel p={isMobile ? 2 : 4}>
                  <MySchedulesTab
                    cache={mySchedulesCache}
                    renderSchedulesList={renderSchedulesList}
                    onOpen={onOpen}
                    resourcePerms={resourcePerms}
                    isMobile={isMobile}
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
      </Box>
    </MainLayout>
  );
}
