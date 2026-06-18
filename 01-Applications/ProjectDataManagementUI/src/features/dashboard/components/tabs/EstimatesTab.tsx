import React from 'react';
import { Box, SimpleGrid, Text } from '@chakra-ui/react';
import type { CostEstimateSummaryWeb } from '../../types/projectDashboard.types';
import { PROG } from '../../utils/formatters';
import { KpiCard } from '../shared/KpiCard';
import { EstimateBlock } from './EstimateBlock';
import { EstimateBudgetBarChart } from '../charts/EstimateBudgetBarChart';
import { EstimateDeviationChart } from '../charts/EstimateDeviationChart';

export interface EstimatesTabProps {
  summaries: CostEstimateSummaryWeb[];
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Zakładka kosztorysów — lista wszystkich powiązanych kosztorysów projektu.
 * Źródło danych: CostEstimateSummaryWeb[].
 */
export function EstimatesTab({
  summaries,
  tenantId,
  projectId,
  onRefetch,
}: EstimatesTabProps): React.ReactElement {
  const totalBudgetNet = summaries.reduce((sum, s) => sum + (s.budgetNet ?? 0), 0);
  const totalBudgetGross = summaries.reduce((sum, s) => sum + (s.budgetGross ?? 0), 0);
  const totalCostsNet = summaries.reduce((sum, s) => sum + (s.costsNet ?? 0), 0);
  const totalCostsGross = summaries.reduce((sum, s) => sum + (s.costsGross ?? 0), 0);
  const coverage = totalBudgetNet > 0 ? (totalCostsNet / totalBudgetNet) * 100 : null;
  const totalItems = summaries.reduce((sum, s) => sum + (s.totalItemsCount ?? 0), 0);
  const totalWithoutCosts = summaries.reduce((sum, s) => sum + (s.itemsWithoutCostsCount ?? 0), 0);
  const totalOverBudget = summaries.reduce((sum, s) => sum + (s.itemsOverBudgetCount ?? 0), 0);

  return (
    <Box>
      <SimpleGrid columns={{ base: 2, md: 4, lg: 7 }} spacing={2} mb={4}>
        <KpiCard label="Budżet łączny" netValue={totalBudgetNet} grossValue={totalBudgetGross} />
        <KpiCard label="Koszty łączne" netValue={totalCostsNet} grossValue={totalCostsGross} />
        <KpiCard label="Pokrycie budżetu" value={PROG(coverage)} />
        <KpiCard label="Kosztorysów" value={String(summaries.length)} />
        <KpiCard label="Pozycji łącznie" value={String(totalItems)} />
        <KpiCard
          label="Bez kosztów"
          value={String(totalWithoutCosts)}
          accent={totalWithoutCosts > 0 ? 'orange.800' : undefined}
        />
        <KpiCard
          label="Przekroczonych"
          value={String(totalOverBudget)}
          accent={totalOverBudget > 0 ? 'orange.800' : undefined}
        />
      </SimpleGrid>

      {totalWithoutCosts > 0 && (
        <Box
          bg="orange.50"
          border="0.5px solid"
          borderColor="orange.600"
          borderRadius="md"
          px={3}
          py={2}
          mb={3}
          fontSize="xs"
          color="orange.800"
        >
          ⚠ {totalWithoutCosts} pozycji kosztorysu nie ma przypisanych kosztów — budżet niezweryfikowany.
        </Box>
      )}

      {summaries.length > 0 && (
        <Box mb={4} display="flex" flexDirection="column" gap={3}>
          <EstimateBudgetBarChart summaries={summaries} title="Budżet vs koszty — wszystkie kosztorysy" />
          <EstimateDeviationChart summaries={summaries} />
        </Box>
      )}

      <Box display="flex" flexDirection="column" gap={2}>
        {summaries.map((summary) => (
          <EstimateBlock
            key={summary.costEstimateId}
            summary={summary}
            tenantId={tenantId}
            projectId={projectId}
            onRefetch={onRefetch}
          />
        ))}
        {summaries.length === 0 && (
          <Text fontSize="sm" color="neutral.400" fontStyle="italic" p={3}>
            Brak powiązanych kosztorysów
          </Text>
        )}
      </Box>
    </Box>
  );
}

export default EstimatesTab;
