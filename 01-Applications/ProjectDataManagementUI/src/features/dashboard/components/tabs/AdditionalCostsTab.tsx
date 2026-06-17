import React, { useState } from 'react';
import { useToken } from '@chakra-ui/react';
import { Sparkles } from 'lucide-react';
import type { ProjectAdditionalCostsWeb, ProjectFinancialSummaryWeb, TrackedCostWeb } from '../../types/projectDashboard.types';
import type { ParsedCostDto } from '../../../../types/ai.types';
import { PROG } from '../../utils/formatters';
import { KpiCard } from '../shared/KpiCard';
import { MiniProgressBar } from '../shared/MiniProgressBar';
import { CostTable } from '../shared/CostTable';
import { CostModal } from '../CostModal';
import AppModal from '../../../../components/ui/AppModal';
import { AICostImportModal } from '../../../../components/CostTracker/AICostImportModal';

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
  const [aiImportModal, setAiImportModal] = useState(false);
  const [aiPrefillData, setAiPrefillData] = useState<{ parsedData: ParsedCostDto; file: File } | null>(null);
  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [confirmDeleteCost, setConfirmDeleteCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [neutral400, level1700, red600, amber400, action50, neutral200, level1500] = useToken('colors', [
    'neutral.400', 'level1.700', 'red.600', 'amber.400', 'action.50', 'neutral.200', 'level1.500',
  ]);

  const reserveNet = financialSummary.projectReserveBudgetNet;
  const reserveGross = financialSummary.projectReserveBudgetGross;
  const additionalNet = data.totalNet ?? 0;
  const additionalGross = data.totalGross ?? 0;
  const remainingReserveNet = reserveNet != null ? reserveNet - additionalNet : null;
  const remainingReserveGross = reserveGross != null ? reserveGross - additionalGross : null;
  const remainingColor =
    remainingReserveNet == null
      ? neutral400
      : remainingReserveNet >= 0
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
        <KpiCard label="Budżet główny" netValue={reserveNet} grossValue={reserveGross} />
        <KpiCard label="Koszty główne" netValue={data.totalNet} grossValue={data.totalGross} accent={amber400} />
        <KpiCard
          label="Pozostały budżet główny"
          netValue={remainingReserveNet}
          grossValue={remainingReserveGross}
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
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <span style={{ fontSize: '0.875rem', fontWeight: 500 }}>Lista kosztów</span>
          <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
            <button
              onClick={() => setAiImportModal(true)}
              style={{
                fontSize: '0.75rem',
                padding: '6px 12px',
                background: 'linear-gradient(135deg, #7c3aed 0%, #a855f7 100%)',
                color: '#fff',
                border: 'none',
                borderRadius: 6,
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: 4,
                fontWeight: 500,
                boxShadow: '0 1px 4px rgba(124, 58, 237, 0.35)',
              }}
            >
              <Sparkles size={12} />
              Importuj z AI
            </button>
            <button
              onClick={() => setCreateModal(true)}
              style={{
                fontSize: '0.75rem',
                padding: '6px 12px',
                background: action50,
                color: level1700,
                border: `0.5px solid ${level1500}`,
                borderRadius: 6,
                cursor: 'pointer',
              }}
            >
              + Dodaj koszt główny
            </button>
          </div>
        </div>
        <div className="dashboard-table-wrap">
          <CostTable
            costs={data.costs}
            onEdit={(cost) => setEditingCost(cost)}
            onDelete={(cost) => setConfirmDeleteCost(cost)}
          />
        </div>
      </div>

      {aiImportModal && (
        <AICostImportModal
          isOpen
          onClose={() => setAiImportModal(false)}
          tenantId={tenantId}
          projectId={projectId}
          costType="TrackedCost"
          onParsed={(data, file) => {
            setAiPrefillData({ parsedData: data, file });
            setAiImportModal(false);
            setCreateModal(true);
          }}
        />
      )}

      {createModal && (
        <CostModal
          type="tracked"
          tenantId={tenantId}
          projectId={projectId}
          mode="create"
          aiPrefill={aiPrefillData ?? undefined}
          onSuccess={() => { onRefetch(); setCreateModal(false); setAiPrefillData(null); }}
          onClose={() => { setCreateModal(false); setAiPrefillData(null); }}
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
