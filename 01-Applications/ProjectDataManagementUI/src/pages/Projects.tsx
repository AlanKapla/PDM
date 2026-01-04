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
import { projectApi } from "../api/projectApi";
import type { ProjectDetailsWeb } from "../types/project.types";
import { getRoleName, getRoleColor } from "../constants/roleCodes";
import { useToastNotification } from "../hooks/useToastNotification";
import { useTenantPermissions } from "../hooks/useTenantPermissions";
import { useAuth as useAuthContext } from "../context/AuthContext";
import { useModal } from "../hooks/useModal";
import { LoadingSpinner, EmptyState, ErrorAlert } from "../components/common";

export default function Projects() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuthContext();
  const [projects, setProjects] = useState<ProjectDetailsWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTenantId, setActiveTenantId] = useState<string | null | undefined>(undefined);
  const [newProjectName, setNewProjectName] = useState("");
  const [creating, setCreating] = useState(false);
  
  const createModal = useModal();
  const { showSuccess, showError } = useToastNotification();
  const permissions = useTenantPermissions();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  // Pobierz aktywnego tenanta i projekty
  useEffect(() => {
    const activeTenantId = user?.activeTenantId;
    
    // Jeśli nie ma aktywnego tenanta - wyświetl info i zatrzymaj
    if (!activeTenantId || activeTenantId === "00000000-0000-0000-0000-000000000000" || activeTenantId.trim() === "") {
      setActiveTenantId(null);
      setProjects([]);
      setLoading(false);
      return;
    }

    const fetchData = async () => {
      setLoading(true);
      setError(null);
      
      try {
        setActiveTenantId(activeTenantId);
        const projectsResponse = await projectApi.getTenantProjects(activeTenantId);
        setProjects(projectsResponse.data);
      } catch (err) {
        console.error("Błąd pobierania projektów:", err);
        setError("Nie udało się pobrać projektów");
        setProjects([]);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [user?.activeTenantId]);

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
      await projectApi.createProject(activeTenantId, newProjectName.trim());

      showSuccess("Projekt utworzony");
      setNewProjectName("");
      createModal.onClose();
      
      // Odśwież listę projektów
      const projectsResponse = await projectApi.getTenantProjects(activeTenantId);
      setProjects(projectsResponse.data);
    } catch (error) {
      console.error("Błąd podczas tworzenia projektu:", error);
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setCreating(false);
    }
  };

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <Heading size={{ base: "lg", md: "xl" }}>
            Projekty
          </Heading>
          {permissions.canCreateProject && (
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
        ) : !activeTenantId ? (
          <EmptyState 
            icon={FolderKanban}
            title="Nie wybrano aktywnej organizacji"
            description="Wybierz organizację z menu, aby zobaczyć jej projekty"
          />
        ) : error ? (
          <ErrorAlert description={error} />
        ) : projects.length === 0 ? (
          <EmptyState 
            icon={FolderKanban}
            title="Nie masz jeszcze żadnych projektów"
            description="Stwórz swój pierwszy projekt, aby zacząć pracę"
            action={
              permissions.canCreateProject && activeTenantId && (
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
                          <Badge colorScheme={getRoleColor(project.userRoleCode)} fontSize="xs">
                            {getRoleName(project.userRoleCode)}
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
