import React from 'react';
import { Box, Text } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { BudgetCoverageDonut } from './charts/BudgetCoverageDonut';
import { CostSourcesDonut } from './charts/CostSourcesDonut';
import { EstimateBudgetBarChart } from './charts/EstimateBudgetBarChart';
import { WorkStatusDonut } from './charts/WorkStatusDonut';
import { ScheduleProgressBarChart } from './charts/ScheduleProgressBarChart';
import { ProjectTimelineSpan } from './charts/ProjectTimelineSpan';

export interface GeneralChartsSectionProps {
  data: ProjectDashboardWeb;
  onShowFinanceTab: () => void;
}

export function GeneralChartsSection({
  data,
  onShowFinanceTab,
}: GeneralChartsSectionProps): React.ReactElement {
  const { financialSummary } = data;

  return (
    <Box as="section" aria-label="Wykresy podsumowujące">
      <Text fontSize="md" fontWeight="semibold" color="neutral.800" mb={3}>
        Wykresy
      </Text>
      <div className="dashboard-general-charts-grid">
        <BudgetCoverageDonut
          coveredPercent={financialSummary.coveredPercent}
          isBudgetExceeded={financialSummary.isBudgetExceeded}
          totalBudget={financialSummary.totalBudgetNet}
          totalCosts={financialSummary.totalCostsNet}
        />
        <WorkStatusDonut data={data.timelineSummary} />
        <CostSourcesDonut
          linkedCosts={financialSummary.linkedCostsNet}
          additionalCosts={financialSummary.additionalCostsNet}
        />
        <ScheduleProgressBarChart summaries={data.scheduleSummaries} />
        <div className="dashboard-chart-row-full">
          <ProjectTimelineSpan
            data={data.timelineSummary}
            referenceDate={data.referenceDate}
          />
        </div>
        <div className="dashboard-chart-row-full">
          <EstimateBudgetBarChart
            summaries={data.costEstimateSummaries}
            limit={5}
            onShowAll={onShowFinanceTab}
            title="Budżet vs koszty — top kosztorysy"
          />
        </div>
      </div>
    </Box>
  );
}

export default GeneralChartsSection;
