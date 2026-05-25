import React, { useState } from 'react';
import { useToken } from '@chakra-ui/react';
import type { ProjectFinancialSummaryWeb } from '../types/projectDashboard.types';
import { PLN, PROG } from '../utils/formatters';
import { KpiCard } from './shared/KpiCard';
import { MiniProgressBar } from './shared/MiniProgressBar';
import { FinancialStatusBadge } from './shared/FinancialStatusBadge';
import { BudgetReserveModal } from './BudgetReserveModal';
import { DEVIATION_COLOR } from '../utils/formatters';
import { useDashboardCurrency } from '../context/DashboardCurrencyContext';

export interface FinancialOverviewProps {
  data: ProjectFinancialSummaryWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Panel finansowy projektu — budżety, koszty, odchylenia.
 * Źródło danych: ProjectFinancialSummaryWeb.
 */
export function FinancialOverview({
  data,
  tenantId,
  projectId,
  onRefetch,
}: FinancialOverviewProps): React.ReactElement {
  const [showBudgetModal, setShowBudgetModal] = useState(false);
  const currencySymbol = useDashboardCurrency();
  const [
    neutral200, amber400, level1500, neutral400,
    action50, action600, level250, level2600, level2100, red400,
  ] = useToken('colors', [
    'neutral.200', 'amber.400', 'level1.500', 'neutral.400',
    'action.50', 'action.600', 'level2.50', 'level2.600', 'level2.100', 'red.400',
  ]);

  const deviationColor = DEVIATION_COLOR(data.deviationNet, data.isBudgetExceeded);

  return (
    <div
      style={{
        background: '#fff',
        border: `0.5px solid ${neutral200}`,
        borderRadius: 12,
        padding: 16,
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
        <span style={{ fontSize: '0.875rem', fontWeight: 500 }}>Finanse projektu</span>
        <FinancialStatusBadge status={data.financialStatus} small />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 12 }}>
        <KpiCard label="Budżet łączny" value={PLN(data.totalBudgetNet, currencySymbol)} small />
        <KpiCard label="Koszty łączne" value={PLN(data.totalCostsNet, currencySymbol)} small />
        <KpiCard
          label="Pozostało do wydania"
          value={PLN(data.deviationNet, currencySymbol)}
          accent={deviationColor}
          small
        />
        <KpiCard label="Koszty główne" value={PLN(data.additionalCostsNet, currencySymbol)} accent={amber400} small />
      </div>

      <MiniProgressBar
        percent={data.coveredPercent}
        color={level1500}
        exceeded={data.isBudgetExceeded}
        height={8}
      />
      <div style={{ fontSize: '0.75rem', color: neutral400, marginTop: 3, marginBottom: 12 }}>
        {PROG(data.coveredPercent)} pokrycia budżetu
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 12 }}>
        <div style={{ background: action50, borderRadius: 8, padding: '10px 12px' }}>
          <div style={{ fontSize: '0.75rem', color: neutral400, marginBottom: 2 }}>Budżet kosztorysów</div>
          <div style={{ fontSize: '1rem', fontWeight: 500, color: action600 }}>
            {PLN(data.estimateBudgetNet, currencySymbol)}
          </div>
        </div>
        <div style={{ background: level250, borderRadius: 8, padding: '10px 12px' }}>
          <div style={{ fontSize: '0.75rem', color: neutral400, marginBottom: 2 }}>Budżet główny</div>
          <div style={{ fontSize: '1rem', fontWeight: 500, color: level2600 }}>
            {PLN(data.projectReserveBudgetNet, currencySymbol)}
          </div>
        </div>
      </div>

      <button
        onClick={() => setShowBudgetModal(true)}
        style={{
          fontSize: '0.75rem',
          padding: '6px 12px',
          background: level250,
          color: level2600,
          border: `0.5px solid ${level2100}`,
          borderRadius: 6,
          cursor: 'pointer',
          marginBottom: 8,
        }}
      >
        Edytuj budżet główny
      </button>

      <div style={{ fontSize: '0.75rem', color: neutral400, marginTop: 4 }}>
        Kosztorysów: {data.costEstimatesCount}
        {data.costEstimatesOverBudgetCount > 0 && (
          <span style={{ color: red400, marginLeft: 6 }}>
            ({data.costEstimatesOverBudgetCount} przekroczone)
          </span>
        )}
      </div>

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
    </div>
  );
}

export default FinancialOverview;
