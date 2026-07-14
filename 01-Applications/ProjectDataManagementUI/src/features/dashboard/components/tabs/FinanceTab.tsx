import React, { useState } from 'react';
import { Box, Text } from '@chakra-ui/react';
import { useNavigate } from 'react-router-dom';
import type { ProjectDashboardWeb, TrackedCostWeb } from '../../types/projectDashboard.types';
import { PROG } from '../../utils/formatters';
import { EstimateProgressList } from '../EstimateProgressList';
import { RecentCostsList } from '../RecentCostsList';
import { CostModal } from '../CostModal';
import { CostCategoryPieChart } from '../charts/CostCategoryPieChart';
import { KpiCard } from '../shared/KpiCard';
import { useProjectPermissions } from '../../../../hooks/useProjectPermissions';

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
  const navigate = useNavigate();
  const { canViewEstimates } = useProjectPermissions(projectId);
  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);

  const costs = data.allCosts ?? [];
  const fs = data.financialSummary;

  const costsFromListNet = costs.reduce((sum, c) => sum + (c.net ?? 0), 0);
  const costsFromListGross = costs.reduce((sum, c) => sum + (c.gross ?? 0), 0);
  const totalCostsNet = fs.totalCostsNet || costsFromListNet || null;
  const totalCostsGross = fs.totalCostsGross || costsFromListGross || null;
  const deviationColorScheme =
    fs.deviationNet != null && fs.deviationNet < 0
      ? 'red'
      : fs.deviationNet != null && fs.deviationNet > 0
        ? 'green'
        : 'gray';

  const handleSelectEstimate = (estimateId: string): void => {
    if (!canViewEstimates) {
      return;
    }
    navigate(`/projects/${projectId}/cost-estimates/${estimateId}`);
  };

  return (
    <Box w="100%">
      <Box className="dashboard-kpi-grid" mb={6}>
        <KpiCard
          label="Budżet łączny"
          netValue={fs.totalBudgetNet}
          grossValue={fs.totalBudgetGross}
          colorScheme="primary"
        />
        <KpiCard
          label="Koszty łączne"
          netValue={totalCostsNet}
          grossValue={totalCostsGross}
          colorScheme="orange"
        />
        <KpiCard
          label="Pozostało do wydania"
          netValue={fs.deviationNet}
          grossValue={fs.deviationGross}
          colorScheme={deviationColorScheme}
        />
        <KpiCard label="Pokrycie budżetu" value={PROG(fs.coveredPercent)} colorScheme="level1" />
      </Box>

      <Box mb={6}>
        <CostCategoryPieChart costByCategory={data.costByCategory ?? []} />
      </Box>

      <Box as="section" aria-label="Kosztorysy" mb={6}>
        <Text fontSize="md" fontWeight="semibold" color="neutral.800" mb={3}>
          Kosztorysy
        </Text>
        <EstimateProgressList
          summaries={data.costEstimateSummaries}
          onSelect={handleSelectEstimate}
          canOpen={canViewEstimates}
        />
      </Box>

      {costs.length > 0 && (
        <Box as="section" aria-label="Ostatnie koszty">
          <Text fontSize="md" fontWeight="semibold" color="neutral.800" mb={3}>
            Ostatnie koszty
          </Text>
          <RecentCostsList costs={costs} onSelect={setEditingCost} />
        </Box>
      )}

      {editingCost && (
        <CostModal
          type="tracked"
          tenantId={tenantId}
          projectId={projectId}
          mode="edit"
          cost={editingCost}
          onSuccess={() => {
            onRefetch();
            setEditingCost(null);
          }}
          onClose={() => setEditingCost(null)}
        />
      )}
    </Box>
  );
}

export default FinanceTab;
