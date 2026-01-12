import { useEffect, useState, useContext, useRef } from "react";
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
} from "@chakra-ui/react";
import { Calendar, Clock, User, BarChart3 } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import WorkScheduleFormModal from "../components/WorkScheduleFormModal";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import { projectApi, ResourceScope } from "../api/projectApi";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useGlobalCache } from "../hooks/useGlobalCache";
import type { WorkScheduleSummaryWeb } from "../types/workSchedule.types";

export default function ProjectSchedules() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showError, showSuccess } = useToastNotification();
  const { isOpen, onOpen, onClose } = useDisclosure();

  const [loading, setLoading] = useState(true);
  const [project, setProject] = useState<any | null>(null);
  const [members, setMembers] = useState<any[]>([]);
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const [analyzingScheduleId, setAnalyzingScheduleId] = useState<string | null>(null);
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
  const handleAnalyzeSchedule = async (scheduleId: string, event: React.MouseEvent) => {
    event.stopPropagation(); // Zapobiega nawigacji do szczegółów
    
    if (!user?.activeTenantId || !projectId) return;

    setAnalyzingScheduleId(scheduleId);
    try {
      const response = await projectApi.analyzeWorkSchedule(user.activeTenantId, projectId, scheduleId);
      
      // Wyświetl wynik analizy
      const analysis = response.data;
      showSuccess(`Analiza zakończona pomyślnie`);
      
      // TODO: Można wyświetlić szczegóły analizy w modalu lub toaście
      console.log('Wynik analizy:', analysis);
      
    } catch (error) {
      console.error('Błąd analizy harmonogramu:', error);
      showError('Nie udało się przeanalizować harmonogramu');
    } finally {
      setAnalyzingScheduleId(null);
    }
  };
  // Oblicz indeksy tabów - zapobiega niepotrzebnemu wywoływaniu useEffect
  const allSchedulesTabIndex = resourcePerms.tabs.showAll ? 0 : -1;
  const mySchedulesTabIndex = 
    resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 1 : 
    !resourcePerms.tabs.showAll && resourcePerms.tabs.showMine ? 0 : -1;

  const renderSchedulesList = (schedules: WorkScheduleSummaryWeb[], showAnalyzeButton: boolean = false) => {
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
      <VStack spacing={4} align="stretch">
        {schedules.map((schedule) => (
          <Box
            key={schedule.id}
            bg={cardBg}
            p={6}
            borderWidth="1px"
            borderColor={borderColor}
            rounded="lg"
            _hover={{ bg: hoverBg, transform: "translateY(-2px)", shadow: "md" }}
            transition="all 0.2s"
            shadow="sm"
          >
            <HStack justify="space-between" align="flex-start">
              <VStack 
                align="flex-start" 
                spacing={3} 
                flex={1}
                cursor="pointer"
                onClick={() => navigate(`/projects/${projectId}/schedules/${schedule.id}`)}
              >
                <Text fontWeight="bold" fontSize="xl">{schedule.name}</Text>
                <HStack spacing={6} fontSize="sm" color="gray.600">
                  <HStack spacing={2}>
                    <Icon as={User} boxSize={4} />
                    <Text>{schedule.createdByUserName}</Text>
                  </HStack>
                  <HStack spacing={2}>
                    <Icon as={Clock} boxSize={4} />
                    <Text>{formatDate(schedule.createdAt)}</Text>
                  </HStack>
                </HStack>
              </VStack>
              {showAnalyzeButton && resourcePerms.mine.canEdit && (
                <Button
                  leftIcon={<BarChart3 size={18} />}
                  colorScheme="purple"
                  size="md"
                  onClick={(e) => handleAnalyzeSchedule(schedule.id, e)}
                  isLoading={analyzingScheduleId === schedule.id}
                  loadingText="Analizowanie..."
                >
                  Analizuj
                </Button>
              )}
            </HStack>
          </Box>
        ))}
      </VStack>
    );
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie harmonogramów..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={Calendar} boxSize={8} color="purple.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Harmonogramy prac</Heading>
              {project && <Text fontSize="sm" color="gray.600">{project.name}</Text>}
            </VStack>
          </HStack>
        </HStack>

        {(!resourcePerms.tabs.showMine && !resourcePerms.tabs.showAll) ? (
          <Box p={8} textAlign="center">
            <EmptyState
              icon={Calendar}
              title="Brak dostępu"
              description="Harmonogramy są dostępne tylko dla edytorów i administratorów projektu"
            />
          </Box>
        ) : (
          <Tabs colorScheme="purple" variant="enclosed" onChange={setActiveTabIndex}>
            <TabList>
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Calendar} boxSize={4} />
                    <Text>Wszystkie harmonogramy</Text>
                    <Badge colorScheme="purple" ml={2}>{allSchedulesCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Calendar} boxSize={4} />
                    <Text>Moje harmonogramy</Text>
                    <Badge colorScheme="blue" ml={2}>{mySchedulesCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showAll && (
                <TabPanel>
                  <AllSchedulesTab
                    cache={allSchedulesCache}
                    renderSchedulesList={renderSchedulesList}
                    onOpen={onOpen}
                    resourcePerms={resourcePerms}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showMine && (
                <TabPanel>
                  <MySchedulesTab
                    cache={mySchedulesCache}
                    renderSchedulesList={renderSchedulesList}
                    onOpen={onOpen}
                    resourcePerms={resourcePerms}
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

// Komponent dla tabu "Moje harmonogramy"
function MySchedulesTab({ cache, renderSchedulesList, onOpen, resourcePerms }: any) {
  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie harmonogramów..." />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between">
        <Text fontSize="sm" color="gray.600">
          Twoje harmonogramy w projekcie
        </Text>
        {resourcePerms.mine.canCreate && (
          <Button
            leftIcon={<Calendar size={18} />}
            colorScheme="purple"
            onClick={onOpen}
          >
            Utwórz harmonogram
          </Button>
        )}
      </HStack>
      {renderSchedulesList(cache.data || [], true)}
    </VStack>
  );
}

// Komponent dla tabu "Wszystkie harmonogramy"
function AllSchedulesTab({ cache, renderSchedulesList, onOpen, resourcePerms }: any) {
  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie harmonogramów..." />;
  }

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between">
        <Text fontSize="sm" color="gray.600">
          Wszystkie harmonogramy w projekcie (admin)
        </Text>
        {resourcePerms.all.canCreate && (
          <Button
            leftIcon={<Calendar size={18} />}
            colorScheme="purple"
            onClick={onOpen}
          >
            Utwórz harmonogram
          </Button>
        )}
      </HStack>
      {renderSchedulesList(cache.data || [])}
    </VStack>
  );
}
