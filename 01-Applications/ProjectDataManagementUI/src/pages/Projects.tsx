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
  Tooltip,
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, Plus, Power } from "lucide-react";
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
  const [projectToToggle, setProjectToToggle] = useState<ProjectDetailsWeb | null>(null);
  const [togglingStatus, setTogglingStatus] = useState(false);
  
  const createModal = useModal();
  const toggleStatusModal = useModal();
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

        if (activeTenantResponse.ok) {
          const activeTenantData = await activeTenantResponse.json();
          
          console.log("🔍 Active tenant data:", activeTenantData);
          console.log("🔍 activeTenantId value:", activeTenantData.activeTenantId);
          console.log("🔍 activeTenantId type:", typeof activeTenantData.activeTenantId);
          
          // Jeśli jest aktywny tenant (sprawdź czy to prawdziwy string z wartością)
          if (activeTenantData.activeTenantId && activeTenantData.activeTenantId !== "00000000-0000-0000-0000-000000000000") {
            console.log("✅ Tenant aktywny - pobieram projekty");
            setActiveTenantId(activeTenantData.activeTenantId);
            
            // Znajdź rolę użytkownika w aktywnym tenancie
            if (tenantsResponse.ok) {
              const tenants = await tenantsResponse.json();
              const activeTenant = tenants.find((t: any) => t.id === activeTenantData.activeTenantId);
              if (activeTenant) {
                setUserTenantRole(activeTenant.role);
              }
            }
            
            // Pobierz projekty
            try {
              const projectsResponse = await tenantApi.getTenantProjects(activeTenantData.activeTenantId);
              if (projectsResponse.ok) {
                const projectsData = await projectsResponse.json();
                setProjects(projectsData);
              }
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
        } else {
          setError("Nie udało się pobrać aktywnego tenanta");
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

  const openToggleStatusModal = (project: ProjectDetailsWeb, e: React.MouseEvent) => {
    e.stopPropagation();
    setProjectToToggle(project);
    toggleStatusModal.onOpen();
  };

  const handleToggleProjectStatus = async () => {
    if (!activeTenantId || !projectToToggle) return;

    const newStatus = !projectToToggle.isActive;
    setTogglingStatus(true);
    
    try {
      const response = await projectApi.toggleProjectStatus(activeTenantId, projectToToggle.id, newStatus);
      
      if (response.ok) {
        showSuccess(
          newStatus ? "Projekt aktywowany" : "Projekt zdezaktywowany",
          newStatus 
            ? "Projekt został pomyślnie aktywowany" 
            : "Projekt został pomyślnie zdezaktywowany"
        );
        
        toggleStatusModal.onClose();
        setProjectToToggle(null);
        
        // Odśwież listę projektów
        const projectsResponse = await tenantApi.getTenantProjects(activeTenantId);
        if (projectsResponse.ok) {
          const projectsData = await projectsResponse.json();
          setProjects(projectsData);
        }
      } else {
        const { title, description } = await handleApiError(response);
        showError(title, description);
      }
    } catch (error) {
      console.error("Błąd podczas toggle project status:", error);
      showError("Błąd", `Wystąpił błąd podczas ${newStatus ? 'aktywacji' : 'dezaktywacji'} projektu`);
    } finally {
      setTogglingStatus(false);
    }
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
      const response = await tenantApi.createProject(activeTenantId, newProjectName.trim());

      if (response.ok) {
        showSuccess("Projekt utworzony");
        setNewProjectName("");
        createModal.onClose();
        
        // Odśwież listę projektów
        const projectsResponse = await tenantApi.getTenantProjects(activeTenantId);
        if (projectsResponse.ok) {
          const data = await projectsResponse.json();
          setProjects(data);
        }
      } else {
        const { title, description } = await handleApiError(response);
        showError(title, description);
      }
    } catch (err) {
      showError("Błąd", "Wystąpił problem z połączeniem");
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
          <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={6}>
            {projects.map((project) => (
              <Card
                key={project.id}
                bg={cardBg}
                borderWidth="1px"
                borderColor={borderColor}
                cursor={project.isActive ? "pointer" : "not-allowed"}
                transition="all 0.2s"
                opacity={project.isActive ? 1 : 0.6}
                _hover={project.isActive ? {
                  transform: "translateY(-4px)",
                  shadow: "xl",
                  borderColor: "blue.500",
                } : {}}
                onClick={() => project.isActive && navigate(`/projects/${project.id}`)}
              >
                <CardBody>
                  <VStack align="flex-start" spacing={4}>
                    <HStack justify="space-between" w="100%">
                      <Icon as={FolderKanban} boxSize={8} color="blue.600" />
                      <HStack spacing={2}>
                        <Badge
                          colorScheme={project.isActive ? "green" : "gray"}
                          fontSize="xs"
                        >
                          {project.isActive ? "Aktywny" : "Nieaktywny"}
                        </Badge>
                        {project.userRole === ProjectRole.Admin && (
                          <Tooltip label={project.isActive ? "Dezaktywuj projekt" : "Aktywuj projekt"}>
                            <IconButton
                              aria-label={project.isActive ? "Dezaktywuj projekt" : "Aktywuj projekt"}
                              icon={<Power size={16} />}
                              size="xs"
                              colorScheme={project.isActive ? "red" : "green"}
                              variant="ghost"
                              onClick={(e) => openToggleStatusModal(project, e)}
                            />
                          </Tooltip>
                        )}
                      </HStack>
                    </HStack>

                    <VStack align="flex-start" spacing={2} w="100%">
                      <Heading size="md" isTruncated w="100%">
                        {project.name}
                      </Heading>

                      <Badge colorScheme={getProjectRoleColor(project.userRole)}>
                        {getProjectRoleName(project.userRole)}
                      </Badge>
                    </VStack>

                    <VStack align="flex-start" spacing={1} w="100%" fontSize="sm" color="gray.600">
                      <HStack>
                        <Icon as={User} boxSize={4} />
                        <Text>{project.createdByUserName}</Text>
                      </HStack>

                      <HStack>
                        <Icon as={Calendar} boxSize={4} />
                        <Text>{formatDate(project.createdAt)}</Text>
                      </HStack>

                      <Text fontWeight="medium">
                        Członków: {project.membersCount}
                      </Text>
                    </VStack>
                  </VStack>
                </CardBody>
              </Card>
            ))}
          </SimpleGrid>
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

      {/* Modal potwierdzenia zmiany statusu projektu */}
      <Modal isOpen={toggleStatusModal.isOpen} onClose={toggleStatusModal.onClose} isCentered size="lg">
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>
            {projectToToggle?.isActive ? "Dezaktywuj projekt" : "Aktywuj projekt"}
          </ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack align="flex-start" spacing={4}>
              <Text>
                Czy na pewno chcesz {projectToToggle?.isActive ? "zdezaktywować" : "aktywować"} projekt <Text as="span" fontWeight="bold" color="blue.500">{projectToToggle?.name}</Text>?
              </Text>
              {projectToToggle?.isActive ? (
                <Box
                  p={4}
                  bg={useColorModeValue("orange.50", "orange.900")}
                  borderRadius="md"
                  borderWidth="1px"
                  borderColor={useColorModeValue("orange.200", "orange.700")}
                  width="100%"
                >
                  <VStack align="flex-start" spacing={3}>
                    <HStack spacing={2}>
                      <Icon as={Power} color="orange.500" />
                      <Text fontWeight="bold" color="orange.600" fontSize="sm">
                        ⚠️ Ważne informacje:
                      </Text>
                    </HStack>
                    <Text fontSize="sm">
                      • Zdezaktywowany projekt <Text as="span" fontWeight="bold">nie będzie widoczny</Text> na liście projektów
                    </Text>
                    <Text fontSize="sm">
                      • Wszystkie dane projektu zostaną zachowane
                    </Text>
                    <Text fontSize="sm">
                      • Możesz ponownie aktywować projekt w każdej chwili
                    </Text>
                    <Text fontSize="sm" fontWeight="medium" color="orange.700" mt={2}>
                      Operacja nie usuwa projektu, tylko ukrywa go przed użytkownikami.
                    </Text>
                  </VStack>
                </Box>
              ) : (
                <Box
                  p={4}
                  bg={useColorModeValue("green.50", "green.900")}
                  borderRadius="md"
                  borderWidth="1px"
                  borderColor={useColorModeValue("green.200", "green.700")}
                  width="100%"
                >
                  <VStack align="flex-start" spacing={3}>
                    <HStack spacing={2}>
                      <Icon as={Power} color="green.500" />
                      <Text fontWeight="bold" color="green.600" fontSize="sm">
                        ℹ️ Informacje:
                      </Text>
                    </HStack>
                    <Text fontSize="sm">
                      • Projekt stanie się <Text as="span" fontWeight="bold">widoczny</Text> na liście projektów
                    </Text>
                    <Text fontSize="sm">
                      • Wszyscy członkowie projektu będą mieli dostęp
                    </Text>
                    <Text fontSize="sm">
                      • Wszystkie dane projektu są zachowane
                    </Text>
                  </VStack>
                </Box>
              )}
            </VStack>
          </ModalBody>
          <ModalFooter>
            <Button 
              variant="ghost" 
              mr={3} 
              onClick={toggleStatusModal.onClose}
              isDisabled={togglingStatus}
            >
              Anuluj
            </Button>
            <Button 
              colorScheme={projectToToggle?.isActive ? "red" : "green"}
              onClick={handleToggleProjectStatus}
              isLoading={togglingStatus}
              loadingText={projectToToggle?.isActive ? "Dezaktywuję..." : "Aktywuję..."}
            >
              {projectToToggle?.isActive ? "Dezaktywuj projekt" : "Aktywuj projekt"}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </MainLayout>
  );
}
