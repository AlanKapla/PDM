import { useState, useEffect, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Button,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Badge,
  IconButton,
  Tooltip,
  useDisclosure,
  Card,
  CardBody,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
} from "@chakra-ui/react";
import { ArrowLeft, Edit, History, Eye, Trash2 } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import {
  costEstimateTemplateApi,
} from "../api/costEstimateTemplateApi";
import { CostEstimateTemplateVersionStatus, type CostEstimateTemplateVersionHistoryItem } from "../types/costEstimate.types";

export default function TemplateVersionHistory() {
  const { templateId } = useParams<{ templateId: string }>();
  const navigate = useNavigate();
  const { showError, showInfo } = useToastNotification();

  const [loading, setLoading] = useState(true);
  const [templateName, setTemplateName] = useState("");
  const [versions, setVersions] = useState<CostEstimateTemplateVersionHistoryItem[]>([]);
  const [pendingEditVersion, setPendingEditVersion] = useState<number | null>(null);
  const [versionToDelete, setVersionToDelete] = useState<{ id: string; versionNumber: number } | null>(null);
  const [deleting, setDeleting] = useState(false);

  const { isOpen: isConfirmApprovedEditOpen, onOpen: onConfirmApprovedEditOpen, onClose: onConfirmApprovedEditClose } = useDisclosure();
  const { isOpen: isDeleteDraftOpen, onOpen: onDeleteDraftOpen, onClose: onDeleteDraftClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);
  const deleteCancelRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    fetchVersionHistory();
  }, [templateId]);

  const fetchVersionHistory = async () => {
    if (!templateId) return;

    setLoading(true);
    try {
      // Pobierz szczegóły szablonu dla nazwy
      const templateDetails = await costEstimateTemplateApi.getTemplateDetails(templateId);
      setTemplateName(templateDetails.name);

      // Pobierz historię wersji
      const versionHistory = await costEstimateTemplateApi.getTemplateVersionHistory(templateId);
      setVersions(versionHistory);
    } catch (error: any) {
      console.error('Error fetching version history:', error);
      showError('Nie udało się załadować historii wersji', error?.message || 'Wystąpił nieoczekiwany błąd');
    } finally {
      setLoading(false);
    }
  };

  const handleEditVersion = (versionNumber: number) => {
    if (!templateId) return;
    
    const selectedVersion = versions.find(v => v.versionNumber === versionNumber);
    if (!selectedVersion) return;
    
    // Jeśli edytujemy zatwierdzoną wersję, pokaż modal potwierdzenia
    if (selectedVersion.status === CostEstimateTemplateVersionStatus.Approved) {
      setPendingEditVersion(versionNumber);
      onConfirmApprovedEditOpen();
      return;
    }
    
    // Dla szkicu - przejdź bezpośrednio do edycji
    navigate(`/cost-estimate-templates/${templateId}/edit`);
  };

  const confirmEditApprovedVersion = () => {
    onConfirmApprovedEditClose();
    if (templateId) {
      navigate(`/cost-estimate-templates/${templateId}/edit`);
    }
    setPendingEditVersion(null);
  };

  const openDeleteDraftModal = (versionId: string, versionNumber: number) => {
    setVersionToDelete({ id: versionId, versionNumber });
    onDeleteDraftOpen();
  };

  const handleDeleteDraft = async () => {
    if (!versionToDelete || !templateId) return;

    setDeleting(true);
    try {
      await costEstimateTemplateApi.deleteVersionDraft(templateId, versionToDelete.id);
      showInfo(`Wersja szkicu v${versionToDelete.versionNumber} została usunięta`);
      fetchVersionHistory();
      onDeleteDraftClose();
      setVersionToDelete(null);
    } catch (error: any) {
      console.error('Error deleting draft version:', error);
      showError('Nie udało się usunąć wersji szkicu', error?.message || 'Wystąpił nieoczekiwany błąd');
    } finally {
      setDeleting(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie historii wersji..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <VStack spacing={6} align="stretch">
          <HStack justify="space-between" flexWrap="wrap" gap={4}>
            <HStack spacing={3}>
              <IconButton
                aria-label="Wróć"
                icon={<ArrowLeft size={20} />}
                onClick={() => navigate('/cost-estimate-templates')}
                variant="ghost"
              />
              <VStack align="flex-start" spacing={0}>
                <HStack spacing={2}>
                  <History size={24} />
                  <Heading size="lg">Historia wersji</Heading>
                </HStack>
                <Text fontSize="sm" color="gray.600">{templateName}</Text>
              </VStack>
            </HStack>
          </HStack>

          {versions.length === 0 ? (
            <EmptyState
              icon={History}
              title="Brak historii wersji"
              description="Ten szablon nie ma jeszcze żadnych wersji"
            />
          ) : (
            <Card>
              <CardBody p={0}>
                <Table variant="simple">
                  <Thead>
                    <Tr>
                      <Th>Wersja</Th>
                      <Th>Status</Th>
                      <Th>Utworzona</Th>
                      <Th>Utworzona przez</Th>
                      <Th>Zatwierdzona</Th>
                      <Th>Zatwierdzona przez</Th>
                      <Th textAlign="center">Akcje</Th>
                    </Tr>
                  </Thead>
                  <Tbody>
                    {versions.map((version) => (
                      <Tr key={version.id}>
                        <Td>
                          <Badge colorScheme="blue" fontSize="md" px={3} py={1}>
                            v{version.versionNumber}
                          </Badge>
                        </Td>
                        <Td>
                          <Badge
                            colorScheme={
                              version.status === CostEstimateTemplateVersionStatus.Approved
                                ? "green"
                                : "gray"
                            }
                          >
                            {version.status === CostEstimateTemplateVersionStatus.Approved
                              ? "Zatwierdzona"
                              : "Szkic"}
                          </Badge>
                        </Td>
                        <Td>
                          <Text fontSize="sm">{formatDate(version.createdAt)}</Text>
                        </Td>
                        <Td>
                          <Text fontSize="sm">{version.createdByUserName}</Text>
                        </Td>
                        <Td>
                          <Text fontSize="sm">
                            {version.approvedAt ? formatDate(version.approvedAt) : '-'}
                          </Text>
                        </Td>
                        <Td>
                          <Text fontSize="sm">{version.approvedByUserName || '-'}</Text>
                        </Td>
                        <Td textAlign="center">
                          <HStack spacing={1} justify="center">
                            <Tooltip 
                              label={
                                version.status === 1 
                                  ? "Edytuj (utworzy nową wersję szkicu)" 
                                  : version.versionNumber === versions[0].versionNumber
                                    ? "Edytuj najnowszą wersję szkicu"
                                    : "Edytuj (załaduje najnowszą wersję)"
                              }
                            >
                              <IconButton
                                aria-label="Edytuj"
                                icon={<Edit size={16} />}
                                size="sm"
                                colorScheme={version.status === 1 ? "orange" : "blue"}
                                variant="ghost"
                                onClick={() => handleEditVersion(version.versionNumber)}
                              />
                            </Tooltip>
                            {version.status === CostEstimateTemplateVersionStatus.Draft && (
                              <Tooltip label="Usuń wersję szkicu">
                                <IconButton
                                  aria-label="Usuń"
                                  icon={<Trash2 size={16} />}
                                  size="sm"
                                  colorScheme="red"
                                  variant="ghost"
                                  onClick={() => openDeleteDraftModal(version.id, version.versionNumber)}
                                />
                              </Tooltip>
                            )}
                          </HStack>
                        </Td>
                      </Tr>
                    ))}
                  </Tbody>
                </Table>
              </CardBody>
            </Card>
          )}

          <Box p={4} bg="blue.50" rounded="md" borderWidth="1px" borderColor="blue.200">
            <Text fontSize="sm" color="blue.800">
              💡 <strong>Informacja:</strong> Możesz edytować każdą wersję szablonu. 
              Edycja zatwierdzonej wersji automatycznie utworzy nową wersję szkicu z kopią struktury.
              Starsze wersje są zachowane jako historia zmian.
            </Text>
          </Box>
        </VStack>

        {/* ALERT DIALOG: Confirm deleting draft version */}
        <AlertDialog
          isOpen={isDeleteDraftOpen}
          leastDestructiveRef={deleteCancelRef}
          onClose={onDeleteDraftClose}
        >
          <AlertDialogOverlay>
            <AlertDialogContent>
              <AlertDialogHeader fontSize="lg" fontWeight="bold">
                Usuń wersję szkicu
              </AlertDialogHeader>

              <AlertDialogBody>
                <VStack align="flex-start" spacing={3}>
                  <Text>
                    Czy na pewno chcesz usunąć wersję szkicu <Badge colorScheme="blue">v{versionToDelete?.versionNumber}</Badge>?
                  </Text>
                  <Box p={3} bg="orange.50" borderRadius="md" borderWidth="1px" borderColor="orange.200" w="full">
                    <HStack spacing={2}>
                      <Text fontSize="2xl">⚠️</Text>
                      <VStack align="flex-start" spacing={1}>
                        <Text fontSize="sm" fontWeight="bold" color="orange.800">
                          Uwaga!
                        </Text>
                        <Text fontSize="sm" color="orange.700">
                          Ta operacja jest nieodwracalna. Tylko wersje szkicu mogą być usunięte.
                        </Text>
                      </VStack>
                    </HStack>
                  </Box>
                </VStack>
              </AlertDialogBody>

              <AlertDialogFooter>
                <Button ref={deleteCancelRef} onClick={onDeleteDraftClose} isDisabled={deleting}>
                  Anuluj
                </Button>
                <Button 
                  colorScheme="red" 
                  onClick={handleDeleteDraft} 
                  ml={3}
                  isLoading={deleting}
                  loadingText="Usuwanie..."
                >
                  Usuń wersję
                </Button>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialogOverlay>
        </AlertDialog>

        {/* ALERT DIALOG: Confirm editing approved version */}
        <AlertDialog
          isOpen={isConfirmApprovedEditOpen}
          leastDestructiveRef={cancelRef}
          onClose={onConfirmApprovedEditClose}
        >
          <AlertDialogOverlay>
            <AlertDialogContent>
              <AlertDialogHeader fontSize="lg" fontWeight="bold">
                Edycja zatwierdzonej wersji
              </AlertDialogHeader>

              <AlertDialogBody>
                <VStack align="flex-start" spacing={3}>
                  <Text>
                    Edycja zatwierdzonej wersji <Badge colorScheme="blue">v{pendingEditVersion}</Badge> spowoduje utworzenie nowej wersji szkicu.
                  </Text>
                  <Box p={3} bg="orange.50" borderRadius="md" borderWidth="1px" borderColor="orange.200" w="full">
                    <HStack spacing={2}>
                      <Text fontSize="2xl">⚠️</Text>
                      <VStack align="flex-start" spacing={1}>
                        <Text fontSize="sm" fontWeight="bold" color="orange.800">
                          Co się stanie?
                        </Text>
                        <Text fontSize="sm" color="orange.700">
                          • Zostanie utworzona nowa wersja szkicu z kopią struktury<br />
                          • Zatwierdzona wersja pozostanie bez zmian<br />
                          • Możesz wprowadzić zmiany w nowej wersji szkicu
                        </Text>
                      </VStack>
                    </HStack>
                  </Box>
                </VStack>
              </AlertDialogBody>

              <AlertDialogFooter>
                <Button ref={cancelRef} onClick={onConfirmApprovedEditClose}>
                  Anuluj
                </Button>
                <Button colorScheme="blue" onClick={confirmEditApprovedVersion} ml={3}>
                  Kontynuuj edycję
                </Button>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialogOverlay>
        </AlertDialog>
      </Box>
    </MainLayout>
  );
}
