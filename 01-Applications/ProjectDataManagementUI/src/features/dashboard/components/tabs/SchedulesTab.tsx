import React from 'react';
import type { ScheduleSummaryWeb, ProjectFinancialSummaryWeb, ProjectTimelineSummaryWeb } from '../../types/projectDashboard.types';
import { PLN, DAYS } from '../../utils/formatters';
import { COLOR_PALETTE } from '../../utils/colors';
import { KpiCard } from '../shared/KpiCard';
import { ScheduleBlock } from './ScheduleBlock';

export interface SchedulesTabProps {
  summaries: ScheduleSummaryWeb[];
  financialSummary: ProjectFinancialSummaryWeb;
  timelineSummary: ProjectTimelineSummaryWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Zakładka harmonogramów — lista harmonogramów projektu.
 * Źródło danych: ScheduleSummaryWeb[].
 */
export function SchedulesTab({
  summaries,
  financialSummary,
  timelineSummary,
  tenantId,
  projectId,
  onRefetch,
}: SchedulesTabProps): React.ReactElement {
  const scs = financialSummary.scheduleCostSummary;

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
        <KpiCard label="Łączne koszty harmonogramów" value={PLN(scs?.totalSchedulesCostsNet ?? null)} />
        <KpiCard label="Harmonogramów" value={String(summaries.length)} />
        <KpiCard label="Zakresów łącznie" value={String(timelineSummary.totalWorkCount)} />
        <KpiCard
          label="Opóźnione zakresy"
          value={String(timelineSummary.delayedCount)}
          accent={timelineSummary.delayedCount > 0 ? COLOR_PALETTE.coral600 : undefined}
        />
        <KpiCard label="W toku" value={String(timelineSummary.inProgressCount)} />
        <KpiCard label="Nie rozpoczęto" value={String(timelineSummary.notStartedCount)} />
        <KpiCard label="Ukończono" value={String(timelineSummary.completedCount)} accent={COLOR_PALETTE.teal600} />
        <KpiCard
          label="Czas projektu"
          value={timelineSummary.totalPlannedDays != null ? DAYS(timelineSummary.totalPlannedDays) : '—'}
        />
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        {summaries.map((summary) => (
          <ScheduleBlock
            key={summary.workScheduleId}
            summary={summary}
            tenantId={tenantId}
            projectId={projectId}
            onRefetch={onRefetch}
          />
        ))}
        {summaries.length === 0 && (
          <div style={{ fontSize: 13, color: COLOR_PALETTE.gray400, fontStyle: 'italic', padding: 12 }}>
            Brak powiązanych harmonogramów
          </div>
        )}
      </div>
    </div>
  );
}

export default SchedulesTab;
