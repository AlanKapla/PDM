import React, { useState } from 'react';
import { Box, Button, Text } from '@chakra-ui/react';
import type { ProjectFinancialSummaryWeb } from '../types/projectDashboard.types';
import { PROG } from '../utils/formatters';
import { KpiCard } from './shared/KpiCard';
import { MiniProgressBar } from './shared/MiniProgressBar';
import { FinancialStatusBadge } from './shared/FinancialStatusBadge';
import { BudgetReserveModal } from './BudgetReserveModal';

export interface FinancialOverviewProps {
  data: ProjectFinancialSummaryWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

export function FinancialOverview({
  data,
  tenantId,
  projectId,
  onRefetch,
}: FinancialOverviewProps): React.ReactElement {
  const [showBudgetModal, setShowBudgetModal] = useState(false);
  const deviationColorScheme =
    data.deviationNet != null && data.deviationNet < 0
      ? 'red'
      : data.deviationNet != null && data.deviationNet > 0
        ? 'green'
        : 'gray';

  return (
    <Box
      bg="white"
      borderWidth="2px"
      borderColor="neutral.200"
      borderRadius="xl"
      p={{ base: 4, md: 5 }}
      w="100%"
    >
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Text fontSize="sm" fontWeight="medium" color="neutral.800">
          Finanse projektu
        </Text>
        <FinancialStatusBadge status={data.financialStatus} small />
      </Box>

      <Box display="grid" gridTemplateColumns="1fr 1fr" gap={3} mb={3}>
        <KpiCard
          label="Budżet łączny"
          netValue={data.totalBudgetNet}
          grossValue={data.totalBudgetGross}
          colorScheme="primary"
          small
        />
        <KpiCard
          label="Koszty łączne"
          netValue={data.totalCostsNet}
          grossValue={data.totalCostsGross}
          colorScheme="orange"
          small
        />
        <KpiCard
          label="Pozostało do wydania"
          netValue={data.deviationNet}
          grossValue={data.deviationGross}
          colorScheme={deviationColorScheme}
          small
        />
        <KpiCard
          label="Koszty główne"
          netValue={data.additionalCostsNet}
          grossValue={data.additionalCostsGross}
          colorScheme="amber"
          small
        />
      </Box>

      <MiniProgressBar
        percent={data.coveredPercent}
        color="level1.500"
        exceeded={data.isBudgetExceeded}
        height={8}
      />
      <Text fontSize="xs" color="neutral.400" mt={1} mb={3}>
        {PROG(data.coveredPercent)} pokrycia budżetu
      </Text>

      <Box display="grid" gridTemplateColumns="1fr 1fr" gap={3} mb={3}>
        <KpiCard
          label="Budżet kosztorysów"
          netValue={data.estimateBudgetNet}
          grossValue={data.estimateBudgetGross}
          colorScheme="level1"
          small
        />
        <KpiCard
          label="Budżet główny"
          netValue={data.projectReserveBudgetNet}
          grossValue={data.projectReserveBudgetGross}
          colorScheme="level2"
          small
        />
      </Box>

      <Button
        size="xs"
        variant="outline"
        borderColor="level2.100"
        color="level2.600"
        mb={2}
        onClick={() => setShowBudgetModal(true)}
      >
        Edytuj budżet główny
      </Button>

      <Text fontSize="xs" color="neutral.400">
        Kosztorysów: {data.costEstimatesCount}
        {data.costEstimatesOverBudgetCount > 0 && (
          <Text as="span" color="red.400" ml={2}>
            ({data.costEstimatesOverBudgetCount} przekroczone)
          </Text>
        )}
      </Text>

      {showBudgetModal && (
        <BudgetReserveModal
          tenantId={tenantId}
          projectId={projectId}
          currentBudgetNet={data.projectReserveBudgetNet}
          currentBudgetGross={data.projectReserveBudgetGross}
          onSuccess={onRefetch}
          onClose={() => setShowBudgetModal(false)}
        />
      )}
    </Box>
  );
}

export default FinancialOverview;
