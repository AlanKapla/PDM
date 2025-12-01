import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Badge,
  Icon,
  Spinner,
  Alert,
  AlertIcon,
  Button,
  useColorModeValue,
  useDisclosure,
<<<<<<< HEAD
  Divider,
} from "@chakra-ui/react";
import {
  FolderKanban,
  User,
  Calendar,
  ArrowLeft,
  Users,
  UserPlus,
} from "lucide-react";

=======
  IconButton,
  useToast,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, ArrowLeft, Users, UserPlus, Trash2 } from "lucide-react";
>>>>>>> c12a0e8179ee79b38c964c19040a6b902b02f719
import MainLayout from "../layout/MainLayout";
import AddProjectMemberModal from "../components/AddProjectMemberModal";
import { projectApi } from "../api/projectApi";
import { tenantApi } from "../api/tenantApi";
import { useAuth } from "../hooks/useAuth";
import { ProjectRole } from "../types/project.types";

/* Helpery UI */
const getProjectRoleName = (role: number) =>
  role === ProjectRole.Admin ? "Administrator" : "Członek";

const getProjectRoleColor = (role: number) =>
  role === ProjectRole.Admin ? "blue" : "green";

export default function ProjectDetails() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { isOpen, onOpen, onClose } = useDisclosure();

  const [project, setProject] = useState<any | null>(null);
  const [members, setMembers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
<<<<<<< HEAD
=======
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();
  const toast = useToast();

  const [userTenantRole, setUserTenantRole] = useState<number | null>(null);
>>>>>>> c12a0e8179ee79b38c964c19040a6b902b02f719

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  const isProjectAdmin = project && project.userRole === ProjectRole.Admin;
  const isTenantAdmin = userTenantRole === 0; // TenantRole.Admin
  
  console.log("🔍 userTenantRole:", userTenantRole, "isTenantAdmin:", isTenantAdmin);

  const fetchProjectDetails = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoading(true);
    setError(null);

    try {
      const response = await projectApi.getProjectDetails(
        user.activeTenantId,
        projectId
      );

      if (!response.ok) {
        setError("Nie udało się pobrać danych projektu");
        setLoading(false);
        return;
      }

      setProject(await response.json());
    } catch (err) {
      console.error(err);
      setError("Błąd podczas pobierania szczegółów projektu");
    } finally {
      setLoading(false);
    }
  };

  const fetchMembers = async () => {
    if (!user?.activeTenantId || !projectId) return;

    setLoadingMembers(true);

    try {
      const response = await projectApi.getProjectMembers(
        user.activeTenantId,
        projectId
      );

      if (response.ok) setMembers(await response.json());
    } catch (err) {
      console.error("Błąd pobierania członków projektu:", err);
    } finally {
      setLoadingMembers(false);
    }
  };

  useEffect(() => {
    fetchProjectDetails();
    fetchMembers();
    
    // Pobierz rolę użytkownika w tenancie
    const fetchUserTenantRole = async () => {
      if (!user?.activeTenantId) {
        console.log("🔴 Brak activeTenantId");
        return;
      }
      
      try {
        const response = await tenantApi.getUserTenants();
        
        console.log("🔵 getUserTenants response:", response.status);
        
        if (response.ok) {
          const tenants = await response.json();
          console.log("🔵 User tenants:", tenants);
          
          // Znajdź aktywny tenant i pobierz rolę
          const activeTenant = tenants.find((t: any) => t.id === user.activeTenantId);
          if (activeTenant) {
            console.log("🔵 Active tenant:", activeTenant);
            console.log("🔵 User role in tenant:", activeTenant.role);
            setUserTenantRole(activeTenant.role);
          }
        }
      } catch (error) {
        console.error("❌ Błąd pobierania roli tenanta:", error);
      }
    };
    
    fetchUserTenantRole();
  }, [projectId, user?.activeTenantId]);

<<<<<<< HEAD
  const formatDate = (date: string) =>
    new Date(date).toLocaleDateString("pl-PL", {
=======
  const handleRemoveMemberClick = (userId: string, memberName: string) => {
    setMemberToRemove({ userId, name: memberName });
    onRemoveModalOpen();
  };

  const handleConfirmRemoveMember = async () => {
    if (!user?.activeTenantId || !projectId || !memberToRemove) return;
    
    setRemovingMember(memberToRemove.userId);
    try {
      const response = await projectApi.removeProjectMember(user.activeTenantId, projectId, memberToRemove.userId);
      
      if (response.ok) {
        toast({
          title: "Sukces",
          description: `Użytkownik ${memberToRemove.name} został usunięty z projektu`,
          status: "success",
          duration: 3000,
        });
        
        // Odśwież listę
        await fetchProjectDetails();
        await fetchMembers();
      } else {
        const errorText = await response.text();
        toast({
          title: "Błąd",
          description: errorText || "Nie udało się usunąć członka",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      toast({
        title: "Błąd",
        description: "Wystąpił błąd podczas usuwania członka",
        status: "error",
        duration: 3000,
      });
    } finally {
      setRemovingMember(null);
      setMemberToRemove(null);
      onRemoveModalClose();
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("pl-PL", {
>>>>>>> c12a0e8179ee79b38c964c19040a6b902b02f719
      year: "numeric",
      month: "long",
      day: "numeric",
    });

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        {/* BACK BUTTON */}
        <Button
          leftIcon={<ArrowLeft size={18} />}
          variant="ghost"
          mb={6}
          onClick={() => navigate("/projects")}
        >
          Wróć do projektów
        </Button>

        {loading ? (
          <HStack justify="center" spacing={4} py={10}>
            <Spinner size="xl" />
            <Text>Ładowanie projektu...</Text>
          </HStack>
        ) : error ? (
          <Alert status="error" rounded="md">
            <AlertIcon />
            {error}
          </Alert>
        ) : !project ? (
          <Alert status="warning" rounded="md">
            <AlertIcon />
            Projekt nie istnieje
          </Alert>
        ) : (
          <VStack spacing={8} align="stretch">
            {/* ====================== PROJECT HEADER ======================= */}
            <Box
              bg={cardBg}
              p={6}
              rounded="xl"
              borderWidth="1px"
              borderColor={borderColor}
              shadow="md"
            >
              <HStack justify="space-between" align="flex-start" mb={4}>
                <HStack spacing={4}>
                  <Icon as={FolderKanban} boxSize={10} color="blue.500" />
                  <VStack align="flex-start" spacing={0}>
                    <Heading size="lg">{project.name}</Heading>
                    <Text fontSize="sm" color="gray.500">
                      Projekt w organizacji {project.tenantId}
                    </Text>
                  </VStack>
                </HStack>

                <HStack spacing={3}>
                  <Badge
                    colorScheme={project.isActive ? "green" : "gray"}
                    fontSize="md"
                    px={3}
                    py={1}
                    rounded="md"
                  >
                    {project.isActive ? "Aktywny" : "Nieaktywny"}
                  </Badge>

                  <Badge
                    colorScheme={getProjectRoleColor(project.userRole)}
                    fontSize="md"
                    px={3}
                    py={1}
                    rounded="md"
                  >
                    {getProjectRoleName(project.userRole)}
                  </Badge>

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
              </HStack>

              <Divider my={4} />

              <VStack align="flex-start" spacing={3}>
                <HStack>
                  <Icon as={User} size={18} />
                  <Text>
                    <strong>Utworzył:</strong> {project.createdByUserName}
                  </Text>
                </HStack>

                <HStack>
                  <Icon as={Calendar} size={18} />
                  <Text>
                    <strong>Data utworzenia:</strong>{" "}
                    {formatDate(project.createdAt)}
                  </Text>
                </HStack>

                <HStack>
                  <Icon as={Users} size={18} />
                  <Text>
                    <strong>Liczba członków:</strong> {members.length}
                  </Text>
                </HStack>
              </VStack>
            </Box>

            {/* ====================== MEMBERS LIST ======================= */}
            <Box
              bg={cardBg}
              p={6}
              rounded="xl"
              borderWidth="1px"
              borderColor={borderColor}
              shadow="md"
            >
              <Heading size="md" mb={4}>
                Członkowie projektu ({members.length})
              </Heading>

              {loadingMembers ? (
                <HStack justify="center" py={6}>
                  <Spinner size="md" />
                </HStack>
              ) : members.length === 0 ? (
                <Text color="gray.500">Brak członków w tym projekcie</Text>
              ) : (
                <VStack spacing={3} align="stretch">
                  {members.map((m) => {
                    const initials = `${m.firstName[0]}${m.lastName[0]}`;

                    return (
                      <Box
                        key={m.userId}
                        p={4}
                        borderWidth="1px"
                        borderColor={borderColor}
                        rounded="md"
                        _hover={{ bg: hoverBg }}
                        transition="0.15s"
                      >
                        <HStack justify="space-between">
<<<<<<< HEAD
                          {/* Left */}
                          <HStack spacing={3}>
=======
                          <HStack spacing={3} flex={1}>
>>>>>>> c12a0e8179ee79b38c964c19040a6b902b02f719
                            <Box
                              w="42px"
                              h="42px"
                              rounded="full"
                              bg="blue.600"
                              color="white"
                              fontWeight="bold"
                              fontSize="sm"
                              display="flex"
                              alignItems="center"
                              justifyContent="center"
                            >
                              {initials}
                            </Box>

                            <VStack align="flex-start" spacing={0}>
                              <Text fontWeight="medium">
                                {m.firstName} {m.lastName}
                              </Text>
                              <Text fontSize="xs" color="gray.500">
                                {m.email}
                              </Text>
                              <Text fontSize="xs" color="gray.500">
                                Dołączył: {formatDate(m.joinedAt)}
                              </Text>
                            </VStack>
                          </HStack>
<<<<<<< HEAD

                          {/* Right */}
                          <Badge
                            colorScheme={getProjectRoleColor(m.role)}
                            fontSize="sm"
                            px={3}
                            py={1}
                          >
                            {getProjectRoleName(m.role)}
                          </Badge>
=======
                          <HStack spacing={2}>
                            <Badge colorScheme={roleColor} fontSize="sm" px={3} py={1}>
                              {roleName}
                            </Badge>
                            {/* Pokaż przycisk usuwania tylko dla admina tenanta i tylko dla innych użytkowników */}
                            {isTenantAdmin && member.email.toLowerCase() !== user?.email.toLowerCase() && (
                              <IconButton
                                aria-label="Usuń członka"
                                icon={<Trash2 size={16} />}
                                size="sm"
                                colorScheme="red"
                                variant="ghost"
                                isDisabled={removingMember !== null}
                                onClick={() => handleRemoveMemberClick(member.userId, `${member.firstName} ${member.lastName}`)}
                              />
                            )}
                          </HStack>
>>>>>>> c12a0e8179ee79b38c964c19040a6b902b02f719
                        </HStack>
                      </Box>
                    );
                  })}
                </VStack>
              )}
            </Box>
          </VStack>
        )}

        {/* MODAL ADD MEMBER */}
        {project && (
          <AddProjectMemberModal
            isOpen={isOpen}
            onClose={onClose}
            tenantId={project.tenantId}
            projectId={project.id}
            projectName={project.name}
            isAdmin={isProjectAdmin}
            onMemberAdded={() => {
              fetchMembers();
              fetchProjectDetails();
            }}
          />
        )}

        {/* Modal potwierdzenia usunięcia członka */}
        <Modal isOpen={isRemoveModalOpen} onClose={onRemoveModalClose} isCentered>
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>Potwierdź usunięcie</ModalHeader>
            <ModalCloseButton />
            <ModalBody>
              <VStack align="flex-start" spacing={3}>
                <Text>
                  Czy na pewno chcesz usunąć <Text as="span" fontWeight="bold">{memberToRemove?.name}</Text> z projektu?
                </Text>
                <Text fontSize="sm" color="gray.500">
                  Ta operacja jest nieodwracalna. Użytkownik straci dostęp do wszystkich zasobów projektu.
                </Text>
              </VStack>
            </ModalBody>
            <ModalFooter>
              <Button 
                variant="ghost" 
                mr={3} 
                onClick={onRemoveModalClose}
                isDisabled={removingMember !== null}
              >
                Anuluj
              </Button>
              <Button 
                colorScheme="red" 
                onClick={handleConfirmRemoveMember}
                isLoading={removingMember !== null}
                loadingText="Usuwanie..."
              >
                Usuń członka
              </Button>
            </ModalFooter>
          </ModalContent>
        </Modal>
      </Box>
    </MainLayout>
  );
}
