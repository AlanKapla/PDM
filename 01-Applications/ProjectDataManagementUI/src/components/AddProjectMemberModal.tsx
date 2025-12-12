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
import { getProjectRoleName, getProjectRoleColor } from "../utils/constants";
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

      if (tenantMembersRes.ok) {
        const members: TenantMemberWeb[] = await tenantMembersRes.json();
        setTenantMembers(members.filter(m => m.isActive));
      }

      if (projectMembersRes.ok) {
        const members: ProjectMemberWeb[] = await projectMembersRes.json();
        setProjectMembers(members);
      }
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
      const response = await projectApi.addProjectMember(tenantId, projectId, userId);
      
      if (response.ok) {
        showSuccess("Sukces", "Członek został dodany do projektu");
        
        // Odśwież listę członków
        await fetchData();
        onMemberAdded?.();
      } else {
        const errorModule = await import("../utils/handleApiError");
        const { title, description } = await errorModule.handleApiError(response);
        showError(title, description);
      }
    } catch (error) {
      console.error("Błąd dodawania członka:", error);
      showError("Błąd", "Nie udało się dodać członka do projektu");
    } finally {
      setAdding(null);
    }
  };

  const isMemberInProject = (userId: string) => {
    return projectMembers.some(pm => pm.userId === userId);
  };

  const availableMembers = tenantMembers.filter(m => !isMemberInProject(m.userId));

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="xl" scrollBehavior="inside">
      <ModalOverlay />
      <ModalContent bg={bgColor}>
        <ModalHeader>
          <VStack align="flex-start" spacing={1}>
            <HStack>
              <Icon as={UserPlus} boxSize={5} />
              <Text>Dodaj członka do projektu</Text>
            </HStack>
            <Text fontSize="sm" fontWeight="normal" color="gray.500">
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
          )}

          {projectMembers.length > 0 && (
            <Box mt={6}>
              <Text fontWeight="semibold" mb={3} fontSize="sm" color="gray.600">
                Już w projekcie ({projectMembers.length})
              </Text>
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
                              <Badge colorScheme={getProjectRoleColor(member.role)} fontSize="xs">
                                {getProjectRoleName(member.role)}
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
          )}
        </ModalBody>
      </ModalContent>
    </Modal>
  );
}
