import { useState, useEffect, useContext } from "react";
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Button,
  VStack,
  Text,
  Alert,
  AlertIcon,
  Box,
  HStack,
  useToast,
  Divider,
  Checkbox,
  Spinner,
} from "@chakra-ui/react";
import { Share2, User, DollarSign } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import { AuthContext } from "../context/AuthContext";
import type { ProjectMemberWeb, ProjectCostListItemWeb } from "../types/project.types";

interface ShareCostsModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  onCostsShared: () => void;
}

export default function ShareCostsModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  onCostsShared,
}: ShareCostsModalProps) {
  const [costs, setCosts] = useState<ProjectCostListItemWeb[]>([]);
  const [selectedCostIds, setSelectedCostIds] = useState<Set<string>>(new Set());
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(false);
  const [loadingCosts, setLoadingCosts] = useState(false);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const toast = useToast();
  const { user } = useContext(AuthContext);

  useEffect(() => {
    if (isOpen) {
      fetchMyCosts();
      fetchProjectMembers();
      setSelectedUserIds(new Set());
      // selectedCostIds jest ustawiany w fetchMyCosts po pobraniu kosztów
    }
  }, [isOpen, tenantId, projectId]);

  const fetchMyCosts = async () => {
    try {
      setLoadingCosts(true);
      const response = await projectApi.getProjectUserCosts(tenantId, projectId);
      const data: ProjectCostListItemWeb[] = response.data;
      setCosts(data);
      // Domyślnie zaznacz wszystkie koszty
      setSelectedCostIds(new Set(data.map((cost) => cost.id)));
    } catch (error) {
      console.error("Błąd podczas pobierania kosztów:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy kosztów",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoadingCosts(false);
    }
  };

  const fetchProjectMembers = async () => {
    try {
      setLoadingMembers(true);
      const response = await projectApi.getProjectMembers(tenantId, projectId);
      const data = response.data;
      // Wyklucz aktualnego użytkownika z listy i filtruj członków bez userId
      const filteredMembers = data.filter((member: ProjectMemberWeb) =>
        member.userId && member.userId !== user?.id
      );
      setMembers(filteredMembers);
    } catch (error) {
      console.error("Błąd podczas pobierania członków:", error);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy członków projektu",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoadingMembers(false);
    }
  };

  const toggleCostSelection = (costId: string) => {
    setSelectedCostIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(costId)) {
        newSet.delete(costId);
      } else {
        newSet.add(costId);
      }
      return newSet;
    });
  };

  const toggleUserSelection = (userId: string) => {
    setSelectedUserIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(userId)) {
        newSet.delete(userId);
      } else {
        newSet.add(userId);
      }
      return newSet;
    });
  };

  const handleShare = async () => {
    if (selectedCostIds.size === 0) {
      toast({
        title: "Błąd",
        description: "Wybierz przynajmniej jeden koszt",
        status: "warning",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    if (selectedUserIds.size === 0) {
      toast({
        title: "Błąd",
        description: "Wybierz przynajmniej jednego użytkownika",
        status: "warning",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    try {
      setLoading(true);
      const projectCostIds = Array.from(selectedCostIds);
      const sharedWithUserIds = Array.from(selectedUserIds);
      await projectApi.shareProjectCosts(tenantId, projectId, { projectCostIds, sharedWithUserIds });

      toast({
        title: "Sukces",
        description: `Udostępniono ${selectedCostIds.size} koszt(ów) dla ${selectedUserIds.size} użytkownik(ów)`,
        status: "success",
        duration: 5000,
        isClosable: true,
      });
      onCostsShared();
      onClose();
    } catch (error) {
      console.error("Błąd podczas udostępniania kosztów:", error);
      const { title, description } = handleApiError(error);
      toast({
        title,
        description,
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    if (amount === null || amount === undefined) return "-";
    return `${amount.toFixed(2)} zł`;
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString("pl-PL");
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} size={{ base: "full", md: "xl" }}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>
          <HStack spacing={2}>
            <Box display={{ base: "none", md: "block" }}>
              <Share2 size={24} />
            </Box>
            <Box display={{ base: "block", md: "none" }}>
              <Share2 size={20} />
            </Box>
            <Text fontSize={{ base: "md", md: "lg" }}>Udostępnij koszty grupowo</Text>
          </HStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            {/* Wybór kosztów */}
            <Box>
              <Text fontWeight="bold" mb={2}>
                Wybierz koszty do udostępnienia ({selectedCostIds.size}):
              </Text>
              {loadingCosts ? (
                <HStack justify="center" py={4}>
                  <Spinner size="md" />
                  <Text fontSize="sm">Ładowanie kosztów...</Text>
                </HStack>
              ) : costs.length === 0 ? (
                <Text fontSize="sm" color="gray.500">
                  Nie masz jeszcze żadnych kosztów do udostępnienia
                </Text>
              ) : (
                <VStack spacing={2} align="stretch" maxH="300px" overflowY="auto" borderWidth="1px" borderRadius="md" p={3}>
                  {costs.map((cost) => (
                    <HStack
                      key={cost.id}
                      p={3}
                      borderRadius="md"
                      borderWidth="1px"
                      cursor="pointer"
                      bg={selectedCostIds.has(cost.id) ? "blue.50" : "transparent"}
                      borderColor={selectedCostIds.has(cost.id) ? "blue.300" : "gray.200"}
                      _hover={{ bg: selectedCostIds.has(cost.id) ? "blue.100" : "gray.50" }}
                      onClick={() => toggleCostSelection(cost.id)}
                    >
                      <Checkbox
                        isChecked={selectedCostIds.has(cost.id)}
                        onChange={(e) => {
                          e.stopPropagation();
                          toggleCostSelection(cost.id);
                        }}
                      />
                      <DollarSign size={16} />
                      <VStack align="start" spacing={0} flex="1">
                        <Text fontSize="sm" fontWeight="medium">
                          {cost.name}
                        </Text>
                        <HStack spacing={2} fontSize="xs" color="gray.600">
                          <Text>{formatDate(cost.date)}</Text>
                          {cost.place && <Text>• {cost.place}</Text>}
                          <Text>• {formatCurrency(cost.grossAmount)}</Text>
                        </HStack>
                      </VStack>
                    </HStack>
                  ))}
                </VStack>
              )}
            </Box>

            <Alert status="info" fontSize="xs">
              <AlertIcon />
              Udostępniasz koszty do wglądu. Członkowie będą mogli je przeglądać.
            </Alert>

            <Divider />

            {/* Wybór użytkowników */}
            <Box>
              <Text fontWeight="bold" mb={2}>
                Udostępnij dla ({selectedUserIds.size}):
              </Text>
              {loadingMembers ? (
                <Text fontSize="sm" color="gray.500">
                  Ładowanie członków...
                </Text>
              ) : members.length === 0 ? (
                <Text fontSize="sm" color="gray.500">
                  Brak członków projektu do udostępnienia
                </Text>
              ) : (
                <VStack align="stretch" spacing={2} maxH="200px" overflowY="auto" p={2} borderWidth="1px" borderRadius="md">
                  {members.map((member) => (
                    <HStack
                      key={member.userId}
                      p={2}
                      borderRadius="md"
                      cursor="pointer"
                      bg={selectedUserIds.has(member.userId) ? "blue.50" : "transparent"}
                      _hover={{ bg: selectedUserIds.has(member.userId) ? "blue.100" : "gray.50" }}
                      onClick={() => toggleUserSelection(member.userId)}
                    >
                      <Checkbox
                        isChecked={selectedUserIds.has(member.userId)}
                        onChange={(e) => {
                          e.stopPropagation();
                          toggleUserSelection(member.userId);
                        }}
                      />
                      <User size={16} />
                      <VStack align="start" spacing={0} flex="1">
                        <Text fontSize="sm" fontWeight="medium">
                          {member.firstName} {member.lastName}
                        </Text>
                        <Text fontSize="xs" color="gray.600">
                          {member.email}
                        </Text>
                      </VStack>
                    </HStack>
                  ))}
                </VStack>
              )}
            </Box>

            <Alert status="info" fontSize="sm">
              <AlertIcon />
              Wybrani członkowie otrzymają dostęp do wybranych kosztów i będą mogli je przeglądać.
            </Alert>
          </VStack>
        </ModalBody>
        <ModalFooter flexDirection={{ base: "column", md: "row" }} gap={2}>
          <Button 
            variant="ghost" 
            onClick={onClose}
            width={{ base: "100%", md: "auto" }}
            order={{ base: 2, md: 1 }}
          >
            Anuluj
          </Button>
          <Button
            colorScheme="blue"
            onClick={handleShare}
            isLoading={loading}
            loadingText="Udostępnianie..."
            isDisabled={selectedCostIds.size === 0 || selectedUserIds.size === 0 || loadingMembers || loadingCosts}
            leftIcon={<Share2 size={18} />}
            width={{ base: "100%", md: "auto" }}
            order={{ base: 1, md: 2 }}
          >
            Udostępnij ({selectedCostIds.size} dla {selectedUserIds.size})
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
