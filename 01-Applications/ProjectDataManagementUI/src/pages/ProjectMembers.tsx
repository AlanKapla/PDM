import React, { useState } from "react";
import { useParams } from "react-router-dom";
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
  Tooltip,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Tabs,
  TabList,
  Tab,
  TabPanels,
  TabPanel,
} from "@chakra-ui/react";
import { Users, UserPlus, Trash2, Settings } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import AddProjectMemberModal from "../components/AddProjectMemberModal";
import EditProjectMemberModal from "../components/EditProjectMemberModal";
import { useAuth } from "../context/AuthContext";
import { useProjectPermissions } from "../hooks/useProjectPermissions";
import { BackToProjectButton, LoadingSpinner, EmptyState } from "../components/common";
import { DeleteAlertDialog } from "../components/ui";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import {
  useProjectDetails,
  useProjectMembers,
  projectKeys,
  useProjectInvitations,
  projectInvitationKeys,
} from "../hooks/queries";
import { useQueryClient } from "@tanstack/react-query";
import { formatDate, formatDateShort } from "../utils/formatters";
import { projectApi } from "../api/projectApi";
import type { ProjectMemberWeb } from "../types/project.types";
import { getInvitationStatusColor, getInvitationStatusName } from "../types/auth.types";
import { PROJECT_MODULE_LABELS } from "../types/projectModulePermissions";
import { ProjectModule } from "../types/projectModulePermissions";

export default function ProjectMembers(): React.ReactElement {
  const { projectId } = useParams<{ projectId: string }>();
  const { user } = useAuth();
  const permissions = useProjectPermissions(projectId);
  const {showError, showApiSuccess, showApiError } = useToastNotification();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();
  const { isOpen: isRevokeInviteOpen, onOpen: onRevokeInviteOpen, onClose: onRevokeInviteClose } = useDisclosure();

  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);
  const [memberToEdit, setMemberToEdit] = useState<ProjectMemberWeb | null>(null);
  const [invitationToRevoke, setInvitationToRevoke] = useState<string | null>(null);
  const [revokingInvitationId, setRevokingInvitationId] = useState<string | null>(null);
  const { isOpen: isEditOpen, onOpen: onEditOpen, onClose: onEditClose } = useDisclosure();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  const tenantId = user?.activeTenantId ?? undefined;
  const { data: project, isLoading: loadingProject } = useProjectDetails(tenantId, projectId);
  const { data: membersData, isLoading: loadingMembers } = useProjectMembers(tenantId, projectId);
  const { data: invitations = [], isLoading: loadingInvitations } = useProjectInvitations(
    tenantId ?? "",
    projectId ?? "",
    !!tenantId && !!projectId && permissions.isAdmin
  );

  const members = membersData ?? [];
  const loading = loadingProject || loadingMembers || loadingInvitations;

  const queryClient = useQueryClient();

  const invalidateCaches = (): void => {
    if (!tenantId || !projectId) {
      return;
    }
    queryClient.invalidateQueries({ queryKey: projectKeys.members(tenantId, projectId) });
    queryClient.invalidateQueries({ queryKey: projectKeys.detail(tenantId, projectId) });
    queryClient.invalidateQueries({ queryKey: projectInvitationKeys.byProject(tenantId, projectId) });
  };

  const handleEditMember = (member: ProjectMemberWeb): void => {
    setMemberToEdit(member);
    onEditOpen();
  };

  const handleEditClose = (): void => {
    setMemberToEdit(null);
    onEditClose();
  };

  const handleRemoveMemberClick = (userId: string, name: string): void => {
    setMemberToRemove({ userId, name });
    onRemoveModalOpen();
  };

  const handleRemoveMember = async (): Promise<void> => {
    if (!memberToRemove || !tenantId || !projectId) {
      return;
    }

    setRemovingMember(memberToRemove.userId);
    try {
      await projectApi.removeProjectMember(tenantId, projectId, memberToRemove.userId);
      showApiSuccess("memberRemoved");
      invalidateCaches();
      onRemoveModalClose();
    } catch (error) {
      showApiError(error);
    } finally {
      setRemovingMember(null);
      setMemberToRemove(null);
    }
  };

  const handleRevokeInvitationClick = (invitationId: string): void => {
    setInvitationToRevoke(invitationId);
    onRevokeInviteOpen();
  };

  const handleRevokeInvitation = async (): Promise<void> => {
    if (!invitationToRevoke || !tenantId || !projectId) {
      return;
    }

    setRevokingInvitationId(invitationToRevoke);
    try {
      await projectApi.revokeProjectInvitation(tenantId, projectId, invitationToRevoke);
      showApiSuccess("inviteCancelled");
      invalidateCaches();
      onRevokeInviteClose();
    } catch (error) {
      showApiError(error);
    } finally {
      setRevokingInvitationId(null);
      setInvitationToRevoke(null);
    }
  };

  const formatModuleLabels = (modules: number[]): string =>
    modules.map((m) => PROJECT_MODULE_LABELS[m as ProjectModule]).join(", ");

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
        <BackToProjectButton />
        {!permissions.isAdmin ? (
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
              <Button
                leftIcon={<UserPlus size={16} />}
                colorScheme="primary"
                onClick={onOpen}
                size={{ base: "sm", md: "md" }}
                fontSize={{ base: "xs", md: "sm" }}
              >
                Dodaj członka
              </Button>
            </HStack>

            <Box bg="white" rounded="lg" borderWidth="1px" borderColor="neutral.200">
              <Tabs>
                <TabList px={{ base: 3, md: 4 }} pt={2}>
                  <Tab>
                    <HStack spacing={2}>
                      <Text>Członkowie</Text>
                      <Badge>{members.length}</Badge>
                    </HStack>
                  </Tab>
                  <Tab>
                    <HStack spacing={2}>
                      <Text>Zaproszenia</Text>
                      <Badge>{invitations.length}</Badge>
                    </HStack>
                  </Tab>
                </TabList>

                <TabPanels>
                  <TabPanel p={0}>
                    {members.length === 0 ? (
                      <Box p={{ base: 3, md: 4 }}>
                        <EmptyState
                          icon={Users}
                          title="Brak członków"
                          description="Ten projekt nie ma jeszcze żadnych członków"
                        />
                      </Box>
                    ) : (
                      <Box overflowX="auto" fontSize={{ base: "xs", md: "sm" }}>
                        <Table variant="simple" size={{ base: "sm", md: "md" }}>
                          <Thead>
                            <Tr>
                              <Th fontSize={{ base: "xs", md: "sm" }}>Imię i nazwisko</Th>
                              <Th fontSize={{ base: "xs", md: "sm" }}>Rola</Th>
                              <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>Email</Th>
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
                                    <Badge ml={2} colorScheme="green" fontSize="xs">Ty</Badge>
                                  )}
                                </Td>
                                <Td fontSize={{ base: "xs", md: "sm" }}>
                                  <Badge colorScheme={member.isAdmin ? "purple" : "gray"} fontSize="xs">
                                    {member.isAdmin ? "Admin" : "Członek"}
                                  </Badge>
                                </Td>
                                <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>{member.email}</Td>
                                <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", lg: "table-cell" }}>{formatDate(member.joinedAt)}</Td>
                                <Td fontSize={{ base: "xs", md: "sm" }}>
                                  <HStack spacing={1}>
                                    <Tooltip label={user?.id === member.userId ? "Nie możesz edytować własnych uprawnień" : member.isAdmin && !permissions.isAdmin ? "Nie możesz edytować uprawnień admina" : "Edytuj uprawnienia"}>
                                      <IconButton
                                        aria-label="Edytuj uprawnienia"
                                        icon={<Settings size={16} />}
                                        size="sm"
                                        colorScheme="primary"
                                        variant="ghost"
                                        isDisabled={user?.id === member.userId || (member.isAdmin && !permissions.isAdmin)}
                                        onClick={() => handleEditMember(member)}
                                      />
                                    </Tooltip>
                                    {member.email.toLowerCase() !== user?.email.toLowerCase() && (
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
                                </Td>
                              </Tr>
                            ))}
                          </Tbody>
                        </Table>
                      </Box>
                    )}
                  </TabPanel>

                  <TabPanel p={0}>
                    {invitations.length === 0 ? (
                      <Box p={{ base: 3, md: 4 }}>
                        <Text color="neutral.500" textAlign="center">
                          Brak aktywnych zaproszeń
                        </Text>
                      </Box>
                    ) : (
                      <Box overflowX="auto" fontSize={{ base: "xs", md: "sm" }}>
                        <Table variant="simple" size={{ base: "sm", md: "md" }}>
                          <Thead>
                            <Tr>
                              <Th fontSize={{ base: "xs", md: "sm" }}>Email</Th>
                              <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", lg: "table-cell" }}>Zaproszony przez</Th>
                              <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>Moduły</Th>
                              <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>Organizacja</Th>
                              <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>Wygasa</Th>
                              <Th fontSize={{ base: "xs", md: "sm" }}>Status</Th>
                              <Th fontSize={{ base: "xs", md: "sm" }}>Akcje</Th>
                            </Tr>
                          </Thead>
                          <Tbody>
                            {invitations.map((invitation) => (
                              <Tr key={invitation.invitationId}>
                                <Td fontSize={{ base: "xs", md: "sm" }}>{invitation.email}</Td>
                                <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", lg: "table-cell" }}>
                                  {invitation.invitedByUserName}
                                </Td>
                                <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>
                                  {formatModuleLabels(invitation.modules)}
                                </Td>
                                <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>
                                  {invitation.tenantName}
                                </Td>
                                <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>
                                  {invitation.expiresAt ? formatDateShort(invitation.expiresAt) : "—"}
                                </Td>
                                <Td fontSize={{ base: "xs", md: "sm" }}>
                                  <Badge colorScheme={getInvitationStatusColor(invitation.status)} fontSize="xs">
                                    {getInvitationStatusName(invitation.status)}
                                  </Badge>
                                </Td>
                                <Td>
                                  <IconButton
                                    aria-label="Anuluj zaproszenie"
                                    icon={<Trash2 size={16} />}
                                    size="sm"
                                    colorScheme="red"
                                    variant="ghost"
                                    isLoading={revokingInvitationId === invitation.invitationId}
                                    onClick={() => handleRevokeInvitationClick(invitation.invitationId)}
                                  />
                                </Td>
                              </Tr>
                            ))}
                          </Tbody>
                        </Table>
                      </Box>
                    )}
                  </TabPanel>
                </TabPanels>
              </Tabs>
            </Box>
          </>
        )}

        <AddProjectMemberModal
          isOpen={isOpen}
          onClose={onClose}
          projectId={projectId || ""}
          tenantId={tenantId || ""}
          projectName={project?.name || ""}
          onMemberAdded={invalidateCaches}
        />

        <DeleteAlertDialog
          isOpen={isRemoveModalOpen}
          onClose={onRemoveModalClose}
          onConfirm={handleRemoveMember}
          itemName={memberToRemove?.name}
          isLoading={removingMember !== null}
        />

        <DeleteAlertDialog
          isOpen={isRevokeInviteOpen}
          onClose={onRevokeInviteClose}
          onConfirm={handleRevokeInvitation}
          isLoading={revokingInvitationId !== null}
        />

        {memberToEdit && (
          <EditProjectMemberModal
            isOpen={isEditOpen}
            onClose={handleEditClose}
            tenantId={tenantId || ""}
            projectId={projectId || ""}
            member={memberToEdit}
          />
        )}
      </Box>
    </MainLayout>
  );
}
