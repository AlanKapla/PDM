import { useEffect, useState, useContext } from "react";
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
} from "@chakra-ui/react";
import { ArrowLeft, Users, UserPlus, Trash2 } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import AddProjectMemberModal from "../components/AddProjectMemberModal";
import { AuthContext } from "../context/AuthContext";
import { LoadingSpinner, EmptyState } from "../components/common";
import { useToastNotification } from "../hooks/useToastNotification";
import { formatDate } from "../utils/formatters";
import { projectApi } from "../api/projectApi";
import { tenantApi } from "../api/tenantApi";
import { ProjectRole } from "../types/project.types";
import { getProjectRoleName, getProjectRoleColor } from "../utils/constants";

export default function ProjectMembers() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const { showSuccess, showError } = useToastNotification();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();

  const [loading, setLoading] = useState(true);
  const [members, setMembers] = useState<any[]>([]);
  const [project, setProject] = useState<any | null>(null);
  const [userTenantRole, setUserTenantRole] = useState<number | null>(null);
  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const isProjectAdmin = project && project.userRole === ProjectRole.Admin;
  const isTenantAdmin = userTenantRole === 0;

  useEffect(() => {
    fetchData();
  }, [projectId]);

  const fetchData = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    try {
      const [projectRes, membersRes, tenantRes] = await Promise.all([
        projectApi.getProjectDetails(user.activeTenantId, projectId),
        projectApi.getProjectMembers(user.activeTenantId, projectId),
        tenantApi.getActiveTenant(),
      ]);

      setProject(projectRes.data);
      setMembers(membersRes.data);
      setUserTenantRole(tenantRes.data.userRole);
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
        <Button
          leftIcon={<ArrowLeft size={18} />}
          variant="ghost"
          mb={6}
          onClick={() => navigate(`/projects/${projectId}`)}
        >
          Wróć do projektu
        </Button>

        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <HStack spacing={3}>
            <Icon as={Users} boxSize={8} color="blue.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Członkowie projektu</Heading>
              {project && <Text fontSize="sm" color="gray.600">{project.name}</Text>}
            </VStack>
          </HStack>
          {isProjectAdmin && (
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
          <VStack spacing={3} align="stretch">
            {members.map((m) => {
              const initials = `${m.firstName[0]}${m.lastName[0]}`;

              return (
                <Box
                  key={m.userId}
                  bg={cardBg}
                  p={6}
                  borderWidth="1px"
                  borderColor={borderColor}
                  rounded="lg"
                  _hover={{ bg: hoverBg }}
                  transition="0.15s"
                  shadow="sm"
                >
                  <HStack justify="space-between">
                    <HStack spacing={4} flex={1}>
                      <Box
                        w="56px"
                        h="56px"
                        rounded="full"
                        bg="blue.600"
                        color="white"
                        fontWeight="bold"
                        fontSize="lg"
                        display="flex"
                        alignItems="center"
                        justifyContent="center"
                      >
                        {initials}
                      </Box>

                      <VStack align="flex-start" spacing={1}>
                        <Text fontWeight="bold" fontSize="lg">
                          {m.firstName} {m.lastName}
                        </Text>
                        <Text fontSize="sm" color="gray.600">
                          {m.email}
                        </Text>
                        <Text fontSize="xs" color="gray.500">
                          Dołączył: {formatDate(m.joinedAt)}
                        </Text>
                      </VStack>
                    </HStack>

                    <HStack spacing={3}>
                      <Badge colorScheme={getProjectRoleColor(m.role)} fontSize="md" px={4} py={2}>
                        {getProjectRoleName(m.role)}
                      </Badge>
                      {isTenantAdmin && m.email.toLowerCase() !== user?.email.toLowerCase() && (
                        <IconButton
                          aria-label="Usuń członka"
                          icon={<Trash2 size={18} />}
                          size="md"
                          colorScheme="red"
                          variant="ghost"
                          isDisabled={removingMember !== null}
                          onClick={() => handleRemoveMemberClick(m.userId, `${m.firstName} ${m.lastName}`)}
                        />
                      )}
                    </HStack>
                  </HStack>
                </Box>
              );
            })}
          </VStack>
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
