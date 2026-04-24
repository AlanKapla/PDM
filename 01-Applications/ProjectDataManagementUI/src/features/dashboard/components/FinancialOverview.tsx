import React, { useState } from 'react';
import type { ProjectFinancialSummaryWeb } from '../types/projectDashboard.types';
import { PLN, PROG } from '../utils/formatters';
import { COLOR_PALETTE } from '../utils/colors';
import { KpiCard } from './shared/KpiCard';
import { MiniProgressBar } from './shared/MiniProgressBar';
import { FinancialStatusBadge } from './shared/FinancialStatusBadge';
import { BudgetReserveModal } from './BudgetReserveModal';
import { DEVIATION_COLOR } from '../utils/formatters';

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

  const deviationColor = DEVIATION_COLOR(data.deviationNet, data.isBudgetExceeded);

  return (
    <div
      style={{
        background: '#fff',
        border: `0.5px solid ${COLOR_PALETTE.border}`,
        borderRadius: 12,
        padding: 16,
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
        <span style={{ fontSize: 13, fontWeight: 500 }}>Finanse projektu</span>
        <FinancialStatusBadge status={data.financialStatus} small />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 12 }}>
        <KpiCard label="Budżet łączny" value={PLN(data.totalBudgetNet)} small />
        <KpiCard label="Koszty łączne" value={PLN(data.totalCostsNet)} small />
        <KpiCard
          label="Pozostało do wydania"
          value={PLN(data.deviationNet)}
          accent={deviationColor}
          small
        />
        <KpiCard label="Koszty główne" value={PLN(data.additionalCostsNet)} accent={COLOR_PALETTE.amber400} small />
      </div>

      <MiniProgressBar
        percent={data.coveredPercent}
        color={COLOR_PALETTE.teal400}
        exceeded={data.isBudgetExceeded}
        height={8}
      />
      <div style={{ fontSize: 12, color: COLOR_PALETTE.gray400, marginTop: 3, marginBottom: 12 }}>
        {PROG(data.coveredPercent)} pokrycia budżetu
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 12 }}>
        <div style={{ background: COLOR_PALETTE.teal50, borderRadius: 8, padding: '10px 12px' }}>
          <div style={{ fontSize: 12, color: COLOR_PALETTE.gray400, marginBottom: 2 }}>Budżet kosztorysów</div>
          <div style={{ fontSize: 15, fontWeight: 500, color: COLOR_PALETTE.teal600 }}>
            {PLN(data.estimateBudgetNet)}
          </div>
        </div>
        <div style={{ background: COLOR_PALETTE.purple50, borderRadius: 8, padding: '10px 12px' }}>
          <div style={{ fontSize: 12, color: COLOR_PALETTE.gray400, marginBottom: 2 }}>Budżet główny</div>
          <div style={{ fontSize: 15, fontWeight: 500, color: COLOR_PALETTE.purple600 }}>
            {PLN(data.projectReserveBudgetNet)}
          </div>
        </div>
      </div>

      <button
        onClick={() => setShowBudgetModal(true)}
        style={{
          fontSize: 12,
          padding: '6px 12px',
          background: COLOR_PALETTE.purple50,
          color: COLOR_PALETTE.purple600,
          border: `0.5px solid ${COLOR_PALETTE.purple100}`,
          borderRadius: 6,
          cursor: 'pointer',
          marginBottom: 8,
        }}
      >
        Edytuj budżet główny
      </button>

      <div style={{ fontSize: 11, color: COLOR_PALETTE.gray400, marginTop: 4 }}>
        Kosztorysów: {data.costEstimatesCount}
        {data.costEstimatesOverBudgetCount > 0 && (
          <span style={{ color: COLOR_PALETTE.red400, marginLeft: 6 }}>
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
