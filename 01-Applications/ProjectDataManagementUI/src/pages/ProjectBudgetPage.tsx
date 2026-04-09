import { useContext } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Box, HStack, Button, Text } from "@chakra-ui/react";
import { ArrowLeft } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import ProjectBudgetDashboard from "../components/CostTracker/ProjectBudgetDashboard";
import { AuthContext } from "../context/AuthContext";

export default function ProjectBudgetPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
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
        <HStack px={{ base: 3, md: 6 }} pt={{ base: 3, md: 6 }} spacing={3}>
          <Button
            variant="ghost"
            size="sm"
            leftIcon={<ArrowLeft size={14} />}
            onClick={() => navigate(`/projects/${projectId}`)}
            minH="44px"
          >
            Projekt
          </Button>
        </HStack>

        <ProjectBudgetDashboard tenantId={tenantId} projectId={projectId} />
      </Box>
    </MainLayout>
  );
}
