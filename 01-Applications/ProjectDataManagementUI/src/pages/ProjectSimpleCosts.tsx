import { useContext, useState } from "react";
import { useParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
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
  Check,
  Clock,
  DollarSign,
  Download,
  Eye,
  Plus,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { ResourceScope } from "../api/projectApi";
import { AuthContext } from "../context/AuthContext";
import { BackToProjectButton, LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatCurrency, formatDate } from "../utils/formatters";
import { CostModal } from "../features/dashboard/components/CostModal";
import { AICostImportModal } from "../components/CostTracker/AICostImportModal";
import ExpenseCard from "../components/ExpenseCard";
import DeleteAlertDialog from "../components/ui/DeleteAlertDialog";
import type { ProjectCostListItemWeb, CostApprovalStatus } from "../types/project.types";
import type { ParsedCostDto } from "../types/ai.types";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import type { ResourcePermissions } from "../hooks/useResourcePermissions";
import { useProjectDetails, useProjectCostsByScope, invalidateProjectCostLists } from "../hooks/queries";
import { useProjectCostMutations } from "../hooks/useProjectCostMutations";

// === TAB COMPONENTS ===

function CostSummaryBar({ costs }: { costs: ProjectCostListItemWeb[] }) {
  const summaryBg = useColorModeValue("primary.50", "primary.900");
  const total = costs.reduce((s, c) => s + (c.gross ?? 0), 0);
  const open = costs.filter(c => c.approvalStatus !== 'Approved').reduce((s, c) => s + (c.gross ?? 0), 0);
  const accepted = costs.filter(c => c.approvalStatus === 'Approved').reduce((s, c) => s + (c.gross ?? 0), 0);

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

function ApprovalStatusBadge({ status }: { status: CostApprovalStatus }) {
  const schemes: Record<CostApprovalStatus, string> = {
    Draft: 'gray',
    PendingApproval: 'orange',
    Approved: 'green',
  };
  const labels: Record<CostApprovalStatus, string> = {
    Draft: 'Szkic',
    PendingApproval: 'Oczekuje',
    Approved: 'Zaakceptowane',
  };
  return <Badge colorScheme={schemes[status]} fontSize="xs">{labels[status]}</Badge>;
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
  onAddCost: () => void;
  onAiImport: () => void;
  onEditCost: (cost: ProjectCostListItemWeb) => void;
  onDeleteCost: (id: string) => void;
}

function AllCostsTab({
  costs,
  loading,
  resourcePerms,
  deletingCostId,
  onAddCost,
  onAiImport,
  onEditCost,
  onDeleteCost,
}: AllCostsTabProps) {
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const viewMode = useBreakpointValue({ base: "mobile", md: "desktop" });

  if (loading) return <LoadingSpinner />;

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between" flexWrap="wrap" gap={4}>
        <Text fontSize="sm" color="neutral.600">
          Wszystkie wydatki w projekcie (admin)
        </Text>
        <HStack spacing={2}>
          {resourcePerms.all.canCreate && (
            <>
              <Button
                leftIcon={<Sparkles size={14} />}
                size="sm"
                background="linear-gradient(135deg, #7c3aed 0%, #a855f7 100%)"
                color="white"
                _hover={{ background: 'linear-gradient(135deg, #6d28d9 0%, #9333ea 100%)' }}
                boxShadow="0 1px 4px rgba(124, 58, 237, 0.35)"
                border="none"
                onClick={onAiImport}
              >
                Importuj z AI
              </Button>
              <Button
                leftIcon={<Plus size={18} />}
                colorScheme="primary"
                size="sm"
                onClick={onAddCost}
              >
                Dodaj koszt
              </Button>
            </>
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
              isDeleting={deletingCostId === cost.id}
              onEdit={() => onEditCost(cost)}
              onDelete={() => onDeleteCost(cost.id)}
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
                <Th>Kontrahent</Th>
                <Th>Data</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Status</Th>
                <Th textAlign="center">Dokument</Th>
                {(resourcePerms.all.canEdit || resourcePerms.all.canDelete) && (
                  <Th textAlign="center">Akcje</Th>
                )}
              </Tr>
            </Thead>
            <Tbody>
              {costs.map((cost) => (
                <Tr key={cost.id} _hover={{ bg: 'neutral.50' }} cursor="pointer" onClick={() => onEditCost(cost)}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td fontSize="sm" color="neutral.600">{cost.userName || "-"}</Td>
                  <Td>{cost.contractorName || "-"}</Td>
                  <Td>{formatDate(cost.date, false)}</Td>
                  <Td isNumeric>{formatCurrency(cost.net ?? 0)}</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.gross ?? 0)}</Td>
                  <Td textAlign="center">
                    <ApprovalStatusBadge status={cost.approvalStatus} />
                  </Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <DocumentCell cost={cost} />
                  </Td>
                  {(resourcePerms.all.canEdit || resourcePerms.all.canDelete) && (
                    <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                      <HStack spacing={1} justify="center">
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
  submittingCostId: string | null;
  withdrawingCostId: string | null;
  onAddCost: () => void;
  onAiImport: () => void;
  onEditCost: (cost: ProjectCostListItemWeb) => void;
  onDeleteCost: (costId: string) => void;
  onSubmitForApproval: (costId: string) => void;
  onWithdrawFromApproval: (costId: string) => void;
}

function MyCostsTab({
  costs,
  loading,
  resourcePerms,
  deletingCostId,
  submittingCostId,
  withdrawingCostId,
  onAddCost,
  onAiImport,
  onEditCost,
  onDeleteCost,
  onSubmitForApproval,
  onWithdrawFromApproval,
}: MyCostsTabProps) {
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const viewMode = useBreakpointValue({ base: "mobile", md: "desktop" });

  if (loading) return <LoadingSpinner />;

  return (
    <VStack spacing={4} align="stretch">
      <HStack justify="space-between" flexWrap="wrap" gap={4}>
        <Text fontSize="sm" color="neutral.600">
          Twoje wydatki w projekcie
        </Text>
        <HStack spacing={2}>
          {resourcePerms.mine.canCreate && (
            <>
              <Button
                leftIcon={<Sparkles size={14} />}
                size="sm"
                background="linear-gradient(135deg, #7c3aed 0%, #a855f7 100%)"
                color="white"
                _hover={{ background: 'linear-gradient(135deg, #6d28d9 0%, #9333ea 100%)' }}
                boxShadow="0 1px 4px rgba(124, 58, 237, 0.35)"
                border="none"
                onClick={onAiImport}
              >
                Importuj z AI
              </Button>
              <Button
                leftIcon={<Plus size={18} />}
                colorScheme="primary"
                size="sm"
                onClick={onAddCost}
              >
                Dodaj koszt
              </Button>
            </>
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
            <Box key={cost.id}>
              <ExpenseCard
                cost={cost}
                canEdit={resourcePerms.mine.canEdit}
                canDelete={resourcePerms.mine.canDelete}
                isDeleting={deletingCostId === cost.id}
                onEdit={() => onEditCost(cost)}
                onDelete={() => onDeleteCost(cost.id)}
              />
              {cost.approvalStatus === 'Draft' && (
                <Button
                  size="xs"
                  colorScheme="orange"
                  leftIcon={<Check size={12} />}
                  onClick={() => onSubmitForApproval(cost.id)}
                  isLoading={submittingCostId === cost.id}
                  mt={1}
                >
                  Skieruj do akceptacji
                </Button>
              )}
              {cost.approvalStatus === 'PendingApproval' && (
                <Button
                  size="xs"
                  colorScheme="gray"
                  leftIcon={<X size={12} />}
                  onClick={() => onWithdrawFromApproval(cost.id)}
                  isLoading={withdrawingCostId === cost.id}
                  mt={1}
                >
                  Wycofaj
                </Button>
              )}
            </Box>
          ))}
        </VStack>
      ) : (
        <Box overflowX="auto" bg="white" p={4} rounded="lg" borderWidth="1px" borderColor="neutral.200">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Kontrahent</Th>
                <Th>Data</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Status</Th>
                <Th textAlign="center">Dokument</Th>
                <Th textAlign="center">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {costs.map((cost) => (
                <Tr key={cost.id} _hover={{ bg: 'neutral.50' }} cursor="pointer" onClick={() => onEditCost(cost)}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td>{cost.contractorName || "-"}</Td>
                  <Td>{formatDate(cost.date, false)}</Td>
                  <Td isNumeric>{formatCurrency(cost.net ?? 0)}</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.gross ?? 0)}</Td>
                  <Td textAlign="center">
                    <ApprovalStatusBadge status={cost.approvalStatus} />
                  </Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <DocumentCell cost={cost} />
                  </Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <HStack spacing={1} justify="center">
                      {cost.approvalStatus === 'Draft' && (
                        <Tooltip label="Skieruj do akceptacji">
                          <IconButton
                            aria-label="Skieruj do akceptacji"
                            icon={<Check size={14} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="orange"
                            onClick={() => onSubmitForApproval(cost.id)}
                            isLoading={submittingCostId === cost.id}
                          />
                        </Tooltip>
                      )}
                      {cost.approvalStatus === 'PendingApproval' && (
                        <Tooltip label="Wycofaj">
                          <IconButton
                            aria-label="Wycofaj z akceptacji"
                            icon={<X size={14} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="gray"
                            onClick={() => onWithdrawFromApproval(cost.id)}
                            isLoading={withdrawingCostId === cost.id}
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

interface PendingApprovalTabProps {
  costs: ProjectCostListItemWeb[];
  loading: boolean;
  approvingCostId: string | null;
  rejectingCostId: string | null;
  onApproveCost: (costId: string) => void;
  onRejectCost: (costId: string) => void;
}

function PendingApprovalTab({
  costs,
  loading,
  approvingCostId,
  rejectingCostId,
  onApproveCost,
  onRejectCost,
}: PendingApprovalTabProps) {
  const viewMode = useBreakpointValue({ base: "mobile", md: "desktop" });

  if (loading) return <LoadingSpinner />;

  return (
    <VStack spacing={4} align="stretch">
      <Text fontSize="sm" color="neutral.600">
        Koszty oczekujące na akceptację
      </Text>

      {costs.length === 0 ? (
        <EmptyState
          icon={DollarSign}
          title="Brak kosztów do akceptacji"
          description="Nie ma żadnych kosztów oczekujących na akceptację"
        />
      ) : viewMode === "mobile" ? (
        <VStack spacing={3} align="stretch">
          {costs.map((cost) => (
            <Box key={cost.id}>
              <ExpenseCard cost={cost} showOwner />
              <HStack mt={1} spacing={2} justify="flex-end">
                <Button
                  size="xs"
                  colorScheme="green"
                  leftIcon={<Check size={12} />}
                  onClick={() => onApproveCost(cost.id)}
                  isLoading={approvingCostId === cost.id}
                  isDisabled={rejectingCostId === cost.id}
                >
                  Akceptuj
                </Button>
                <Button
                  size="xs"
                  colorScheme="red"
                  leftIcon={<X size={12} />}
                  onClick={() => onRejectCost(cost.id)}
                  isLoading={rejectingCostId === cost.id}
                  isDisabled={approvingCostId === cost.id}
                >
                  Odrzuć
                </Button>
              </HStack>
            </Box>
          ))}
        </VStack>
      ) : (
        <Box overflowX="auto" bg="white" p={4} rounded="lg" borderWidth="1px" borderColor="neutral.200">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Właściciel</Th>
                <Th>Kontrahent</Th>
                <Th>Data</Th>
                <Th isNumeric>Netto</Th>
                <Th isNumeric>Brutto</Th>
                <Th textAlign="center">Dokument</Th>
                <Th textAlign="center">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {costs.map((cost) => (
                <Tr key={cost.id} _hover={{ bg: 'neutral.50' }}>
                  <Td fontWeight="medium">{cost.name}</Td>
                  <Td fontSize="sm" color="neutral.600">{cost.userName || "-"}</Td>
                  <Td>{cost.contractorName || "-"}</Td>
                  <Td>{formatDate(cost.date, false)}</Td>
                  <Td isNumeric>{formatCurrency(cost.net ?? 0)}</Td>
                  <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.gross ?? 0)}</Td>
                  <Td textAlign="center">
                    <DocumentCell cost={cost} />
                  </Td>
                  <Td textAlign="center" onClick={(e) => e.stopPropagation()}>
                    <HStack spacing={1} justify="center">
                      <Tooltip label="Akceptuj">
                        <IconButton
                          aria-label="Akceptuj koszt"
                          icon={<Check size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="green"
                          onClick={() => onApproveCost(cost.id)}
                          isLoading={approvingCostId === cost.id}
                          isDisabled={rejectingCostId === cost.id}
                        />
                      </Tooltip>
                      <Tooltip label="Odrzuć">
                        <IconButton
                          aria-label="Odrzuć koszt"
                          icon={<X size={14} />}
                          size="xs"
                          variant="ghost"
                          colorScheme="red"
                          onClick={() => onRejectCost(cost.id)}
                          isLoading={rejectingCostId === cost.id}
                          isDisabled={approvingCostId === cost.id}
                        />
                      </Tooltip>
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

// === MAIN COMPONENT ===

export default function ProjectSimpleCosts() {
  const { projectId } = useParams<{ projectId: string }>();
  const queryClient = useQueryClient();
  const { user } = useContext(AuthContext);
  const { showError, showApiSuccess } = useToastNotification();

  const [deletingCostId, setDeletingCostId] = useState<string | null>(null);
  const [editingClosedCostId, setEditingClosedCostId] = useState<string | null>(null);
  const [savingClosedCost, setSavingClosedCost] = useState(false);

  const [submittingCostId, setSubmittingCostId] = useState<string | null>(null);
  const [withdrawingCostId, setWithdrawingCostId] = useState<string | null>(null);
  const [approvingCostId, setApprovingCostId] = useState<string | null>(null);
  const [rejectingCostId, setRejectingCostId] = useState<string | null>(null);

  // Modal state for add/edit expense
  const [editingCost, setEditingCost] = useState<ProjectCostListItemWeb | null>(null);
  const [isAiImportOpen, setIsAiImportOpen] = useState(false);
  const [aiPrefillData, setAiPrefillData] = useState<{ parsedData: ParsedCostDto; file: File } | null>(null);
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

  const resourcePerms = useResourcePermissions(projectId, "costs");
  const queriesReady = !resourcePerms.raw.loading && Boolean(user?.activeTenantId && projectId);

  const allCostsQuery = useProjectCostsByScope(
    user?.activeTenantId ?? undefined,
    projectId,
    ResourceScope.All,
    queriesReady && resourcePerms.tabs.showAll,
  );
  const myCostsQuery = useProjectCostsByScope(
    user?.activeTenantId ?? undefined,
    projectId,
    ResourceScope.Mine,
    queriesReady && resourcePerms.tabs.showMine,
  );
  const pendingCostsQuery = useProjectCostsByScope(
    user?.activeTenantId ?? undefined,
    projectId,
    ResourceScope.PendingApproval,
    queriesReady && resourcePerms.tabs.showPendingApproval,
  );

  const { data: project } = useProjectDetails(
    user?.activeTenantId ?? undefined,
    projectId
  );

  const loading = resourcePerms.raw.loading || (
    queriesReady && (
      (resourcePerms.tabs.showAll && allCostsQuery.isPending) ||
      (resourcePerms.tabs.showMine && myCostsQuery.isPending) ||
      (resourcePerms.tabs.showPendingApproval && pendingCostsQuery.isPending)
    )
  );

  const { updateCost, deleteCost, submitCostForApproval, withdrawCostFromApproval, approveCost, rejectCost } = useProjectCostMutations(
    user?.activeTenantId ?? '',
    projectId ?? ''
  );

  const refreshData = (): void => {
    if (!user?.activeTenantId || !projectId) {
      return;
    }
    void invalidateProjectCostLists(queryClient, user.activeTenantId, projectId);
  };

  const findCostById = (costId: string): ProjectCostListItemWeb | undefined => {
    return allCostsQuery.data?.find((c) => c.id === costId)
      ?? myCostsQuery.data?.find((c) => c.id === costId)
      ?? pendingCostsQuery.data?.find((c) => c.id === costId);
  };

  // ── Modal handlers ────────────────────────────────────────

  const handleOpenAddModal = () => {
    setEditingCost(null);
    onExpenseModalOpen();
  };

  const handleOpenEditModal = (cost: ProjectCostListItemWeb) => {
    setEditingCost(cost);
    onExpenseModalOpen();
  };

  const handleCloseModal = () => {
    onExpenseModalClose();
    setEditingCost(null);
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
      await deleteCost(costToDelete);
      showApiSuccess('costDeleted');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas usuwania kosztu");
    } finally {
      setDeletingCostId(null);
      setCostToDelete(null);
    }
  };

  const handleToggleCostClosed = async (costId: string, currentIsClosed: boolean) => {
    if (!user?.activeTenantId || !projectId) return;

    setEditingClosedCostId(costId);
    setSavingClosedCost(true);
    try {
      const cost = findCostById(costId);
      if (!cost) {
        showError("Nie znaleziono kosztu");
        return;
      }

      await updateCost(costId, {
        name: cost.name,
        number: cost.number ?? undefined,
        contractorId: cost.contractorId ?? undefined,
        date: new Date(cost.date),
        description: cost.description || undefined,
        net: cost.net ?? null,
        gross: cost.gross ?? null,
        removeDocument: false,
      });

      showApiSuccess('statusUpdated');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas aktualizacji statusu");
    } finally {
      setEditingClosedCostId(null);
      setSavingClosedCost(false);
    }
  };

  const handleSubmitForApproval = async (costId: string) => {
    if (!user?.activeTenantId || !projectId) return;
    setSubmittingCostId(costId);
    try {
      await submitCostForApproval(costId);
      showApiSuccess('statusUpdated');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas wysyłania do akceptacji");
    } finally {
      setSubmittingCostId(null);
    }
  };

  const handleApproveCost = async (costId: string) => {
    if (!user?.activeTenantId || !projectId) return;
    setApprovingCostId(costId);
    try {
      await approveCost(costId);
      showApiSuccess('statusUpdated');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas akceptacji kosztu");
    } finally {
      setApprovingCostId(null);
    }
  };

  const handleRejectCost = async (costId: string) => {
    if (!user?.activeTenantId || !projectId) return;
    setRejectingCostId(costId);
    try {
      await rejectCost(costId);
      showApiSuccess('statusUpdated');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas odrzucania kosztu");
    } finally {
      setRejectingCostId(null);
    }
  };

  const handleWithdrawFromApproval = async (costId: string) => {
    if (!user?.activeTenantId || !projectId) return;
    setWithdrawingCostId(costId);
    try {
      await withdrawCostFromApproval(costId);
      showApiSuccess('statusUpdated');
      refreshData();
    } catch {
      showError("Wystąpił błąd podczas wycofywania z akceptacji");
    } finally {
      setWithdrawingCostId(null);
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
        <BackToProjectButton />
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={DollarSign} boxSize={8} color="red.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Wydatki</Heading>
              {project && <Text fontSize="sm" color="neutral.600">{project.name}</Text>}
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
                    <Badge colorScheme="level2" ml={2}>{allCostsQuery.data?.length ?? 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showMine && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={DollarSign} boxSize={4} />
                    <Text>Moje</Text>
                    <Badge colorScheme="primary" ml={2}>{myCostsQuery.data?.length ?? 0}</Badge>
                  </HStack>
                </Tab>
              )}
              {resourcePerms.tabs.showPendingApproval && (
                <Tab fontWeight="bold">
                  <HStack spacing={2}>
                    <Icon as={Clock} boxSize={4} />
                    <Text>Do akceptacji</Text>
                    <Badge colorScheme="orange" ml={2}>{pendingCostsQuery.data?.length ?? 0}</Badge>
                  </HStack>
                </Tab>
              )}

            </TabList>

            <TabPanels>
              {resourcePerms.tabs.showAll && (
                <TabPanel>
                  <AllCostsTab
                    costs={allCostsQuery.data ?? []}
                    loading={allCostsQuery.isPending}
                    resourcePerms={resourcePerms}
                    deletingCostId={deletingCostId}
                    onAddCost={handleOpenAddModal}
                    onAiImport={() => setIsAiImportOpen(true)}
                    onEditCost={handleOpenEditModal}
                    onDeleteCost={handleDeleteCost}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showMine && (
                <TabPanel>
                  <MyCostsTab
                    costs={myCostsQuery.data ?? []}
                    loading={myCostsQuery.isPending}
                    deletingCostId={deletingCostId}
                    submittingCostId={submittingCostId}
                    withdrawingCostId={withdrawingCostId}
                    resourcePerms={resourcePerms}
                    onAddCost={handleOpenAddModal}
                    onAiImport={() => setIsAiImportOpen(true)}
                    onEditCost={handleOpenEditModal}
                    onDeleteCost={handleDeleteCost}
                    onSubmitForApproval={handleSubmitForApproval}
                    onWithdrawFromApproval={handleWithdrawFromApproval}
                  />
                </TabPanel>
              )}
              {resourcePerms.tabs.showPendingApproval && (
                <TabPanel>
                  <PendingApprovalTab
                    costs={pendingCostsQuery.data ?? []}
                    loading={pendingCostsQuery.isPending}
                    approvingCostId={approvingCostId}
                    rejectingCostId={rejectingCostId}
                    onApproveCost={handleApproveCost}
                    onRejectCost={handleRejectCost}
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

        {/* MODAL: Importuj koszt z AI */}
        {isAiImportOpen && user?.activeTenantId && projectId && (
          <AICostImportModal
            isOpen
            onClose={() => setIsAiImportOpen(false)}
            tenantId={user.activeTenantId}
            projectId={projectId}
            costType="ProjectCost"
            onParsed={(data, file) => {
              setAiPrefillData({ parsedData: data, file });
              setIsAiImportOpen(false);
              setEditingCost(null);
              onExpenseModalOpen();
            }}
          />
        )}

        {/* MODAL: Dodaj / Edytuj koszt */}
        {isExpenseModalOpen && user?.activeTenantId && projectId && (
          <CostModal
            type="project"
            tenantId={user.activeTenantId}
            projectId={projectId}
            mode={editingCost ? 'edit' : 'create'}
            cost={editingCost ?? undefined}
            aiPrefill={!editingCost ? (aiPrefillData ?? undefined) : undefined}
            onSuccess={() => {
              showApiSuccess(editingCost ? 'costUpdated' : 'costAdded');
              handleCloseModal();
              setAiPrefillData(null);
              refreshData();
            }}
            onClose={() => {
              handleCloseModal();
              setAiPrefillData(null);
            }}
          />
        )}

        {/* ALERT: Potwierdzenie usunięcia kosztu */}
        <DeleteAlertDialog
          isOpen={isDeleteAlertOpen}
          onClose={onDeleteAlertClose}
          onConfirm={confirmDeleteCost}
          isLoading={deletingCostId !== null}
        />

      </Box>
    </MainLayout>
  );
}
