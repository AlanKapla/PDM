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
import { projectApi } from "../api/projectApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { formatDate } from "../utils/formatters";
import CreateCostEstimateModal from "../components/CreateCostEstimateModal";
import CopyCostEstimateModal from "../components/CopyCostEstimateModal";
import type { CostEstimateListItem, CostEstimateStatus } from "../types/costEstimate.types";
import { useProjectPermissions } from "../hooks/useProjectPermissions";

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
  const [myCostEstimates, setMyCostEstimates] = useState<CostEstimateListItem[]>([]);
  const [sharedCostEstimates, setSharedCostEstimates] = useState<CostEstimateListItem[]>([]);
  const [costEstimateToCopy, setCostEstimateToCopy] = useState<CostEstimateListItem | null>(null);

  const { isOpen: isCreateModalOpen, onOpen: onCreateModalOpen, onClose: onCreateModalClose } = useDisclosure();
  const { isOpen: isCopyModalOpen, onOpen: onCopyModalOpen, onClose: onCopyModalClose } = useDisclosure();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  const permissions = useProjectPermissions(projectId);

  useEffect(() => {
    fetchData();
  }, [projectId, permissions.hasAnyResourceAccess]);

  const fetchData = async () => {
    console.log("=== fetchData CALLED ===");
    console.log("user?.activeTenantId:", user?.activeTenantId);
    console.log("projectId:", projectId);
    
    if (!user?.activeTenantId || !projectId) {
      console.log("Missing tenantId or projectId, returning early");
      return;
    }

    setLoading(true);
    try {
      console.log("Fetching project details and cost estimates...");
      const projectResponse = await projectApi.getProjectDetails(user.activeTenantId, projectId);
      setProject(projectResponse.data);
      
      if (permissions.hasAnyResourceAccess) {
        const costEstimates = await costEstimateApi.getCostEstimates(user.activeTenantId, projectId);
        console.log("Data fetched successfully:", { projectResponse, costEstimates });

        // Filter my cost estimates (owned by current user)
        const myCosts = costEstimates.filter((ce) => ce.ownerId === user?.id);
        setMyCostEstimates(myCosts);
      }

      // Shared cost estimates (TODO: implement sharing logic)
      // For now, empty array - will be implemented later
      setSharedCostEstimates([]);
    } catch (error: any) {
      console.error('Error fetching data:', error);
      showError('Nie udało się załadować danych', error?.message || 'Wystąpił nieoczekiwany błąd');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteCostEstimate = async (costEstimateId: string) => {
    if (!user?.activeTenantId || !projectId) return;
    if (!confirm("Czy na pewno chcesz usunąć ten kosztorys?")) return;

    try {
      await costEstimateApi.deleteCostEstimate(user.activeTenantId, projectId, costEstimateId);
      showSuccess("Kosztorys został usunięty");
      fetchData();
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
          {permissions.canWriteResources && (
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="blue"
              onClick={onCreateModalOpen}
            >
              Nowy kosztorys
            </Button>
          )}
        </HStack>

        {!project || !permissions.canReadResources ? (
          <Box p={8} textAlign="center">
            <EmptyState
              icon={FileText}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania kosztorysów w tym projekcie"
            />
          </Box>
        ) : (
        <Tabs colorScheme="blue" variant="enclosed">
          <TabList>
            {permissions.canWriteResources && (
              <Tab fontWeight="bold">
                <HStack spacing={2}>
                  <Icon as={FileText} boxSize={4} />
                  <Text>Moje kosztorysy</Text>
                  <Badge colorScheme="blue" ml={2}>{myCostEstimates.length}</Badge>
                </HStack>
              </Tab>
            )}
            {(permissions.canReadSharedResources || permissions.canWriteSharedResources) && (
              <Tab fontWeight="bold">
                <HStack spacing={2}>
                  <Icon as={FileText} boxSize={4} />
                  <Text>Udostępnione</Text>
                  <Badge colorScheme="teal" ml={2}>{sharedCostEstimates.length}</Badge>
                </HStack>
              </Tab>
            )}
          </TabList>

          <TabPanels>
            {/* TAB 1: MOJE KOSZTORYSY */}
            {permissions.canWriteResources && (
            <TabPanel>
              <VStack spacing={4} align="stretch">
                <Text fontSize="sm" color="gray.600">
                  Twoje kosztorysy w projekcie - widoczne tylko dla Ciebie
                </Text>

                {myCostEstimates.length === 0 ? (
                  <EmptyState
                    icon={FileText}
                    title="Brak kosztorysów"
                    description="Utwórz swój pierwszy kosztorys na podstawie szablonu"
                  />
                ) : (
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
                        {myCostEstimates.map((costEstimate) => (
                          <Tr key={costEstimate.id} _hover={{ bg: useColorModeValue("gray.50", "gray.700") }}>
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
                                <IconButton
                                  aria-label="Usuń"
                                  icon={<Trash2 size={14} />}
                                  size="xs"
                                  colorScheme="red"
                                  variant="ghost"
                                  onClick={() => handleDeleteCostEstimate(costEstimate.id)}
                                />
                              </HStack>
                            </Td>
                          </Tr>
                        ))}
                      </Tbody>
                    </Table>
                  </Box>
                )}
              </VStack>
            </TabPanel>
            )}

            {/* TAB 2: UDOSTĘPNIONE KOSZTORYSY */}
            {(permissions.canReadSharedResources || permissions.canWriteSharedResources) && (
            <TabPanel>
              <VStack spacing={4} align="stretch">
                <Text fontSize="sm" color="gray.600">
                  Kosztorysy udostępnione przez innych członków projektu
                </Text>

                {sharedCostEstimates.length === 0 ? (
                  <EmptyState
                    icon={FileText}
                    title="Brak udostępnionych kosztorysów"
                    description="Nikt jeszcze nie udostępnił Ci kosztorysów w tym projekcie"
                  />
                ) : (
                  <Box overflowX="auto" bg={cardBg} p={4} rounded="lg" borderWidth="1px" borderColor={borderColor}>
                    <Table size="sm" variant="simple">
                      <Thead>
                        <Tr>
                          <Th>Nazwa</Th>
                          <Th>Właściciel</Th>
                          <Th>Szablon</Th>
                          <Th>Status</Th>
                          <Th isNumeric>Wartość netto</Th>
                          <Th isNumeric>Wartość brutto</Th>
                          <Th>Utworzony</Th>
                          <Th textAlign="center">Akcje</Th>
                        </Tr>
                      </Thead>
                      <Tbody>
                        {sharedCostEstimates.map((costEstimate) => (
                          <Tr key={costEstimate.id}>
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
                            <Td>{costEstimate.ownerName}</Td>
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
                            <Td textAlign="center">
                              <IconButton
                                aria-label="Podgląd"
                                icon={<Eye size={14} />}
                                size="xs"
                                colorScheme="blue"
                                variant="ghost"
                                onClick={() => handleViewCostEstimate(costEstimate.id)}
                              />
                            </Td>
                          </Tr>
                        ))}
                      </Tbody>
                    </Table>
                  </Box>
                )}
              </VStack>
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
            onCostEstimateCreated={fetchData}
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
