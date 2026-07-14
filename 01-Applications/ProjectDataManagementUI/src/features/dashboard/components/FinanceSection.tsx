import React from 'react';
import { Box, Text } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { BudgetCoverageDonut } from './charts/BudgetCoverageDonut';
import { BudgetCompositionChart } from './charts/BudgetCompositionChart';
import { CostSourcesDonut } from './charts/CostSourcesDonut';
import { EstimateBudgetBarChart } from './charts/EstimateBudgetBarChart';
import { CostTimeSeriesChart } from './charts/CostTimeSeriesChart';
import { ScheduleCostsBarChart } from './charts/ScheduleCostsBarChart';
import { CostSourceTypeChart } from './charts/CostSourceTypeChart';
import { TopContractorsChart } from './charts/TopContractorsChart';
import { EstimateDeviationChart } from './charts/EstimateDeviationChart';

export interface FinanceSectionProps {
  data: ProjectDashboardWeb;
  showAllEstimates?: boolean;
  onShowAllEstimates?: () => void;
}

export function FinanceSection({
  data,
  showAllEstimates = false,
  onShowAllEstimates,
}: FinanceSectionProps): React.ReactElement {
  const { financialSummary } = data;

  return (
    <Box as="section" mb={6} aria-label="Sekcja wykresów finansowych">
      <Text fontSize="md" fontWeight="semibold" color="neutral.800" mb={3}>
        Wykresy finansowe
      </Text>
      <div className="dashboard-finance-grid">
        <BudgetCoverageDonut
          coveredPercent={financialSummary.coveredPercent}
          isBudgetExceeded={financialSummary.isBudgetExceeded}
          totalBudget={financialSummary.totalBudgetNet}
          totalCosts={financialSummary.totalCostsNet}
        />
        <BudgetCompositionChart
          estimateBudget={financialSummary.estimateBudgetNet}
          reserveBudget={financialSummary.projectReserveBudgetNet}
        />
        <CostSourcesDonut
          linkedCosts={financialSummary.linkedCostsNet}
          additionalCosts={financialSummary.additionalCostsNet}
        />
        <ScheduleCostsBarChart summaries={data.scheduleSummaries} />
        <div className="dashboard-chart-row-full">
          <EstimateBudgetBarChart
            summaries={data.costEstimateSummaries}
            limit={showAllEstimates ? undefined : 5}
            onShowAll={showAllEstimates ? undefined : onShowAllEstimates}
          />
        </div>
        <CostSourceTypeChart costs={data.allCosts ?? []} />
        <TopContractorsChart costs={data.allCosts ?? []} limit={5} />
        <div className="dashboard-chart-row-full">
          <CostTimeSeriesChart costs={data.allCosts ?? []} />
        </div>
        <div className="dashboard-chart-row-full">
          <EstimateDeviationChart summaries={data.costEstimateSummaries} />
        </div>
      </div>
    </Box>
  );
}

export default FinanceSection;
