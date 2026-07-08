import React from 'react';
import { Box, Text } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { BudgetCoverageDonut } from './charts/BudgetCoverageDonut';
import { EstimateBudgetBarChart } from './charts/EstimateBudgetBarChart';

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
