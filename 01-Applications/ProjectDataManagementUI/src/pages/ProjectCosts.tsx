import React, { useEffect, useState, useRef } from "react";
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
  Tooltip,
} from "@chakra-ui/react";
import { Eye, Trash2, Plus, FileText, Copy, Share2, Users } from "lucide-react";
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
import ShareCostEstimateModal from "../components/ShareCostEstimateModal";
import type { CostEstimateListItemWeb, CostEstimateShareWeb } from "../types/costEstimate.types.new";
import type { CostEstimateStatus } from "../types/costEstimate.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { ResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useGlobalCache } from "../hooks/useGlobalCache";

/** Formatuje kwotę z separatorami tysięcy (spacjami) */
const formatCurrency = (value: number | null | undefined): string => {
  if (value == null) return '-';
  return `${value.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} PLN`;
};

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

interface TabCacheResult<T> {
  data: T | null;
  loading: boolean;
  fetch: () => Promise<void>;
  setData: (data: T) => void;
  clear: () => void;
}

interface CostEstimatesTabProps {
  cache: TabCacheResult<CostEstimateListItemWeb[]>;
  cardBg: string;
  borderColor: string;
  hoverBg: string;
  costEstimateStatusLabels: Record<CostEstimateStatus, string>;
  costEstimateStatusColors: Record<CostEstimateStatus, string>;
  formatDate: (date: string | Date | null | undefined) => string;
  handleViewCostEstimate: (id: string) => void;
  handleCopyCostEstimate: (costEstimate: CostEstimateListItemWeb) => void;
  handleDeleteCostEstimate: (id: string) => void;
  handleShareCostEstimate: (costEstimate: CostEstimateListItemWeb) => void;
  resourcePerms: ResourcePermissions;
  onCreateModalOpen: () => void;
  /** Czy wyświetlać kolumnę Właściciel (zakładka Wszystkie i Udostępnione) */
  showOwnerColumn?: boolean;
  /** Czy pokazywać przycisk Udostępnij */
  canShare?: boolean;
  /** Czy pokazywać przycisk Kopiuj */
  canCopy?: boolean;
  /** Czy pokazywać przycisk Usuń */
  canDelete?: boolean;
}

// Współdzielona tabela kosztorysów używana przez wszystkie trzy zakładki
const CostEstimatesTable = React.memo<CostEstimatesTabProps>(({
  cache,
  cardBg,
  borderColor,
  hoverBg,
  costEstimateStatusLabels,
  costEstimateStatusColors,
  formatDate,
  handleViewCostEstimate,
  handleCopyCostEstimate,
  handleDeleteCostEstimate,
  handleShareCostEstimate,
  onCreateModalOpen,
  showOwnerColumn = false,
  canShare = false,
  canCopy = false,
  canDelete = false,
}) => {
  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie kosztorysów..." />;
  }

  const costEstimates = cache.data || [];

  return (
    <VStack spacing={4} align="stretch">
      {costEstimates.length === 0 ? (
        <EmptyState
          icon={FileText}
          title="Brak kosztorysów"
          description="Brak kosztorysów do wyświetlenia"
        />
      ) : (
        <Box overflowX="auto" bg={cardBg} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                {showOwnerColumn && <Th>Właściciel</Th>}
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
              {costEstimates.map((costEstimate) => (
                <Tr key={costEstimate.id} _hover={{ bg: hoverBg }}>
                  <Td fontWeight="medium">
                    <VStack align="flex-start" spacing={0}>
                      <HStack spacing={2}>
                        <Text>{costEstimate.name}</Text>
                        {costEstimate.isSharedByMe && (
                          <Tooltip label={`Udostępniono ${costEstimate.sharedWithUsers?.length ?? 0} osobom`}>
                            <Badge colorScheme="purple" fontSize="2xs" display="flex" alignItems="center" gap={1}>
                              <Users size={10} />
                              {costEstimate.sharedWithUsers?.length ?? 0}
                            </Badge>
                          </Tooltip>
                        )}
                        {costEstimate.isSharedWithMe && (
                          <Badge colorScheme="teal" fontSize="2xs">Udostępniony</Badge>
                        )}
                      </HStack>
                      {costEstimate.description && (
                        <Text fontSize="xs" color="gray.500" noOfLines={1}>
                          {costEstimate.description}
                        </Text>
                      )}
                    </VStack>
                  </Td>
                  {showOwnerColumn && (
                    <Td>
                      <Text fontSize="xs">{costEstimate.ownerName}</Text>
                    </Td>
                  )}
                  <Td>
                    <Text fontSize="sm">{costEstimate.templateName}</Text>
                  </Td>
                  <Td>
                    <Badge colorScheme={costEstimateStatusColors[costEstimate.status]}>
                      {costEstimateStatusLabels[costEstimate.status]}
                    </Badge>
                  </Td>
                  <Td isNumeric>
                    {formatCurrency(costEstimate.totalNet)}
                  </Td>
                  <Td isNumeric fontWeight="bold" color="green.600">
                    {formatCurrency(costEstimate.totalGross)}
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
                      <Tooltip label="Otwórz">
                        <IconButton
                          aria-label="Otwórz"
                          icon={<Eye size={14} />}
                          size="xs"
                          colorScheme="blue"
                          variant="ghost"
                          onClick={() => handleViewCostEstimate(costEstimate.id)}
                        />
                      </Tooltip>
                      {canShare && (
                        <Tooltip label="Udostępnij">
                          <IconButton
                            aria-label="Udostępnij"
                            icon={<Share2 size={14} />}
                            size="xs"
                            colorScheme="teal"
                            variant="ghost"
                            onClick={() => handleShareCostEstimate(costEstimate)}
                          />
                        </Tooltip>
                      )}
                      {canCopy && (
                        <Tooltip label="Kopiuj">
                          <IconButton
                            aria-label="Kopiuj"
                            icon={<Copy size={14} />}
                            size="xs"
                            colorScheme="purple"
                            variant="ghost"
                            onClick={() => handleCopyCostEstimate(costEstimate)}
                          />
                        </Tooltip>
                      )}
                      {canDelete && (
                        <Tooltip label="Usuń">
                          <IconButton
                            aria-label="Usuń"
                            icon={<Trash2 size={14} />}
                            size="xs"
                            colorScheme="red"
                            variant="ghost"
                            onClick={() => handleDeleteCostEstimate(costEstimate.id)}
                          />
                        </Tooltip>
                      )}
                    </HStack>
                  </Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
});

export default function ProjectCosts() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showError, showSuccess } = useToastNotification();

  const [loading, setLoading] = useState(true);
  const [project, setProject] = useState<any | null>(null);
  const [costEstimateToCopy, setCostEstimateToCopy] = useState<CostEstimateListItemWeb | null>(null);
  const [costEstimateToShare, setCostEstimateToShare] = useState<CostEstimateListItemWeb | null>(null);
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const hasFetchedProjectData = useRef(false);

  const { isOpen: isCreateModalOpen, onOpen: onCreateModalOpen, onClose: onCreateModalClose } = useDisclosure();
  const { isOpen: isCopyModalOpen, onOpen: onCopyModalOpen, onClose: onCopyModalClose } = useDisclosure();
  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const resourcePerms = useResourcePermissions(projectId);

  // Tab cache dla Moje kosztorysy
  const myCostEstimatesCache = useTabCache<CostEstimateListItemWeb[]>(
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
  const allCostEstimatesCache = useTabCache<CostEstimateListItemWeb[]>(
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

  // Tab cache dla Udostępnione kosztorysy
  const sharedCostEstimatesCache = useTabCache<CostEstimateListItemWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      return await costEstimateApi.getCostEstimatesByScope(
        user.activeTenantId,
        projectId,
        ResourceScope.Shared
      );
    },
    `cost-estimates-shared-${projectId}`
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

  useEffect(() => {
    if (resourcePerms.raw.loading) return;
    if (hasFetchedProjectData.current) return;

    hasFetchedProjectData.current = true;
    fetchProjectData();
  }, [projectId, resourcePerms.raw.loading]);

  const fetchProjectData = async () => {
    if (!user?.activeTenantId || !projectId) return;

    const hasAnyTab =
      resourcePerms.tabs.showMine ||
      resourcePerms.tabs.showAll ||
      resourcePerms.tabs.showShared;

    if (!hasAnyTab) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const projectData = await projectDetailsCache.fetch();
      setProject(projectData);

      // Pobierz zakładki równolegle według uprawnień
      const fetchPromises = [];
      if (resourcePerms.tabs.showAll) fetchPromises.push(allCostEstimatesCache.fetch());
      if (resourcePerms.tabs.showMine) fetchPromises.push(myCostEstimatesCache.fetch());
      if (resourcePerms.tabs.showShared) fetchPromises.push(sharedCostEstimatesCache.fetch());

      await Promise.all(fetchPromises);
    } catch (error: any) {
      showError('Nie udało się załadować danych', error?.message || 'Wystąpił nieoczekiwany błąd');
    } finally {
      setLoading(false);
    }
  };

  const refreshData = () => {
    myCostEstimatesCache.clear();
    allCostEstimatesCache.clear();
    sharedCostEstimatesCache.clear();
    projectDetailsCache.clear();
    hasFetchedProjectData.current = false;
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
      showError('Nie udało się usunąć kosztorysu', error?.message || 'Wystąpił nieoczekiwany błąd');
    }
  };

  const handleViewCostEstimate = (costEstimateId: string) => {
    navigate(`/projects/${projectId}/cost-estimates/${costEstimateId}`);
  };

  const handleCopyCostEstimate = (costEstimate: CostEstimateListItemWeb) => {
    setCostEstimateToCopy(costEstimate);
    onCopyModalOpen();
  };

  const handleShareCostEstimate = (costEstimate: CostEstimateListItemWeb) => {
    setCostEstimateToShare(costEstimate);
    onShareModalOpen();
  };

  const handleShareUpdated = () => {
    // Odśwież dane po zmianie udostępnienia
    myCostEstimatesCache.clear();
    allCostEstimatesCache.clear();
    hasFetchedProjectData.current = false;
    fetchProjectData();
  };

  const commonTableProps = {
    cardBg,
    borderColor,
    hoverBg,
    costEstimateStatusLabels,
    costEstimateStatusColors,
    formatDate,
    handleViewCostEstimate,
    handleCopyCostEstimate,
    handleDeleteCostEstimate,
    handleShareCostEstimate,
    resourcePerms,
    onCreateModalOpen,
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie kosztorysów..." />
        </Box>
      </MainLayout>
    );
  }

  const hasNoAccess =
    !resourcePerms.tabs.showMine &&
    !resourcePerms.tabs.showAll &&
    !resourcePerms.tabs.showShared;

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={FileText} boxSize={8} color="blue.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Kosztorysy projektowe</Heading>
              {project && <Text fontSize="sm" color="gray.600">{project.name}</Text>}
            </VStack>
          </HStack>
          {(resourcePerms.mine.canCreate || resourcePerms.all.canCreate) && (
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="blue"
              onClick={onCreateModalOpen}
            >
              Nowy kosztorys
            </Button>
          )}
        </HStack>

        {hasNoAccess ? (
          <Box p={{ base: 3, sm: 4, md: 8 }} textAlign="center">
            <EmptyState
              icon={FileText}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania kosztorysów w tym projekcie"
            />
          </Box>
        ) : (
          <Tabs colorScheme="blue" variant="enclosed" onChange={setActiveTabIndex}>
            <TabList>
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} />
                    <Text>Wszystkie</Text>
                    <Badge colorScheme="purple" ml={1}>{allCostEstimatesCache.data?.length ?? 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} />
                    <Text>Moje</Text>
                    <Badge colorScheme="blue" ml={1}>{myCostEstimatesCache.data?.length ?? 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showShared && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Users} boxSize={4} />
                    <Text>Udostępnione</Text>
                    <Badge colorScheme="teal" ml={1}>{sharedCostEstimatesCache.data?.length ?? 0}</Badge>
                  </HStack>
                </Tab>
              )}
            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showAll && (
                <TabPanel>
                  <CostEstimatesTable
                    {...commonTableProps}
                    cache={allCostEstimatesCache}
                    showOwnerColumn
                    canShare={resourcePerms.all.canShare}
                    canCopy={resourcePerms.all.canEdit}
                    canDelete={resourcePerms.all.canDelete}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showMine && (
                <TabPanel>
                  <CostEstimatesTable
                    {...commonTableProps}
                    cache={myCostEstimatesCache}
                    canShare={resourcePerms.mine.canShare}
                    canCopy={resourcePerms.mine.canEdit}
                    canDelete={resourcePerms.mine.canDelete}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showShared && (
                <TabPanel>
                  <CostEstimatesTable
                    {...commonTableProps}
                    cache={sharedCostEstimatesCache}
                    showOwnerColumn
                    canShare={false}
                    canCopy={false}
                    canDelete={false}
                  />
                </TabPanel>
              )}
            </TabPanels>
          </Tabs>
        )}

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
            onSuccess={() => refreshData()}
          />
        )}

        {/* MODAL: SHARE COST ESTIMATE */}
        {costEstimateToShare && user?.activeTenantId && projectId && (
          <ShareCostEstimateModal
            isOpen={isShareModalOpen}
            onClose={onShareModalClose}
            tenantId={user.activeTenantId}
            projectId={projectId}
            costEstimateId={costEstimateToShare.id}
            costEstimateName={costEstimateToShare.name}
            ownerId={costEstimateToShare.ownerId}
            currentUserId={user.id ?? ""}
            currentSharedUsers={costEstimateToShare.sharedWithUsers ?? []}
            onShareUpdated={handleShareUpdated}
          />
        )}
      </Box>
    </MainLayout>
  );
}


