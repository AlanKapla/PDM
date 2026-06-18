import { useContext } from "react";
import { useParams } from "react-router-dom";
import { Box, Spinner, Text } from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import { BackToProjectButton } from "../components/common";
import { ProjectDashboard } from "../features/dashboard/components/ProjectDashboard";
import { AuthContext } from "../context/AuthContext";
import { useProjectPermissions } from "../hooks/useProjectPermissions";
import { useProjectDetails } from "../hooks/queries";

export default function ProjectBudgetPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const { user } = useContext(AuthContext);
  const { loading: permissionsLoading } = useProjectPermissions(projectId);

  const tenantId = user?.activeTenantId;

  const { data: projectData, isLoading: projectLoading } = useProjectDetails(
    tenantId ?? undefined,
    projectId
  );

  if (!tenantId || !projectId) {
    return (
      <MainLayout>
        <Box p={8}>
          <Text color="neutral.500">Brak wymaganego kontekstu (tenant lub projekt).</Text>
        </Box>
      </MainLayout>
    );
  }

  if (permissionsLoading || projectLoading) {
    return (
      <MainLayout>
        <Box display="flex" justifyContent="center" alignItems="center" h="50vh">
          <Spinner size="xl" />
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh" w="100%" maxW="100%">
        <BackToProjectButton />
        <ProjectDashboard
          tenantId={tenantId}
          projectId={projectId}
          projectName={projectData?.name}
        />
      </Box>
    </MainLayout>
  );
}
