import { useContext } from "react";
import { useParams } from "react-router-dom";
import { Alert, AlertIcon, Box, Spinner, Text } from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import ProjectBudgetDashboard from "../components/CostTracker/ProjectBudgetDashboard";
import { AuthContext } from "../context/AuthContext";
import { useProjectPermissions } from "../hooks/useProjectPermissions";
import { RoleCodes } from "../constants/roleCodes";

const ADMIN_ROLE_CODES = [
  RoleCodes.PROJECT_ADMIN,
  RoleCodes.TENANT_ADMIN,
  RoleCodes.SYSTEM_SUPERADMIN,
] as const;

export default function ProjectBudgetPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const { user } = useContext(AuthContext);
  const { roleCode, loading: permissionsLoading } = useProjectPermissions(projectId);

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

  if (permissionsLoading) {
    return (
      <MainLayout>
        <Box display="flex" justifyContent="center" alignItems="center" h="50vh">
          <Spinner size="xl" />
        </Box>
      </MainLayout>
    );
  }

  const isAdmin = !!roleCode && ADMIN_ROLE_CODES.includes(roleCode as typeof ADMIN_ROLE_CODES[number]);

  if (!isAdmin) {
    return (
      <MainLayout>
        <Box p={8}>
          <Alert status="warning">
            <AlertIcon />
            Ta strona jest dostępna tylko dla administratorów projektu.
          </Alert>
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
