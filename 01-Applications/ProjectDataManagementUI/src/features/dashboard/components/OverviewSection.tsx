import React from 'react';
import { SimpleGrid } from '@chakra-ui/react';
import type { ProjectFinancialSummaryWeb, ProjectTimelineSummaryWeb } from '../types/projectDashboard.types';
import { FinancialOverview } from './FinancialOverview';
import { TimelineOverview } from './TimelineOverview';

export interface OverviewSectionProps {
  financialData: ProjectFinancialSummaryWeb;
  timelineData: ProjectTimelineSummaryWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/** Sekcja podwójna: Finanse + Postęp projektu obok siebie. */
export function OverviewSection({
  financialData,
  timelineData,
  tenantId,
  projectId,
  onRefetch,
}: OverviewSectionProps): React.ReactElement {
  return (
    <SimpleGrid
      columns={{ base: 1, lg: 2 }}
      spacing={4}
      mb={6}
      w="100%"
    >
      <FinancialOverview
        data={financialData}
        tenantId={tenantId}
        projectId={projectId}
        onRefetch={onRefetch}
      />
      <TimelineOverview data={timelineData} />
    </SimpleGrid>
  );
}

export default OverviewSection;
