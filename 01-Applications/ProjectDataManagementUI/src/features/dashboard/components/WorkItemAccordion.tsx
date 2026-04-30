import React, { useState } from 'react';
import { useToken } from '@chakra-ui/react';
import { TimelineStatus } from '../types/projectDashboard.types';
import type { WorkItemLinkWeb, TrackedCostWeb } from '../types/projectDashboard.types';
import { PLN, DATE, DAYS, PROG } from '../utils/formatters';
import { Accordion } from './shared/Accordion';
import { MiniProgressBar } from './shared/MiniProgressBar';
import { FinancialStatusBadge } from './shared/FinancialStatusBadge';
import { TimelineStatusBadge } from './shared/TimelineStatusBadge';
import { CostTable } from './shared/CostTable';
import { Badge } from './shared/Badge';
import { TrackedCostModal } from './TrackedCostModal';
import { DEVIATION_COLOR } from '../utils/formatters';

export interface WorkItemAccordionProps {
  item: WorkItemLinkWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
  showCosts?: boolean;
  /** schedule = row z mini-tagami kosztów; estimate = 6-kolumnowy wiersz */
  displayMode?: 'schedule' | 'estimate';
}

/**
 * Accordion dla pojedynczej pozycji kosztorysu/harmonogramu.
 * displayMode='schedule' — widok harmonogramu (mini-tagi, costsNet).
 * displayMode='estimate' — widok kosztorysu (6 kolumn: nazwa, czas, budżet, koszty, odchylenie, status).
 */
export function WorkItemAccordion({
  item,
  tenantId,
  projectId,
  onRefetch,
  showCosts = true,
  displayMode = 'schedule',
}: WorkItemAccordionProps): React.ReactElement {
  const [createModal, setCreateModal] = useState(false);
  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [
    neutral400, neutral50, neutral600, neutral200,
    orange600, primary500, level1500,
    action50, action600, level250, level2600,
  ] = useToken('colors', [
    'neutral.400', 'neutral.50', 'neutral.600', 'neutral.200',
    'orange.600', 'primary.500', 'level1.500',
    'action.50', 'action.600', 'level2.50', 'level2.600',
  ]);

  /* ── SCHEDULE HEADER ── */
  const scheduleHeader = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 6, flex: 1, flexWrap: 'wrap', minWidth: 0 }}>
      <span style={{ fontSize: "xs", fontWeight: "medium", flex: 1, minWidth: 0 }}>{item.displayName}</span>
      <TimelineStatusBadge status={item.timelineStatus} small />
      {item.timeline && (
        <span style={{ fontSize: "xs", color: neutral400, whiteSpace: 'nowrap' }}>
          {DATE(item.timeline.plannedStart)} – {DATE(item.timeline.plannedEnd)}
          {item.timeline.totalPlannedDays != null && ` · ${DAYS(item.timeline.totalPlannedDays)}`}
        </span>
      )}
      {(item.costs ?? []).length > 0 && (
        <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
          {(item.costs ?? []).map((cost) => (
            <span
              key={cost.id}
              style={{
                fontSize: "xs",
                background: neutral50,
                color: neutral600,
                borderRadius: 4,
                padding: '2px 6px',
                border: `0.5px solid ${neutral200}`,
              }}
            >
              {[cost.number, cost.contractor].filter(Boolean).join(' · ')}
            </span>
          ))}
        </div>
      )}
      <span
        style={{
          fontSize: "xs",
          fontWeight: "medium",
          color: item.costsNet != null ? orange600 : neutral400,
          whiteSpace: 'nowrap',
        }}
      >
        {item.costsNet != null ? PLN(item.costsNet) : '—'}
      </span>
      <div style={{ width: 60, flexShrink: 0 }}>
        <MiniProgressBar
          percent={item.timeline?.progressPercent ?? null}
          color={
            item.timeline?.overallStatus === TimelineStatus.Delayed
              ? orange600
              : primary500
          }
          height={4}
        />
      </div>
    </div>
  );

  /* ── ESTIMATE HEADER (6-column grid) ── */
  const coverPercent = item.coveredPercent;
  const barColor = item.isBudgetExceeded
    ? orange600
    : coverPercent != null && coverPercent < 50
    ? primary500
    : level1500;
  const deviationColor = DEVIATION_COLOR(item.deviationNet, item.isBudgetExceeded);

  const estimateHeader = (
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: '3fr 1.5fr 1fr 1fr 1fr 1fr',
        gap: 8,
        flex: 1,
        alignItems: 'center',
        fontSize: "xs",
        minWidth: 0,
      }}
    >
      {/* Kolumna 1: nazwa + pasek pokrycia */}
      <div>
        <div style={{ fontWeight: "medium", marginBottom: 3, fontSize: "xs" }}>{item.displayName}</div>
        <MiniProgressBar
          percent={coverPercent}
          color={barColor}
          exceeded={item.isBudgetExceeded}
          height={4}
        />
        <div style={{ display: 'flex', gap: 4, marginTop: 3, alignItems: 'center', flexWrap: 'wrap' }}>
          <span style={{ color: neutral400 }}>{PROG(coverPercent)} pokrycia</span>
          {item.hasLinkedSchedule && (
            <Badge text="Powiązany" bg={level250} color={level2600} small />
          )}
        </div>
      </div>
      {/* Kolumna 2: czas */}
      <div style={{ color: neutral600, lineHeight: '1.5' }}>
        {item.timeline ? (
          <>
            <div>{DATE(item.timeline.plannedStart)}</div>
            <div>{DATE(item.timeline.plannedEnd)}</div>
            <div style={{ color: neutral400 }}>{DAYS(item.timeline.totalPlannedDays)}</div>
          </>
        ) : (
          <span style={{ color: neutral400 }}>Brak</span>
        )}
      </div>
      {/* Kolumna 3: budżet */}
      <div style={{ textAlign: 'right', color: neutral600 }}>{PLN(item.budgetNet)}</div>
      {/* Kolumna 4: koszty */}
      <div style={{ textAlign: 'right', fontWeight: "medium" }}>{PLN(item.costsNet)}</div>
      {/* Kolumna 5: odchylenie */}
      <div style={{ textAlign: 'right', color: deviationColor }}>
        {item.deviationNet != null ? PLN(item.deviationNet) : '—'}
      </div>
      {/* Kolumna 6: status */}
      <div>
        <FinancialStatusBadge status={item.financialStatus} small />
      </div>
    </div>
  );

  const header = displayMode === 'estimate' ? estimateHeader : scheduleHeader;

  return (
    <>
      <Accordion header={header}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {showCosts && (
            <>
              <CostTable
                costs={item.costs}
                onEdit={(cost) => setEditingCost(cost)}
                onDelete={(cost) => setConfirmDeleteId(cost.id)}
              />

              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 6 }}>
                <button
                  onClick={() => setCreateModal(true)}
                  style={{
                    fontSize: "xs",
                    padding: '5px 10px',
                    background: action50,
                    color: action600,
                    border: `0.5px solid ${level1500}`,
                    borderRadius: 6,
                    cursor: 'pointer',
                  }}
                >
                  + Dodaj koszt
                </button>
              </div>
            </>
          )}
        </div>
      </Accordion>

      {createModal && (
        <TrackedCostModal
          tenantId={tenantId}
          projectId={projectId}
          mode="create"
          workItemType={item.workItemType}
          workItemLinkId={item.workItemLinkId}
          costEstimateItemId={item.costEstimateItemId}
          workScheduleStageWorkId={item.workScheduleStageWorkId}
          onSuccess={() => onRefetch()}
          onClose={() => setCreateModal(false)}
        />
      )}

      {editingCost && (
        <TrackedCostModal
          tenantId={tenantId}
          projectId={projectId}
          mode="edit"
          cost={editingCost}
          onSuccess={() => onRefetch()}
          onClose={() => setEditingCost(null)}
        />
      )}

      {confirmDeleteId && (
        <DeleteConfirmOverlay
          tenantId={tenantId}
          projectId={projectId}
          costId={confirmDeleteId}
          onConfirmed={() => {
            setConfirmDeleteId(null);
            onRefetch();
          }}
          onCancel={() => setConfirmDeleteId(null)}
        />
      )}
    </>
  );
}

// Inline komponent potwierdzenia usunięcia
interface DeleteConfirmOverlayProps {
  tenantId: string;
  projectId: string;
  costId: string;
  onConfirmed: () => void;
  onCancel: () => void;
}

function DeleteConfirmOverlay({
  tenantId,
  projectId,
  costId,
  onConfirmed,
  onCancel,
}: DeleteConfirmOverlayProps): React.ReactElement {
  const [isDeleting, setIsDeleting] = useState(false);
  const [neutral200, neutral600, red600] = useToken('colors', ['neutral.200', 'neutral.600', 'red.600']);

  const handleDelete = async () => {
    setIsDeleting(true);
    try {
      const { deleteTrackedCost } = await import('../services/dashboardApi');
      await deleteTrackedCost(tenantId, projectId, costId);
      onConfirmed();
    } catch {
      setIsDeleting(false);
    }
  };

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0,0,0,0.35)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1000,
      }}
    >
      <div
        style={{
          background: '#fff',
          borderRadius: 12,
          padding: 24,
          width: 320,
          border: `0.5px solid ${neutral200}`,
        }}
      >
        <div style={{ fontSize: "sm", fontWeight: "medium", marginBottom: 12 }}>Usuń koszt</div>
        <div style={{ fontSize: "xs", color: neutral600, marginBottom: 16 }}>
          Czy na pewno chcesz usunąć ten koszt? Operacji nie można cofnąć.
        </div>
        <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
          <button
            onClick={onCancel}
            style={{
              padding: '7px 14px',
              border: `0.5px solid ${neutral200}`,
              borderRadius: 6,
              background: '#fff',
              cursor: 'pointer',
              fontSize: "xs",
            }}
          >
            Anuluj
          </button>
          <button
            onClick={handleDelete}
            disabled={isDeleting}
            style={{
              padding: '7px 14px',
              background: red600,
              color: '#fff',
              border: 'none',
              borderRadius: 6,
              cursor: isDeleting ? 'not-allowed' : 'pointer',
              fontSize: "xs",
              opacity: isDeleting ? 0.7 : 1,
            }}
          >
            Usuń
          </button>
        </div>
      </div>
    </div>
  );
}

export default WorkItemAccordion;
