import React from 'react';
import { useToken } from '@chakra-ui/react';
import type { CostEstimateSummaryWeb } from '../../types/projectDashboard.types';
import { PROG } from '../../utils/formatters';
import { KpiCard } from '../shared/KpiCard';
import { EstimateBlock } from './EstimateBlock';

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
  const [orange50, orange600, orange800, neutral400] = useToken('colors', [
    'orange.50', 'orange.600', 'orange.800', 'neutral.400',
  ]);

  const totalBudgetNet = summaries.reduce((sum, s) => sum + (s.budgetNet ?? 0), 0);
  const totalBudgetGross = summaries.reduce((sum, s) => sum + (s.budgetGross ?? 0), 0);
  const totalCostsNet = summaries.reduce((sum, s) => sum + (s.costsNet ?? 0), 0);
  const totalCostsGross = summaries.reduce((sum, s) => sum + (s.costsGross ?? 0), 0);
  const coverage = totalBudgetNet > 0 ? (totalCostsNet / totalBudgetNet) * 100 : null;
  const totalItems = summaries.reduce((sum, s) => sum + (s.totalItemsCount ?? 0), 0);
  const totalWithoutCosts = summaries.reduce((sum, s) => sum + (s.itemsWithoutCostsCount ?? 0), 0);
  const totalOverBudget = summaries.reduce((sum, s) => sum + (s.itemsOverBudgetCount ?? 0), 0);

  return (
    <div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))',
          gap: 8,
          marginBottom: 16,
        }}
      >
        <KpiCard label="Budżet łączny" netValue={totalBudgetNet} grossValue={totalBudgetGross} />
        <KpiCard label="Koszty łączne" netValue={totalCostsNet} grossValue={totalCostsGross} />
        <KpiCard label="Pokrycie budżetu" value={PROG(coverage)} />
        <KpiCard label="Kosztorysów" value={String(summaries.length)} />
        <KpiCard label="Pozycji łącznie" value={String(totalItems)} />
        <KpiCard
          label="Bez kosztów"
          value={String(totalWithoutCosts)}
          accent={totalWithoutCosts > 0 ? orange800 : undefined}
        />
        <KpiCard
          label="Przekroczonych"
          value={String(totalOverBudget)}
          accent={totalOverBudget > 0 ? orange800 : undefined}
        />
      </div>

      {totalWithoutCosts > 0 && (
        <div
          style={{
            background: orange50,
            border: `0.5px solid ${orange600}`,
            borderRadius: 8,
            padding: '10px 14px',
            marginBottom: 12,
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            fontSize: "xs",
            color: orange800,
          }}
        >
          <span>⚠</span>
          <span>
            {totalWithoutCosts} pozycji kosztorysu nie ma przypisanych kosztów — budżet niezweryfikowany.
          </span>
        </div>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
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
          <div style={{ fontSize: "sm", color: neutral400, fontStyle: 'italic', padding: 12 }}>
            Brak powiązanych kosztorysów
          </div>
        )}
      </div>
    </div>
  );
}

export default EstimatesTab;
