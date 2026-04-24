import React from 'react';
import type { ScheduleSummaryWeb } from '../../types/projectDashboard.types';
import { PLN, PROG, DATE, DAYS } from '../../utils/formatters';
import { COLOR_PALETTE } from '../../utils/colors';
import { Accordion } from '../shared/Accordion';
import { TimelineStatusBadge } from '../shared/TimelineStatusBadge';
import { KpiCard } from '../shared/KpiCard';
import { Badge } from '../shared/Badge';
import { MiniGantt } from './MiniGantt';
import { StageAccordion } from './StageAccordion';

export interface ScheduleBlockProps {
  summary: ScheduleSummaryWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Accordion harmonogramu z wykresem Gantta i etapami.
 * Źródło danych: ScheduleSummaryWeb.
 */
export function ScheduleBlock({
  summary,
  tenantId,
  projectId,
  onRefetch,
}: ScheduleBlockProps): React.ReactElement {
  const header = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1, flexWrap: 'wrap' }}>
      <span style={{ fontSize: 13, fontWeight: 500, flex: 1 }}>{summary.workScheduleName}</span>
      <Badge
        text={`${summary.totalWorkItemsCount ?? '?'} zakresów`}
        bg={COLOR_PALETTE.blue50}
        color={COLOR_PALETTE.blue600}
        small
      />
      {summary.hasLinkedEstimate && (
        <Badge
          text="Z kosztorysem"
          bg={COLOR_PALETTE.purple50}
          color={COLOR_PALETTE.purple600}
          small
        />
      )}
      {summary.totalCostsNet != null && (
        <span style={{ fontSize: 12, fontWeight: 500, color: COLOR_PALETTE.coral400 }}>
          {PLN(summary.totalCostsNet)}
        </span>
      )}
      <TimelineStatusBadge status={summary.timelineStatus} small />
    </div>
  );

  return (
    <Accordion header={header} headerBg={COLOR_PALETTE.blue50}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {/* Sekcja A: KPI finansowe */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
          <KpiCard label="Koszty netto" value={PLN(summary.totalCostsNet)} small />
          <KpiCard label="Koszty brutto" value={PLN(summary.totalCostsGross)} small />
          <KpiCard
            label="Zakresów z kosztami"
            value={`${summary.workItemsWithCostsCount} / ${summary.totalWorkItemsCount}`}
            small
          />
          <KpiCard
            label="Opóźnionych zakresów"
            value={String(summary.workItemsDelayedCount)}
            accent={summary.workItemsDelayedCount > 0 ? COLOR_PALETTE.coral600 : undefined}
            small
          />
        </div>

        {/* Sekcja B: Oś czasowa */}
        {summary.timeline != null && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
            <KpiCard label="Planowany start" value={DATE(summary.timeline.plannedStart)} small />
            <KpiCard label="Planowany koniec" value={DATE(summary.timeline.plannedEnd)} small />
            <KpiCard label="Czas trwania" value={DAYS(summary.timeline.totalPlannedDays)} small />
            <KpiCard
              label="W toku / Nie rozp."
              value={`${summary.timeline.inProgressCount} / ${summary.timeline.notStartedCount}`}
              small
            />
          </div>
        )}

        {summary.timeline != null && (
          <MiniGantt stages={summary.stages} timeline={summary.timeline} />
        )}

        <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
          {summary.stages.map((stage) => (
            <StageAccordion
              key={stage.stageId}
              stage={stage}
              tenantId={tenantId}
              projectId={projectId}
              onRefetch={onRefetch}
            />
          ))}
        </div>
      </div>
    </Accordion>
  );
}

export default ScheduleBlock;

