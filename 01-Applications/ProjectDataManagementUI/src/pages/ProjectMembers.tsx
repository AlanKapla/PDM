import React, { useEffect, useState, useContext } from "react";
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
  useDisclosure,
  IconButton,
  Select,
  Tooltip,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
} from "@chakra-ui/react";
import { ArrowLeft, Users, UserPlus, Trash2, Shield, Save, X } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import AddProjectMemberModal from "../components/AddProjectMemberModal";
import { useAuth } from "../context/AuthContext";
import { useProjectPermissions } from "../hooks/useProjectPermissions";
import { LoadingSpinner, EmptyState } from "../components/common";
import { DeleteAlertDialog } from "../components/ui";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import { useProjectDetails, useProjectMembers, projectKeys } from "../hooks/queries";
import { useQueryClient } from "@tanstack/react-query";
import { formatDate } from "../utils/formatters";
import { projectApi } from "../api/projectApi";
import { roleApi, type RoleWeb } from "../api/roleApi";
import type { ProjectMemberWeb } from "../types/project.types";
import { getRoleName, getRoleColor } from "../constants/roleCodes";

export default function ProjectMembers() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const permissions = useProjectPermissions(projectId);
  const { showSuccess, showError, showApiSuccess } = useToastNotification();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();

  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);

  const [editingRoleMemberId, setEditingRoleMemberId] = useState<string | null>(null);
  const [editedRoleId, setEditedRoleId] = useState<string>("");
  const [updatingRole, setUpdatingRole] = useState(false);
  const [availableRoles, setAvailableRoles] = useState<RoleWeb[]>([]);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  // React Query — dane projektu i członkowie (współdzielony cache między stronami projektu)
  const { data: project, isLoading: loadingProject } = useProjectDetails(
    user?.activeTenantId ?? undefined,
    projectId
  );
  const { data: membersData, isLoading: loadingMembers } = useProjectMembers(
    user?.activeTenantId ?? undefined,
    projectId
  );
  const members = membersData ?? [];
  const loading = loadingProject || loadingMembers;

  const queryClient = useQueryClient();

  useEffect(() => {
    fetchRoles();
  }, [projectId]);

  const fetchRoles = async () => {
    try {
      const roles = await roleApi.getAvailableRoles('project');
      setAvailableRoles(roles);
    } catch (error) {
    }
  };

  const handleRemoveMemberClick = (userId: string, name: string) => {
    setMemberToRemove({ userId, name });
    onRemoveModalOpen();
  };

  const handleRemoveMember = async () => {
    if (!memberToRemove || !user?.activeTenantId || !projectId) return;

    setRemovingMember(memberToRemove.userId);
    try {
      await projectApi.removeProjectMember(
        user.activeTenantId,
        projectId,
        memberToRemove.userId
      );

      showApiSuccess('memberRemoved');
      queryClient.invalidateQueries({
        queryKey: projectKeys.members(user.activeTenantId!, projectId!)
      });
      queryClient.invalidateQueries({
        queryKey: projectKeys.detail(user.activeTenantId!, projectId!)
      });
      onRemoveModalClose();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setRemovingMember(null);
      setMemberToRemove(null);
    }
  };

  const handleUpdateMemberRole = async (userId: string) => {
    if (!user?.activeTenantId || !projectId) return;

    if (user.id === userId) {
      showError("Nie możesz zmienić własnej roli");
      return;
    }

    setUpdatingRole(true);
    try {
      await projectApi.updateProjectMemberRole(
        user.activeTenantId,
        projectId,
        userId,
        editedRoleId
      );

      // Odśwież dane po zmianie
      queryClient.invalidateQueries({
        queryKey: projectKeys.members(user.activeTenantId!, projectId!)
      });
      queryClient.invalidateQueries({
        queryKey: projectKeys.detail(user.activeTenantId!, projectId!)
      });
      setEditingRoleMemberId(null);
      showApiSuccess('memberUpdated');
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setUpdatingRole(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie członków..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        {!permissions.canViewMembers ? (
          <Box bg={cardBg} p={8} rounded="lg" borderWidth="1px" borderColor={borderColor}>
            <VStack spacing={4}>
              <Heading size="md" color="red.500">Brak dostępu</Heading>
              <Text>Nie masz uprawnień do przeglądania członków tego projektu.</Text>
              <Text fontSize="sm" color="neutral.600">Wymagana rola: co najmniej Przeglądający</Text>
            </VStack>
          </Box>
        ) : (
          <>
        <HStack justify="space-between" mb={{ base: 6, md: 8 }} flexWrap="wrap" gap={{ base: 2, md: 4 }}>
          <HStack spacing={{ base: 2, md: 3 }}>
            <Icon as={Users} boxSize={{ base: 6, md: 8 }} color="primary.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size={{ base: "md", md: "lg" }}>Członkowie projektu</Heading>
              {project && <Text fontSize={{ base: "xs", md: "sm" }} color="neutral.600">{project.name}</Text>}
            </VStack>
          </HStack>
          {permissions.canManageMembers && (
            <Button
              leftIcon={<UserPlus size={16} />}
              colorScheme="primary"
              onClick={onOpen}
              size={{ base: "sm", md: "md" }}
              fontSize={{ base: "xs", md: "sm" }}
            >
              Dodaj członka
            </Button>
          )}
        </HStack>

        {members.length === 0 ? (
          <EmptyState
            icon={Users}
            title="Brak członków"
            description="Ten projekt nie ma jeszcze żadnych członków"
          />
        ) : (
          <Box 
            bg="white" 
            rounded="lg" 
            borderWidth="1px" 
            borderColor="neutral.200"
            overflowX="auto"
            fontSize={{ base: "xs", md: "sm" }}
          >
            <Table variant="simple" size={{ base: "sm", md: "md" }}>
              <Thead>
                <Tr>
                  <Th fontSize={{ base: "xs", md: "sm" }}>Imię i nazwisko</Th>
                  <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>Email</Th>
                  <Th fontSize={{ base: "xs", md: "sm" }}>Rola</Th>
                  <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", lg: "table-cell" }}>Data dołączenia</Th>
                  <Th fontSize={{ base: "xs", md: "sm" }}>Akcje</Th>
                </Tr>
              </Thead>
              <Tbody>
                {members.map((member) => (
                  <Tr key={member.userId}>
                    <Td fontSize={{ base: "xs", md: "sm" }}>
                      {member.firstName} {member.lastName}
                      {user?.id === member.userId && (
                        <Badge ml={2} colorScheme="green" fontSize="xs">
                          Ty
                        </Badge>
                      )}
                    </Td>
                    <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>{member.email}</Td>
                    <Td fontSize={{ base: "xs", md: "sm" }}>
                      <Badge colorScheme={getRoleColor(member.roleCode)}>
                        {getRoleName(member.roleCode)}
                      </Badge>
                    </Td>
                    <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", lg: "table-cell" }}>{formatDate(member.joinedAt)}</Td>
                    <Td fontSize={{ base: "xs", md: "sm" }}>
                      {editingRoleMemberId === member.userId ? (
                        <HStack spacing={2}>
                          <Select
                            size="sm"
                            value={editedRoleId}
                            onChange={(e) => setEditedRoleId(e.target.value)}
                            isDisabled={updatingRole}
                            width="150px"
                          >
                            {availableRoles.map((role) => (
                              <option key={role.id} value={role.id}>
                                {getRoleName(role.code)}
                              </option>
                            ))}
                          </Select>
                          <Tooltip label="Zapisz rolę">
                            <IconButton
                              aria-label="Zapisz rolę"
                              icon={<Save size={14} />}
                              size="sm"
                              colorScheme="primary"
                              onClick={() => handleUpdateMemberRole(member.userId)}
                              isLoading={updatingRole}
                            />
                          </Tooltip>
                          <Tooltip label="Anuluj">
                            <IconButton
                              aria-label="Anuluj"
                              icon={<X size={14} />}
                              size="sm"
                              variant="ghost"
                              onClick={() => setEditingRoleMemberId(null)}
                              isDisabled={updatingRole}
                            />
                          </Tooltip>
                        </HStack>
                      ) : (
                        <HStack spacing={2}>
                          {permissions.canManageMembers && member.email.toLowerCase() !== user?.email.toLowerCase() && (
                            <Tooltip label="Zmień rolę">
                              <IconButton
                                aria-label="Edytuj rolę"
                                icon={<Shield size={14} />}
                                size="sm"
                                variant="ghost"
                                onClick={() => {
                                  setEditingRoleMemberId(member.userId);
                                  // Znajdź role ID na podstawie roleCode
                                  const role = availableRoles.find(r => r.code === member.roleCode);
                                  setEditedRoleId(role?.id || "");
                                }}
                              />
                            </Tooltip>
                          )}
                          {permissions.canManageMembers && member.email.toLowerCase() !== user?.email.toLowerCase() && (
                            <Tooltip label="Usuń członka">
                              <IconButton
                                aria-label="Usuń członka"
                                icon={<Trash2 size={16} />}
                                size="sm"
                                colorScheme="red"
                                variant="ghost"
                                onClick={() => handleRemoveMemberClick(member.userId, `${member.firstName} ${member.lastName}`)}
                              />
                            </Tooltip>
                          )}
                        </HStack>
                      )}
                    </Td>
                  </Tr>
                ))}
              </Tbody>
            </Table>
          </Box>
        )}
        </>
        )}

        <AddProjectMemberModal
          isOpen={isOpen}
          onClose={onClose}
          projectId={projectId || ""}
          tenantId={user?.activeTenantId || ""}
          projectName={project?.name || ""}
          onMemberAdded={() => queryClient.invalidateQueries({ queryKey: projectKeys.members(user?.activeTenantId ?? '', projectId ?? '') })}
        />

        <DeleteAlertDialog
          isOpen={isRemoveModalOpen}
          onClose={onRemoveModalClose}
          onConfirm={handleRemoveMember}
          itemName={memberToRemove?.name}
          isLoading={removingMember !== null}
        />
      </Box>
    </MainLayout>
  );
}
