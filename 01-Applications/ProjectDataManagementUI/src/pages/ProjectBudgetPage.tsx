import { useContext } from "react";
import { useParams } from "react-router-dom";
import { Box, Text } from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import ProjectBudgetDashboard from "../components/CostTracker/ProjectBudgetDashboard";
import { AuthContext } from "../context/AuthContext";

export default function ProjectBudgetPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const { user } = useContext(AuthContext);

  const tenantId = user?.activeTenantId;

  if (!tenantId || !projectId) {
    return (
      <MainLayout>
        <Box p={8}>
          <Text color="gray.500">Brak wymaganego kontekstu (tenant lub projekt).</Text>
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box>
        <ProjectBudgetDashboard tenantId={tenantId} projectId={projectId} />
      </Box>
    </MainLayout>
  );
}
