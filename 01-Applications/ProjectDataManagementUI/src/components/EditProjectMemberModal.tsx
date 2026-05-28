import { useState } from "react";
import {
  VStack,
  HStack,
  Text,
  Checkbox,
  Stack,
  Divider,
  Badge,
} from "@chakra-ui/react";
import AppModal from "./ui/AppModal";
import { ProjectModule, PROJECT_MODULE_LABELS } from "../types/projectModulePermissions";
import { UserAvatar } from "./common";
import { projectApi } from "../api/projectApi";
import { useQueryClient } from "@tanstack/react-query";
import { projectKeys } from "../hooks/queries";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import type { ProjectMemberWeb } from "../types/project.types";

const ALL_MODULES = (Object.values(ProjectModule) as number[]).filter(m => m !== ProjectModule.Settings);

interface EditProjectMemberModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  member: ProjectMemberWeb;
}

export default function EditProjectMemberModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  member,
}: EditProjectMemberModalProps) {
  const { showError, showApiSuccess } = useToastNotification();
  const queryClient = useQueryClient();

  const [selectedModules, setSelectedModules] = useState<Set<number>>(new Set(member.modules ?? []));
  const [saving, setSaving] = useState(false);

  const handleSave = async () => {
    setSaving(true);
    try {
      const modules = Array.from(selectedModules);

      await projectApi.updateProjectMemberPermissions(
        tenantId,
        projectId,
        member.userId,
        member.isAdmin,
        modules
      );

      showApiSuccess('memberUpdated');
      queryClient.invalidateQueries({ queryKey: projectKeys.members(tenantId, projectId) });
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(tenantId, projectId) });
      onClose();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setSaving(false);
    }
  };

  const handleClose = () => {
    setSelectedModules(new Set(member.modules ?? []));
    onClose();
  };

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Edytuj uprawnienia"
      actionLabel="Zapisz"
      actionColorScheme="primary"
      onAction={handleSave}
      isActionLoading={saving}
    >
      <VStack spacing={4} align="stretch">
        <HStack spacing={3}>
          <UserAvatar firstName={member.firstName} lastName={member.lastName} />
          <VStack align="flex-start" spacing={0}>
            <HStack spacing={2}>
              <Text fontWeight="semibold" fontSize="sm">
                {member.firstName} {member.lastName}
              </Text>
              {member.isAdmin && (
                <Badge colorScheme="purple" fontSize="xs">
                  Admin
                </Badge>
              )}
            </HStack>
            <Text fontSize="xs" color="neutral.500">
              {member.email}
            </Text>
          </VStack>
        </HStack>

        <Divider />

        <Text fontSize="xs" fontWeight="semibold" color="neutral.500" textTransform="uppercase">
          Uprawnienia do modułów
        </Text>

        <Stack spacing={2}>
          {ALL_MODULES.map((mod) => (
            <Checkbox
              key={mod}
              isChecked={selectedModules.has(mod)}
              onChange={(e) => {
                setSelectedModules((prev) => {
                  const next = new Set(prev);
                  if (e.target.checked) {
                    next.add(mod);
                  } else {
                    next.delete(mod);
                  }
                  return next;
                });
              }}
            >
              {PROJECT_MODULE_LABELS[mod as ProjectModule]}
            </Checkbox>
          ))}
        </Stack>
      </VStack>
    </AppModal>
  );
}
