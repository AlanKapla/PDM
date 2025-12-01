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
} from "@chakra-ui/react";
import { FolderKanban, User, Calendar, ArrowLeft, Users } from "lucide-react";
import MainLayout from "../layout/MainLayout";

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

export default function ProjectDetails() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const [project] = useState<ProjectDetailsWeb | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  useEffect(() => {
    // TODO: Implementacja pobierania szczegółów projektu z API
    // Placeholder - symulacja ładowania
    setLoading(false);
    setError("Endpoint szczegółów projektu nie jest jeszcze zaimplementowany");
  }, [projectId]);

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
          </Box>
        ) : error ? (
          <Alert status="error">
            <AlertIcon />
            {error}
          </Alert>
        ) : project ? (
          <VStack spacing={6} align="stretch">
            {/* Nagłówek projektu */}
            <Box bg={cardBg} p={6} rounded="lg" borderWidth="1px" borderColor={borderColor}>
              <HStack justify="space-between" mb={4} flexWrap="wrap">
                <HStack>
                  <Icon as={FolderKanban} boxSize={10} color="blue.600" />
                  <Heading size="lg">{project.name}</Heading>
                </HStack>
                <HStack>
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

            {/* Sekcje projektu - do zaimplementowania */}
            <Box bg={cardBg} p={6} rounded="lg" borderWidth="1px" borderColor={borderColor}>
              <Heading size="md" mb={4}>Sekcje projektu</Heading>
              <Text color="gray.500">Funkcje projektu będą dostępne wkrótce...</Text>
            </Box>
          </VStack>
        ) : null}
      </Box>
    </MainLayout>
  );
}
