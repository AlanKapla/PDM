import React, { useState } from 'react';
import { useToken } from '@chakra-ui/react';
import type { ProjectAdditionalCostsWeb, ProjectFinancialSummaryWeb, TrackedCostWeb } from '../../types/projectDashboard.types';
import { PLN, PROG } from '../../utils/formatters';
import { KpiCard } from '../shared/KpiCard';
import { MiniProgressBar } from '../shared/MiniProgressBar';
import { CostTable } from '../shared/CostTable';
import { CostModal } from '../CostModal';
import AppModal from '../../../../components/ui/AppModal';
import { useDashboardCurrency } from '../../context/DashboardCurrencyContext';

export interface AdditionalCostsTabProps {
  data: ProjectAdditionalCostsWeb;
  financialSummary: ProjectFinancialSummaryWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Zakładka kosztów dodatkowych (niespiętych z kosztorysem).
 * Źródło danych: ProjectAdditionalCostsWeb + ProjectFinancialSummaryWeb.
 */
export function AdditionalCostsTab({
  data,
  financialSummary,
  tenantId,
  projectId,
  onRefetch,
}: AdditionalCostsTabProps): React.ReactElement {
  const [createModal, setCreateModal] = useState(false);
  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [confirmDeleteCost, setConfirmDeleteCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const currencySymbol = useDashboardCurrency();
  const [neutral400, level1700, red600, amber400, action50, neutral200, level1500] = useToken('colors', [
    'neutral.400', 'level1.700', 'red.600', 'amber.400', 'action.50', 'neutral.200', 'level1.500',
  ]);

  const reserveNet = financialSummary.projectReserveBudgetNet;
  const additionalNet = data.totalNet ?? 0;
  const remainingReserve = reserveNet != null ? reserveNet - additionalNet : null;
  const remainingColor =
    remainingReserve == null
      ? neutral400
      : remainingReserve >= 0
      ? level1700
      : red600;

  const coveragePercent =
    reserveNet != null && reserveNet > 0
      ? (additionalNet / reserveNet) * 100
      : null;

  const handleDeleteConfirmed = async () => {
    if (!confirmDeleteCost) return;
    setIsDeleting(true);
    try {
      const { deleteTrackedCost } = await import('../../services/dashboardApi');
      await deleteTrackedCost(tenantId, projectId, confirmDeleteCost.id);
      setConfirmDeleteCost(null);
      onRefetch();
    } catch {
      // błąd API obsługiwany przez backend
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div>
      <div className="dashboard-kpi-3col">
        <KpiCard label="Budżet główny" value={PLN(reserveNet, currencySymbol)} />
        <KpiCard label="Koszty główne" value={PLN(data.totalNet, currencySymbol)} accent={amber400} />
        <KpiCard
          label="Pozostały budżet główny"
          value={PLN(remainingReserve, currencySymbol)}
          accent={remainingColor}
        />
      </div>

      <MiniProgressBar
        percent={coveragePercent}
        color={amber400}
        exceeded={(coveragePercent ?? 0) > 100}
        height={8}
      />
      <div style={{ fontSize: "xs", color: neutral400, marginTop: 3, marginBottom: 16 }}>
        {PROG(coveragePercent)} wykorzystania budżetu głównego
      </div>

      <div
        style={{
          background: '#fff',
          border: `0.5px solid ${neutral200}`,
          borderRadius: 12,
          padding: 16,
          marginBottom: 12,
        }}
      >
        <div className="dashboard-table-wrap">
          <CostTable
            costs={data.costs}
            onEdit={(cost) => setEditingCost(cost)}
            onDelete={(cost) => setConfirmDeleteCost(cost)}
          />
        </div>
      </div>

      <button
        onClick={() => setCreateModal(true)}
        style={{
          fontSize: "xs",
          padding: '7px 14px',
          background: action50,
          color: level1700,
          border: `0.5px solid ${level1500}`,
          borderRadius: 6,
          cursor: 'pointer',
        }}
      >
        + Dodaj koszt główny
      </button>

      {createModal && (
        <CostModal
          type="tracked"
          tenantId={tenantId}
          projectId={projectId}
          mode="create"
          onSuccess={() => { onRefetch(); setCreateModal(false); }}
          onClose={() => setCreateModal(false)}
        />
      )}

      {editingCost && (
        <CostModal
          type="tracked"
          tenantId={tenantId}
          projectId={projectId}
          mode="edit"
          cost={editingCost}
          onSuccess={() => { onRefetch(); setEditingCost(null); }}
          onClose={() => setEditingCost(null)}
        />
      )}

      {confirmDeleteCost && (
        <AppModal
          isOpen
          onClose={() => setConfirmDeleteCost(null)}
          title="Usuń koszt"
          actionLabel="Usuń"
          actionColorScheme="red"
          onAction={handleDeleteConfirmed}
          isActionLoading={isDeleting}
          desktopSize="sm"
        >
          <span style={{ fontSize: "sm" }}>
            Czy na pewno chcesz usunąć <strong>{confirmDeleteCost.name}</strong>? Operacji nie można cofnąć.
          </span>
        </AppModal>
      )}
    </div>
  );
}

export default AdditionalCostsTab;
