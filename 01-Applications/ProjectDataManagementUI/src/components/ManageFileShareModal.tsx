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
  Text,
  Box,
} from "@chakra-ui/react";
import { useState, useEffect } from "react";
import type { ProjectMemberWeb } from "../types/project.types";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import { useToastNotification } from "../hooks/useToastNotification";

interface ManageFileShareModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  fileId: string;
  fileName: string;
  sharedWithUserIds: string[];
  members: ProjectMemberWeb[];
  currentUserId: string;
  ownerUserId?: string;
  onShareUpdated: () => void;
}

export const ManageFileShareModal = ({
  isOpen,
  onClose,
  tenantId,
  projectId,
  fileId,
  fileName,
  sharedWithUserIds,
  members,
  currentUserId,
  ownerUserId,
  onShareUpdated,
}: ManageFileShareModalProps) => {
  const {showSuccess, showError, showWarning, showInfo, toast, showApiError } = useToastNotification();
  const [loading, setLoading] = useState(false);
  const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (isOpen) {
      setSelectedUserIds(new Set(sharedWithUserIds));
    }
  }, [isOpen, sharedWithUserIds]);

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
      const userIds = Array.from(selectedUserIds);
      await projectApi.updateFileShare(tenantId, projectId, fileId, userIds);

      showSuccess("Sukces", "Zaktualizowano udostępnienie pliku");
      onShareUpdated();
      onClose();
    } catch (error) {
      showApiError(error);
    } finally {
      setLoading(false);
    }
  };

  // Filtruj członków - usuń aktualnego użytkownika i właściciela pliku
  const excludeIds = new Set([currentUserId, ownerUserId].filter(Boolean));
  const availableMembers = members.filter(
    (member) => !excludeIds.has(member.userId)
  );

  return (
    <Modal isOpen={isOpen} onClose={onClose} size={{ base: "full", md: "md" }}>
      <ModalOverlay />
      <ModalContent mx={{ base: 0, md: "auto" }}>
        <ModalHeader fontSize={{ base: "lg", md: "xl" }}>Udostępnij</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack align="stretch" spacing={4}>
            <Box>
              <Text fontWeight="bold" mb={2}>
                Plik: {fileName}
              </Text>
              <Text fontSize="sm" color="gray.600">
                Wybierz członków projektu, którym chcesz udostępnić ten plik
              </Text>
            </Box>

            {availableMembers.length === 0 ? (
              <Text color="gray.500">Brak członków projektu do udostępnienia</Text>
            ) : (
              <VStack align="stretch" spacing={2} maxH="300px" overflowY="auto">
                {availableMembers.map((member) => (
                  <Checkbox
                    key={member.userId}
                    isChecked={selectedUserIds.has(member.userId)}
                    onChange={() => toggleUser(member.userId)}
                  >
                    {member.firstName} {member.lastName} ({member.email})
                  </Checkbox>
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
            colorScheme="primary"
            onClick={handleSave}
            isLoading={loading}
            isDisabled={availableMembers.length === 0}
          >
            Zapisz
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
};
