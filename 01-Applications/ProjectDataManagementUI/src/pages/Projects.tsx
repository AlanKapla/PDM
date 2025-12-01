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
  Spinner,
  Alert,
  AlertIcon,
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
  useDisclosure,
  useToast,
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, Plus } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { tenantApi } from "../api/tenantApi";
import { TenantRole } from "../types/auth.types";

const ProjectRole = {
  Owner: 0,
  Admin: 1,
  Member: 2,
  Viewer: 3
} as const;

type ProjectRole = typeof ProjectRole[keyof typeof ProjectRole];

interface ProjectDetailsWeb {
  id: string;
  tenantId: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  userRole: ProjectRole;
  membersCount: number;
}

const getProjectRoleName = (role: ProjectRole): string => {
  switch (role) {
    case ProjectRole.Owner:
      return 'Właściciel';
    case ProjectRole.Admin:
      return 'Administrator';
    case ProjectRole.Member:
      return 'Członek';
    case ProjectRole.Viewer:
      return 'Przeglądający';
    default:
      return 'Nieznana rola';
  }
};

const getProjectRoleColor = (role: ProjectRole): string => {
  switch (role) {
    case ProjectRole.Owner:
      return 'purple';
    case ProjectRole.Admin:
      return 'blue';
    case ProjectRole.Member:
      return 'green';
    case ProjectRole.Viewer:
      return 'gray';
    default:
      return 'gray';
  }
};

export default function Projects() {
  const location = useLocation();
  const navigate = useNavigate();
  const [projects, setProjects] = useState<ProjectDetailsWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTenantId, setActiveTenantId] = useState<string | null>(null);
  const [userTenantRole, setUserTenantRole] = useState<TenantRole | null>(null);
  const [newProjectName, setNewProjectName] = useState("");
  const [creating, setCreating] = useState(false);
  
  const { isOpen, onOpen, onClose } = useDisclosure();
  const toast = useToast();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  // Pobierz aktywnego tenanta i rolę użytkownika przy każdym wejściu na stronę
  useEffect(() => {
    const fetchActiveTenant = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const [activeTenantResponse, tenantsResponse] = await Promise.all([
          tenantApi.getActiveTenant(),
          tenantApi.getUserTenants(),
        ]);

        if (activeTenantResponse.ok) {
          const activeTenantData = await activeTenantResponse.json();
          setActiveTenantId(activeTenantData.activeTenantId);
          
          // Znajdź rolę użytkownika w aktywnym tenancie
          if (tenantsResponse.ok && activeTenantData.activeTenantId) {
            const tenants = await tenantsResponse.json();
            const activeTenant = tenants.find((t: any) => t.id === activeTenantData.activeTenantId);
            if (activeTenant) {
              setUserTenantRole(activeTenant.role);
            }
          }
        } else {
          setError("Nie udało się pobrać aktywnego tenanta");
        }
      } catch (err) {
        console.error("Błąd pobierania aktywnego tenanta:", err);
        setError("Błąd połączenia z serwerem");
      }
    };

    fetchActiveTenant();
  }, [location.key]);

  useEffect(() => {
    if (!activeTenantId) return;

    const fetchProjects = async () => {
      setLoading(true);
      setError(null);

      try {
        const response = await tenantApi.getTenantProjects(activeTenantId);

        if (!response.ok) {
          throw new Error("Nie udało się pobrać projektów");
        }

        const data = await response.json();
        setProjects(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Wystąpił błąd");
      } finally {
        setLoading(false);
      }
    };

    fetchProjects();
  }, [activeTenantId]);

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("pl-PL", {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  const handleCreateProject = async () => {
    if (!newProjectName.trim()) {
      toast({
        title: "Nazwa projektu wymagana",
        status: "warning",
        duration: 3000,
      });
      return;
    }

    if (!activeTenantId) {
      toast({
        title: "Brak aktywnego tenanta",
        status: "error",
        duration: 3000,
      });
      return;
    }

    setCreating(true);
    try {
      const response = await tenantApi.createProject(activeTenantId, newProjectName.trim());

      if (response.ok) {
        toast({
          title: "Projekt utworzony",
          status: "success",
          duration: 3000,
        });
        setNewProjectName("");
        onClose();
        
        // Odśwież listę projektów
        const projectsResponse = await tenantApi.getTenantProjects(activeTenantId);
        if (projectsResponse.ok) {
          const data = await projectsResponse.json();
          setProjects(data);
        }
      } else {
        const errorData = await response.json().catch(() => ({}));
        toast({
          title: "Błąd tworzenia projektu",
          description: errorData.message || "Nie udało się utworzyć projektu",
          status: "error",
          duration: 3000,
        });
      }
    } catch (err) {
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
      });
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
              onClick={onOpen}
              size={{ base: "sm", md: "md" }}
            >
              Nowy projekt
            </Button>
          )}
        </HStack>

        {loading ? (
          <Box display="flex" justifyContent="center" alignItems="center" minH="200px">
            <Spinner size="xl" color="blue.500" />
          </Box>
        ) : error ? (
          <Alert status="error">
            <AlertIcon />
            {error}
          </Alert>
        ) : projects.length === 0 ? (
          <Alert status="info">
            <AlertIcon />
            Nie masz jeszcze żadnych projektów
          </Alert>
        ) : (
          <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={6}>
            {projects.map((project) => (
              <Card
                key={project.id}
                bg={cardBg}
                borderWidth="1px"
                borderColor={borderColor}
                cursor="pointer"
                transition="all 0.2s"
                _hover={{
                  transform: "translateY(-4px)",
                  shadow: "xl",
                  borderColor: "blue.500",
                }}
                onClick={() => navigate(`/projects/${project.id}`)}
              >
                <CardBody>
                  <VStack align="flex-start" spacing={4}>
                    <HStack justify="space-between" w="100%">
                      <Icon as={FolderKanban} boxSize={8} color="blue.600" />
                      <Badge
                        colorScheme={project.isActive ? "green" : "gray"}
                        fontSize="xs"
                      >
                        {project.isActive ? "Aktywny" : "Nieaktywny"}
                      </Badge>
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
      <Modal isOpen={isOpen} onClose={onClose}>
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
            <Button variant="ghost" mr={3} onClick={onClose} isDisabled={creating}>
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
