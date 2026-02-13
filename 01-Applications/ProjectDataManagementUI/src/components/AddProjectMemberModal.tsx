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
  Badge,
  useColorModeValue,
  Box,
  Icon,
} from "@chakra-ui/react";
import { UserPlus, Check } from "lucide-react";
import { tenantApi } from "../api/tenantApi";
import { projectApi } from "../api/projectApi";
import { useProjectCache } from "../hooks/useProjectCache";
import { getRoleName, getRoleColor } from "../constants/roleCodes";
import { useToastNotification } from "../hooks/useToastNotification";
import { LoadingSpinner, EmptyState, UserAvatar, DataCard } from "./common";
import type { TenantMemberWeb, ProjectMemberWeb } from "../types/project.types";

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
  const { showSuccess, showError } = useToastNotification();
  const { invalidateProject } = useProjectCache();
  const [tenantMembers, setTenantMembers] = useState<TenantMemberWeb[]>([]);
  const [projectMembers, setProjectMembers] = useState<ProjectMemberWeb[]>([]);
  const [loading, setLoading] = useState(false);
  const [adding, setAdding] = useState<string | null>(null);

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
      console.error("Błąd pobierania członków:", error);
      showError("Błąd", "Nie udało się pobrać listy członków");
    } finally {
      setLoading(false);
    }
  };

  const handleAddMember = async (userId: string) => {
    setAdding(userId);
    try {
      await projectApi.addProjectMember(tenantId, projectId, userId);
      
      showSuccess("Sukces", "Członek został dodany do projektu");
      
      // Invalidate project cache - permissions might have changed
      invalidateProject(projectId);
      
      // Odśwież listę członków
      await fetchData();
      onMemberAdded?.();
    } catch (error) {
      console.error("Błąd dodawania członka:", error);
      const errorModule = await import("../utils/handleApiError");
      const { title, description } = errorModule.handleApiError(error);
      showError(title, description);
    } finally {
      setAdding(null);
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
            <Text fontSize={{ base: "xs", md: "sm" }} fontWeight="normal" color="gray.500">
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
                  const isAdding = adding === member.userId;

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
                            <Text fontSize="xs" color="gray.500">
                              {member.email}
                            </Text>
                          </VStack>
                        </HStack>
                        <Button
                          size="sm"
                          colorScheme="blue"
                          leftIcon={isAdding ? undefined : <Icon as={UserPlus} />}
                          onClick={() => handleAddMember(member.userId)}
                          isLoading={isAdding}
                          loadingText="Dodawanie..."
                          isDisabled={adding !== null}
                        >
                          Dodaj
                        </Button>
                      </HStack>
                    </DataCard>
                  );
                })}
              </VStack>
            </Box>
          )}

          {projectMembers.length > 0 && (
            <Box mt={6}>
              <Text fontWeight="semibold" mb={3} fontSize="sm" color="gray.600">
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
                                <Badge colorScheme={getRoleColor(member.roleCode)} fontSize="xs">
                                  {getRoleName(member.roleCode)}
                                </Badge>
                              </HStack>
                              <Text fontSize="xs" color="gray.500">
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
