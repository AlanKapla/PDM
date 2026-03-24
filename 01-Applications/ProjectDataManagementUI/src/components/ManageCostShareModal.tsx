import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalFooter,
  ModalBody,
  ModalCloseButton,
  Button,
  VStack,
  Checkbox,
  useToast,
  Text,
  Box,
  HStack,
  Spinner,
} from "@chakra-ui/react";
import { useState, useEffect } from "react";
import type { ProjectMemberWeb } from "../types/project.types";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import { User } from "lucide-react";

interface ManageCostShareModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  costId: string;
  costName: string;
  sharedWithUserIds: string[];
  currentUserId: string;
  ownerUserId?: string;
  onShareUpdated: () => void;
}

export const ManageCostShareModal = ({
  isOpen,
  onClose,
  tenantId,
  projectId,
  costId,
  costName,
  sharedWithUserIds,
  currentUserId,
  ownerUserId,
  onShareUpdated,
}: ManageCostShareModalProps) => {
  const toast = useToast();
  const [loading, setLoading] = useState(false);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (isOpen) {
      setSelectedUserIds(new Set(sharedWithUserIds));
      fetchProjectMembers();
    }
  }, [isOpen, sharedWithUserIds]);

  const fetchProjectMembers = async () => {
    try {
      setLoadingMembers(true);
      const response = await projectApi.getProjectMembers(tenantId, projectId);
      const data = response.data;
      // Wyklucz aktualnego użytkownika i właściciela kosztu z listy
      const excludeIds = new Set([currentUserId, ownerUserId].filter(Boolean));
      const filteredMembers = data.filter((member: ProjectMemberWeb) => !excludeIds.has(member.userId));
      setMembers(filteredMembers);
    } catch (error) {
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

  const toggleUser = (userId: string) => {
    const newSelection = new Set(selectedUserIds);
    if (newSelection.has(userId)) {
      newSelection.delete(userId);
    } else {
      newSelection.add(userId);
    }
    setSelectedUserIds(newSelection);
  };

  const handleSave = async () => {
    try {
      setLoading(true);
      const sharedWithUserIds = Array.from(selectedUserIds);
      await projectApi.updateCostShare(tenantId, projectId, costId, sharedWithUserIds);

      toast({
        title: "Sukces",
        description: "Zaktualizowano udostępnienie kosztu",
        status: "success",
        duration: 3000,
        isClosable: true,
      });
      onShareUpdated();
      onClose();
    } catch (error) {
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

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="md">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Udostępnij koszt</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack align="stretch" spacing={4}>
            <Box>
              <Text fontWeight="bold" mb={2}>
                Koszt: {costName}
              </Text>
              <Text fontSize="sm" color="gray.600">
                Wybierz członków projektu, którym chcesz udostępnić ten koszt
              </Text>
            </Box>

            {loadingMembers ? (
              <HStack justify="center" py={4}>
                <Spinner size="md" />
                <Text fontSize="sm">Ładowanie członków...</Text>
              </HStack>
            ) : members.length === 0 ? (
              <Text color="gray.500">Brak członków projektu do udostępnienia</Text>
            ) : (
              <VStack align="stretch" spacing={2} maxH="300px" overflowY="auto" borderWidth="1px" borderRadius="md" p={3}>
                {members.map((member) => (
                  <HStack
                    key={member.userId}
                    p={2}
                    borderRadius="md"
                    cursor="pointer"
                    bg={selectedUserIds.has(member.userId) ? "blue.50" : "transparent"}
                    _hover={{ bg: selectedUserIds.has(member.userId) ? "blue.100" : "gray.50" }}
                    onClick={() => toggleUser(member.userId)}
                  >
                    <Checkbox
                      isChecked={selectedUserIds.has(member.userId)}
                      onChange={() => toggleUser(member.userId)}
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
          </VStack>
        </ModalBody>

        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={onClose} isDisabled={loading}>
            Anuluj
          </Button>
          <Button
            colorScheme="blue"
            onClick={handleSave}
            isLoading={loading}
            isDisabled={loadingMembers}
          >
            Zapisz
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
};
