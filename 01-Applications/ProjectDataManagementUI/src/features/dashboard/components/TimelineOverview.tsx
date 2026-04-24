import React from 'react';
import { TimelineStatus } from '../types/projectDashboard.types';
import type { ProjectTimelineSummaryWeb } from '../types/projectDashboard.types';
import { PROG, DAYS } from '../utils/formatters';
import { COLOR_PALETTE, TIMELINE_STATUS_MAP } from '../utils/colors';
import { KpiCard } from './shared/KpiCard';
import { MiniProgressBar } from './shared/MiniProgressBar';
import { TimelineStatusBadge } from './shared/TimelineStatusBadge';
import { Badge } from './shared/Badge';

export interface TimelineOverviewProps {
  data: ProjectTimelineSummaryWeb;
}

/**
 * Panel postępu czasowego projektu.
 * Źródło danych: ProjectTimelineSummaryWeb.
 */
export function TimelineOverview({ data }: TimelineOverviewProps): React.ReactElement {
  const progressColor = (() => {
    switch (data.overallStatus) {
      case TimelineStatus.Completed:
      case TimelineStatus.CompletedLate:
        return COLOR_PALETTE.teal400;
      case TimelineStatus.Delayed:
        return COLOR_PALETTE.coral400;
      default:
        return COLOR_PALETTE.blue400;
    }
  })();

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
        <span style={{ fontSize: 13, fontWeight: 500 }}>Postęp projektu</span>
        <TimelineStatusBadge status={data.overallStatus} small />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 12 }}>
        <KpiCard
          label="Postęp ogólny"
          value={PROG(data.progressPercent)}
          small
        />
        <KpiCard
          label="Opóźnione"
          value={String(data.delayedCount)}
          accent={data.delayedCount > 0 ? COLOR_PALETTE.coral400 : undefined}
          small
        />
        <KpiCard
          label="W toku"
          value={String(data.inProgressCount)}
          accent={COLOR_PALETTE.blue600}
          small
        />
        <KpiCard
          label="Czas projektu"
          value={DAYS(data.totalPlannedDays ?? null)}
          small
        />
      </div>

      <MiniProgressBar
        percent={data.progressPercent}
        color={progressColor}
        height={8}
      />
      <div style={{ fontSize: 12, color: COLOR_PALETTE.gray400, marginTop: 3, marginBottom: 12 }}>
        {PROG(data.progressPercent)} ukończenia
      </div>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 5, marginBottom: 12 }}>
        {(
          [
            { label: `Ukończone ${data.completedCount}`, status: TimelineStatus.Completed },
            { label: `W toku ${data.inProgressCount}`, status: TimelineStatus.InProgress },
            { label: `Opóźnione ${data.delayedCount}`, status: TimelineStatus.Delayed },
            { label: `Nie rozpoczęto ${data.notStartedCount}`, status: TimelineStatus.NotStarted },
          ] as const
        ).map(({ label, status }) => {
          const cfg = TIMELINE_STATUS_MAP[status];
          return <Badge key={label} text={label} bg={cfg.bg} color={cfg.color} small />;
        })}
      </div>

      <div style={{ fontSize: 11, color: COLOR_PALETTE.gray400 }}>
        Harmonogramów: {data.workSchedulesCount}
        {data.activeSchedulesCount > 0 && (
          <span style={{ marginLeft: 6 }}>(aktywnych: {data.activeSchedulesCount})</span>
        )}
      </div>
    </div>
  );
}

export default TimelineOverview;
