import React, { useState } from 'react';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { PLN } from '../../utils/formatters';
import { COLOR_PALETTE } from '../../utils/colors';
import { KpiCard } from '../shared/KpiCard';
import { TrackedCostModal } from '../TrackedCostModal';
import AppModal from '../../../../components/ui/AppModal';

export interface AllCostsTabProps {
  costs: TrackedCostWeb[];
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Zakładka wszystkich kosztów projektu (spłaszczona lista z AllCosts).
 */
export function AllCostsTab({
  costs,
  tenantId,
  projectId,
  onRefetch,
}: AllCostsTabProps): React.ReactElement {
  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [confirmDeleteCost, setConfirmDeleteCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const totalNet = costs.reduce((sum, c) => sum + (c.net ?? 0), 0);
  const totalGross = costs.reduce((sum, c) => sum + (c.gross ?? 0), 0);
  const countSchedule = costs.filter(
    (c) => c.sourceType === 'ScheduleWorkItem' || c.sourceType === 'LinkedWorkItem'
  ).length;
  const countEstimate = costs.filter(
    (c) => c.sourceType === 'EstimateItem' || c.sourceType === 'LinkedWorkItem'
  ).length;
  const countAdditional = costs.filter((c) => c.sourceType === 'ProjectAdditional' || !c.sourceType).length;

  const handleDeleteConfirmed = async () => {
    if (!confirmDeleteCost) return;
    setIsDeleting(true);
    try {
      const { deleteTrackedCost } = await import('../../services/dashboardApi');
      await deleteTrackedCost(tenantId, projectId, confirmDeleteCost.id);
      setConfirmDeleteCost(null);
      onRefetch();
    } catch {
      // błąd obsługiwany przez backend
    } finally {
      setIsDeleting(false);
    }
  };

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
        <KpiCard label="Łączne koszty" value={PLN(totalNet)} />
        <KpiCard label="Liczba pozycji" value={String(costs.length)} />
        <KpiCard label="Z harmonogramu" value={String(countSchedule)} />
        <KpiCard label="Z kosztorysu" value={String(countEstimate)} />
        <KpiCard label="Koszty główne" value={String(countAdditional)} />
      </div>

      <div
        style={{
          background: '#fff',
          border: `0.5px solid ${COLOR_PALETTE.border}`,
          borderRadius: 12,
          padding: 16,
        }}
      >
        <div className="dashboard-table-wrap">
        {costs.length === 0 ? (
          <div style={{ fontSize: 11, color: COLOR_PALETTE.gray400, fontStyle: 'italic' }}>
            Brak kosztów
          </div>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 11 }}>
            <thead>
              <tr>
                {['Nazwa / Nr', 'Źródło', 'Etap / Zakres', 'Wykonawca', 'Netto', 'Brutto', ''].map((col) => (
                  <th
                    key={col}
                    style={{
                      textAlign: col === 'Netto' || col === 'Brutto' ? 'right' : 'left',
                      padding: '4px 6px',
                      color: COLOR_PALETTE.gray400,
                      fontWeight: 500,
                      borderBottom: `0.5px solid ${COLOR_PALETTE.border}`,
                    }}
                  >
                    {col}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {costs.map((cost) => (
                <tr key={cost.id}>
                  <td style={{ padding: '4px 6px' }}>
                    <div style={{ fontWeight: 500 }}>{cost.name}</div>
                    {cost.number && (
                      <div style={{ fontSize: 10, color: COLOR_PALETTE.gray400 }}>{cost.number}</div>
                    )}
                  </td>
                  <td style={{ padding: '4px 6px' }}>
                    {(() => {
                      const st = cost.sourceType;
                      if (st === 'ScheduleWorkItem') {
                        return (
                          <span
                            style={{
                              fontSize: 10,
                              background: COLOR_PALETTE.purple50,
                              color: COLOR_PALETTE.purple600,
                              borderRadius: 4,
                              padding: '2px 6px',
                              whiteSpace: 'nowrap',
                            }}
                          >
                            Harmonogram{cost.scheduleName ? `: ${cost.scheduleName}` : ''}
                          </span>
                        );
                      }
                      if (st === 'EstimateItem') {
                        return (
                          <span
                            style={{
                              fontSize: 10,
                              background: COLOR_PALETTE.teal50,
                              color: COLOR_PALETTE.teal600,
                              borderRadius: 4,
                              padding: '2px 6px',
                              whiteSpace: 'nowrap',
                            }}
                          >
                            Kosztorys{cost.estimateName ? `: ${cost.estimateName}` : ''}
                          </span>
                        );
                      }
                      if (st === 'LinkedWorkItem') {
                        return (
                          <div>
                            <span
                              style={{
                                fontSize: 10,
                                background: COLOR_PALETTE.blue50,
                                color: COLOR_PALETTE.blue600,
                                borderRadius: 4,
                                padding: '2px 6px',
                                whiteSpace: 'nowrap',
                              }}
                            >
                              Powiązany
                            </span>
                            {(cost.scheduleName || cost.estimateName) && (
                              <div style={{ fontSize: 10, color: COLOR_PALETTE.gray600, marginTop: 2 }}>
                                {[cost.scheduleName, cost.estimateName].filter(Boolean).join(' + ')}
                              </div>
                            )}
                          </div>
                        );
                      }
                      return (
                        <span
                          style={{
                            fontSize: 10,
                            background: COLOR_PALETTE.gray50,
                            color: COLOR_PALETTE.gray600,
                            borderRadius: 4,
                            padding: '2px 6px',
                            whiteSpace: 'nowrap',
                          }}
                        >
                          Koszty główne
                        </span>
                      );
                    })()}
                  </td>
                  <td style={{ padding: '4px 6px', color: COLOR_PALETTE.gray600 }}>
                    {(() => {
                      const st = cost.sourceType;
                      if (st === 'ScheduleWorkItem' || st === 'LinkedWorkItem') {
                        if (cost.stageName || cost.workItemName) {
                          return `${cost.stageName ?? '—'} / ${cost.workItemName ?? '—'}`;
                        }
                      }
                      if (st === 'EstimateItem' || st === 'LinkedWorkItem') {
                        if (cost.estimateGroupName || cost.estimateItemName) {
                          return `${cost.estimateGroupName ?? '—'} / ${cost.estimateItemName ?? '—'}`;
                        }
                      }
                      return '—';
                    })()}
                  </td>
                  <td style={{ padding: '4px 6px', color: COLOR_PALETTE.gray600 }}>
                    {cost.contractor ?? '—'}
                  </td>
                  <td style={{ padding: '4px 6px', textAlign: 'right', color: COLOR_PALETTE.coral400, fontWeight: 500 }}>
                    {PLN(cost.net)}
                  </td>
                  <td style={{ padding: '4px 6px', textAlign: 'right', color: COLOR_PALETTE.gray600 }}>
                    {PLN(cost.gross)}
                  </td>
                  <td style={{ padding: '4px 6px', whiteSpace: 'nowrap' }}>
                    <button
                      onClick={() => setEditingCost(cost)}
                      style={{
                        fontSize: 11,
                        padding: '4px 10px',
                        background: COLOR_PALETTE.gray50,
                        color: COLOR_PALETTE.gray600,
                        border: `0.5px solid ${COLOR_PALETTE.border}`,
                        borderRadius: 4,
                        cursor: 'pointer',
                        marginRight: 4,
                      }}
                    >
                      Edytuj
                    </button>
                    <button
                      onClick={() => setConfirmDeleteCost(cost)}
                      style={{
                        fontSize: 11,
                        padding: '4px 10px',
                        background: COLOR_PALETTE.red50,
                        color: COLOR_PALETTE.red600,
                        border: `0.5px solid ${COLOR_PALETTE.red400}`,
                        borderRadius: 4,
                        cursor: 'pointer',
                      }}
                    >
                      Usuń
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <td
                  colSpan={7}
                  style={{
                    padding: '6px 6px',
                    borderTop: `0.5px solid ${COLOR_PALETTE.border}`,
                    fontSize: 11,
                    color: COLOR_PALETTE.gray600,
                    textAlign: 'right',
                    fontWeight: 500,
                  }}
                >
                  Suma łączna: <span style={{ color: COLOR_PALETTE.coral400 }}>{PLN(totalNet)}</span>
                </td>
              </tr>
            </tfoot>
          </table>
        )}
        </div>
      </div>

      {editingCost && (
        <TrackedCostModal
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
          <span style={{ fontSize: 13 }}>
            Czy na pewno chcesz usunąć <strong>{confirmDeleteCost.name}</strong>? Operacji nie można cofnąć.
          </span>
        </AppModal>
      )}
    </div>
  );
}

export default AllCostsTab;
