import { useContext, useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import {
  Badge,
  Box,
  Button,
  Checkbox,
  Heading,
  HStack,
  Icon,
  IconButton,
  SimpleGrid,
  Tab,
  TabList,
  TabPanel,
  TabPanels,
  Table,
  Tabs,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tooltip,
  Tr,
  useBreakpointValue,
  useColorModeValue,
  useDisclosure,
  VStack,
} from "@chakra-ui/react";
import {
  DollarSign,
  Download,
  Eye,
  Plus,
  Share2,
  Trash2,
} from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { projectApi, ResourceScope } from "../api/projectApi";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatCurrency, formatDate } from "../utils/formatters";
import ShareCostModal from "../components/ShareCostModal";
import { ManageCostShareModal } from "../components/ManageCostShareModal";
import ShareCostsModal from "../components/ShareCostsModal";
import ExpenseFormModal, {
  type ExpenseFormData,
} from "../components/ExpenseFormModal";
import ExpenseCard from "../components/ExpenseCard";
import DeleteAlertDialog from "../components/ui/DeleteAlertDialog";
import type { ProjectCostListItemWeb } from "../types/project.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import { useTabCache } from "../hooks/useTabCache";
import { useProjectDetails } from "../hooks/queries";

// === TAB COMPONENTS ===

function CostSummaryBar({ costs }: { costs: ProjectCostListItemWeb[] }) {
  const summaryBg = useColorModeValue("primary.50", "primary.900");
  const total = costs.reduce((s, c) => s + c.grossAmount, 0);
  const open = costs.filter(c => !c.isAccepted).reduce((s, c) => s + c.grossAmount, 0);
  const accepted = costs.filter(c => c.isAccepted).reduce((s, c) => s + c.grossAmount, 0);

  return (
    <SimpleGrid columns={{ base: 2, md: 3 }} spacing={3} p={3} bg={summaryBg} rounded="md">
      <Box>
        <Text fontSize="xs" color="neutral.600">Total:</Text>
        <Text fontSize="md" fontWeight="bold">{formatCurrency(total)}</Text>
      </Box>
      <Box>
        <Text fontSize="xs" color="neutral.600">Niezaakceptowane:</Text>
        <Text fontSize="md" fontWeight="bold" color="orange.500">{formatCurrency(open)}</Text>
      </Box>
      <Box>
        <Text fontSize="xs" color="neutral.600">Zaakceptowane:</Text>
        <Text fontSize="md" fontWeight="bold" color="green.500">{formatCurrency(accepted)}</Text>
      </Box>
    </SimpleGrid>
  );
}

interface ClosedBadgeProps {
  isClosed: boolean;
  canToggle: boolean;
  isDisabled: boolean;
  onClick: () => void;
}

function ClosedBadge({ isClosed, canToggle, isDisabled, onClick }: ClosedBadgeProps) {
  if (!canToggle) {
    return (
      <Badge colorScheme={isClosed ? "green" : "orange"} fontSize="xs">
        {isClosed ? "Rozliczone" : "Aktywne"}
      </Badge>
    );
  }
  return (
    <Badge
      colorScheme={isClosed ? "green" : "orange"}
      fontSize="xs"
      cursor={isDisabled ? "not-allowed" : "pointer"}
      onClick={isDisabled ? undefined : onClick}
      _hover={isDisabled ? undefined : { opacity: 0.75 }}
      aria-label={isClosed ? "Oznacz jako nierozliczone" : "Oznacz jako rozliczone"}
    >
      {isClosed ? "Rozliczone" : "Aktywne"}
    </Badge>
  );
}

function DocumentCell({ cost }: { cost: ProjectCostListItemWeb }) {
  if (!cost.hasDocument || !cost.previewSasUrl || !cost.downloadSasUrl) {
    return <Badge colorScheme="gray" fontSize="xs">Brak</Badge>;
  }
  return (
    <HStack spacing={1} justify="center">
      <Tooltip label={`Podgląd: ${cost.documentFileName}`}>
        <IconButton
          aria-label={`Podgląd dokumentu: ${cost.documentFileName}`}
          icon={<Eye size={14} />}
          size="xs"
          variant="ghost"
          colorScheme="level2"
          onClick={() => window.open(cost.previewSasUrl!, '_blank')}
        />
      </Tooltip>
      <Tooltip label={`Pobierz: ${cost.documentFileName}`}>
        <IconButton
          aria-label={`Pobierz dokument: ${cost.documentFileName}`}
          icon={<Download size={14} />}
          size="xs"
          variant="ghost"
          colorScheme="gray"
          onClick={() => window.open(cost.downloadSasUrl!, '_blank')}
        />
      </Tooltip>
    </HStack>
  );
}

interface AllCostsTabProps {
  costs: ProjectCostListItemWeb[];
  loading: boolean;
  resourcePerms: any;
  deletingCostId: string | null;
  editingClosedCostId: string | null;
  savingClosedCost: boolean;
  onShareCostsModalOpen: () => void;
  onAddCost: () => void;
  onManageShare: (cost: ProjectCostListItemWeb) => void;
  onEditCost: (cost: ProjectCostListItemWeb) => void;
  onDeleteCost: (id: string) => void;
  onToggleCostClosed: (costId: string, currentIsClosed: boolean) => void;
}

function AllCostsTab({
  costs,
  loading,
  resourcePerms,
  deletingCostId,
  editingClosedCostId,
  savingClosedCost,
  onShareCostsModalOpen,
  onAddCost,
  onManageShare,
  onEditCost,
  onDeleteCost,
  onToggleCostClosed,
}: AllCostsTabProps) {
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const viewMode = useBreakpointValue({ base: "mobile", md: "desktop" });
  const canToggleClosed = resourcePerms.all.canEdit;

  if (loading) return <LoadingSpinner />;

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between" flexWrap="wrap" gap={4}>
        <Text fontSize="sm" color="neutral.600">
          Wszystkie wydatki w projekcie (admin)
        </Text>
        <HStack spacing={2}>
          {resourcePerms.all.canShare && (
            <Button
              leftIcon={<Share2 size={18} />}
              colorScheme="gray"
              variant="outline"
              size="sm"
              onClick={onShareCostsModalOpen}
            >
              Udostępnij grupowo
            </Button>
          )}
          {resourcePerms.all.canCreate && (
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="primary"
              size="sm"
              onClick={onAddCost}
            >
              Dodaj koszt
            </Button>
          )}
        </HStack>
      </HStack>

      <CostSummaryBar costs={costs} />

      {costs.length === 0 ? (
        <EmptyState
          icon={DollarSign}
          title="Brak kosztów"
          description="Nie ma jeszcze żadnych kosztów w tym projekcie"
        />
      ) : viewMode === "mobile" ? (
        <VStack spacing={3} align="stretch">
          {costs.map((cost) => (
            <ExpenseCard
              key={cost.id}
              cost={cost}
              showOwner
              canEdit={resourcePerms.all.canEdit}
              canDelete={resourcePerms.all.canDelete}
              canManageShare={resourcePerms.all.canManageShare}
              canToggleAccepted={canToggleClosed}
              isTogglingAccepted={editingClosedCostId === cost.id && savingClosedCost}
              isDeleting={deletingCostId === cost.id}
              onEdit={() => onEditCost(cost)}
              onDelete={() => onDeleteCost(cost.id)}
              onManageShare={() => onManageShare(cost)}
              onToggleAccepted={() => onToggleCostClosed(cost.id, cost.isAccepted)}
            />
          ))}
        </VStack>
      ) : (
        <Box overflowX="auto" bg="white" p={4} rounded="lg" borderWidth="1px" borderColor="neutral.200">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Właściciel</Th>
                <Th>Miejsce</Th>
                <Th>Data</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Zaakceptowane</Th>
                <Th textAlign="center">Dokument</Th>
                {(resourcePerms.all.canEdit || resourcePerms.all.canDelete || resourcePerms.all.canManageShare) && (
                  <Th textAlign="center">Akcje</Th>
                )}
              </Tr>
            </Thead>
            <Tbody>
              {costs.map((cost) => (
                <Tr key={cost.id} _hover={{ bg: 'neutral.50' }} cursor="pointer" onClick={() => onEditCost(cost)}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td fontSize="sm" color="neutral.600">{cost.userName || "-"}</Td>
                  <Td>{cost.place || "-"}</Td>
                  <Td>{formatDate(cost.date, false)}</Td>
                  <Td isNumeric>{formatCurrency(cost.netAmount ?? 0)}</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.grossAmount)}</Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <Checkbox
                      isChecked={cost.isAccepted}
                      onChange={() => onToggleCostClosed(cost.id, cost.isAccepted)}
                      colorScheme="green"
                      isDisabled={!canToggleClosed || (editingClosedCostId === cost.id && savingClosedCost)}
                    />
                  </Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <DocumentCell cost={cost} />
                  </Td>
                  {(resourcePerms.all.canEdit || resourcePerms.all.canDelete || resourcePerms.all.canManageShare) && (
                    <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                      <HStack spacing={1} justify="center">
                        {resourcePerms.all.canManageShare && (
                          <Tooltip label="Udostępnij">
                            <IconButton
                              aria-label="Zarządzaj udostępnieniem kosztu"
                              icon={<Share2 size={14} />}
                              size="xs"
                              variant="ghost"
                              colorScheme="gray"
                              onClick={() => onManageShare(cost)}
                            />
                          </Tooltip>
                        )}
                        {resourcePerms.all.canDelete && (
                          <Tooltip label="Usuń">
                            <IconButton
                              aria-label="Usuń koszt"
                              icon={<Trash2 size={14} />}
                              size="xs"
                              variant="ghost"
                              colorScheme="red"
                              onClick={() => onDeleteCost(cost.id)}
                              isLoading={deletingCostId === cost.id}
                            />
                          </Tooltip>
                        )}
                      </HStack>
                    </Td>
                  )}
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
}

interface MyCostsTabProps {
  costs: ProjectCostListItemWeb[];
  loading: boolean;
  resourcePerms: any;
  deletingCostId: string | null;
  editingClosedCostId: string | null;
  savingClosedCost: boolean;
  onShareCostsModalOpen: () => void;
  onAddCost: () => void;
  onEditCost: (cost: ProjectCostListItemWeb) => void;
  onShareCost: (cost: ProjectCostListItemWeb) => void;
  onDeleteCost: (costId: string) => void;
  onToggleCostClosed: (costId: string, currentIsClosed: boolean) => void;
}

function MyCostsTab({
  costs,
  loading,
  resourcePerms,
  deletingCostId,
  editingClosedCostId,
  savingClosedCost,
  onShareCostsModalOpen,
  onAddCost,
  onEditCost,
  onShareCost,
  onDeleteCost,
  onToggleCostClosed,
}: MyCostsTabProps) {
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const viewMode = useBreakpointValue({ base: "mobile", md: "desktop" });
  const canToggleClosed = resourcePerms.mine.canEdit;

  if (loading) return <LoadingSpinner />;

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between" flexWrap="wrap" gap={4}>
        <Text fontSize="sm" color="neutral.600">
          Twoje wydatki w projekcie
        </Text>
        <HStack spacing={2}>
          {resourcePerms.mine.canShare && (
            <Button
              leftIcon={<Share2 size={18} />}
              colorScheme="gray"
              variant="outline"
              size="sm"
              onClick={onShareCostsModalOpen}
            >
              Udostępnij grupowo
            </Button>
          )}
          {resourcePerms.mine.canCreate && (
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="primary"
              size="sm"
              onClick={onAddCost}
            >
              Dodaj koszt
            </Button>
          )}
        </HStack>
      </HStack>

      <CostSummaryBar costs={costs} />

      {costs.length === 0 ? (
        <EmptyState
          icon={DollarSign}
          title="Brak kosztów"
          description="Dodaj pierwszy koszt do projektu"
        />
      ) : viewMode === "mobile" ? (
        <VStack spacing={3} align="stretch">
          {costs.map((cost) => (
            <ExpenseCard
              key={cost.id}
              cost={cost}
              canEdit={resourcePerms.mine.canEdit}
              canDelete={resourcePerms.mine.canDelete}
              canManageShare={resourcePerms.mine.canManageShare}
              canToggleAccepted={canToggleClosed}
              isTogglingAccepted={editingClosedCostId === cost.id && savingClosedCost}
              isDeleting={deletingCostId === cost.id}
              onEdit={() => onEditCost(cost)}
              onDelete={() => onDeleteCost(cost.id)}
              onManageShare={() => onShareCost(cost)}
              onToggleAccepted={() => onToggleCostClosed(cost.id, cost.isAccepted)}
            />
          ))}
        </VStack>
      ) : (
        <Box overflowX="auto" bg="white" p={4} rounded="lg" borderWidth="1px" borderColor="neutral.200">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Miejsce</Th>
                <Th>Data</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Zaakceptowane</Th>
                <Th textAlign="center">Dokument</Th>
                <Th textAlign="center">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {costs.map((cost) => (
                <Tr key={cost.id} _hover={{ bg: 'neutral.50' }} cursor="pointer" onClick={() => onEditCost(cost)}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td>{cost.place || "-"}</Td>
                  <Td>{formatDate(cost.date, false)}</Td>
                  <Td isNumeric>{formatCurrency(cost.netAmount ?? 0)}</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.grossAmount)}</Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <Checkbox
                      isChecked={cost.isAccepted}
                      onChange={() => onToggleCostClosed(cost.id, cost.isAccepted)}
                      colorScheme="green"
                      isDisabled={!canToggleClosed || (editingClosedCostId === cost.id && savingClosedCost)}
                    />
                  </Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <DocumentCell cost={cost} />
                  </Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <HStack spacing={1} justify="center">
                      {resourcePerms.mine.canManageShare && (
                        <Tooltip label="Udostępnij">
                          <IconButton
                            aria-label="Udostępnij koszt"
                            icon={<Share2 size={14} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="gray"
                            onClick={() => onShareCost(cost)}
                          />
                        </Tooltip>
                      )}
                      {resourcePerms.mine.canDelete && (
                        <Tooltip label="Usuń">
                          <IconButton
                            aria-label="Usuń koszt"
                            icon={<Trash2 size={14} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="red"
                            onClick={() => onDeleteCost(cost.id)}
                            isLoading={deletingCostId === cost.id}
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
}

interface SharedCostsTabProps {
  costs: ProjectCostListItemWeb[];
  loading: boolean;
  editingSharedCostId: string | null;
  savingSharedCost: boolean;
  resourcePerms: any;
  onToggleSharedCostClosed: (costId: string, currentIsClosed: boolean) => void;
}

function SharedCostsTab({
  costs,
  loading,
  editingSharedCostId,
  savingSharedCost,
  resourcePerms,
  onToggleSharedCostClosed,
}: SharedCostsTabProps) {
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const viewMode = useBreakpointValue({ base: "mobile", md: "desktop" });
  const canToggleClosed = resourcePerms.shared.canEdit;

  if (loading) return <LoadingSpinner />;

  return (
    <VStack spacing={4} align="stretch">
      <Text fontSize="sm" color="neutral.600">
        Wydatki udostępnione przez innych członków projektu
      </Text>

      <CostSummaryBar costs={costs} />

      {costs.length === 0 ? (
        <EmptyState
          icon={Share2}
          title="Brak udostępnionych kosztów"
          description="Nikt jeszcze nie udostępnił Ci kosztów w tym projekcie"
        />
      ) : viewMode === "mobile" ? (
        <VStack spacing={3} align="stretch">
          {costs.map((cost) => (
            <ExpenseCard
              key={cost.id}
              cost={cost}
              showOwner
              canToggleAccepted={canToggleClosed}
              isTogglingAccepted={editingSharedCostId === cost.id && savingSharedCost}
              onToggleAccepted={() => onToggleSharedCostClosed(cost.id, cost.isAccepted)}
            />
          ))}
        </VStack>
      ) : (
        <Box overflowX="auto" bg="white" p={4} rounded="lg" borderWidth="1px" borderColor="neutral.200">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Miejsce</Th>
                <Th>Data</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Zaakceptowane</Th>
                <Th textAlign="center">Dokument</Th>
                <Th>Udostępnione przez</Th>
              </Tr>
            </Thead>
            <Tbody>
              {costs.map((cost) => (
                <Tr key={cost.id} _hover={{ bg: 'neutral.50' }}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td>{cost.place || "-"}</Td>
                  <Td>{formatDate(cost.date, false)}</Td>
                  <Td isNumeric>{formatCurrency(cost.netAmount ?? 0)}</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.grossAmount)}</Td>
                  <Td textAlign="center">
                    <Checkbox
                      isChecked={cost.isAccepted}
                      onChange={() => onToggleSharedCostClosed(cost.id, cost.isAccepted)}
                      colorScheme="green"
                      isDisabled={!canToggleClosed || (editingSharedCostId === cost.id && savingSharedCost)}
                    />
                  </Td>
                  <Td textAlign="center">
                    <DocumentCell cost={cost} />
                  </Td>
                  <Td>{cost.userName}</Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}
    </VStack>
  );
}

// === MAIN COMPONENT ===

export default function ProjectSimpleCosts() {
  const { projectId } = useParams<{ projectId: string }>();
  const { user } = useContext(AuthContext);
  const { showSuccess, showError, showApiSuccess } = useToastNotification();

  const [loading, setLoading] = useState(true);
  const [project, setProject] = useState<any | null>(null);
  const [projectName, setProjectName] = useState("");
  const hasFetchedProjectData = useRef(false);

  const [savingCost, setSavingCost] = useState(false);
  const [deletingCostId, setDeletingCostId] = useState<string | null>(null);
  const [costToShare, setCostToShare] = useState<ProjectCostListItemWeb | null>(null);
  const [costToManageShare, setCostToManageShare] = useState<ProjectCostListItemWeb | null>(null);
  const [editingSharedCostId, setEditingSharedCostId] = useState<string | null>(null);
  const [savingSharedCost, setSavingSharedCost] = useState(false);
  const [editingClosedCostId, setEditingClosedCostId] = useState<string | null>(null);
  const [savingClosedCost, setSavingClosedCost] = useState(false);

  // Modal state for add/edit expense
  const [editingCost, setEditingCost] = useState<ProjectCostListItemWeb | null>(null);
  const [modalDocumentFile, setModalDocumentFile] = useState<File | null>(null);
  const {
    isOpen: isExpenseModalOpen,
    onOpen: onExpenseModalOpen,
    onClose: onExpenseModalClose,
  } = useDisclosure();

  // Delete confirmation dialog
  const [costToDelete, setCostToDelete] = useState<string | null>(null);
  const {
    isOpen: isDeleteAlertOpen,
    onOpen: onDeleteAlertOpen,
    onClose: onDeleteAlertClose,
  } = useDisclosure();

  // Tab cache dla wszystkich kosztów
  const allCostsCache = useTabCache<ProjectCostListItemWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectCosts(user.activeTenantId, projectId, ResourceScope.All);
      return res.data;
    },
    `costs-all-${projectId}`
  );

  // Tab cache dla moich kosztów
  const myCostsCache = useTabCache<ProjectCostListItemWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectCosts(user.activeTenantId, projectId, ResourceScope.Mine);
      return res.data;
    },
    `costs-mine-${projectId}`
  );

  // Tab cache dla udostępnionych kosztów
  const sharedCostsCache = useTabCache<ProjectCostListItemWeb[]>(
    async () => {
      if (!user?.activeTenantId || !projectId) return [];
      const res = await projectApi.getProjectCosts(user.activeTenantId, projectId, ResourceScope.Shared);
      return res.data;
    },
    `costs-shared-${projectId}`
  );

  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();
  const { isOpen: isManageShareModalOpen, onOpen: onManageShareModalOpen, onClose: onManageShareModalClose } = useDisclosure();
  const { isOpen: isShareCostsModalOpen, onOpen: onShareCostsModalOpen, onClose: onShareCostsModalClose } = useDisclosure();

  const resourcePerms = useResourcePermissions(projectId);

  // React Query — dane projektu (współdzielony cache między stronami projektu)
  const { data: projectData } = useProjectDetails(
    user?.activeTenantId ?? undefined,
    projectId
  );
  useEffect(() => {
    if (projectData) {
      setProject(projectData);
      setProjectName(projectData.name);
    }
  }, [projectData]);

  useEffect(() => {
    if (resourcePerms.raw.loading) return;
    if (hasFetchedProjectData.current) return;

    hasFetchedProjectData.current = true;
    fetchProjectData();
  }, [projectId, resourcePerms.raw.loading]);

  const fetchProjectData = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    try {
      const fetchPromises = [];
      if (resourcePerms.tabs.showAll) fetchPromises.push(allCostsCache.fetch());
      if (resourcePerms.tabs.showMine) fetchPromises.push(myCostsCache.fetch());
      if (resourcePerms.tabs.showShared) fetchPromises.push(sharedCostsCache.fetch());

      await Promise.all(fetchPromises);
    } catch {
      // błąd obsługiwany przez cache
    } finally {
      setLoading(false);
    }
  };

  const refreshData = () => {
    allCostsCache.clear();
    myCostsCache.clear();
    sharedCostsCache.clear();
    hasFetchedProjectData.current = false;
    fetchProjectData();
  };

  // ── Modal handlers ────────────────────────────────────────

  const handleOpenAddModal = () => {
    setEditingCost(null);
    setModalDocumentFile(null);
    onExpenseModalOpen();
  };

  const handleOpenEditModal = (cost: ProjectCostListItemWeb) => {
    setEditingCost(cost);
    setModalDocumentFile(null);
    onExpenseModalOpen();
  };

  const handleCloseModal = () => {
    onExpenseModalClose();
    setEditingCost(null);
    setModalDocumentFile(null);
  };

  const handleSaveCost = async (formData: ExpenseFormData) => {
    if (!user?.activeTenantId || !projectId) return;

    setSavingCost(true);
    try {
      if (editingCost) {
        await projectApi.updateProjectCost(user.activeTenantId, projectId, editingCost.id, {
          name: formData.name,
          place: formData.place || undefined,
          date: new Date(formData.date),
          description: formData.description || undefined,
          netAmount: formData.netAmount ? parseFloat(formData.netAmount) : null,
          grossAmount: formData.grossAmount ? parseFloat(formData.grossAmount) : null,
          isAccepted: formData.isAccepted,
          // Jeśli koszt miał już dokument i dodajemy nowy plik → UpdatedDocument (zastąpienie)
          // Jeśli koszt nie miał dokumentu i dodajemy plik → Document (nowy)
          document: modalDocumentFile && !editingCost.hasDocument ? modalDocumentFile : undefined,
          updatedDocument: modalDocumentFile && editingCost.hasDocument ? modalDocumentFile : undefined,
          removeDocument: formData.removeDocument,
        });
        showApiSuccess('costUpdated');
      } else {
        await projectApi.createProjectCost(user.activeTenantId, projectId, {
          name: formData.name,
          place: formData.place || undefined,
          date: new Date(formData.date),
          description: formData.description || undefined,
          netAmount: formData.netAmount ? parseFloat(formData.netAmount) : null,
          grossAmount: formData.grossAmount ? parseFloat(formData.grossAmount) : null,
          isAccepted: formData.isAccepted,
          document: modalDocumentFile || undefined,
        });
        showApiSuccess('costAdded');
      }

      handleCloseModal();
      refreshData();
    } catch {
      showError(
        editingCost
          ? "Wystąpił błąd podczas aktualizacji kosztu"
          : "Wystąpił błąd podczas dodawania kosztu",
      );
    } finally {
      setSavingCost(false);
    }
  };

  const handleDeleteCost = (costId: string) => {
    setCostToDelete(costId);
    onDeleteAlertOpen();
  };

  const confirmDeleteCost = async () => {
    if (!user?.activeTenantId || !projectId || !costToDelete) return;

    setDeletingCostId(costToDelete);
    onDeleteAlertClose();
    try {
      await projectApi.deleteProjectCost(user.activeTenantId, projectId, costToDelete);
      showApiSuccess('costDeleted');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas usuwania kosztu");
    } finally {
      setDeletingCostId(null);
      setCostToDelete(null);
    }
  };

  const handleShareCost = (cost: ProjectCostListItemWeb) => {
    setCostToShare(cost);
    onShareModalOpen();
  };

  const handleManageShare = (cost: ProjectCostListItemWeb) => {
    setCostToManageShare(cost);
    onManageShareModalOpen();
  };

  const handleShareUpdated = () => {
    refreshData();
    onManageShareModalClose();
  };

  const handleToggleCostClosed = async (costId: string, currentIsClosed: boolean) => {
    if (!user?.activeTenantId || !projectId) return;

    setEditingClosedCostId(costId);
    setSavingClosedCost(true);
    try {
      // Znajdujemy koszt w cache aby pobrać wszystkie dane
      const cost = allCostsCache.data?.find(c => c.id === costId) || myCostsCache.data?.find(c => c.id === costId);
      if (!cost) {
        showError("Nie znaleziono kosztu");
        return;
      }

      await projectApi.updateProjectCost(
        user.activeTenantId,
        projectId,
        costId,
        {
          name: cost.name,
          place: cost.place || undefined,
          date: new Date(cost.date),
          description: cost.description || undefined,
          netAmount: cost.netAmount ?? null,
          grossAmount: cost.grossAmount ?? null,
          isAccepted: !currentIsClosed,
          removeDocument: false,
        }
      );

      showApiSuccess('statusUpdated');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas aktualizacji statusu");
    } finally {
      setEditingClosedCostId(null);
      setSavingClosedCost(false);
    }
  };

  const handleToggleSharedCostClosed = async (costId: string, currentIsClosed: boolean) => {
    if (!user?.activeTenantId || !projectId) return;
    if (!resourcePerms.shared.canEdit) return;

    setEditingSharedCostId(costId);
    setSavingSharedCost(true);
    try {
      // Znajdujemy koszt w cache aby pobrać wszystkie dane
      const cost = sharedCostsCache.data?.find(c => c.id === costId);
      if (!cost) {
        showError("Nie znaleziono kosztu");
        return;
      }

      await projectApi.updateProjectCost(
        user.activeTenantId,
        projectId,
        costId,
        {
          name: cost.name,
          place: cost.place || undefined,
          date: new Date(cost.date),
          description: cost.description || undefined,
          netAmount: cost.netAmount ?? null,
          grossAmount: cost.grossAmount ?? null,
          isAccepted: !currentIsClosed,
          removeDocument: false,
        }
      );

      showApiSuccess('statusUpdated');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas aktualizacji statusu");
    } finally {
      setEditingSharedCostId(null);
      setSavingSharedCost(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie kosztów..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={DollarSign} boxSize={8} color="red.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Wydatki</Heading>
              {projectName && <Text fontSize="sm" color="neutral.600">{projectName}</Text>}
            </VStack>
          </HStack>
        </HStack>

        {!project || !resourcePerms.hasAnyAccess ? (
          <Box p={{ base: 3, sm: 4, md: 8 }} textAlign="center">
            <EmptyState
              icon={DollarSign}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania wydatków w tym projekcie"
            />
          </Box>
        ) : (
          <Tabs colorScheme="primary" variant="enclosed">
            <TabList>
              {resourcePerms.tabs.showAll && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={DollarSign} boxSize={4} />
                    <Text>Wszystkie</Text>
                    <Badge colorScheme="level2" ml={2}>{allCostsCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={DollarSign} boxSize={4} />
                    <Text>Moje</Text>
                    <Badge colorScheme="primary" ml={2}>{myCostsCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showShared && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Share2} boxSize={4} />
                    <Text>Udostępnione</Text>
                    <Badge colorScheme="action" ml={2}>{sharedCostsCache.data?.length || 0}</Badge>
                  </HStack>
                </Tab>
              )}
            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showAll && (
                <TabPanel>
                  <AllCostsTab
                    costs={allCostsCache.data || []}
                    loading={allCostsCache.loading}
                    resourcePerms={resourcePerms}
                    deletingCostId={deletingCostId}
                    editingClosedCostId={editingClosedCostId}
                    savingClosedCost={savingClosedCost}
                    onShareCostsModalOpen={onShareCostsModalOpen}
                    onAddCost={handleOpenAddModal}
                    onManageShare={handleManageShare}
                    onEditCost={handleOpenEditModal}
                    onDeleteCost={handleDeleteCost}
                    onToggleCostClosed={handleToggleCostClosed}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showMine && (
                <TabPanel>
                  <MyCostsTab
                    costs={myCostsCache.data || []}
                    loading={myCostsCache.loading}
                    deletingCostId={deletingCostId}
                    editingClosedCostId={editingClosedCostId}
                    savingClosedCost={savingClosedCost}
                    resourcePerms={resourcePerms}
                    onShareCostsModalOpen={onShareCostsModalOpen}
                    onAddCost={handleOpenAddModal}
                    onEditCost={handleOpenEditModal}
                    onShareCost={handleShareCost}
                    onDeleteCost={handleDeleteCost}
                    onToggleCostClosed={handleToggleCostClosed}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showShared && (
                <TabPanel>
                  <SharedCostsTab
                    costs={sharedCostsCache.data || []}
                    loading={sharedCostsCache.loading}
                    editingSharedCostId={editingSharedCostId}
                    savingSharedCost={savingSharedCost}
                    resourcePerms={resourcePerms}
                    onToggleSharedCostClosed={handleToggleSharedCostClosed}
                  />
                </TabPanel>
              )}
            </TabPanels>
          </Tabs>
        )}

        <Box mt={6} p={4} bg="primary.50" rounded="md" borderWidth="1px" borderColor="primary.200">
          <Text fontSize="sm" color="primary.800">
            💡 <strong>Wskazówka:</strong> Tu rejestrujesz wydatki poniesione w projekcie (faktury, paragony, rozliczenia). Dla zaawansowanych kosztorysów według szablonów przejdź do zakładki "Kosztorysy".
          </Text>
        </Box>

        {/* MODAL: Dodaj / Edytuj koszt */}
        <ExpenseFormModal
          isOpen={isExpenseModalOpen}
          onClose={handleCloseModal}
          editingCost={editingCost}
          documentFile={modalDocumentFile}
          onDocumentFileChange={setModalDocumentFile}
          onSave={handleSaveCost}
          isSaving={savingCost}
        />

        {/* ALERT: Potwierdzenie usunięcia kosztu */}
        <DeleteAlertDialog
          isOpen={isDeleteAlertOpen}
          onClose={onDeleteAlertClose}
          onConfirm={confirmDeleteCost}
          isLoading={deletingCostId !== null}
        />

        {/* MODAL: MANAGE COST SHARE (pojedynczy koszt) */}
        {costToManageShare && user?.activeTenantId && projectId && (
          <ManageCostShareModal
            isOpen={isManageShareModalOpen}
            onClose={() => {
              onManageShareModalClose();
              setCostToManageShare(null);
            }}
            tenantId={user.activeTenantId}
            projectId={projectId}
            costId={costToManageShare.id}
            costName={costToManageShare.name}
            sharedWithUserIds={costToManageShare.sharedWithUserIds || []}
            currentUserId={user?.id || ""}
            ownerUserId={costToManageShare.userId}
            onShareUpdated={handleShareUpdated}
          />
        )}

        {/* MODAL: SHARE COSTS (grupowe udostępnianie) */}
        {user?.activeTenantId && projectId && (
          <ShareCostsModal
            isOpen={isShareCostsModalOpen}
            onClose={onShareCostsModalClose}
            tenantId={user.activeTenantId}
            projectId={projectId}
            onCostsShared={refreshData}
          />
        )}

        {/* MODAL: SHARE COST (backward compatibility) */}
        {isShareModalOpen && costToShare && user?.activeTenantId && projectId && (
          <ShareCostModal
            isOpen={isShareModalOpen}
            onClose={() => {
              onShareModalClose();
              setCostToShare(null);
            }}
            tenantId={user.activeTenantId}
            projectId={projectId}
            cost={costToShare}
            onCostShared={refreshData}
          />
        )}

      </Box>
    </MainLayout>
  );
}
