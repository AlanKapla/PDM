import { useEffect, useState } from "react";
import {
  Box,
  Heading,
  Text,
  Badge,
  VStack,
  HStack,
  Icon,
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
import { useToastNotification } from "../hooks/useToastNotification";
import { useTenantPermissions } from "../hooks/useTenantPermissions";
import { useAuth as useAuthContext } from "../context/AuthContext";
import { useModal } from "../hooks/useModal";
import { LoadingSpinner, EmptyState, ErrorAlert } from "../components/common";
import { changeActiveTenant } from "../services/tenantService";
import { useProjects, useMyTenants, projectKeys } from "../hooks/queries";
import { useQueryClient } from "@tanstack/react-query";
import { formatDate } from "../utils/formatters";

export default function Projects() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, refreshUser } = useAuthContext();
  const queryClient = useQueryClient();

  // Sprawdź czy aktywny tenant jest poprawny
  const rawTenantId = user?.activeTenantId;
  const isValidTenant = Boolean(
    rawTenantId &&
    rawTenantId !== "00000000-0000-0000-0000-000000000000" &&
    rawTenantId.trim() !== ""
  );
  const activeTenantId = isValidTenant ? rawTenantId : null;

  const {
    data: projects = [],
    isLoading: loading,
    error: projectsError,
  } = useProjects(activeTenantId ?? undefined);

  const {
    data: tenants = [],
    isLoading: tenantsLoading,
  } = useMyTenants();

  const error = projectsError
    ? "Nie udało się pobrać projektów"
    : null;

  const [newProjectName, setNewProjectName] = useState("");
  const [creating, setCreating] = useState(false);
  const [switching, setSwitching] = useState(false);
  
  const createModal = useModal();
  const { showSuccess, showError, showApiSuccess } = useToastNotification();
  const permissions = useTenantPermissions();

  const handleTenantSwitch = async (newTenantId: string) => {
    if (!newTenantId || newTenantId === activeTenantId) return;

    setSwitching(true);
    try {
      await changeActiveTenant(newTenantId);
      await refreshUser();
      showApiSuccess('tenantSwitched');
    } catch (err) {
      const { title, description } = handleApiError(err);
      showError(title, description);
    } finally {
      setSwitching(false);
    }
  };

  const handleCreateProject = async () => {
    if (!newProjectName.trim()) {
      showError("Sprawdź formularz", "Nazwa projektu jest wymagana");
      return;
    }

    if (!activeTenantId) {
      showError("Sprawdź formularz", "Brak aktywnej organizacji");
      return;
    }

    setCreating(true);
    try {
      await projectApi.createProject(activeTenantId, newProjectName.trim());

      showApiSuccess('projectCreated');
      setNewProjectName("");
      createModal.onClose();
      
      // Odśwież listę projektów
      queryClient.invalidateQueries({
        queryKey: projectKeys.list(activeTenantId!)
      });
    } catch (error) {
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
          bg="white"
          borderWidth="1px"
          borderColor="neutral.200"
          borderRadius="lg"
        >
          <HStack spacing={{ base: 2, md: 4 }} flexWrap="wrap" gap={{ base: 2, md: 3 }}>
            <HStack spacing={2} flexShrink={0}>
              <Icon as={Building2} boxSize={{ base: 4, md: 5 }} color="level2.500" />
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
                  borderColor="neutral.200"
                  _hover={{ borderColor: "primary.300" }}
                  _focus={{ borderColor: "primary.400", boxShadow: "0 0 0 1px var(--chakra-colors-primary-400)" }}
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
              <Text fontSize="xs" color="level2.500" fontWeight="medium">
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
              colorScheme="primary"
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
                <Button leftIcon={<Icon as={Plus} />} colorScheme="primary" onClick={createModal.onOpen}>
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
                bg="white"
                rounded="lg"
                borderWidth="1px"
                borderColor="neutral.200"
                overflow="hidden"
                cursor="pointer"
                onClick={() => navigate(`/projects/${project.id}`)}
                _hover={{
                  borderColor: "primary.300",
                  bg: "neutral.25",
                }}
                transition="all 0.2s"
              >
                <Box p={{ base: 3, md: 4 }}>
                  <HStack justify="space-between" align="flex-start" spacing={{ base: 2, md: 3 }} flexWrap={{ base: "wrap", md: "nowrap" }}>
                    <HStack spacing={{ base: 2, md: 3 }} flex={1} minW="0">
                      <Icon as={FolderKanban} boxSize={{ base: 5, md: 6 }} color="primary.600" flexShrink={0} />
                      <VStack align="flex-start" spacing={1} flex={1} minW="0">
                        <HStack spacing={2} flexWrap="wrap">
                          <Text fontWeight="bold" fontSize={{ base: "sm", md: "lg" }} noOfLines={1}>
                            {project.name}
                          </Text>
                          <Badge colorScheme={project.isAdmin ? "purple" : "blue"} fontSize={{ base: "xs", md: "xs" }}>
                            {project.isAdmin ? "Admin" : "Cz\u0142onek"}
                          </Badge>
                        </HStack>
                          <HStack spacing={{ base: 2, md: 4 }} fontSize={{ base: "xs", md: "sm" }} color="neutral.600" flexWrap="wrap">
                          <HStack spacing={1}>
                            <Icon as={User} boxSize={3} />
                            <Text noOfLines={1}>{project.createdByUserName}</Text>
                          </HStack>
                          <HStack spacing={1}>
                            <Icon as={Calendar} boxSize={3} />
                            <Text noOfLines={1}>{formatDate(project.createdAt, false)}</Text>
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
              colorScheme="primary"
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
