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
  Divider,
  useColorModeValue,
  Box,
  Icon,
  Input,
  FormControl,
  FormLabel,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
} from "@chakra-ui/react";
import { UserPlus, Check, Mail } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { useQueryClient } from "@tanstack/react-query";
import { projectKeys, projectInvitationKeys } from "../hooks/queries";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import { LoadingSpinner, EmptyState, UserAvatar, DataCard } from "./common";
import type { TenantMemberWeb, ProjectMemberWeb } from "../types/project.types";
import { tenantApi } from "../api/tenantApi";
import {
  ProjectModulePermissionPicker,
  createDefaultInviteModulesSet,
} from "./ProjectModulePermissionPicker";

interface AddProjectMemberModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  projectName: string;
  onMemberAdded?: () => void;
}

export default function AddProjectMemberModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  projectName,
  onMemberAdded,
}: AddProjectMemberModalProps): React.ReactElement {
  const { showError, showApiSuccess, showApiError } = useToastNotification();
  const queryClient = useQueryClient();
  const [tenantMembers, setTenantMembers] = useState<TenantMemberWeb[]>([]);
  const [projectMembers, setProjectMembers] = useState<ProjectMemberWeb[]>([]);
  const [loading, setLoading] = useState(false);
  const [adding, setAdding] = useState<string | null>(null);
  const [configuringUserId, setConfiguringUserId] = useState<string | null>(null);
  const [selectedModules, setSelectedModules] = useState<Set<number>>(new Set());
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteModules, setInviteModules] = useState<Set<number>>(createDefaultInviteModulesSet());
  const [sendingInvite, setSendingInvite] = useState(false);

  const scrollbarTrack = useColorModeValue("#f1f1f1", "#2d3748");
  const scrollbarThumb = useColorModeValue("#cbd5e0", "#4a5568");
  const scrollbarThumbHover = useColorModeValue("#a0aec0", "#718096");
  const memberCardBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    if (isOpen) {
      fetchData();
      setInviteModules(createDefaultInviteModulesSet());
      setInviteEmail("");
    }
  }, [isOpen, tenantId, projectId]);

  const fetchData = async (): Promise<void> => {
    setLoading(true);
    try {
      const [tenantMembersRes, projectMembersRes] = await Promise.all([
        tenantApi.getTenantMembers(tenantId),
        projectApi.getProjectMembers(tenantId, projectId),
      ]);

      const members: TenantMemberWeb[] = tenantMembersRes.data;
      setTenantMembers(members.filter((m) => m.isActive));
      setProjectMembers(projectMembersRes.data);
    } catch (error) {
      showApiError(error);
    } finally {
      setLoading(false);
    }
  };

  const invalidateMemberCaches = (): void => {
    queryClient.invalidateQueries({ queryKey: projectKeys.detail(tenantId, projectId) });
    queryClient.invalidateQueries({ queryKey: projectKeys.members(tenantId, projectId) });
    queryClient.invalidateQueries({ queryKey: projectInvitationKeys.byProject(tenantId, projectId) });
  };

  const handleAddMember = async (userId: string): Promise<void> => {
    setAdding(userId);
    try {
      const modules = Array.from(selectedModules);
      await projectApi.addProjectMember(tenantId, projectId, userId, modules);
      showApiSuccess("memberAdded");
      setConfiguringUserId(null);
      setSelectedModules(new Set());
      invalidateMemberCaches();
      await fetchData();
      onMemberAdded?.();
    } catch (error) {
      showApiError(error);
    } finally {
      setAdding(null);
    }
  };

  const handleInviteByEmail = async (): Promise<void> => {
    if (!inviteEmail.trim()) {
      showError("Błąd walidacji", "Adres email nie może być pusty");
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(inviteEmail)) {
      showError("Błąd walidacji", "Podaj prawidłowy adres email");
      return;
    }

    if (inviteModules.size === 0) {
      showError("Błąd walidacji", "Wybierz co najmniej jeden moduł");
      return;
    }

    setSendingInvite(true);
    try {
      await projectApi.inviteProjectMember(tenantId, projectId, {
        email: inviteEmail.trim(),
        modules: Array.from(inviteModules),
      });
      showApiSuccess("inviteSent");
      setInviteEmail("");
      setInviteModules(createDefaultInviteModulesSet());
      invalidateMemberCaches();
      await fetchData();
      onMemberAdded?.();
    } catch (error) {
      showApiError(error);
    } finally {
      setSendingInvite(false);
    }
  };

  const openConfig = (userId: string): void => {
    setConfiguringUserId(userId);
    setSelectedModules(new Set());
  };

  const cancelConfig = (): void => {
    setConfiguringUserId(null);
    setSelectedModules(new Set());
  };

  const isMemberInProject = (userId: string): boolean =>
    projectMembers.some((pm) => pm.userId === userId);

  const availableMembers = tenantMembers.filter((m) => !isMemberInProject(m.userId));

  const listScrollbarCss = {
    "&::-webkit-scrollbar": { width: "8px" },
    "&::-webkit-scrollbar-track": { background: scrollbarTrack },
    "&::-webkit-scrollbar-thumb": {
      background: scrollbarThumb,
      borderRadius: "4px",
    },
    "&::-webkit-scrollbar-thumb:hover": { background: scrollbarThumbHover },
  };

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
          ) : (
            <VStack align="stretch" spacing={6}>
              <Tabs variant="enclosed" colorScheme="primary">
                <TabList>
                  <Tab>Członkowie organizacji</Tab>
                  <Tab>Zaproś mailem</Tab>
                </TabList>
                <TabPanels>
                  <TabPanel px={0}>
                    {availableMembers.length === 0 ? (
                      <EmptyState
                        title="Wszyscy członkowie organizacji są już w projekcie"
                        description="Użyj zakładki „Zaproś mailem”, aby dodać osobę spoza organizacji"
                      />
                    ) : (
                      <Box maxH="400px" overflowY="auto" pr={2} css={listScrollbarCss}>
                        <VStack spacing={2} align="stretch">
                          {availableMembers.map((member) => (
                            <DataCard key={member.userId} p={3} hoverable>
                              <HStack justify="space-between">
                                <HStack spacing={3}>
                                  <UserAvatar firstName={member.firstName} lastName={member.lastName} />
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
                                  <ProjectModulePermissionPicker
                                    selectedModules={selectedModules}
                                    onChange={setSelectedModules}
                                    isDisabled={adding !== null}
                                  />
                                  <HStack spacing={2} justify="flex-end" mt={3}>
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
                                      isDisabled={adding !== null || selectedModules.size === 0}
                                    >
                                      Dodaj
                                    </Button>
                                  </HStack>
                                </Box>
                              )}
                            </DataCard>
                          ))}
                        </VStack>
                      </Box>
                    )}
                  </TabPanel>

                  <TabPanel px={0}>
                    <VStack align="stretch" spacing={4}>
                      <FormControl>
                        <FormLabel fontSize="sm">Adres email</FormLabel>
                        <Input
                          type="email"
                          value={inviteEmail}
                          onChange={(e) => setInviteEmail(e.target.value)}
                          placeholder="jan.kowalski@example.com"
                          onKeyDown={(e) => {
                            if (e.key === "Enter" && !sendingInvite) {
                              void handleInviteByEmail();
                            }
                          }}
                        />
                      </FormControl>
                      <ProjectModulePermissionPicker
                        selectedModules={inviteModules}
                        onChange={setInviteModules}
                        isDisabled={sendingInvite}
                      />
                      <Button
                        colorScheme="primary"
                        leftIcon={<Icon as={Mail} />}
                        onClick={() => void handleInviteByEmail()}
                        isLoading={sendingInvite}
                        loadingText="Wysyłanie..."
                        isDisabled={inviteModules.size === 0}
                      >
                        Wyślij zaproszenie
                      </Button>
                      <Text fontSize="xs" color="neutral.500">
                        Jeśli osoba jest już w organizacji, zostanie dodana od razu. W przeciwnym razie otrzyma zaproszenie e-mailem do projektu i organizacji.
                      </Text>
                    </VStack>
                  </TabPanel>
                </TabPanels>
              </Tabs>

              {projectMembers.length > 0 && (
                <Box>
                  <Text fontWeight="semibold" mb={3} fontSize="sm" color="neutral.600">
                    Już w projekcie ({projectMembers.length})
                  </Text>
                  <Box maxH="300px" overflowY="auto" pr={2} css={listScrollbarCss}>
                    <VStack spacing={2} align="stretch">
                      {projectMembers.map((member) => (
                        <DataCard key={member.userId} p={3} bg={memberCardBg}>
                          <HStack justify="space-between">
                            <HStack spacing={3}>
                              <UserAvatar
                                firstName={member.firstName}
                                lastName={member.lastName}
                                bg="green.600"
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
                            <Icon as={Check} boxSize={5} color="green.500" />
                          </HStack>
                        </DataCard>
                      ))}
                    </VStack>
                  </Box>
                </Box>
              )}
            </VStack>
          )}
        </ModalBody>
      </ModalContent>
    </Modal>
  );
}
