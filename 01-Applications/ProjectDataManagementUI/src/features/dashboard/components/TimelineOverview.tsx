import React from 'react';
import { useToken } from '@chakra-ui/react';
import { TimelineStatus } from '../types/projectDashboard.types';
import type { ProjectTimelineSummaryWeb } from '../types/projectDashboard.types';
import { PROG, DAYS, TIMELINE_STATUS_MAP } from '../utils/formatters';
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
  const [level1500, coral400, primary500, neutral200, neutral400, primary600] = useToken('colors', [
    'level1.500', 'orange.600', 'primary.500', 'neutral.200', 'neutral.400', 'primary.600',
  ]);

  const progressColor = (() => {
    switch (data.overallStatus) {
      case TimelineStatus.Completed:
      case TimelineStatus.CompletedLate:
        return level1500;
      case TimelineStatus.Delayed:
        return coral400;
      default:
        return primary500;
    }
  })();

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
        <span style={{ fontSize: "sm", fontWeight: "medium" }}>Postęp projektu</span>
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
          accent={data.delayedCount > 0 ? coral400 : undefined}
          small
        />
        <KpiCard
          label="W toku"
          value={String(data.inProgressCount)}
          accent={primary600}
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
      <div style={{ fontSize: "xs", color: neutral400, marginTop: 3, marginBottom: 12 }}>
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

      <div style={{ fontSize: "xs", color: neutral400 }}>
        Harmonogramów: {data.workSchedulesCount}
        {data.activeSchedulesCount > 0 && (
          <span style={{ marginLeft: 6 }}>(aktywnych: {data.activeSchedulesCount})</span>
        )}
      </div>
    </div>
  );
}

export default TimelineOverview;
