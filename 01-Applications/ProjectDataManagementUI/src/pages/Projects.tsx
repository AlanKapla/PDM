import { useEffect, useState } from "react";
import {
  Box,
  Heading,
  SimpleGrid,
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
import { ProjectRole } from "../types/project.types";

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

const getProjectRoleName = (role: number) =>
  role === ProjectRole.Admin ? "Administrator" : "Członek";

const getProjectRoleColor = (role: number) =>
  role === ProjectRole.Admin ? "blue" : "green";

export default function Projects() {
  const location = useLocation();
  const navigate = useNavigate();

  const [projects, setProjects] = useState<ProjectDetailsWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [activeTenantId, setActiveTenantId] = useState<string | null>(null);
  const [userTenantRole, setUserTenantRole] = useState<number | null>(null);

  const [newProjectName, setNewProjectName] = useState("");
  const [creating, setCreating] = useState(false);

  const { isOpen, onOpen, onClose } = useDisclosure();
  const toast = useToast();

  const cardBg = useColorModeValue("#ffffff", "#0f0f0f");
  const borderColor = useColorModeValue("#e3e3e3", "#1f1f1f");

  /** Pobierz aktywnego tenanta */
  useEffect(() => {
    async function loadTenant() {
      try {
        const [active, tenants] = await Promise.all([
          tenantApi.getActiveTenant(),
          tenantApi.getUserTenants(),
        ]);

        if (active.ok) {
          const activeData = await active.json();
          setActiveTenantId(activeData.activeTenantId);

          if (tenants.ok && activeData.activeTenantId) {
            const t = await tenants.json();
            const matched = t.find((x: any) => x.id === activeData.activeTenantId);
            if (matched) setUserTenantRole(matched.role);
          }
        }
      } catch (err) {
        setError("Błąd podczas pobierania aktywnego tenanta");
      }
    }

    loadTenant();
  }, [location.key]);

  /** Pobierz projekty */
  useEffect(() => {
    if (!activeTenantId) return;

    async function loadProjects() {
      setLoading(true);
      setError(null);

      try {
        if (!activeTenantId) return;

        const res = await tenantApi.getTenantProjects(activeTenantId);

        if (!res.ok) throw new Error("Nie udało się pobrać projektów");

        setProjects(await res.json());
      } catch (err: any) {
        setError(err.message || "Wystąpił nieoczekiwany błąd");
      } finally {
        setLoading(false);
      }
    }

    loadProjects();
  }, [activeTenantId]);

  const formatDate = (d: string) =>
    new Date(d).toLocaleDateString("pl-PL", {
      year: "numeric",
      month: "long",
      day: "numeric",
    });

  const isAdmin = userTenantRole === TenantRole.Admin;

  /** Tworzenie projektu */
  const handleCreateProject = async () => {
    if (!newProjectName.trim()) {
      toast({
        title: "Podaj nazwę projektu",
        status: "warning",
      });
      return;
    }

    if (!activeTenantId) {
      toast({
        title: "Brak aktywnego tenanta",
        status: "error",
      });
      return;
    }

    setCreating(true);

    try {
      const res = await tenantApi.createProject(activeTenantId, newProjectName.trim());

      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.message || "Nie udało się utworzyć projektu");
      }

      toast({ title: "Projekt utworzony", status: "success" });
      setNewProjectName("");
      onClose();

      const refresh = await tenantApi.getTenantProjects(activeTenantId);
      if (refresh.ok) setProjects(await refresh.json());
    } catch (err: any) {
      toast({
        title: "Błąd",
        description: err.message || "Wystąpił problem",
        status: "error",
      });
    } finally {
      setCreating(false);
    }
  };

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        {/* HEADER */}
        <HStack justify="space-between" mb={8} flexWrap="wrap" gap={4}>
          <Heading size="lg">Projekty</Heading>

          {isAdmin && (
            <Button leftIcon={<Plus size={20} />} colorScheme="blue" onClick={onOpen}>
              Nowy projekt
            </Button>
          )}
        </HStack>

        {/* CONTENT */}
        {loading ? (
          <HStack justify="center" py={20}>
            <Spinner size="xl" />
          </HStack>
        ) : error ? (
          <Alert status="error">
            <AlertIcon />
            {error}
          </Alert>
        ) : projects.length === 0 ? (
          <Alert status="info">
            <AlertIcon />
            Nie masz jeszcze żadnych projektów.
          </Alert>
        ) : (
          <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={6}>
            {projects.map((p) => (
              <Box
                key={p.id}
                bg={cardBg}
                border="1px solid"
                borderColor={borderColor}
                rounded="lg"
                p={6}
                cursor="pointer"
                transition="0.2s"
                _hover={{
                  borderColor: "blue.500",
                  transform: "translateY(-4px)",
                  shadow: "xl",
                }}
                onClick={() => navigate(`/projects/${p.id}`)}
              >
                <VStack align="flex-start" spacing={4}>

                  {/* Icon + status */}
                  <HStack justify="space-between" w="100%">
                    <Icon as={FolderKanban} boxSize={8} color="blue.500" />
                    <Badge
                      colorScheme={p.isActive ? "green" : "gray"}
                      fontSize="xs"
                      px={2}
                      py={0.5}
                      rounded="md"
                    >
                      {p.isActive ? "Aktywny" : "Nieaktywny"}
                    </Badge>
                  </HStack>

                  {/* Project name + role */}
                  <VStack align="flex-start" spacing={1} w="100%">
                    <Heading size="md" isTruncated maxW="100%">
                      {p.name}
                    </Heading>

                    <Badge colorScheme={getProjectRoleColor(p.userRole)} fontSize="xs">
                      {getProjectRoleName(p.userRole)}
                    </Badge>
                  </VStack>

                  {/* Meta info */}
                  <VStack align="flex-start" spacing={1} color="gray.500" fontSize="sm">
                    <HStack>
                      <Icon as={User} size={16} />
                      <Text>{p.createdByUserName}</Text>
                    </HStack>

                    <HStack>
                      <Icon as={Calendar} size={16} />
                      <Text>{formatDate(p.createdAt)}</Text>
                    </HStack>

                    <Text>Członków: {p.membersCount}</Text>
                  </VStack>
                </VStack>
              </Box>
            ))}
          </SimpleGrid>
        )}
      </Box>

      {/* MODAL TWORZENIA PROJEKTU */}
      <Modal isOpen={isOpen} onClose={onClose} isCentered>
        <ModalOverlay />

        <ModalContent>
          <ModalHeader>Nowy projekt</ModalHeader>
          <ModalCloseButton />

          <ModalBody>
            <FormControl>
              <FormLabel>Nazwa projektu</FormLabel>
              <Input
                placeholder="Wprowadź nazwę projektu"
                value={newProjectName}
                onChange={(e) => setNewProjectName(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && !creating && handleCreateProject()}
              />
            </FormControl>
          </ModalBody>

          <ModalFooter>
            <Button variant="ghost" mr={3} onClick={onClose} isDisabled={creating}>
              Anuluj
            </Button>
            <Button colorScheme="blue" onClick={handleCreateProject} isLoading={creating}>
              Utwórz
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </MainLayout>
  );
}
