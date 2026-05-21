import React, { useState } from 'react';
import { Spinner, Alert, AlertIcon, Box } from '@chakra-ui/react';
import { useProjectDashboard } from '../hooks/useProjectDashboard';
import '../dashboard.css';
import { OverviewSection } from './OverviewSection';
import { DashboardTabs } from './DashboardTabs';
import type { DashboardTab } from './DashboardTabs';
import { EstimatesTab } from './tabs/EstimatesTab';
import { SchedulesTab } from './tabs/SchedulesTab';
import { AdditionalCostsTab } from './tabs/AdditionalCostsTab';
import { AllCostsTab } from './tabs/AllCostsTab';
import { DashboardCurrencyProvider } from '../context/DashboardCurrencyContext';

export interface ProjectDashboardProps {
  tenantId: string;
  projectId: string;
  projectName: string;
}

/**
 * Główny komponent dashboardu projektu.
 * Zarządza stanem zakładek i pobieraniem danych.
 */
export function ProjectDashboard({
  tenantId,
  projectId,
  projectName,
}: ProjectDashboardProps): React.ReactElement {
  const { data, isLoading, error, refetch } = useProjectDashboard(tenantId, projectId);
  const [activeTab, setActiveTab] = useState<DashboardTab>('estimates');

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
        <Alert status="error">
          <AlertIcon />
          {error ?? 'Nie udało się załadować dashboardu.'}
        </Alert>
      </Box>
    );
  }

  return (
    <DashboardCurrencyProvider currencySymbol={data.selectedCurrencySymbol ?? 'zł'}>
      <Box px={{ base: 3, md: 5 }} py={4} maxW={1400}>
        <OverviewSection
          financialData={data.financialSummary}
          timelineData={data.timelineSummary}
          tenantId={tenantId}
          projectId={projectId}
          onRefetch={refetch}
        />

        <DashboardTabs
          activeTab={activeTab}
          onTabChange={setActiveTab}
          estimatesCount={data.costEstimateSummaries.length}
          schedulesCount={data.scheduleSummaries.length}
          additionalCount={data.projectAdditionalCosts?.costsCount ?? 0}
          allCostsCount={data.allCosts?.length ?? 0}
        />

        {activeTab === 'estimates' && (
          <EstimatesTab
            summaries={data.costEstimateSummaries}
            tenantId={tenantId}
            projectId={projectId}
            onRefetch={refetch}
          />
        )}

        {activeTab === 'schedules' && (
          <SchedulesTab
            summaries={data.scheduleSummaries}
            financialSummary={data.financialSummary}
            timelineSummary={data.timelineSummary}
            tenantId={tenantId}
            projectId={projectId}
            onRefetch={refetch}
          />
        )}

        {activeTab === 'additional' && data.projectAdditionalCosts && (
          <AdditionalCostsTab
            data={data.projectAdditionalCosts}
            financialSummary={data.financialSummary}
            tenantId={tenantId}
            projectId={projectId}
            onRefetch={refetch}
          />
        )}

        {activeTab === 'all' && (
          <AllCostsTab
            costs={data.allCosts ?? []}
            tenantId={tenantId}
            projectId={projectId}
            onRefetch={refetch}
          />
        )}
      </Box>
    </DashboardCurrencyProvider>
  );
}

export default ProjectDashboard;
