import React from 'react';
import { Box, SimpleGrid, Text } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../../types/projectDashboard.types';
import { PROG } from '../../utils/formatters';
import { KpiCard } from '../shared/KpiCard';
import { FinanceSection } from '../FinanceSection';
import { EstimateBlock } from './EstimateBlock';

export interface FinanceTabProps {
  data: ProjectDashboardWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

export function FinanceTab({
  data,
  tenantId,
  projectId,
  onRefetch,
}: FinanceTabProps): React.ReactElement {
  const summaries = data.costEstimateSummaries;
  const totalBudgetNet = summaries.reduce((sum, s) => sum + (s.budgetNet ?? 0), 0);
  const totalCostsNet = summaries.reduce((sum, s) => sum + (s.costsNet ?? 0), 0);
  const coverage = totalBudgetNet > 0 ? (totalCostsNet / totalBudgetNet) * 100 : null;
  const totalWithoutCosts = summaries.reduce((sum, s) => sum + (s.itemsWithoutCostsCount ?? 0), 0);
  const totalOverBudget = summaries.reduce((sum, s) => sum + (s.itemsOverBudgetCount ?? 0), 0);

  return (
    <Box w="100%">
      <SimpleGrid columns={{ base: 2, md: 3, lg: 6 }} spacing={3} mb={6}>
        <KpiCard
          label="Budżet łączny"
          netValue={data.financialSummary.totalBudgetNet}
          grossValue={data.financialSummary.totalBudgetGross}
          colorScheme="primary"
        />
        <KpiCard
          label="Koszty łączne"
          netValue={data.financialSummary.totalCostsNet}
          grossValue={data.financialSummary.totalCostsGross}
          colorScheme="orange"
        />
        <KpiCard
          label="Pokrycie budżetu"
          value={PROG(data.financialSummary.coveredPercent)}
          colorScheme="level1"
        />
        <KpiCard label="Kosztorysów" value={String(summaries.length)} colorScheme="level2" />
        <KpiCard label="Pokrycie kosztorysów" value={PROG(coverage)} colorScheme="primary" />
        <KpiCard
          label="Przekroczonych"
          value={String(totalOverBudget)}
          colorScheme={totalOverBudget > 0 ? 'red' : 'gray'}
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
          mb={4}
          fontSize="xs"
          color="orange.800"
        >
          ⚠ {totalWithoutCosts} pozycji kosztorysu nie ma przypisanych kosztów — budżet niezweryfikowany.
        </Box>
      )}

      <FinanceSection data={data} showAllEstimates />

      <Box mt={6}>
        <Text fontSize="md" fontWeight="semibold" color="neutral.800" mb={3}>
          Kosztorysy
        </Text>
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
    </Box>
  );
}

export default FinanceTab;
