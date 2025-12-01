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
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  useDisclosure,
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
import MainLayout from "../layout/MainLayout";
import AddProjectMemberModal from "../components/AddProjectMemberModal";
import { projectApi } from "../api/projectApi";
import { tenantApi } from "../api/tenantApi";
import { useAuth } from "../hooks/useAuth";
import { ProjectRole } from "../types/project.types";

interface ProjectMemberWeb {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: number;
  joinedAt: string;
}

interface ProjectDetailsWeb {
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  userRole: number; // ProjectRole z backendu (0 = Admin, 1 = Member)
  membersCount: number;
}

const getProjectRoleName = (role: number): string => {
  switch (role) {
    case ProjectRole.Admin:
      return 'Administrator';
    case ProjectRole.Member:
      return 'Członek';
    default:
      return 'Nieznana rola';
  }
};

const getProjectRoleColor = (role: number): string => {
  switch (role) {
    case ProjectRole.Admin:
      return 'blue';
    case ProjectRole.Member:
      return 'green';
    default:
      return 'gray';
  }
};

export default function ProjectDetails() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [project, setProject] = useState<ProjectDetailsWeb | null>(null);
  const [members, setMembers] = useState<ProjectMemberWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMembers, setLoadingMembers] = useState(false);
  const [removingMember, setRemovingMember] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ userId: string; name: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();
  const toast = useToast();

  const [userTenantRole, setUserTenantRole] = useState<number | null>(null);

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
      const response = await projectApi.getProjectDetails(user.activeTenantId, projectId);
      if (response.ok) {
        const data: ProjectDetailsWeb = await response.json();
        setProject(data);
      } else {
        setError("Nie udało się pobrać szczegółów projektu");
      }
    } catch (error) {
      console.error("Błąd pobierania szczegółów projektu:", error);
      setError("Wystąpił błąd podczas ładowania projektu");
    } finally {
      setLoading(false);
    }
  };

  const fetchMembers = async () => {
    if (!user?.activeTenantId || !projectId) return;
    
    setLoadingMembers(true);
    try {
      const response = await projectApi.getProjectMembers(user.activeTenantId, projectId);
      if (response.ok) {
        const data: ProjectMemberWeb[] = await response.json();
        setMembers(data);
      }
    } catch (error) {
      console.error("Błąd pobierania członków:", error);
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
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        {/* Breadcrumbs */}
        <Breadcrumb mb={6} fontSize="sm">
          <BreadcrumbItem>
            <BreadcrumbLink onClick={() => navigate("/projects")}>
              Projekty
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbItem isCurrentPage>
            <BreadcrumbLink>Szczegóły projektu</BreadcrumbLink>
          </BreadcrumbItem>
        </Breadcrumb>

        {/* Przycisk powrotu */}
        <Button
          leftIcon={<ArrowLeft size={20} />}
          variant="ghost"
          mb={6}
          onClick={() => navigate("/projects")}
        >
          Wróć do projektów
        </Button>

        {loading ? (
          <Box display="flex" justifyContent="center" alignItems="center" minH="200px">
            <Spinner size="xl" color="blue.500" />
            <Text ml={4}>Ładowanie szczegółów projektu...</Text>
          </Box>
        ) : error ? (
          <Alert status="error" borderRadius="md">
            <AlertIcon />
            {error}
          </Alert>
        ) : !project ? (
          <Alert status="warning" borderRadius="md">
            <AlertIcon />
            Nie znaleziono projektu
          </Alert>
        ) : (
          <VStack spacing={6} align="stretch">
            {/* Nagłówek projektu */}
            <Box bg={cardBg} p={6} rounded="lg" borderWidth="1px" borderColor={borderColor}>
              <HStack justify="space-between" mb={4} flexWrap="wrap" gap={3}>
                <HStack>
                  <Icon as={FolderKanban} boxSize={10} color="blue.600" />
                  <Heading size="lg">{project.name}</Heading>
                </HStack>
                <HStack spacing={2}>
                  {isProjectAdmin && (
                    <Button
                      leftIcon={<UserPlus size={18} />}
                      colorScheme="blue"
                      size="sm"
                      onClick={onOpen}
                    >
                      Dodaj członka
                    </Button>
                  )}
                  <Badge colorScheme={project.isActive ? "green" : "gray"} fontSize="md" px={3} py={1}>
                    {project.isActive ? "Aktywny" : "Nieaktywny"}
                  </Badge>
                  <Badge colorScheme={getProjectRoleColor(project.userRole)} fontSize="md" px={3} py={1}>
                    {getProjectRoleName(project.userRole)}
                  </Badge>
                </HStack>
              </HStack>

              <VStack align="flex-start" spacing={3}>
                <HStack>
                  <Icon as={User} boxSize={5} />
                  <Text><strong>Utworzył:</strong> {project.createdByUserName}</Text>
                </HStack>
                <HStack>
                  <Icon as={Calendar} boxSize={5} />
                  <Text><strong>Data utworzenia:</strong> {formatDate(project.createdAt)}</Text>
                </HStack>
                <HStack>
                  <Icon as={Users} boxSize={5} />
                  <Text><strong>Liczba członków:</strong> {project.membersCount}</Text>
                </HStack>
              </VStack>
            </Box>

            {/* Członkowie projektu */}
            <Box bg={cardBg} p={6} rounded="lg" borderWidth="1px" borderColor={borderColor}>
              <HStack justify="space-between" mb={4}>
                <Heading size="md">Członkowie projektu ({members.length})</Heading>
              </HStack>
              
              {loadingMembers ? (
                <Box textAlign="center" py={6}>
                  <Spinner size="md" color="blue.500" />
                </Box>
              ) : members.length === 0 ? (
                <Text color="gray.500">Brak członków w projekcie</Text>
              ) : (
                <VStack spacing={2} align="stretch">
                  {members.map((member) => {
                    const initials = `${member.firstName[0]}${member.lastName[0]}`.toUpperCase();
                    const roleColor = member.role === ProjectRole.Admin ? "blue" : "green";
                    const roleName = member.role === ProjectRole.Admin ? "Administrator" : "Członek";
                    
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
                          <HStack spacing={3} flex={1}>
                            <Box
                              w="40px"
                              h="40px"
                              borderRadius="full"
                              bg="blue.600"
                              color="white"
                              display="flex"
                              alignItems="center"
                              justifyContent="center"
                              fontWeight="bold"
                              fontSize="sm"
                            >
                              {initials}
                            </Box>
                            <VStack align="flex-start" spacing={0}>
                              <Text fontWeight="medium" fontSize="sm">
                                {member.firstName} {member.lastName}
                              </Text>
                              <Text fontSize="xs" color="gray.500">
                                {member.email}
                              </Text>
                              <Text fontSize="xs" color="gray.500">
                                Dołączył: {formatDate(member.joinedAt)}
                              </Text>
                            </VStack>
                          </HStack>
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
                        </HStack>
                      </Box>
                    );
                  })}
                </VStack>
              )}
            </Box>
          </VStack>
        )}

        {/* Modal dodawania członka */}
        {project && (
          <AddProjectMemberModal
            isOpen={isOpen}
            onClose={onClose}
            tenantId={project.tenantId}
            projectId={project.id}
            projectName={project.name}
            isAdmin={isProjectAdmin || false}
            onMemberAdded={() => {
              // Odśwież listę członków i szczegóły projektu po dodaniu
              fetchProjectDetails();
              fetchMembers();
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
