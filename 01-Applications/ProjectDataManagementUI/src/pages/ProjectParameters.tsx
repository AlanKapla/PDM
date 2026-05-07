import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Icon,
  Button,
  useColorModeValue,
} from "@chakra-ui/react";
import { ArrowLeft, Settings } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { useAuth } from "../context/AuthContext";
import { useProjectPermissions } from "../hooks/useProjectPermissions";
import { useProjectDetails } from "../hooks/queries";
import { LoadingSpinner } from "../components/common";
import CurrencySelector from "../components/ProjectParameters/CurrencySelector";

export default function ProjectParameters() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const permissions = useProjectPermissions(projectId);

  const { data: project, isLoading } = useProjectDetails(
    user?.activeTenantId ?? undefined,
    projectId
  );

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  if (isLoading || permissions.loading) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
          <LoadingSpinner />
        </Box>
      </MainLayout>
    );
  }

  if (!permissions.canView) {
    return (
      <MainLayout>
        <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
          <Box bg={cardBg} p={8} rounded="lg" borderWidth="1px" borderColor={borderColor}>
            <VStack spacing={4}>
              <Heading size="md" color="red.500">Brak dostępu</Heading>
              <Text>Nie masz uprawnień do przeglądania parametrów tego projektu.</Text>
            </VStack>
          </Box>
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <Button
          leftIcon={<ArrowLeft size={16} />}
          variant="ghost"
          size="sm"
          mb={4}
          onClick={() => navigate(`/projects/${projectId}`)}
        >
          Powrót do projektu
        </Button>

        <HStack justify="space-between" mb={{ base: 6, md: 8 }} flexWrap="wrap" gap={{ base: 2, md: 4 }}>
          <HStack spacing={{ base: 2, md: 3 }}>
            <Icon as={Settings} boxSize={{ base: 6, md: 8 }} color="action.600" />
            <VStack align="flex-start" spacing={0}>
              <Heading size={{ base: "md", md: "lg" }}>Parametry</Heading>
              {project && (
                <Text fontSize={{ base: "xs", md: "sm" }} color="neutral.600">
                  {project.name}
                </Text>
              )}
            </VStack>
          </HStack>
        </HStack>

        <VStack align="stretch" spacing={6}>
          <Box
            bg={cardBg}
            p={{ base: 4, md: 6 }}
            rounded="lg"
            borderWidth="1px"
            borderColor={borderColor}
            shadow="sm"
          >
            <Heading size="sm" mb={4}>Waluta</Heading>
            {user?.activeTenantId && projectId && (
              <CurrencySelector
                tenantId={user.activeTenantId}
                projectId={projectId}
                currentCurrency={project?.currency}
                canEdit={permissions.canEdit}
              />
            )}
          </Box>
        </VStack>
      </Box>
    </MainLayout>
  );
}
