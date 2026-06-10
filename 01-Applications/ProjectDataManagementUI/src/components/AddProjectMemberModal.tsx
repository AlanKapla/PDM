import { useState, useEffect } from "react";
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalCloseButton,
  VStack,
  HStack,
  Text,
  Button,
  Checkbox,
  Stack,
  Divider,
  useColorModeValue,
  Box,
  Icon,
  Input,
  FormControl,
  FormLabel,
} from "@chakra-ui/react";
import { UserPlus, Check } from "lucide-react";
import { ProjectModule, PROJECT_MODULE_LABELS } from "../types/projectModulePermissions";
import { tenantApi } from "../api/tenantApi";
import { projectApi } from "../api/projectApi";
import { useQueryClient } from "@tanstack/react-query";
import { projectKeys } from "../hooks/queries";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from '../utils/handleApiError';
import { LoadingSpinner, EmptyState, UserAvatar, DataCard } from "./common";
import { useTenantPermissions } from "../hooks/useTenantPermissions";
import type { TenantMemberWeb, ProjectMemberWeb } from "../types/project.types";

const ALL_MODULES = (Object.values(ProjectModule) as number[]).filter(m => m !== ProjectModule.Settings);

interface AddProjectMemberModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  projectName: string;
  onMemberAdded?: () => void;
  isAdmin?: boolean; // Czy użytkownik jest adminem projektu
}

export default function AddProjectMemberModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  projectName,
  onMemberAdded
}: AddProjectMemberModalProps) {
  const { showError, showApiSuccess } = useToastNotification();
  const { canManageMembers } = useTenantPermissions();
  const queryClient = useQueryClient();
  const [tenantMembers, setTenantMembers] = useState<TenantMemberWeb[]>([]);
  const [projectMembers, setProjectMembers] = useState<ProjectMemberWeb[]>([]);
  const [loading, setLoading] = useState(false);
  const [adding, setAdding] = useState<string | null>(null);
  const [configuringUserId, setConfiguringUserId] = useState<string | null>(null);
  const [selectedModules, setSelectedModules] = useState<Set<number>>(new Set());
  const [showInviteForm, setShowInviteForm] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [sendingInvite, setSendingInvite] = useState(false);

  const bgColor = useColorModeValue("white", "gray.800");

  useEffect(() => {
    if (isOpen) {
      fetchData();
    }
  }, [isOpen, tenantId, projectId]);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [tenantMembersRes, projectMembersRes] = await Promise.all([
        tenantApi.getTenantMembers(tenantId),
        projectApi.getProjectMembers(tenantId, projectId),
      ]);

      const members: TenantMemberWeb[] = tenantMembersRes.data;
      setTenantMembers(members.filter(m => m.isActive));

      const projectMembers: ProjectMemberWeb[] = projectMembersRes.data;
      setProjectMembers(projectMembers);
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setLoading(false);
    }
  };

  const handleAddMember = async (userId: string) => {
    setAdding(userId);
    try {
      const modules = Array.from(selectedModules);

      await projectApi.addProjectMember(tenantId, projectId, userId, modules);
      
      showApiSuccess('memberAdded');
      setConfiguringUserId(null);
      setSelectedModules(new Set());
      
      // Invalidate project caches — permissions and members might have changed
      queryClient.invalidateQueries({
        queryKey: projectKeys.detail(tenantId, projectId),
      });
      queryClient.invalidateQueries({
        queryKey: projectKeys.members(tenantId, projectId),
      });
      
      // Odśwież listę członków
      await fetchData();
      onMemberAdded?.();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setAdding(null);
    }
  };

  const openConfig = (userId: string) => {
    setConfiguringUserId(userId);
    setSelectedModules(new Set());
  };

  const cancelConfig = () => {
    setConfiguringUserId(null);
    setSelectedModules(new Set());
  };

  const handleInviteToOrganization = async () => {
    if (!inviteEmail.trim()) {
      showError("Błąd walidacji", "Adres email nie może być pusty");
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(inviteEmail)) {
      showError("Błąd walidacji", "Podaj prawidłowy adres email");
      return;
    }

    setSendingInvite(true);
    try {
      await tenantApi.inviteMember(tenantId, inviteEmail);
      showApiSuccess('inviteSent');
      setShowInviteForm(false);
      setInviteEmail("");
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setSendingInvite(false);
    }
  };

  const isMemberInProject = (userId: String) => {
    return projectMembers.some(pm => pm.userId === userId);
  };

  const availableMembers = tenantMembers.filter(m => !isMemberInProject(m.userId));

  return (
    <Modal isOpen={isOpen} onClose={onClose} size={{ base: "full", md: "xl" }} scrollBehavior="inside">
      <ModalOverlay />
      <ModalContent mx={{ base: 0, md: "auto" }}>
        <ModalHeader fontSize={{ base: "lg", md: "xl" }}>
          <VStack align="flex-start" spacing={1}>
            <HStack spacing={2}>
              <Icon as={UserPlus} boxSize={{ base: 4, md: 5 }} />
              <Text fontSize={{ base: "sm", md: "md" }}>Dodaj członka do projektu</Text>
            </HStack>
            <Text fontSize={{ base: "xs", md: "sm" }} fontWeight="normal" color="neutral.500">
              {projectName}
            </Text>
          </VStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody pb={6}>
          {loading ? (
            <LoadingSpinner />
          ) : availableMembers.length === 0 ? (
            <EmptyState 
              title="Wszyscy członkowie są już w projekcie"
              description="Wszystkie osoby z organizacji zostały już dodane do tego projektu"
              action={
                canManageMembers ? (
                  showInviteForm ? (
                    <Box width="full" textAlign="left">
                      <VStack spacing={3} align="stretch">
                        <FormControl>
                          <FormLabel fontSize="sm">Adres email osoby zapraszanej</FormLabel>
                          <Input
                            type="email"
                            value={inviteEmail}
                            onChange={(e) => setInviteEmail(e.target.value)}
                            placeholder="jan.kowalski@example.com"
                            onKeyDown={(e) => {
                              if (e.key === "Enter" && !sendingInvite) {
                                handleInviteToOrganization();
                              }
                            }}
                          />
                        </FormControl>
                        <HStack spacing={2} justify="center">
                          <Button
                            size="sm"
                            colorScheme="primary"
                            onClick={handleInviteToOrganization}
                            isLoading={sendingInvite}
                            loadingText="Wysyłanie..."
                            leftIcon={<Icon as={UserPlus} />}
                          >
                            Wyślij zaproszenie
                          </Button>
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => {
                              setShowInviteForm(false);
                              setInviteEmail("");
                            }}
                            isDisabled={sendingInvite}
                          >
                            Anuluj
                          </Button>
                        </HStack>
                      </VStack>
                    </Box>
                  ) : (
                    <Button
                      size="sm"
                      colorScheme="primary"
                      variant="outline"
                      leftIcon={<Icon as={UserPlus} />}
                      onClick={() => setShowInviteForm(true)}
                    >
                      Zaproś nowego członka do organizacji
                    </Button>
                  )
                ) : undefined
              }
            />
          ) : (
            <Box
              maxH="400px"
              overflowY="auto"
              pr={2}
              css={{
                '&::-webkit-scrollbar': {
                  width: '8px',
                },
                '&::-webkit-scrollbar-track': {
                  background: useColorModeValue('#f1f1f1', '#2d3748'),
                },
                '&::-webkit-scrollbar-thumb': {
                  background: useColorModeValue('#cbd5e0', '#4a5568'),
                  borderRadius: '4px',
                },
                '&::-webkit-scrollbar-thumb:hover': {
                  background: useColorModeValue('#a0aec0', '#718096'),
                },
              }}
            >
              <VStack spacing={2} align="stretch">
                {availableMembers.map((member) => {

                  return (
                    <DataCard
                      key={member.userId}
                      p={3}
                      hoverable
                    >
                      <HStack justify="space-between">
                        <HStack spacing={3}>
                          <UserAvatar 
                            firstName={member.firstName}
                            lastName={member.lastName}
                          />
                          <VStack align="flex-start" spacing={0}>
                            <Text fontWeight="medium" fontSize="sm">
                              {member.firstName} {member.lastName}
                            </Text>
                            <Text fontSize="xs" color="neutral.500">
                              {member.email}
                            </Text>
                          </VStack>
                        </HStack>
                        {configuringUserId !== member.userId && (
                          <Button
                            size="sm"
                            colorScheme="primary"
                            onClick={() => openConfig(member.userId)}
                            isDisabled={adding !== null}
                          >
                            Konfiguruj dostęp
                          </Button>
                        )}
                      </HStack>

                      {configuringUserId === member.userId && (
                        <Box mt={3}>
                          <Divider mb={3} />
                          <Text fontSize="xs" fontWeight="semibold" color="neutral.500" mb={2}>
                            UPRAWNIENIA DO MODUŁÓW
                          </Text>
                          <Stack spacing={2} mb={3}>
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
                          <HStack spacing={2} justify="flex-end">
                            <Button size="sm" variant="ghost" onClick={cancelConfig} isDisabled={adding !== null}>
                              Anuluj
                            </Button>
                            <Button
                              size="sm"
                              colorScheme="primary"
                              leftIcon={adding === member.userId ? undefined : <Icon as={UserPlus} />}
                              onClick={() => handleAddMember(member.userId)}
                              isLoading={adding === member.userId}
                              loadingText="Dodawanie..."
                              isDisabled={adding !== null}
                            >
                              Dodaj
                            </Button>
                          </HStack>
                        </Box>
                      )}
                    </DataCard>
                  );
                })}
              </VStack>
            </Box>
          )}

          {projectMembers.length > 0 && (
            <Box mt={6}>
              <Text fontWeight="semibold" mb={3} fontSize="sm" color="neutral.600">
                Już w projekcie ({projectMembers.length})
              </Text>
              <Box
                maxH="300px"
                overflowY="auto"
                pr={2}
                css={{
                  '&::-webkit-scrollbar': {
                    width: '8px',
                  },
                  '&::-webkit-scrollbar-track': {
                    background: useColorModeValue('#f1f1f1', '#2d3748'),
                  },
                  '&::-webkit-scrollbar-thumb': {
                    background: useColorModeValue('#cbd5e0', '#4a5568'),
                    borderRadius: '4px',
                  },
                  '&::-webkit-scrollbar-thumb:hover': {
                    background: useColorModeValue('#a0aec0', '#718096'),
                  },
                }}
              >
                <VStack spacing={2} align="stretch">
                  {projectMembers.map((member) => {
                    return (
                      <DataCard
                        key={member.userId}
                        p={3}
                        bg={useColorModeValue("gray.50", "gray.700")}
                      >
                        <HStack justify="space-between">
                          <HStack spacing={3}>
                            <UserAvatar 
                              firstName={member.firstName}
                              lastName={member.lastName}
                              bg="green.600"
                            />
                            <VStack align="flex-start" spacing={0}>
                              <HStack>
                                <Text fontWeight="medium" fontSize="sm">
                                  {member.firstName} {member.lastName}
                                </Text>
                              </HStack>
                              <Text fontSize="xs" color="neutral.500">
                                {member.email}
                              </Text>
                            </VStack>
                          </HStack>
                          <Icon as={Check} boxSize={5} color="green.500" />
                        </HStack>
                      </DataCard>
                    );
                  })}
                </VStack>
              </Box>
            </Box>
          )}
        </ModalBody>
      </ModalContent>
    </Modal>
  );
}
