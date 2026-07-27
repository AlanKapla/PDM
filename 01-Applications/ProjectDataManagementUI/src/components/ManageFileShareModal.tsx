import {
  VStack,
  Checkbox,
  Text,
  Box,
} from "@chakra-ui/react";
import { useState, useEffect } from "react";
import AppModal from "./ui/AppModal";
import type { ProjectMemberWeb } from "../types/project.types";
import { projectApi } from "../api/projectApi";
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
  const { showSuccess, showApiError } = useToastNotification();
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

  const handleClose = () => {
    if (!loading) {
      onClose();
    }
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

  const excludeIds = new Set([currentUserId, ownerUserId].filter(Boolean));
  const availableMembers = members.filter(
    (member) => !excludeIds.has(member.userId)
  );

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Udostępnij"
      actionLabel="Zapisz"
      actionColorScheme="primary"
      onAction={handleSave}
      isActionLoading={loading}
      isActionDisabled={availableMembers.length === 0}
      desktopSize="md"
    >
      <VStack align="stretch" spacing={4}>
        <Box>
          <Text fontWeight="bold" mb={2} wordBreak="break-word">
            Plik: {fileName}
          </Text>
          <Text fontSize="sm" color="neutral.600">
            Wybierz członków projektu, którym chcesz udostępnić ten plik
          </Text>
        </Box>

        {availableMembers.length === 0 ? (
          <Text color="neutral.600">Brak członków projektu do udostępnienia</Text>
        ) : (
          <VStack align="stretch" spacing={2} maxH={{ base: "50dvh", md: "300px" }} overflowY="auto">
            {availableMembers.map((member) => (
              <Checkbox
                key={member.userId}
                isChecked={selectedUserIds.has(member.userId)}
                onChange={() => toggleUser(member.userId)}
                minH="44px"
                alignItems="flex-start"
                py={2}
              >
                <Text fontSize="sm" wordBreak="break-word">
                  {member.firstName} {member.lastName} ({member.email})
                </Text>
              </Checkbox>
            ))}
          </VStack>
        )}
      </VStack>
    </AppModal>
  );
};
