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
  Card,
  CardBody,
  SimpleGrid,
  IconButton,
  Tooltip,
  useDisclosure,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  FormControl,
  FormLabel,
  Input,
  Textarea,
} from "@chakra-ui/react";
import { ArrowLeft, FileText, Plus, Trash2, Copy, Lock, Unlock } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import type { CostEstimateListItem } from "../types/costEstimate.types";

export default function CostEstimates() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { showSuccess, showError } = useToastNotification();

  const [loading, setLoading] = useState(true);
  const [costEstimates, setCostEstimates] = useState<CostEstimateListItem[]>([]);

  const { isOpen: isEstimateModalOpen, onOpen: onEstimateModalOpen, onClose: onEstimateModalClose } = useDisclosure();

  const [newEstimateName, setNewEstimateName] = useState("");
  const [newEstimateDescription, setNewEstimateDescription] = useState("");

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    fetchData();
  }, [projectId]);

  const fetchData = async () => {
    setLoading(true);
    try {
      // TODO: Integrate with API when available
      setCostEstimates([]);
    } catch (error) {
      showError("Nie udało się pobrać danych");
    } finally {
      setLoading(false);
    }
  };

  const handleCreateEstimate = async () => {
    if (!newEstimateName.trim()) {
      showError("Nazwa kosztorysu jest wymagana");
      return;
    }

    try {
      // TODO: Replace with actual API call
      showSuccess("Kosztorys został utworzony");
      setNewEstimateName("");
      setNewEstimateDescription("");
      onEstimateModalClose();
      fetchData();
    } catch (error) {
      showError("Nie udało się utworzyć kosztorysu");
    }
  };

  const handleDeleteEstimate = async (_estimateId: string) => {
    if (!confirm("Czy na pewno chcesz usunąć ten kosztorys?")) return;

    try {
      // TODO: Replace with actual API call
      showSuccess("Kosztorys został usunięty");
      fetchData();
    } catch (error) {
      showError("Nie udało się usunąć kosztorysu");
    }
  };

  const handleToggleLock = async (_estimateId: string) => {
    try {
      // TODO: Replace with actual API call to update status
      showSuccess("Status kosztorysu został zmieniony");
      fetchData();
    } catch (error) {
      showError("Nie udało się zmienić statusu");
    }
  };

  const handleDuplicateEstimate = async (_estimateId: string) => {
    try {
      // TODO: Replace with actual API call
      showSuccess("Kosztorys został zduplikowany");
      fetchData();
    } catch (error) {
      showError("Nie udało się zduplikować kosztorysu");
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie kosztorysów..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        {/* BACK BUTTON */}
        {/* HEADER */
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={FileText} boxSize={8} color="green.600" />
            <Heading size="lg">Kosztorysy</Heading>
          </HStack>
        </HStack>

        {/* KOSZTORYSY */}
        <VStack spacing={4} align="stretch">
          <HStack justify="space-between">
            <Text fontSize="sm" color="gray.600">
              Kosztorysy projektu zawierające szczegółowe zestawienia kosztów. Szablony zarządzasz w sekcji "Szablony kosztorysów" w menu bocznym.
            </Text>
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="blue"
              onClick={onEstimateModalOpen}
            >
              Nowy kosztorys
            </Button>
          </HStack>

                {costEstimates.length === 0 ? (
                  <EmptyState
                    icon={FileText}
                    title="Brak kosztorysów"
                    description="Utwórz pierwszy kosztorys dla tego projektu"
                  />
                ) : (
                  <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={4}>
                    {costEstimates.map((estimate) => (
                      <Card
                        key={estimate.id}
                        bg={cardBg}
                        borderWidth="1px"
                        borderColor={borderColor}
                        _hover={{ bg: hoverBg, transform: "translateY(-2px)" }}
                        transition="all 0.2s"
                        cursor="pointer"
                      >
                        <CardBody>
                          <VStack align="stretch" spacing={3}>
                            <HStack justify="space-between">
                              <HStack spacing={2}>
                                <Icon as={FileText} boxSize={6} color="blue.600" />
                                {estimate.status === 3 && (
                                  <Tooltip label="Kosztorys zatwierdzony">
                                    <Box>
                                      <Icon as={Lock} boxSize={4} color="green.500" />
                                    </Box>
                                  </Tooltip>
                                )}
                              </HStack>
                              <HStack spacing={1}>
                                <Tooltip label="Zmień status">
                                  <IconButton
                                    aria-label="Toggle status"
                                    icon={estimate.status === 3 ? <Unlock size={16} /> : <Lock size={16} />}
                                    size="sm"
                                    variant="ghost"
                                    colorScheme={estimate.status === 3 ? "green" : "orange"}
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      handleToggleLock(estimate.id);
                                    }}
                                  />
                                </Tooltip>
                                <Tooltip label="Duplikuj">
                                  <IconButton
                                    aria-label="Duplikuj"
                                    icon={<Copy size={16} />}
                                    size="sm"
                                    variant="ghost"
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      handleDuplicateEstimate(estimate.id);
                                    }}
                                  />
                                </Tooltip>
                                <Tooltip label="Usuń">
                                  <IconButton
                                    aria-label="Usuń"
                                    icon={<Trash2 size={16} />}
                                    size="sm"
                                    variant="ghost"
                                    colorScheme="red"
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      handleDeleteEstimate(estimate.id);
                                    }}
                                  />
                                </Tooltip>
                              </HStack>
                            </HStack>

                            <VStack align="flex-start" spacing={1}>
                              <Text fontWeight="bold" fontSize="lg" noOfLines={1}>
                                {estimate.name}
                              </Text>
                              {estimate.description && (
                                <Text fontSize="sm" color="gray.600" noOfLines={2}>
                                  {estimate.description}
                                </Text>
                              )}
                              {estimate.templateName && (
                                <Badge colorScheme="purple" fontSize="xs">
                                  Szablon: {estimate.templateName}
                                </Badge>
                              )}
                            </VStack>

                            <VStack align="stretch" spacing={1} fontSize="sm">
                              <HStack justify="space-between">
                                <Text color="gray.600">Netto:</Text>
                                <Text fontWeight="bold">
                                  {estimate.totalNet ? `${estimate.totalNet.toFixed(2)} PLN` : '-'}
                                </Text>
                              </HStack>
                              <HStack justify="space-between">
                                <Text color="gray.600">Brutto:</Text>
                                <Text fontWeight="bold" color="green.600">
                                  {estimate.totalGross ? `${estimate.totalGross.toFixed(2)} PLN` : '-'}
                                </Text>
                              </HStack>
                            </VStack>

                            <HStack justify="space-between" fontSize="xs" color="gray.500">
                              <Badge colorScheme={
                                estimate.status === 0 ? "gray" :
                                estimate.status === 1 ? "blue" :
                                estimate.status === 2 ? "orange" :
                                estimate.status === 3 ? "green" :
                                estimate.status === 4 ? "red" : "purple"
                              }>
                                {estimate.status === 0 ? "Szkic" :
                                 estimate.status === 1 ? "W trakcie" :
                                 estimate.status === 2 ? "Do przeglądu" :
                                 estimate.status === 3 ? "Zatwierdzony" :
                                 estimate.status === 4 ? "Odrzucony" : "Zarchiwizowany"}
                              </Badge>
                              <Text>{formatDate(estimate.createdAt)}</Text>
                            </HStack>

                            <Text fontSize="xs" color="gray.500">
                              Utworzył: {estimate.ownerName}
                            </Text>
                          </VStack>
                        </CardBody>
                      </Card>
                    ))}
                  </SimpleGrid>
                )}
              </VStack>

        {/* MODAL: CREATE ESTIMATE */}
        <Modal isOpen={isEstimateModalOpen} onClose={onEstimateModalClose}>
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>Nowy kosztorys</ModalHeader>
            <ModalCloseButton />
            <ModalBody>
              <VStack spacing={4}>
                <FormControl isRequired>
                  <FormLabel>Nazwa kosztorysu</FormLabel>
                  <Input
                    value={newEstimateName}
                    onChange={(e) => setNewEstimateName(e.target.value)}
                    placeholder="Wprowadź nazwę kosztorysu"
                  />
                </FormControl>
                <FormControl>
                  <FormLabel>Opis</FormLabel>
                  <Textarea
                    value={newEstimateDescription}
                    onChange={(e) => setNewEstimateDescription(e.target.value)}
                    placeholder="Opcjonalny opis kosztorysu"
                    rows={3}
                  />
                </FormControl>
              </VStack>
            </ModalBody>
            <ModalFooter>
              <Button variant="ghost" mr={3} onClick={onEstimateModalClose}>
                Anuluj
              </Button>
              <Button colorScheme="blue" onClick={handleCreateEstimate}>
                Utwórz kosztorys
              </Button>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </Box>
    </MainLayout>
  );
}
