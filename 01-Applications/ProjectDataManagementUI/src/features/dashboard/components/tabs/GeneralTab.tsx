import React from 'react';
import { Box } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../../types/projectDashboard.types';
import { OverviewSection } from '../OverviewSection';
import { GeneralChartsSection } from '../GeneralChartsSection';

export interface GeneralTabProps {
  data: ProjectDashboardWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
  onShowFinanceTab: () => void;
}

export function GeneralTab({
  data,
  tenantId,
  projectId,
  onRefetch,
  onShowFinanceTab,
}: GeneralTabProps): React.ReactElement {
  return (
    <Box w="100%">
      <OverviewSection
        financialData={data.financialSummary}
        timelineData={data.timelineSummary}
        tenantId={tenantId}
        projectId={projectId}
        onRefetch={onRefetch}
      />
      <GeneralChartsSection data={data} onShowFinanceTab={onShowFinanceTab} />
    </Box>
  );
}

export default GeneralTab;
