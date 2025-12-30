import { useEffect, useState } from "react";
import {
  Box,
  Heading,
  SimpleGrid,
  Card,
  CardBody,
  Text,
  Badge,
  VStack,
  HStack,
  Icon,
  useColorModeValue,
  Button,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  FormControl,
  FormLabel,
  Input,
  IconButton,
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, Plus } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { handleApiError } from "../utils/handleApiError";
import { tenantApi } from "../api/tenantApi";
import { projectApi } from "../api/projectApi";
import { TenantRole } from "../types/auth.types";
import { ProjectRole } from "../types/project.types";
import { getProjectRoleName, getProjectRoleColor } from "../utils/constants";
import { useToastNotification } from "../hooks/useToastNotification";
import { useModal } from "../hooks/useModal";
import { LoadingSpinner, EmptyState, ErrorAlert } from "../components/common";

interface ProjectDetailsWeb {
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  userRole: number;
  membersCount: number;
}

export default function Projects() {
  const location = useLocation();
  const navigate = useNavigate();
  const [projects, setProjects] = useState<ProjectDetailsWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTenantId, setActiveTenantId] = useState<string | null | undefined>(undefined);
  const [userTenantRole, setUserTenantRole] = useState<number | null>(null);
  const [newProjectName, setNewProjectName] = useState("");
  const [creating, setCreating] = useState(false);
  
  const createModal = useModal();
  const { showSuccess, showError } = useToastNotification();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  // Pobierz aktywnego tenanta i projekty
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const [activeTenantResponse, tenantsResponse] = await Promise.all([
          tenantApi.getActiveTenant(),
          tenantApi.getUserTenants(),
        ]);

        const activeTenantData = activeTenantResponse.data;
        
        console.log("🔍 Active tenant data:", activeTenantData);
        console.log("🔍 activeTenantId value:", activeTenantData.activeTenantId);
        console.log("🔍 activeTenantId type:", typeof activeTenantData.activeTenantId);
        
        // Jeśli jest aktywny tenant (sprawdź czy to prawdziwy string z wartością)
        if (activeTenantData.activeTenantId && activeTenantData.activeTenantId !== "00000000-0000-0000-0000-000000000000") {
          console.log("✅ Tenant aktywny - pobieram projekty");
          setActiveTenantId(activeTenantData.activeTenantId);
          
          // Znajdź rolę użytkownika w aktywnym tenancie
          const tenants = tenantsResponse.data;
          const activeTenant = tenants.find((t: any) => t.id === activeTenantData.activeTenantId);
          if (activeTenant) {
            setUserTenantRole(activeTenant.role);
          }
          
          // Pobierz projekty
          try {
            const projectsResponse = await tenantApi.getTenantProjects(activeTenantData.activeTenantId);
            setProjects(projectsResponse.data);
          } catch (projectErr) {
            console.error("Błąd pobierania projektów:", projectErr);
            setError("Nie udało się pobrać projektów");
          }
        } else {
          // Brak aktywnego tenanta
          console.log("❌ Brak aktywnego tenanta - NIE pobieram projektów");
          setActiveTenantId(null);
          setUserTenantRole(null);
        }
      } catch (err) {
        console.error("Błąd pobierania danych:", err);
        setError("Błąd połączenia z serwerem");
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [location.key]);

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("pl-PL", {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  const handleCreateProject = async () => {
    if (!newProjectName.trim()) {
      showError("Nazwa projektu wymagana");
      return;
    }

    if (!activeTenantId) {
      showError("Brak aktywnego tenanta");
      return;
    }

    setCreating(true);
    try {
      await tenantApi.createProject(activeTenantId, newProjectName.trim());

      showSuccess("Projekt utworzony");
      setNewProjectName("");
      createModal.onClose();
      
      // Odśwież listę projektów
      const projectsResponse = await tenantApi.getTenantProjects(activeTenantId);
      setProjects(projectsResponse.data);
    } catch (error) {
      console.error("Błąd podczas tworzenia projektu:", error);
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setCreating(false);
    }
  };

  const isAdmin = userTenantRole === TenantRole.Admin;

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <Heading size={{ base: "lg", md: "xl" }}>
            Projekty
          </Heading>
          {isAdmin && (
            <Button
              leftIcon={<Plus size={20} />}
              colorScheme="blue"
              onClick={createModal.onOpen}
              size={{ base: "sm", md: "md" }}
            >
              Nowy projekt
            </Button>
          )}
        </HStack>

        {loading ? (
          <LoadingSpinner />
        ) : error ? (
          <ErrorAlert description={error} />
        ) : projects.length === 0 ? (
          <EmptyState 
            icon={FolderKanban}
            title="Nie masz jeszcze żadnych projektów"
            description="Stwórz swój pierwszy projekt, aby zacząć pracę"
            action={
              userTenantRole === TenantRole.Admin && activeTenantId && (
                <Button leftIcon={<Icon as={Plus} />} colorScheme="blue" onClick={createModal.onOpen}>
                  Utwórz projekt
                </Button>
              )
            }
          />
        ) : (
          <VStack spacing={4} align="stretch">
            {projects.map((project) => (
              <Box
                key={project.id}
                bg={cardBg}
                rounded="lg"
                shadow="md"
                borderWidth="1px"
                borderColor={borderColor}
                overflow="hidden"
                cursor="pointer"
                onClick={() => navigate(`/projects/${project.id}`)}
                _hover={{
                  shadow: "lg",
                  borderColor: "blue.500",
                }}
                transition="all 0.2s"
              >
                <Box p={4}>
                  <HStack justify="space-between" align="center" spacing={3}>
                    <HStack spacing={3} flex={1}>
                      <Icon as={FolderKanban} boxSize={6} color="blue.600" />
                      <VStack align="flex-start" spacing={1} flex={1}>
                        <HStack spacing={2}>
                          <Text fontWeight="bold" fontSize={{ base: "md", md: "lg" }}>
                            {project.name}
                          </Text>
                          <Badge colorScheme={project.isActive ? "green" : "gray"} fontSize="xs">
                            {project.isActive ? "Aktywny" : "Nieaktywny"}
                          </Badge>
                          <Badge colorScheme={getProjectRoleColor(project.userRole)} fontSize="xs">
                            {getProjectRoleName(project.userRole)}
                          </Badge>
                        </HStack>
                        <HStack spacing={4} fontSize="sm" color="gray.600">
                          <HStack>
                            <Icon as={User} boxSize={3} />
                            <Text>{project.createdByUserName}</Text>
                          </HStack>
                          <HStack>
                            <Icon as={Calendar} boxSize={3} />
                            <Text>{formatDate(project.createdAt)}</Text>
                          </HStack>
                          <Text>
                            Członków: {project.membersCount}
                          </Text>
                        </HStack>
                      </VStack>
                    </HStack>
                  </HStack>
                </Box>
              </Box>
            ))}
          </VStack>
        )}
      </Box>

      {/* Modal tworzenia projektu */}
      <Modal isOpen={createModal.isOpen} onClose={createModal.onClose}>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Utwórz nowy projekt</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <FormControl>
              <FormLabel>Nazwa projektu</FormLabel>
              <Input
                placeholder="Wprowadź nazwę projektu"
                value={newProjectName}
                onChange={(e) => setNewProjectName(e.target.value)}
                onKeyPress={(e) => {
                  if (e.key === "Enter" && !creating) {
                    handleCreateProject();
                  }
                }}
              />
            </FormControl>
          </ModalBody>
          <ModalFooter>
            <Button variant="ghost" mr={3} onClick={createModal.onClose} isDisabled={creating}>
              Anuluj
            </Button>
            <Button
              colorScheme="blue"
              onClick={handleCreateProject}
              isLoading={creating}
            >
              Utwórz
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </MainLayout>
  );
}
