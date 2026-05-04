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
  useBreakpointValue,
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
import { Trash2, Plus, FileText, Copy, Share2, Users } from "lucide-react";
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
import DeleteAlertDialog from "../components/ui/DeleteAlertDialog";
import type { CostEstimateListItemWeb, CostEstimateShareWeb } from "../types/costEstimate.types.new";
import type { CostEstimateStatus } from "../types/costEstimate.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { ResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useProjectDetails } from "../hooks/queries";
import { handleApiError } from "../utils/handleApiError";

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
  handleDeleteCostEstimate: (costEstimate: CostEstimateListItemWeb) => void;
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
  const viewMode = useBreakpointValue({ base: "mobile", md: "desktop" });

  if (cache.loading) {
    return <LoadingSpinner message="Ładowanie kosztorysów..." />;
  }

  const costEstimates = cache.data || [];

  if (viewMode === "mobile") {
    return (
      <VStack spacing={4} align="stretch">
        {costEstimates.length === 0 ? (
          <EmptyState
            icon={FileText}
            title="Brak kosztorysów"
            description="Brak kosztorysów do wyświetlenia"
          />
        ) : (
          costEstimates.map((costEstimate) => (
            <Box
              key={costEstimate.id}
              bg="white"
              border="1px"
              borderColor="neutral.200"
              borderRadius="lg"
              p={3}
              cursor="pointer"
              onClick={() => handleViewCostEstimate(costEstimate.id)}
            >
              <VStack align="stretch" spacing={2}>
                <HStack justify="space-between" align="flex-start">
                  <VStack align="flex-start" spacing={0} flex={1} minW={0}>
                    <Text fontWeight="semibold" fontSize="sm" noOfLines={2}>{costEstimate.name}</Text>
                    {costEstimate.description && (
                      <Text fontSize="xs" color="neutral.500" noOfLines={1}>{costEstimate.description}</Text>
                    )}
                    {showOwnerColumn && costEstimate.ownerName && (
                      <Text fontSize="xs" color="neutral.500">{costEstimate.ownerName}</Text>
                    )}
                  </VStack>
                  <Badge
                    colorScheme={costEstimateStatusColors[costEstimate.status]}
                    flexShrink={0}
                    ml={2}
                  >
                    {costEstimateStatusLabels[costEstimate.status]}
                  </Badge>
                </HStack>

                <HStack justify="space-between">
                  <VStack align="flex-start" spacing={0}>
                    <Text fontSize="xs" color="neutral.500">Netto</Text>
                    <Text fontSize="sm" fontWeight="medium">{formatCurrency(costEstimate.totalNet)}</Text>
                  </VStack>
                  <VStack align="flex-end" spacing={0}>
                    <Text fontSize="xs" color="neutral.500">Brutto</Text>
                    <Text fontSize="sm" fontWeight="bold" color="green.600">{formatCurrency(costEstimate.totalGross)}</Text>
                  </VStack>
                </HStack>

                <HStack justify="space-between">
                  <Text fontSize="xs" color="neutral.500">{formatDate(costEstimate.createdAt)}</Text>
                  <HStack spacing={1} onClick={(e) => e.stopPropagation()}>
                    {canShare && (
                      <IconButton
                        aria-label="Udostępnij kosztorys"
                        icon={<Share2 size={14} />}
                        size="xs"
                        colorScheme="gray"
                        variant="ghost"
                        onClick={() => handleShareCostEstimate(costEstimate)}
                      />
                    )}
                    {canCopy && (
                      <IconButton
                        aria-label="Kopiuj kosztorys"
                        icon={<Copy size={14} />}
                        size="xs"
                        colorScheme="level2"
                        variant="ghost"
                        onClick={() => handleCopyCostEstimate(costEstimate)}
                      />
                    )}
                    {canDelete && (
                      <IconButton
                        aria-label="Usuń kosztorys"
                        icon={<Trash2 size={14} />}
                        size="xs"
                        colorScheme="red"
                        variant="ghost"
                        onClick={() => handleDeleteCostEstimate(costEstimate)}
                      />
                    )}
                  </HStack>
                </HStack>
              </VStack>
            </Box>
          ))
        )}
      </VStack>
    );
  }

  return (
    <VStack spacing={4} align="stretch">
      {costEstimates.length === 0 ? (
        <EmptyState
          icon={FileText}
          title="Brak kosztorysów"
          description="Brak kosztorysów do wyświetlenia"
        />
      ) : (
        <Box overflowX="auto" bg="white" p={4} rounded="lg" borderWidth="1px" borderColor="neutral.200">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                {showOwnerColumn && <Th>Właściciel</Th>}
                <Th>Szablon</Th>
                <Th isNumeric>Wartość netto</Th>
                <Th isNumeric>Wartość brutto</Th>
                <Th>Utworzony</Th>
                <Th>Aktualizacja</Th>
                <Th textAlign="center">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {costEstimates.map((costEstimate) => (
                <Tr key={costEstimate.id} _hover={{ bg: 'neutral.50' }} cursor="pointer" onClick={() => handleViewCostEstimate(costEstimate.id)}>
                  <Td fontWeight="medium">
                    <VStack align="flex-start" spacing={0}>
                      <HStack spacing={2}>
                        <Text>{costEstimate.name}</Text>
                        {costEstimate.isSharedByMe && (
                          <Tooltip label={`Udostępniono ${costEstimate.sharedWithUsers?.length ?? 0} osobom`}>
                            <Badge colorScheme="level2" fontSize="2xs" display="flex" alignItems="center" gap={1}>
                              <Users size={10} />
                              {costEstimate.sharedWithUsers?.length ?? 0}
                            </Badge>
                          </Tooltip>
                        )}
                        {costEstimate.isSharedWithMe && (
                          <Badge colorScheme="action" fontSize="2xs">Udostępniony</Badge>
                        )}
                      </HStack>
                      {costEstimate.description && (
                        <Text fontSize="xs" color="neutral.500" noOfLines={1}>
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
                    <HStack spacing={1} justify="center" onClick={(e) => e.stopPropagation()}>
                      {canShare && (
                        <Tooltip label="Udostępnij">
                          <IconButton
                            aria-label="Udostępnij"
                            icon={<Share2 size={14} />}
                            size="xs"
                            colorScheme="gray"
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
                            colorScheme="level2"
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
                            onClick={() => handleDeleteCostEstimate(costEstimate)}
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
  const [costEstimateToCopy, setCostEstimateToCopy] = useState<CostEstimateListItemWeb | null>(null);
  const [costEstimateToShare, setCostEstimateToShare] = useState<CostEstimateListItemWeb | null>(null);
  const [costEstimateToDelete, setCostEstimateToDelete] = useState<CostEstimateListItemWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const hasFetchedProjectData = useRef(false);

  const { isOpen: isCreateModalOpen, onOpen: onCreateModalOpen, onClose: onCreateModalClose } = useDisclosure();
  const { isOpen: isCopyModalOpen, onOpen: onCopyModalOpen, onClose: onCopyModalClose } = useDisclosure();
  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();
  const { isOpen: isDeleteModalOpen, onOpen: onDeleteModalOpen, onClose: onDeleteModalClose } = useDisclosure();

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

  // React Query — nazwa projektu (współdzielony cache między stronami projektu)
  const { data: project } = useProjectDetails(
    user?.activeTenantId ?? undefined,
    projectId
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
      // Pobierz zakładki równolegle według uprawnień
      const fetchPromises = [];
      if (resourcePerms.tabs.showAll) fetchPromises.push(allCostEstimatesCache.fetch());
      if (resourcePerms.tabs.showMine) fetchPromises.push(myCostEstimatesCache.fetch());
      if (resourcePerms.tabs.showShared) fetchPromises.push(sharedCostEstimatesCache.fetch());

      await Promise.all(fetchPromises);
    } catch (error: unknown) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setLoading(false);
    }
  };

  const refreshData = () => {
    myCostEstimatesCache.clear();
    allCostEstimatesCache.clear();
    sharedCostEstimatesCache.clear();
    hasFetchedProjectData.current = false;
    fetchProjectData();
  };

  const handleDeleteCostEstimate = (costEstimate: CostEstimateListItemWeb) => {
    setCostEstimateToDelete(costEstimate);
    onDeleteModalOpen();
  };

  const handleDeleteConfirm = async () => {
    if (!user?.activeTenantId || !projectId || !costEstimateToDelete) return;
    setIsDeleting(true);
    try {
      await costEstimateApi.deleteCostEstimate(user.activeTenantId, projectId, costEstimateToDelete.id);
      showSuccess("Kosztorys został usunięty");
      onDeleteModalClose();
      setCostEstimateToDelete(null);
      refreshData();
    } catch (error: unknown) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setIsDeleting(false);
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
            <Icon as={FileText} boxSize={8} color="primary.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Kosztorysy projektowe</Heading>
              {project && <Text fontSize="sm" color="neutral.600">{project.name}</Text>}
            </VStack>
          </HStack>
          {(resourcePerms.mine.canCreate || resourcePerms.all.canCreate) && (
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="primary"
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
          <Tabs colorScheme="primary" variant="enclosed" onChange={setActiveTabIndex}>
            <TabList>
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} />
                    <Text>Wszystkie</Text>
                    <Badge colorScheme="level2" ml={1}>{allCostEstimatesCache.data?.length ?? 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={FileText} boxSize={4} />
                    <Text>Moje</Text>
                    <Badge colorScheme="primary" ml={1}>{myCostEstimatesCache.data?.length ?? 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showShared && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Users} boxSize={4} />
                    <Text>Udostępnione</Text>
                    <Badge colorScheme="action" ml={1}>{sharedCostEstimatesCache.data?.length ?? 0}</Badge>
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
        {/* MODAL: DELETE COST ESTIMATE */}
        <DeleteAlertDialog
          isOpen={isDeleteModalOpen}
          onClose={onDeleteModalClose}
          onConfirm={handleDeleteConfirm}
          itemName={costEstimateToDelete?.name}
          isLoading={isDeleting}
        />
      </Box>
    </MainLayout>
  );
}


