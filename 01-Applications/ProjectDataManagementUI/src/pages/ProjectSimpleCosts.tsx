import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
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
  Input,
  Textarea,
  useDisclosure,
  Badge,
  Icon,
} from "@chakra-ui/react";
import { ArrowLeft, Plus, Share2, Edit2, Trash2, DollarSign } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { projectApi } from "../api/projectApi";
import { AuthContext } from "../context/AuthContext";
import { useContext } from "react";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate, formatCurrency } from "../utils/formatters";
import ShareCostModal from "../components/ShareCostModal";
import type { ProjectCostListItemWeb, SharedProjectCostWeb } from "../types/project.types";

export default function ProjectSimpleCosts() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showSuccess, showError } = useToastNotification();

  const [projectCosts, setProjectCosts] = useState<ProjectCostListItemWeb[]>([]);
  const [sharedCosts, setSharedCosts] = useState<SharedProjectCostWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingShared, setLoadingShared] = useState(false);
  const [projectName, setProjectName] = useState("");
  const [showNewCostRow, setShowNewCostRow] = useState(false);
  const [addingNewCost, setAddingNewCost] = useState(false);
  const [editingCostId, setEditingCostId] = useState<string | null>(null);
  const [editingCostData, setEditingCostData] = useState<any>(null);
  const [savingCost, setSavingCost] = useState(false);
  const [deletingCostId, setDeletingCostId] = useState<string | null>(null);
  const [costToShare, setCostToShare] = useState<ProjectCostListItemWeb | null>(null);

  const { isOpen: isShareModalOpen, onOpen: onShareModalOpen, onClose: onShareModalClose } = useDisclosure();

  const [newCostData, setNewCostData] = useState({
    name: '',
    place: '',
    date: new Date().toISOString().split('T')[0],
    description: '',
    netAmount: '',
    vatRate: '',
    grossAmount: '',
  });

  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  useEffect(() => {
    if (projectId && user?.activeTenantId) {
      fetchProjectDetails();
      fetchProjectCosts();
      fetchSharedCosts();
    }
  }, [projectId, user?.activeTenantId]);

  const fetchProjectDetails = async () => {
    if (!user?.activeTenantId || !projectId) return;

    try {
      const response = await projectApi.getProjectDetails(user.activeTenantId, projectId);
      setProjectName(response.data.name);
    } catch (error) {
      console.error("Błąd podczas pobierania projektu:", error);
    }
  };

  const fetchProjectCosts = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    try {
      const response = await projectApi.getProjectUserCosts(user.activeTenantId, projectId);
      setProjectCosts(response.data);
    } catch (error) {
      console.error("Błąd podczas pobierania kosztów:", error);
      showError("Wystąpił błąd podczas pobierania kosztów");
    } finally {
      setLoading(false);
    }
  };

  const fetchSharedCosts = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoadingShared(true);
    try {
      const response = await projectApi.getSharedProjectCosts(user.activeTenantId, projectId);
      setSharedCosts(response.data);
    } catch (error) {
      console.error("Błąd podczas pobierania udostępnionych kosztów:", error);
    } finally {
      setLoadingShared(false);
    }
  };

  const handleAddCost = async () => {
    if (!user?.activeTenantId || !projectId) return;
    if (!newCostData.name.trim() || !newCostData.grossAmount) {
      showError("Nazwa i kwota brutto są wymagane");
      return;
    }

    setAddingNewCost(true);
    try {
      await projectApi.createProjectCost(
        user.activeTenantId,
        projectId,
        {
          name: newCostData.name,
          place: newCostData.place || undefined,
          date: new Date(newCostData.date),
          description: newCostData.description || undefined,
          netAmount: newCostData.netAmount ? parseFloat(newCostData.netAmount) : undefined,
          vatRate: newCostData.vatRate ? parseFloat(newCostData.vatRate) : undefined,
          grossAmount: parseFloat(newCostData.grossAmount),
        }
      );

      showSuccess("Koszt został dodany");
      setNewCostData({
        name: '',
        place: '',
        date: new Date().toISOString().split('T')[0],
        description: '',
        netAmount: '',
        vatRate: '',
        grossAmount: '',
      });
      setShowNewCostRow(false);
      fetchProjectCosts();
    } catch (error) {
      console.error("Błąd podczas dodawania kosztu:", error);
      showError("Wystąpił błąd podczas dodawania kosztu");
    } finally {
      setAddingNewCost(false);
    }
  };

  const handleEditCost = (cost: ProjectCostListItemWeb) => {
    setEditingCostId(cost.id);
    setEditingCostData({
      name: cost.name,
      place: cost.place || '',
      date: cost.date.split('T')[0],
      description: cost.description || '',
      netAmount: (cost.netAmount ?? 0).toString(),
      vatRate: (cost.vatRate ?? 0).toString(),
      grossAmount: cost.grossAmount.toString(),
    });
  };

  const handleSaveEdit = async () => {
    if (!user?.activeTenantId || !projectId || !editingCostId) return;

    setSavingCost(true);
    try {
      await projectApi.updateProjectCost(
        user.activeTenantId,
        projectId,
        editingCostId,
        {
          name: editingCostData.name,
          place: editingCostData.place || undefined,
          date: new Date(editingCostData.date),
          description: editingCostData.description || undefined,
          netAmount: parseFloat(editingCostData.netAmount),
          vatRate: parseFloat(editingCostData.vatRate),
          grossAmount: parseFloat(editingCostData.grossAmount),
          removeDocument: false,
        }
      );

      showSuccess("Koszt został zaktualizowany");
      setEditingCostId(null);
      setEditingCostData(null);
      fetchProjectCosts();
    } catch (error) {
      console.error("Błąd podczas aktualizacji kosztu:", error);
      showError("Wystąpił błąd podczas aktualizacji kosztu");
    } finally {
      setSavingCost(false);
    }
  };

  const handleDeleteCost = async (costId: string) => {
    if (!user?.activeTenantId || !projectId) return;
    if (!confirm("Czy na pewno chcesz usunąć ten koszt?")) return;

    setDeletingCostId(costId);
    try {
      await projectApi.deleteProjectCost(user.activeTenantId, projectId, costId);

      showSuccess("Koszt został usunięty");
      fetchProjectCosts();
    } catch (error) {
      console.error("Błąd podczas usuwania kosztu:", error);
      showError("Wystąpił błąd podczas usuwania kosztu");
    } finally {
      setDeletingCostId(null);
    }
  };

  const handleShareCost = (cost: ProjectCostListItemWeb) => {
    setCostToShare(cost);
    onShareModalOpen();
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
        <Button
          leftIcon={<ArrowLeft size={18} />}
          variant="ghost"
          mb={6}
          onClick={() => navigate(`/projects/${projectId}`)}
        >
          Wróć do projektu
        </Button>

        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={DollarSign} boxSize={8} color="red.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Koszty projektu</Heading>
              {projectName && <Text fontSize="sm" color="gray.600">{projectName}</Text>}
            </VStack>
          </HStack>
          <Button
            leftIcon={<Plus size={18} />}
            colorScheme="blue"
            onClick={() => setShowNewCostRow(true)}
            isDisabled={showNewCostRow}
          >
            Dodaj koszt
          </Button>
        </HStack>

        <Tabs colorScheme="blue" variant="enclosed">
          <TabList>
            <Tab fontWeight="bold">
              <HStack spacing={2}>
                <Icon as={DollarSign} boxSize={4} />
                <Text>Moje koszty</Text>
                <Badge colorScheme="blue" ml={2}>{projectCosts.length}</Badge>
              </HStack>
            </Tab>
            <Tab fontWeight="bold">
              <HStack spacing={2}>
                <Icon as={Share2} boxSize={4} />
                <Text>Udostępnione</Text>
                <Badge colorScheme="teal" ml={2}>{sharedCosts.length}</Badge>
              </HStack>
            </Tab>
          </TabList>

          <TabPanels>
            {/* TAB 1: MOJE KOSZTY */}
            <TabPanel>
              <VStack spacing={4} align="stretch">
                <Text fontSize="sm" color="gray.600">
                  Proste koszty projektu - faktury, paragony, wydatki
                </Text>

                {projectCosts.length === 0 && !showNewCostRow ? (
                  <EmptyState
                    icon={DollarSign}
                    title="Brak kosztów"
                    description="Dodaj pierwszy koszt do projektu"
                  />
                ) : (
                  <Box overflowX="auto" bg={bgColor} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
                    <Table size="sm" variant="simple">
                      <Thead>
                        <Tr>
                          <Th>Nazwa</Th>
                          <Th>Miejsce</Th>
                          <Th>Data</Th>
                          <Th>Opis</Th>
                          <Th isNumeric>Netto</Th>
                          <Th isNumeric>VAT %</Th>
                          <Th isNumeric>Brutto</Th>
                          <Th textAlign="center">Akcje</Th>
                        </Tr>
                      </Thead>
                      <Tbody>
                        {showNewCostRow && (
                          <Tr bg={useColorModeValue("blue.50", "blue.900")}>
                            <Td><Input size="sm" value={newCostData.name} onChange={(e) => setNewCostData({ ...newCostData, name: e.target.value })} placeholder="Nazwa" /></Td>
                            <Td><Input size="sm" value={newCostData.place} onChange={(e) => setNewCostData({ ...newCostData, place: e.target.value })} placeholder="Miejsce" /></Td>
                            <Td><Input size="sm" type="date" value={newCostData.date} onChange={(e) => setNewCostData({ ...newCostData, date: e.target.value })} /></Td>
                            <Td><Textarea size="sm" value={newCostData.description} onChange={(e) => setNewCostData({ ...newCostData, description: e.target.value })} placeholder="Opis" rows={2} /></Td>
                            <Td><Input size="sm" type="number" step="0.01" value={newCostData.netAmount} onChange={(e) => setNewCostData({ ...newCostData, netAmount: e.target.value })} placeholder="0.00" /></Td>
                            <Td><Input size="sm" type="number" step="0.01" value={newCostData.vatRate} onChange={(e) => setNewCostData({ ...newCostData, vatRate: e.target.value })} placeholder="0" /></Td>
                            <Td><Input size="sm" type="number" step="0.01" value={newCostData.grossAmount} onChange={(e) => setNewCostData({ ...newCostData, grossAmount: e.target.value })} placeholder="0.00" /></Td>
                            <Td textAlign="center">
                              <HStack spacing={1} justify="center">
                                <Button size="sm" colorScheme="green" onClick={handleAddCost} isLoading={addingNewCost}>Zapisz</Button>
                                <Button size="sm" variant="ghost" onClick={() => setShowNewCostRow(false)}>Anuluj</Button>
                              </HStack>
                            </Td>
                          </Tr>
                        )}

                        {projectCosts.map((cost) =>
                          editingCostId === cost.id ? (
                            <Tr key={cost.id} bg={useColorModeValue("yellow.50", "yellow.900")}>
                              <Td><Input size="sm" value={editingCostData.name} onChange={(e) => setEditingCostData({ ...editingCostData, name: e.target.value })} /></Td>
                              <Td><Input size="sm" value={editingCostData.place} onChange={(e) => setEditingCostData({ ...editingCostData, place: e.target.value })} /></Td>
                              <Td><Input size="sm" type="date" value={editingCostData.date} onChange={(e) => setEditingCostData({ ...editingCostData, date: e.target.value })} /></Td>
                              <Td><Textarea size="sm" value={editingCostData.description} onChange={(e) => setEditingCostData({ ...editingCostData, description: e.target.value })} rows={2} /></Td>
                              <Td><Input size="sm" type="number" step="0.01" value={editingCostData.netAmount} onChange={(e) => setEditingCostData({ ...editingCostData, netAmount: e.target.value })} /></Td>
                              <Td><Input size="sm" type="number" step="0.01" value={editingCostData.vatRate} onChange={(e) => setEditingCostData({ ...editingCostData, vatRate: e.target.value })} /></Td>
                              <Td><Input size="sm" type="number" step="0.01" value={editingCostData.grossAmount} onChange={(e) => setEditingCostData({ ...editingCostData, grossAmount: e.target.value })} /></Td>
                              <Td textAlign="center">
                                <HStack spacing={1} justify="center">
                                  <Button size="sm" colorScheme="green" onClick={handleSaveEdit} isLoading={savingCost}>Zapisz</Button>
                                  <Button size="sm" variant="ghost" onClick={() => { setEditingCostId(null); setEditingCostData(null); }}>Anuluj</Button>
                                </HStack>
                              </Td>
                            </Tr>
                          ) : (
                            <Tr key={cost.id} _hover={{ bg: useColorModeValue("gray.50", "gray.700") }}>
                              <Td fontWeight="medium">{cost.name}</Td>
                              <Td>{cost.place || "-"}</Td>
                              <Td>{formatDate(cost.date)}</Td>
                              <Td>{cost.description || "-"}</Td>
                              <Td isNumeric>{formatCurrency(cost.netAmount ?? 0)}</Td>
                              <Td isNumeric>{cost.vatRate ?? 0}%</Td>
                              <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.grossAmount)}</Td>
                              <Td textAlign="center">
                                <HStack spacing={1} justify="center">
                                  <IconButton aria-label="Edytuj" icon={<Edit2 size={14} />} size="xs" variant="ghost" onClick={() => handleEditCost(cost)} />
                                  <IconButton aria-label="Udostępnij" icon={<Share2 size={14} />} size="xs" variant="ghost" colorScheme="blue" onClick={() => handleShareCost(cost)} />
                                  <IconButton aria-label="Usuń" icon={<Trash2 size={14} />} size="xs" variant="ghost" colorScheme="red" onClick={() => handleDeleteCost(cost.id)} isLoading={deletingCostId === cost.id} />
                                </HStack>
                              </Td>
                            </Tr>
                          )
                        )}
                      </Tbody>
                    </Table>
                  </Box>
                )}
              </VStack>
            </TabPanel>

            {/* TAB 2: UDOSTĘPNIONE KOSZTY */}
            <TabPanel>
              <VStack spacing={4} align="stretch">
                <Text fontSize="sm" color="gray.600">
                  Koszty udostępnione przez innych członków projektu
                </Text>

                {loadingShared ? (
                  <LoadingSpinner message="Ładowanie udostępnionych kosztów..." />
                ) : sharedCosts.length === 0 ? (
                  <EmptyState
                    icon={Share2}
                    title="Brak udostępnionych kosztów"
                    description="Nikt jeszcze nie udostępnił Ci kosztów w tym projekcie"
                  />
                ) : (
                  <Box overflowX="auto" bg={bgColor} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
                    <Table size="sm" variant="simple">
                      <Thead>
                        <Tr>
                          <Th>Nazwa</Th>
                          <Th>Miejsce</Th>
                          <Th>Data</Th>
                          <Th>Opis</Th>
                          <Th isNumeric>Netto</Th>
                          <Th isNumeric>VAT %</Th>
                          <Th isNumeric>Brutto</Th>
                          <Th>Udostępnione przez</Th>
                        </Tr>
                      </Thead>
                      <Tbody>
                        {sharedCosts.map((cost) => (
                          <Tr key={cost.id} _hover={{ bg: useColorModeValue("gray.50", "gray.700") }}>
                            <Td fontWeight="medium">{cost.costName}</Td>
                            <Td>{cost.costPlace || "-"}</Td>
                            <Td>{formatDate(cost.costDate)}</Td>
                            <Td>{cost.costDescription || "-"}</Td>
                            <Td isNumeric>{formatCurrency(cost.costNetAmount ?? 0)}</Td>
                            <Td isNumeric>{cost.costVatRate ?? 0}%</Td>
                            <Td isNumeric fontWeight="bold" color="green.600">{formatCurrency(cost.costGrossAmount)}</Td>
                            <Td>{cost.sharedByUserName}</Td>
                          </Tr>
                        ))}
                      </Tbody>
                    </Table>
                  </Box>
                )}
              </VStack>
            </TabPanel>
          </TabPanels>
        </Tabs>

        <Box mt={6} p={4} bg="blue.50" rounded="md" borderWidth="1px" borderColor="blue.200">
          <Text fontSize="sm" color="blue.800">
            💡 <strong>Wskazówka:</strong> To są proste koszty projektu (faktury, paragony). Dla zaawansowanych kosztorysów według szablonów przejdź do zakładki "Kosztorysy".
          </Text>
        </Box>

        {/* MODAL: SHARE COST */}
        {costToShare && user?.activeTenantId && projectId && (
          <ShareCostModal
            isOpen={isShareModalOpen}
            onClose={() => {
              onShareModalClose();
              setCostToShare(null);
            }}
            tenantId={user.activeTenantId}
            projectId={projectId}
            cost={costToShare}
            onCostShared={() => {
              fetchProjectCosts();
            }}
          />
        )}
      </Box>
    </MainLayout>
  );
}
