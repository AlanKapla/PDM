import { useContext, useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { Alert, AlertIcon, Box, Spinner, Text } from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import { ProjectDashboard } from "../features/dashboard/components/ProjectDashboard";
import { AuthContext } from "../context/AuthContext";
import { useProjectPermissions } from "../hooks/useProjectPermissions";
import { RoleCodes } from "../constants/roleCodes";
import { projectApi } from "../api/projectApi";

const ADMIN_ROLE_CODES = [
  RoleCodes.PROJECT_ADMIN,
  RoleCodes.TENANT_ADMIN,
  RoleCodes.SYSTEM_SUPERADMIN,
] as const;

export default function ProjectBudgetPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const { user } = useContext(AuthContext);
  const { roleCode, loading: permissionsLoading } = useProjectPermissions(projectId);
  const [projectName, setProjectName] = useState<string>('');

  const tenantId = user?.activeTenantId;

  useEffect(() => {
    if (!tenantId || !projectId) return;
    projectApi.getProjectDetails(tenantId, projectId).then((res) => {
      setProjectName(res.data.name);
    }).catch(() => {
      // nazwa projektu niedostępna — wyświetlamy puste
    });
  }, [tenantId, projectId]);

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
        <ProjectDashboard tenantId={tenantId} projectId={projectId} projectName={projectName} />
      </Box>
    </MainLayout>
  );
}
