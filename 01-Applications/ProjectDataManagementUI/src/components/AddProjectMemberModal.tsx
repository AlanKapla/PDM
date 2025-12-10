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
  Avatar,
  Badge,
  useColorModeValue,
  useToast,
  Box,
  Spinner,
  Icon,
} from "@chakra-ui/react";
import { UserPlus, Check } from "lucide-react";
import { tenantApi } from "../api/tenantApi";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import { ProjectRole } from "../types/project.types";
import { TenantRole } from "../types/auth.types";
import type { TenantMemberWeb, ProjectMemberWeb } from "../types/project.types";

const getProjectRoleName = (role: number): string => {
  switch (role) {
    case ProjectRole.Admin: return "Administrator";
    case ProjectRole.Member: return "Członek";
    default: return "Nieznana";
  }
};

const getProjectRoleColor = (role: number): string => {
  switch (role) {
    case ProjectRole.Admin: return "blue";
    case ProjectRole.Member: return "green";
    default: return "gray";
  }
};

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
  const toast = useToast();
  const [tenantMembers, setTenantMembers] = useState<TenantMemberWeb[]>([]);
  const [projectMembers, setProjectMembers] = useState<ProjectMemberWeb[]>([]);
  const [loading, setLoading] = useState(false);
  const [adding, setAdding] = useState<string | null>(null);

  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

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
      toast({
        title: "Błąd",
        description: "Nie udało się pobrać listy członków",
        status: "error",
        duration: 3000,
      });
    } finally {
      setLoading(false);
    }
  };

  const handleAddMember = async (userId: string) => {
    setAdding(userId);
    try {
      const response = await projectApi.addProjectMember(tenantId, projectId, userId);
      
      if (response.ok) {
        toast({
          title: "Sukces",
          description: "Członek został dodany do projektu",
          status: "success",
          duration: 3000,
        });
        
        // Odśwież listę członków
        await fetchData();
        onMemberAdded?.();
      } else {
        const { title, description } = await handleApiError(response);
        toast({
          title,
          description,
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd podczas dodawania członka:", error);
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
            <Box textAlign="center" py={10}>
              <Spinner size="lg" color="blue.500" />
            </Box>
          ) : availableMembers.length === 0 ? (
            <Box textAlign="center" py={10}>
              <Text color="gray.500">
                Wszyscy członkowie organizacji są już w tym projekcie
              </Text>
            </Box>
          ) : (
            <VStack spacing={2} align="stretch">
              {availableMembers.map((member) => {
                const initials = `${member.firstName[0]}${member.lastName[0]}`.toUpperCase();
                const isAdding = adding === member.userId;

                return (
                  <Box
                    key={member.userId}
                    p={3}
                    border="1px"
                    borderColor={borderColor}
                    borderRadius="md"
                    _hover={{ bg: hoverBg }}
                    transition="background 0.2s"
                  >
                    <HStack justify="space-between">
                      <HStack spacing={3}>
                        <Avatar size="sm" bg="blue.600" color="white" name={initials}>
                          {initials}
                        </Avatar>
                        <VStack align="flex-start" spacing={0}>
                          <Text fontWeight="medium" fontSize="sm">
                            {member.firstName} {member.lastName}
                          </Text>
                          <Text fontSize="xs" color="gray.500">
                            {member.email}
                          </Text>
                        </VStack>
                        <Badge colorScheme={member.role === TenantRole.Admin ? "purple" : "gray"} fontSize="xs">
                          {member.role === TenantRole.Admin ? "Administrator" : "Członek"}
                        </Badge>
                      </HStack>
                      <Button
                        size="sm"
                        colorScheme="blue"
                        leftIcon={<Icon as={UserPlus} boxSize={4} />}
                        onClick={() => handleAddMember(member.userId)}
                        isLoading={isAdding}
                        isDisabled={adding !== null}
                      >
                        Dodaj
                      </Button>
                    </HStack>
                  </Box>
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
                  const initials = `${member.firstName[0]}${member.lastName[0]}`.toUpperCase();

                  return (
                    <Box
                      key={member.userId}
                      p={3}
                      border="1px"
                      borderColor={borderColor}
                      borderRadius="md"
                      bg={useColorModeValue("gray.50", "gray.700")}
                    >
                      <HStack justify="space-between">
                        <HStack spacing={3}>
                          <Avatar size="sm" bg="green.600" color="white" name={initials}>
                            {initials}
                          </Avatar>
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
                    </Box>
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
