import { useEffect, useState } from "react";
import {
  Box,
  Heading,
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
  Select,
  Skeleton,
  Tooltip,
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, Plus, Building2 } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { handleApiError } from "../utils/handleApiError";
import { projectApi } from "../api/projectApi";
import type { ProjectDetailsWeb } from "../types/project.types";
import type { UserTenant } from "../types/auth.types";
import { getRoleName, getRoleColor } from "../constants/roleCodes";
import { useToastNotification } from "../hooks/useToastNotification";
import { useTenantPermissions } from "../hooks/useTenantPermissions";
import { useAuth as useAuthContext } from "../context/AuthContext";
import { useModal } from "../hooks/useModal";
import { LoadingSpinner, EmptyState, ErrorAlert } from "../components/common";
import { getUserTenants, changeActiveTenant } from "../services/tenantService";

export default function Projects() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, refreshUser } = useAuthContext();
  const [projects, setProjects] = useState<ProjectDetailsWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTenantId, setActiveTenantId] = useState<string | null | undefined>(undefined);
  const [newProjectName, setNewProjectName] = useState("");
  const [creating, setCreating] = useState(false);

  // Przełączanie organizacji
  const [tenants, setTenants] = useState<UserTenant[]>([]);
  const [tenantsLoading, setTenantsLoading] = useState(true);
  const [switching, setSwitching] = useState(false);
  
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

  // Pobierz listę organizacji użytkownika
  useEffect(() => {
    const fetchTenants = async () => {
      try {
        const data = await getUserTenants();
        setTenants(data);
      } catch (err) {
        console.error("Błąd pobierania organizacji:", err);
      } finally {
        setTenantsLoading(false);
      }
    };
    fetchTenants();
  }, []);

  const handleTenantSwitch = async (newTenantId: string) => {
    if (!newTenantId || newTenantId === activeTenantId) return;

    setSwitching(true);
    try {
      await changeActiveTenant(newTenantId);
      await refreshUser();
      showSuccess("Organizacja przełączona");
    } catch (err) {
      console.error("Błąd przełączania organizacji:", err);
      const { title, description } = handleApiError(err);
      showError(title, description);
    } finally {
      setSwitching(false);
    }
  };

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
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        {/* Przełącznik organizacji */}
        <Box
          mb={{ base: 4, md: 6 }}
          p={{ base: 3, md: 4 }}
          bg={cardBg}
          borderWidth="1px"
          borderColor={borderColor}
          borderRadius="lg"
        >
          <HStack spacing={{ base: 2, md: 4 }} flexWrap="wrap" gap={{ base: 2, md: 3 }}>
            <HStack spacing={2} flexShrink={0}>
              <Icon as={Building2} boxSize={{ base: 4, md: 5 }} color="purple.500" />
              <Text
                fontWeight="semibold"
                fontSize={{ base: "xs", md: "sm" }}
                color="gray.600"
                whiteSpace="nowrap"
              >
                Organizacja:
              </Text>
            </HStack>
            {tenantsLoading ? (
              <Skeleton height="36px" width="220px" borderRadius="md" />
            ) : tenants.length <= 1 ? (
              <Text fontWeight="bold" fontSize={{ base: "sm", md: "md" }}>
                {tenants.find((t) => t.id === activeTenantId)?.name ?? "—"}
              </Text>
            ) : (
              <Tooltip label="Przełącz aktywną organizację" openDelay={600}>
                <Select
                  value={activeTenantId ?? ""}
                  onChange={(e) => handleTenantSwitch(e.target.value)}
                  isDisabled={switching}
                  maxW={{ base: "100%", md: "360px" }}
                  size={{ base: "sm", md: "md" }}
                  fontWeight="semibold"
                  borderColor="purple.300"
                  _hover={{ borderColor: "purple.400" }}
                  _focus={{ borderColor: "purple.500", boxShadow: "0 0 0 1px var(--chakra-colors-purple-500)" }}
                  icon={switching ? <></> : undefined}
                >
                  {tenants.map((tenant) => (
                    <option key={tenant.id} value={tenant.id}>
                      {tenant.name}
                    </option>
                  ))}
                </Select>
              </Tooltip>
            )}
            {switching && (
              <Text fontSize="xs" color="purple.500" fontWeight="medium">
                Przełączanie…
              </Text>
            )}
          </HStack>
        </Box>

        <HStack justify="space-between" mb={{ base: 4, md: 6 }} flexWrap="wrap" gap={{ base: 2, md: 4 }}>
          <Heading size={{ base: "md", sm: "lg", md: "xl" }}>
            Projekty
          </Heading>
          {permissions.canCreateProject && (
            <Button
              leftIcon={<Plus size={16} />}
              colorScheme="blue"
              onClick={createModal.onOpen}
              size={{ base: "sm", md: "md" }}
              fontSize={{ base: "xs", md: "sm" }}
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
            title={permissions.canCreateProject ? "Nie masz jeszcze żadnych projektów" : "Brak projektów w tej organizacji"}
            description={permissions.canCreateProject ? "Stwórz swój pierwszy projekt, aby zacząć pracę" : undefined}
            action={
              permissions.canCreateProject && activeTenantId && (
                <Button leftIcon={<Icon as={Plus} />} colorScheme="blue" onClick={createModal.onOpen}>
                  Utwórz projekt
                </Button>
              )
            }
          />
        ) : (
          <VStack spacing={{ base: 2, md: 4 }} align="stretch">
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
                <Box p={{ base: 3, md: 4 }}>
                  <HStack justify="space-between" align="flex-start" spacing={{ base: 2, md: 3 }} flexWrap={{ base: "wrap", md: "nowrap" }}>
                    <HStack spacing={{ base: 2, md: 3 }} flex={1} minW="0">
                      <Icon as={FolderKanban} boxSize={{ base: 5, md: 6 }} color="blue.600" flexShrink={0} />
                      <VStack align="flex-start" spacing={1} flex={1} minW="0">
                        <HStack spacing={2} flexWrap="wrap">
                          <Text fontWeight="bold" fontSize={{ base: "sm", md: "lg" }} noOfLines={1}>
                            {project.name}
                          </Text>
                          <Badge colorScheme={project.isActive ? "green" : "gray"} fontSize={{ base: "10px", md: "xs" }}>
                            {project.isActive ? "Aktywny" : "Nieaktywny"}
                          </Badge>
                          <Badge colorScheme={getRoleColor(project.userRoleCode)} fontSize={{ base: "10px", md: "xs" }}>
                            {getRoleName(project.userRoleCode)}
                          </Badge>
                        </HStack>
                        <HStack spacing={{ base: 2, md: 4 }} fontSize={{ base: "10px", md: "sm" }} color="gray.600" flexWrap="wrap">
                          <HStack spacing={1}>
                            <Icon as={User} boxSize={3} />
                            <Text noOfLines={1}>{project.createdByUserName}</Text>
                          </HStack>
                          <HStack spacing={1}>
                            <Icon as={Calendar} boxSize={3} />
                            <Text noOfLines={1}>{formatDate(project.createdAt)}</Text>
                          </HStack>
                          <Text noOfLines={1}>
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
      <Modal isOpen={createModal.isOpen} onClose={createModal.onClose} size={{ base: "full", md: "md" }}>
        <ModalOverlay />
        <ModalContent mx={{ base: 0, md: "auto" }}>
          <ModalHeader fontSize={{ base: "lg", md: "xl" }}>Utwórz nowy projekt</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <FormControl>
              <FormLabel fontSize={{ base: "sm", md: "md" }}>Nazwa projektu</FormLabel>
              <Input
                placeholder="Wprowadź nazwę projektu"
                value={newProjectName}
                onChange={(e) => setNewProjectName(e.target.value)}
                onKeyPress={(e) => {
                  if (e.key === "Enter" && !creating) {
                    handleCreateProject();
                  }
                }}
                fontSize={{ base: "sm", md: "md" }}
              />
            </FormControl>
          </ModalBody>
          <ModalFooter gap={2}>
            <Button variant="ghost" onClick={createModal.onClose} isDisabled={creating} size={{ base: "sm", md: "md" }}>
              Anuluj
            </Button>
            <Button
              colorScheme="blue"
              onClick={handleCreateProject}
              isLoading={creating}
              size={{ base: "sm", md: "md" }}
            >
              Utwórz
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </MainLayout>
  );
}
