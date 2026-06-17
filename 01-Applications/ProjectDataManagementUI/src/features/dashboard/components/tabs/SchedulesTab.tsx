import React from 'react';
import { useToken } from '@chakra-ui/react';
import type { ScheduleSummaryWeb, ProjectFinancialSummaryWeb, ProjectTimelineSummaryWeb } from '../../types/projectDashboard.types';
import { DAYS } from '../../utils/formatters';
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
  const [orange800, level1700, neutral400] = useToken('colors', [
    'orange.800', 'level1.700', 'neutral.400',
  ]);

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
        <KpiCard
          label="Łączne koszty harmonogramów"
          netValue={scs?.totalSchedulesCostsNet ?? null}
          grossValue={scs?.totalSchedulesCostsGross ?? null}
        />
        <KpiCard label="Harmonogramów" value={String(summaries.length)} />
        <KpiCard label="Zakresów łącznie" value={String(timelineSummary.totalWorkCount)} />
        <KpiCard
          label="Opóźnione zakresy"
          value={String(timelineSummary.delayedCount)}
          accent={timelineSummary.delayedCount > 0 ? orange800 : undefined}
        />
        <KpiCard label="W toku" value={String(timelineSummary.inProgressCount)} />
        <KpiCard label="Nie rozpoczęto" value={String(timelineSummary.notStartedCount)} />
        <KpiCard label="Ukończono" value={String(timelineSummary.completedCount)} accent={level1700} />
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
          <div style={{ fontSize: "sm", color: neutral400, fontStyle: 'italic', padding: 12 }}>
            Brak powiązanych harmonogramów
          </div>
        )}
      </div>
    </div>
  );
}

export default SchedulesTab;
