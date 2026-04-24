import React from 'react';
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
    <div className="dashboard-overview-grid">
      <FinancialOverview
        data={financialData}
        tenantId={tenantId}
        projectId={projectId}
        onRefetch={onRefetch}
      />
      <TimelineOverview data={timelineData} />
    </div>
  );
}

export default OverviewSection;
