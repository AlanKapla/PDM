import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Badge,
  Icon,
  Button,
  useColorModeValue,
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
  IconButton,
  useDisclosure,
} from "@chakra-ui/react";
import { ArrowLeft, Eye, Trash2, Plus, FileText, Copy } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { AuthContext } from "../context/AuthContext";
import { useContext } from "react";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { projectApi, ResourceScope } from "../api/projectApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { formatDate } from "../utils/formatters";
import CreateCostEstimateModal from "../components/CreateCostEstimateModal";
import CopyCostEstimateModal from "../components/CopyCostEstimateModal";
import type { CostEstimateListItem, CostEstimateStatus } from "../types/costEstimate.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";

const costEstimateStatusLabels: Record<CostEstimateStatus, string> = {
  [0]: "Szkic",
  [1]: "W trakcie",
  [2]: "Do przeglądu",
  [3]: "Zatwierdzony",
  [4]: "Odrzucony",
  [5]: "Zarchiwizowany",
};

const costEstimateStatusColors: Record<CostEstimateStatus, string> = {
  [0]: "gray",
  [1]: "blue",
  [2]: "orange",
  [3]: "green",
  [4]: "red",
  [5]: "purple",
};

export default function ProjectCosts() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showError, showSuccess } = useToastNotification();

  console.log("=== ProjectCosts RENDER ===");
  console.log("projectId:", projectId);
  console.log("user:", user);
  console.log("user?.activeTenantId:", user?.activeTenantId);

  const [loading, setLoading] = useState(true);
  const [project, setProject] = useState<any | null>(null);
  const [costEstimateToCopy, setCostEstimateToCopy] = useState<CostEstimateListItem | null>(null);
  const [activeTabIndex, setActiveTabIndex] = useState(0);

  const { isOpen: isCreateModalOpen, onOpen: onCreateModalOpen, onClose: onCreateModalClose } = useDisclosure();
  const { isOpen: isCopyModalOpen, onOpen: onCopyModalOpen, onClose: onCopyModalClose } = useDisclosure();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const resourcePerms = useResourcePermissions(projectId);

  // Tab cache dla Moje kosztorysy
  const myCostEstimatesCache = useTabCache<CostEstimateListItem[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      return await costEstimateApi.getCostEstimatesByScope(
        user.activeTenantId,
        projectId,
        ResourceScope.Mine
      );
    },
    `cost-estimates-mine-${projectId}`
  );

  // Tab cache dla Wszystkie kosztorysy
  const allCostEstimatesCache = useTabCache<CostEstimateListItem[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      return await costEstimateApi.getCostEstimatesByScope(
        user.activeTenantId,
        projectId,
        ResourceScope.All
      );
    },
    `cost-estimates-all-${projectId}`
  );

  useEffect(() => {
    fetchProjectData();
  }, [projectId]);

  const fetchProjectData = async () => {
    if (!user?.activeTenantId || !projectId) return;
    
    if (!resourcePerms.tabs.showMine && !resourcePerms.tabs.showAll) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const projectResponse = await projectApi.getProjectDetails(user.activeTenantId, projectId);
      setProject(projectResponse.data);
    } catch (error: any) {
      console.error('Error fetching data:', error);
      showError('Nie udało się załadować danych', error?.message || 'Wystąpił nieoczekiwany błąd');
    } finally {
      setLoading(false);
    }
  };

  const refreshData = () => {
    myCostEstimatesCache.clear();
    allCostEstimatesCache.clear();
    fetchProjectData();
  };

  const handleDeleteCostEstimate = async (costEstimateId: string) => {
    if (!user?.activeTenantId || !projectId) return;
    if (!confirm("Czy na pewno chcesz usunąć ten kosztorys?")) return;

    try {
      await costEstimateApi.deleteCostEstimate(user.activeTenantId, projectId, costEstimateId);
      showSuccess("Kosztorys został usunięty");
      refreshData();
    } catch (error: any) {
      console.error('Error deleting cost estimate:', error);
      showError('Nie udało się usunąć kosztorysu', error?.message || 'Wystąpił nieoczekiwany błąd');
    }
  };

  const handleViewCostEstimate = (costEstimateId: string) => {
    navigate(`/projects/${projectId}/cost-estimates/${costEstimateId}`);
  };

  const handleCopyCostEstimate = (costEstimate: CostEstimateListItem) => {
    setCostEstimateToCopy(costEstimate);
    onCopyModalOpen();
  };

  const handleCopySuccess = () => {
    // Opcjonalnie możesz odświeżyć listę kosztorysów
    // fetchData();
  };



  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie kosztów..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={FileText} boxSize={8} color="blue.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Kosztorysy projektowe</Heading>
              {project && <Text fontSize="sm" color="gray.600">{project.name}</Text>}
            </VStack>
          </HStack>
          {resourcePerms.mine.canCreate && (
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="blue"
              onClick={onCreateModalOpen}
            >
              Nowy kosztorys
            </Button>
          )}
        </HStack>

        {(!resourcePerms.tabs.showMine && !resourcePerms.tabs.showAll) ? (
          <Box p={8} textAlign="center">
            <EmptyState
              icon={FileText}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania kosztorysów w tym projekcie"
            />
          </Box>
        ) : (
          <Tabs colorScheme="blue" variant="enclosed" onChange={setActiveTabIndex}>
            <TabList>
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} />
                    <Text>Moje kosztorysy</Text>
                    <Badge colorScheme="blue" ml={2}>{myCostEstimatesCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} />
                    <Text>Wszystkie kosztorysy</Text>
                    <Badge colorScheme="green" ml={2}>{allCostEstimatesCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showMine && (
                <TabPanel>
                  <MyCostEstimatesTab
                    cache={myCostEstimatesCache}
                    isActive={activeTabIndex === 0}
                    cardBg={cardBg}
                    borderColor={borderColor}
                    hoverBg={hoverBg}
                    costEstimateStatusLabels={costEstimateStatusLabels}
                    costEstimateStatusColors={costEstimateStatusColors}
                    formatDate={formatDate}
                    handleViewCostEstimate={handleViewCostEstimate}
                    handleCopyCostEstimate={handleCopyCostEstimate}
                    handleDeleteCostEstimate={handleDeleteCostEstimate}
                    resourcePerms={resourcePerms}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showAll && (
                <TabPanel>
                  <AllCostEstimatesTab
                    cache={allCostEstimatesCache}
                    isActive={resourcePerms.tabs.showMine ? activeTabIndex === 1 : activeTabIndex === 0}
                    cardBg={cardBg}
                    borderColor={borderColor}
                    hoverBg={hoverBg}
                    costEstimateStatusLabels={costEstimateStatusLabels}
                    costEstimateStatusColors={costEstimateStatusColors}
                    formatDate={formatDate}
                    handleViewCostEstimate={handleViewCostEstimate}
                    handleCopyCostEstimate={handleCopyCostEstimate}
                    handleDeleteCostEstimate={handleDeleteCostEstimate}
                    resourcePerms={resourcePerms}
                  />
                </TabPanel>
              )}
            </TabPanels>
          </Tabs>
        )}

        <Box mt={6} p={4} bg="blue.50" rounded="md" borderWidth="1px" borderColor="blue.200">
          <Text fontSize="sm" color="blue.800">
            💡 <strong>Wskazówka:</strong> Kosztorysy to zaawansowane narzędzie do zarządzania kosztami projektu według zdefiniowanych szablonów. W przyszłości zostanie dodana funkcja udostępniania kosztorysów innym członkom zespołu.
          </Text>
        </Box>

        {/* MODAL: CREATE COST ESTIMATE */}
        {user?.activeTenantId && projectId && (
          <CreateCostEstimateModal
            isOpen={isCreateModalOpen}
            onClose={onCreateModalClose}
            tenantId={user.activeTenantId}
            projectId={projectId}
            onCostEstimateCreated={refreshData}
          />
        )}

        {/* MODAL: COPY COST ESTIMATE */}
        {costEstimateToCopy && projectId && (
          <CopyCostEstimateModal
            isOpen={isCopyModalOpen}
            onClose={onCopyModalClose}
            costEstimateId={costEstimateToCopy.id}
            costEstimateName={costEstimateToCopy.name}
            currentProjectId={projectId}
            onSuccess={handleCopySuccess}
          />
        )}
      </Box>
    </MainLayout>
  );
}

// Komponent dla tabu "Moje kosztorysy" z lazy loading
function MyCostEstimatesTab({ cache, isActive, cardBg, borderColor, hoverBg, costEstimateStatusLabels, costEstimateStatusColors, formatDate, handleViewCostEstimate, handleCopyCostEstimate, handleDeleteCostEstimate, resourcePerms }: any) {
  useEffect(() => {
    if (isActive && !cache.data) {
      cache.fetch();
    }
  }, [isActive]);

  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie kosztorysów..." />;
  }

  const costEstimates = cache.data || [];

  if (costEstimates.length === 0) {
    return (
      <EmptyState
        icon={FileText}
        title="Brak kosztorysów"
        description="Utwórz swój pierwszy kosztorys na podstawie szablonu"
      />
    );
  }

  return (
    <Box overflowX="auto" bg={cardBg} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Nazwa</Th>
            <Th>Szablon</Th>
            <Th>Status</Th>
            <Th isNumeric>Wartość netto</Th>
            <Th isNumeric>Wartość brutto</Th>
            <Th>Utworzony</Th>
            <Th>Aktualizacja</Th>
            <Th textAlign="center">Akcje</Th>
          </Tr>
        </Thead>
        <Tbody>
          {costEstimates.map((costEstimate: any) => (
            <Tr key={costEstimate.id} _hover={{ bg: hoverBg }}>
              <Td fontWeight="medium">
                <VStack align="flex-start" spacing={0}>
                  <Text>{costEstimate.name}</Text>
                  {costEstimate.description && (
                    <Text fontSize="xs" color="gray.500" noOfLines={1}>
                      {costEstimate.description}
                    </Text>
                  )}
                </VStack>
              </Td>
              <Td>
                <Text fontSize="sm">{costEstimate.templateName}</Text>
              </Td>
              <Td>
                <Badge colorScheme={costEstimateStatusColors[costEstimate.status]}>
                  {costEstimateStatusLabels[costEstimate.status]}
                </Badge>
              </Td>
              <Td isNumeric>
                {costEstimate.totalNet ? `${costEstimate.totalNet.toFixed(2)} PLN` : '-'}
              </Td>
              <Td isNumeric fontWeight="bold" color="green.600">
                {costEstimate.totalGross ? `${costEstimate.totalGross.toFixed(2)} PLN` : '-'}
              </Td>
              <Td>
                <Text fontSize="xs">{formatDate(costEstimate.createdAt)}</Text>
              </Td>
              <Td>
                <Text fontSize="xs">
                  {costEstimate.updatedAt ? formatDate(costEstimate.updatedAt) : '-'}
                </Text>
              </Td>
              <Td textAlign="center">
                <HStack spacing={1} justify="center">
                  <IconButton
                    aria-label="Otwórz"
                    icon={<Eye size={14} />}
                    size="xs"
                    colorScheme="blue"
                    variant="ghost"
                    onClick={() => handleViewCostEstimate(costEstimate.id)}
                  />
                  <IconButton
                    aria-label="Kopiuj"
                    icon={<Copy size={14} />}
                    size="xs"
                    colorScheme="purple"
                    variant="ghost"
                    onClick={() => handleCopyCostEstimate(costEstimate)}
                  />
                  {resourcePerms.mine.canDelete && (
                    <IconButton
                      aria-label="Usuń"
                      icon={<Trash2 size={14} />}
                      size="xs"
                      colorScheme="red"
                      variant="ghost"
                      onClick={() => handleDeleteCostEstimate(costEstimate.id)}
                    />
                  )}
                </HStack>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
}

// Komponent dla tabu "Wszystkie kosztorysy" z lazy loading
function AllCostEstimatesTab({ cache, isActive, cardBg, borderColor, hoverBg, costEstimateStatusLabels, costEstimateStatusColors, formatDate, handleViewCostEstimate, handleCopyCostEstimate, handleDeleteCostEstimate, resourcePerms }: any) {
  useEffect(() => {
    if (isActive && !cache.data) {
      cache.fetch();
    }
  }, [isActive]);

  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie kosztorysów..." />;
  }

  const costEstimates = cache.data || [];

  if (costEstimates.length === 0) {
    return (
      <EmptyState
        icon={FileText}
        title="Brak kosztorysów"
        description="Nie znaleziono żadnych kosztorysów"
      />
    );
  }

  return (
    <Box overflowX="auto" bg={cardBg} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Nazwa</Th>
            <Th>Szablon</Th>
            <Th>Status</Th>
            <Th isNumeric>Wartość netto</Th>
            <Th isNumeric>Wartość brutto</Th>
            <Th>Utworzony</Th>
            <Th>Aktualizacja</Th>
            <Th textAlign="center">Akcje</Th>
          </Tr>
        </Thead>
        <Tbody>
          {costEstimates.map((costEstimate: any) => (
            <Tr key={costEstimate.id} _hover={{ bg: hoverBg }}>
              <Td fontWeight="medium">
                <VStack align="flex-start" spacing={0}>
                  <Text>{costEstimate.name}</Text>
                  {costEstimate.description && (
                    <Text fontSize="xs" color="gray.500" noOfLines={1}>
                      {costEstimate.description}
                    </Text>
                  )}
                </VStack>
              </Td>
              <Td>
                <Text fontSize="sm">{costEstimate.templateName}</Text>
              </Td>
              <Td>
                <Badge colorScheme={costEstimateStatusColors[costEstimate.status]}>
                  {costEstimateStatusLabels[costEstimate.status]}
                </Badge>
              </Td>
              <Td isNumeric>
                {costEstimate.totalNet ? `${costEstimate.totalNet.toFixed(2)} PLN` : '-'}
              </Td>
              <Td isNumeric fontWeight="bold" color="green.600">
                {costEstimate.totalGross ? `${costEstimate.totalGross.toFixed(2)} PLN` : '-'}
              </Td>
              <Td>
                <Text fontSize="xs">{formatDate(costEstimate.createdAt)}</Text>
              </Td>
              <Td>
                <Text fontSize="xs">
                  {costEstimate.updatedAt ? formatDate(costEstimate.updatedAt) : '-'}
                </Text>
              </Td>
              <Td textAlign="center">
                <HStack spacing={1} justify="center">
                  <IconButton
                    aria-label="Otwórz"
                    icon={<Eye size={14} />}
                    size="xs"
                    colorScheme="blue"
                    variant="ghost"
                    onClick={() => handleViewCostEstimate(costEstimate.id)}
                  />
                  <IconButton
                    aria-label="Kopiuj"
                    icon={<Copy size={14} />}
                    size="xs"
                    colorScheme="purple"
                    variant="ghost"
                    onClick={() => handleCopyCostEstimate(costEstimate)}
                  />
                  {resourcePerms.all.canDelete && (
                    <IconButton
                      aria-label="Usuń"
                      icon={<Trash2 size={14} />}
                      size="xs"
                      colorScheme="red"
                      variant="ghost"
                      onClick={() => handleDeleteCostEstimate(costEstimate.id)}
                    />
                  )}
                </HStack>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
}
