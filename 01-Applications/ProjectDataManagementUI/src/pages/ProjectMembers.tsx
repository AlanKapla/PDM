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
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
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
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import { projectApi } from "../api/projectApi";
import { roleApi, type RoleWeb } from "../api/roleApi";
import type { ProjectDetailsWeb, ProjectMemberWeb } from "../types/project.types";
import { getRoleName, getRoleColor } from "../constants/roleCodes";

export default function ProjectMembers() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const permissions = useProjectPermissions(projectId);
  const { showSuccess, showError } = useToastNotification();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();

  const [loading, setLoading] = useState(true);
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [project, setProject] = useState<ProjectDetailsWeb | null>(null);
  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);

  const [editingRoleMemberId, setEditingRoleMemberId] = useState<string | null>(null);
  const [editedRoleId, setEditedRoleId] = useState<string>("");
  const [updatingRole, setUpdatingRole] = useState(false);
  const [availableRoles, setAvailableRoles] = useState<RoleWeb[]>([]);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    fetchData();
    fetchRoles();
  }, [projectId]);

  const fetchRoles = async () => {
    try {
      const roles = await roleApi.getAvailableRoles('project');
      setAvailableRoles(roles);
    } catch (error) {
      console.error("Błąd ładowania ról:", error);
    }
  };

  const fetchData = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    try {
      const [projectRes, membersRes] = await Promise.all([
        projectApi.getProjectDetails(user.activeTenantId, projectId),
        projectApi.getProjectMembers(user.activeTenantId, projectId),
      ])

      setProject(projectRes.data);
      setMembers(membersRes.data);
    } catch (error) {
      showError("Nie udało się pobrać danych");
    } finally {
      setLoading(false);
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

      showSuccess(`Usunięto członka: ${memberToRemove.name}`);
      setMembers((prev) => prev.filter((m) => m.userId !== memberToRemove.userId));
      onRemoveModalClose();
    } catch (error) {
      showError("Błąd podczas usuwania członka");
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
      await fetchData();
      setEditingRoleMemberId(null);
      showSuccess("Zaktualizowano rolę członka");
    } catch (error) {
      showError("Nie udało się zmienić roli");
    } finally {
      setUpdatingRole(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 10 }} minH="100vh">
          <LoadingSpinner message="Ładowanie członków..." />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        {!permissions.canViewMembers ? (
          <Box bg={cardBg} p={8} rounded="lg" borderWidth="1px" borderColor={borderColor}>
            <VStack spacing={4}>
              <Heading size="md" color="red.500">Brak dostępu</Heading>
              <Text>Nie masz uprawnień do przeglądania członków tego projektu.</Text>
              <Text fontSize="sm" color="gray.600">Wymagana rola: conajmniej Przeglądający</Text>
            </VStack>
          </Box>
        ) : (
          <>
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={Users} boxSize={8} color="blue.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Członkowie projektu</Heading>
              {project && <Text fontSize="sm" color="gray.600">{project.name}</Text>}
            </VStack>
          </HStack>
          {permissions.canManageMembers && (
            <Button
              leftIcon={<UserPlus size={18} />}
              colorScheme="blue"
              onClick={onOpen}
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
          <Box bg={cardBg} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
            <Table variant="simple" size="sm">
              <Thead>
                <Tr>
                  <Th>Imię i nazwisko</Th>
                  <Th>Email</Th>
                  <Th>Rola</Th>
                  <Th>Data dołączenia</Th>
                  <Th>Akcje</Th>
                </Tr>
              </Thead>
              <Tbody>
                {members.map((member) => (
                  <Tr key={member.userId}>
                    <Td>
                      {member.firstName} {member.lastName}
                      {user?.id === member.userId && (
                        <Badge ml={2} colorScheme="green" fontSize="xs">
                          Ty
                        </Badge>
                      )}
                    </Td>
                    <Td>{member.email}</Td>
                    <Td>
                      <Badge colorScheme={getRoleColor(member.roleCode)}>
                        {getRoleName(member.roleCode)}
                      </Badge>
                    </Td>
                    <Td>{formatDate(member.joinedAt)}</Td>
                    <Td>
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
                                {role.name}
                              </option>
                            ))}
                          </Select>
                          <IconButton
                            aria-label="Zapisz rolę"
                            icon={<Save size={14} />}
                            size="sm"
                            colorScheme="green"
                            onClick={() => handleUpdateMemberRole(member.userId)}
                            isLoading={updatingRole}
                          />
                          <IconButton
                            aria-label="Anuluj"
                            icon={<X size={14} />}
                            size="sm"
                            variant="ghost"
                            onClick={() => setEditingRoleMemberId(null)}
                            isDisabled={updatingRole}
                          />
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
          onMemberAdded={fetchData}
        />

        <Modal isOpen={isRemoveModalOpen} onClose={onRemoveModalClose}>
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>Usuń członka</ModalHeader>
            <ModalCloseButton />
            <ModalBody>
              Czy na pewno chcesz usunąć <strong>{memberToRemove?.name}</strong> z projektu?
            </ModalBody>
            <ModalFooter>
              <Button variant="ghost" mr={3} onClick={onRemoveModalClose}>
                Anuluj
              </Button>
              <Button
                colorScheme="red"
                onClick={handleRemoveMember}
                isLoading={removingMember !== null}
              >
                Usuń
              </Button>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </Box>
    </MainLayout>
  );
}
