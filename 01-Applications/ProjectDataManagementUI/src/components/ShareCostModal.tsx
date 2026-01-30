import { useState, useEffect } from "react";
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
  Badge,
  HStack,
  useToast,
  Checkbox,
  Stack,
  Spinner,
  useBreakpointValue
} from "@chakra-ui/react";
import { Share2, User } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import { useAuth } from "../hooks/useAuth";
import type { ProjectMemberWeb, ProjectCostListItemWeb } from "../types/project.types";

interface ShareCostModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  cost: ProjectCostListItemWeb;
  onCostShared: () => void;
}

const shareSize = 20;

export default function ShareCostModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  cost,
  onCostShared,
}: ShareCostModalProps) {
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const toast = useToast();
  // const { user } = useAuth();

  useEffect(() => {
    if (isOpen) {
      fetchMembers();
      setSelectedUserIds([...cost.sharedWithUserIds]);
    }
  }, [isOpen, cost.sharedWithUserIds]);

  const fetchMembers = async () => {
    setLoadingMembers(true);
    try {
      const response = await projectApi.getProjectMembers(tenantId, projectId);
      const data: ProjectMemberWeb[] = response.data;
      // Filtruj, aby nie pokazywać właściciela kosztu
      const filteredMembers = data.filter((m) => m.userId !== cost.userId);
      setMembers(filteredMembers);
    } catch (err) {
      console.error("Błąd pobierania członków:", err);
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy członków projektu",
        status: "error",
        duration: 3000,
      });
    } finally {
      setLoadingMembers(false);
    }
  };

  const handleToggleUser = (userId: string) => {
    setSelectedUserIds((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    );
  };

  const handleShare = async () => {
    // Zawsze wysyłamy pełną listę zaznaczonych użytkowników (może być pusta)
    // Backend sam zarządza dodawaniem i usuwaniem udostępnień
    setLoading(true);
    try {
      await projectApi.updateCostShare(tenantId, projectId, cost.id, selectedUserIds);

      toast({
        title: "Sukces",
        description: "Udostępnianie zaktualizowane",
        status: "success",
        duration: 3000,
      });

      onCostShared();
      onClose();
    } catch (err) {
      console.error("Błąd podczas udostępniania kosztu:", err);
      const { title, description } = handleApiError(err);
      toast({ title, description, status: "error", duration: 5000 });
    } finally {
      setLoading(false);
    }
  };

  const handleCloseModal = () => {
    setSelectedUserIds([]);
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleCloseModal} size={{ base: "full", md: "md" }} isCentered>
      <ModalOverlay />
      <ModalContent mx={{ base: 0, md: "auto" }}>
        <ModalHeader fontSize={{ base: "lg", md: "xl" }}>
          <HStack spacing={2}>
            <Share2 size={shareSize} />
            <Text>Udostępnij koszt</Text>
          </HStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack align="stretch" spacing={4}>
            <Box>
              <Text fontSize="sm" fontWeight="bold" mb={2}>
                Koszt:
              </Text>
              <Badge colorScheme="blue" fontSize="md" p={2}>
                {cost.name}
              </Badge>
            </Box>

            {loadingMembers ? (
              <HStack justify="center" py={4}>
                <Spinner size="md" />
                <Text>Ładowanie członków...</Text>
              </HStack>
            ) : members.length === 0 ? (
              <Alert status="info">
                <AlertIcon />
                Brak innych członków w projekcie
              </Alert>
            ) : (
              <Box>
                <Text fontSize="sm" fontWeight="bold" mb={2}>
                  Wybierz użytkowników:
                </Text>
                <Stack spacing={2} maxH="300px" overflowY="auto" p={2} borderWidth="1px" borderRadius="md">
                  {members.map((member) => (
                    <Checkbox
                      key={member.userId}
                      isChecked={selectedUserIds.includes(member.userId)}
                      onChange={() => handleToggleUser(member.userId)}
                    >
                      <HStack spacing={2}>
                        <User size={16} />
                        <Text fontSize="sm">
                          {member.firstName} {member.lastName}
                        </Text>
                        <Text fontSize="xs" color="gray.500">
                          ({member.email})
                        </Text>
                      </HStack>
                    </Checkbox>
                  ))}
                </Stack>
              </Box>
            )}

            {selectedUserIds.length > 0 && (
              <Alert status="info">
                <AlertIcon />
                Wybrano: {selectedUserIds.length} {selectedUserIds.length === 1 ? "użytkownik" : "użytkowników"}
              </Alert>
            )}
          </VStack>
        </ModalBody>
        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={handleCloseModal} isDisabled={loading}>
            Anuluj
          </Button>
          <Button
            colorScheme="blue"
            leftIcon={<Share2 size={18} />}
            onClick={handleShare}
            isLoading={loading}
            isDisabled={loadingMembers}
          >
            Zapisz
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
