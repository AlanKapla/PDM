import React, { useState } from 'react';
import { Alert, AlertIcon, Box, Button, Spinner, TabPanel } from '@chakra-ui/react';
import { useProjectDashboard } from '../hooks/useProjectDashboard';
import '../dashboard.css';
import { computeDashboardAlert } from '../utils/dashboardAlert';
import { DashboardHeader } from './DashboardHeader';
import { DashboardMainTabs, DASHBOARD_TAB_INDEX } from './DashboardMainTabs';
import { DashboardPageHeader } from './DashboardPageHeader';
import { GeneralTab } from './tabs/GeneralTab';
import { FinanceTab } from './tabs/FinanceTab';
import { SchedulesTab } from './tabs/SchedulesTab';
import { CostsTab } from './tabs/CostsTab';
import { DashboardCurrencyProvider } from '../context/DashboardCurrencyContext';

export interface ProjectDashboardProps {
  tenantId: string;
  projectId: string;
  projectName?: string;
}

export function ProjectDashboard({
  tenantId,
  projectId,
  projectName,
}: ProjectDashboardProps): React.ReactElement {
  const { data, isLoading, error, refetch } = useProjectDashboard(tenantId, projectId);
  const [tabIndex, setTabIndex] = useState<number>(DASHBOARD_TAB_INDEX.general);

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" h="40vh">
        <Spinner size="xl" />
      </Box>
    );
  }

  if (error || !data) {
    return (
      <Box p={6}>
        <Alert status="error" mb={4}>
          <AlertIcon />
          {error ?? 'Nie udało się załadować dashboardu.'}
        </Alert>
        <Button size="sm" onClick={() => refetch()}>
          Spróbuj ponownie
        </Button>
      </Box>
    );
  }

  const alert = computeDashboardAlert(data);

  return (
    <DashboardCurrencyProvider currencySymbol={data.selectedCurrencySymbol ?? 'zł'}>
      <Box className="dashboard-page" w="100%" maxW="100%">
        <DashboardPageHeader
          projectName={projectName}
          tenantId={tenantId}
          projectId={projectId}
          onRefetch={refetch}
        />

        <DashboardHeader data={data} />

        {alert && (
          <Alert status={alert.status} borderRadius="md" mb={6} fontSize="sm">
            <AlertIcon />
            {alert.message}
          </Alert>
        )}

        <DashboardMainTabs
          tabIndex={tabIndex}
          onTabChange={setTabIndex}
          estimatesCount={data.costEstimateSummaries.length}
          schedulesCount={data.scheduleSummaries.length}
          costsCount={data.allCosts?.length ?? 0}
        >
          <TabPanel px={{ base: 2, md: 4 }} pt={4}>
            <GeneralTab
              data={data}
              tenantId={tenantId}
              projectId={projectId}
              onRefetch={refetch}
              onShowFinanceTab={() => setTabIndex(DASHBOARD_TAB_INDEX.finance)}
            />
          </TabPanel>
          <TabPanel px={{ base: 2, md: 4 }} pt={4}>
            <FinanceTab
              data={data}
              tenantId={tenantId}
              projectId={projectId}
              onRefetch={refetch}
            />
          </TabPanel>
          <TabPanel px={{ base: 2, md: 4 }} pt={4}>
            <SchedulesTab
              data={data}
              tenantId={tenantId}
              projectId={projectId}
              onRefetch={refetch}
            />
          </TabPanel>
          <TabPanel px={{ base: 2, md: 4 }} pt={4}>
            <CostsTab
              costs={data.allCosts ?? []}
              costByCategory={data.costByCategory ?? []}
              tenantId={tenantId}
              projectId={projectId}
              onRefetch={refetch}
            />
          </TabPanel>
        </DashboardMainTabs>
      </Box>
    </DashboardCurrencyProvider>
  );
}

export default ProjectDashboard;
