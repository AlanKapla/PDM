import React from 'react';
import { TimelineStatus } from '../../types/projectDashboard.types';
import type { ScheduleStageWeb, TimelineStatsWeb, WorkItemLinkWeb } from '../../types/projectDashboard.types';
import { DATE } from '../../utils/formatters';
import { COLOR_PALETTE, TIMELINE_STATUS_MAP } from '../../utils/colors';
import { TimelineStatusBadge } from '../shared/TimelineStatusBadge';

export interface MiniGanttProps {
  stages: ScheduleStageWeb[];
  timeline: TimelineStatsWeb;
}

/**
 * Miniaturowy wykres Gantta dla harmonogramu.
 * Oś czasu rozciąga się od timeline.plannedStart do timeline.plannedEnd.
 * Źródło danych: ScheduleStageWeb[] + TimelineStatsWeb.
 */
export function MiniGantt({ stages, timeline }: MiniGanttProps): React.ReactElement {
  const rangeStart = timeline.plannedStart ? new Date(timeline.plannedStart).getTime() : null;
  const rangeEnd = timeline.plannedEnd ? new Date(timeline.plannedEnd).getTime() : null;
  const totalMs = rangeStart != null && rangeEnd != null ? rangeEnd - rangeStart : null;

  const calcBar = (start: string | null, end: string | null) => {
    if (!totalMs || !rangeStart || !start || !end) return null;
    const s = new Date(start).getTime();
    const e = new Date(end).getTime();
    const left = Math.max(0, ((s - rangeStart) / totalMs) * 100);
    const width = Math.max(1, ((e - s) / totalMs) * 100);
    return { left: `${left}%`, width: `${Math.min(width, 100 - left)}%` };
  };

  // Flatten wszystkich workItems ze wszystkich etapów rekurencyjnie (wraz z childStages)
  const flattenStage = (stage: ScheduleStageWeb): WorkItemLinkWeb[] => [
    ...(stage.workItems ?? []),
    ...(stage.childStages ?? []).flatMap(flattenStage),
  ];
  const allItems = stages.flatMap(flattenStage).filter(Boolean);

  const statusColors: Record<TimelineStatus, string> = {
    [TimelineStatus.NoSchedule]: COLOR_PALETTE.gray100,
    [TimelineStatus.NotStarted]: COLOR_PALETTE.gray100,
    [TimelineStatus.InProgress]: COLOR_PALETTE.blue400,
    [TimelineStatus.Delayed]: COLOR_PALETTE.coral400,
    [TimelineStatus.Completed]: COLOR_PALETTE.teal400,
    [TimelineStatus.CompletedLate]: COLOR_PALETTE.amber400,
    [TimelineStatus.NoWorkItems]: COLOR_PALETTE.gray100,
  };

  return (
    <div style={{ marginBottom: 8 }}>
      {/* Nagłówek osi czasu */}
      <div style={{ display: 'flex', marginBottom: 6 }}>
        <div style={{ width: 140, flexShrink: 0 }} />
        <div style={{ flex: 1, display: 'flex', justifyContent: 'space-between', fontSize: 12, color: COLOR_PALETTE.gray400 }}>
          {rangeStart != null && <span>{DATE(timeline.plannedStart)}</span>}
          {rangeEnd != null && <span>{DATE(timeline.plannedEnd)}</span>}
        </div>
      </div>

      {/* Wiersze workItems */}
      {allItems.map((item) => {
        const bar = calcBar(
          item.timeline?.plannedStart ?? null,
          item.timeline?.plannedEnd ?? null
        );
        const barColor = statusColors[item.timelineStatus];

        return (
          <div
            key={item.workItemLinkId}
            style={{ display: 'flex', alignItems: 'center', marginBottom: 3, height: 22 }}
          >
            <div
              style={{
                width: 140,
                flexShrink: 0,
                fontSize: 11,
                color: COLOR_PALETTE.gray600,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                paddingRight: 8,
              }}
            >
              {item.displayName}
            </div>
            <div style={{ flex: 1, position: 'relative', height: 10, background: COLOR_PALETTE.gray50, borderRadius: 4 }}>
              {bar && (
                <div
                  style={{
                    position: 'absolute',
                    left: bar.left,
                    width: bar.width,
                    height: '100%',
                    background: barColor,
                    borderRadius: 3,
                  }}
                />
              )}
            </div>
            <div style={{ width: 120, flexShrink: 0, paddingLeft: 8 }}>
              <TimelineStatusBadge status={item.timelineStatus} small />
            </div>
          </div>
        );
      })}

      {/* Legenda */}
      <div style={{ display: 'flex', gap: 10, marginTop: 8, flexWrap: 'wrap' }}>
        {(
          [
            { label: 'Ukończone', status: TimelineStatus.Completed },
            { label: 'W toku', status: TimelineStatus.InProgress },
            { label: 'Opóźnione', status: TimelineStatus.Delayed },
            { label: 'Nie rozpoczęto', status: TimelineStatus.NotStarted },
          ] as const
        ).map(({ label, status }) => (
          <div key={status} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <div
              style={{
                width: 12,
                height: 6,
                background: statusColors[status],
                borderRadius: 2,
              }}
            />
            <span style={{ fontSize: 12, color: COLOR_PALETTE.gray400 }}>{label}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

export default MiniGantt;
