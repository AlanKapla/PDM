import React from 'react';
import { useToken } from '@chakra-ui/react';
import type { ScheduleSummaryWeb } from '../../types/projectDashboard.types';
import { DATE, DAYS } from '../../utils/formatters';
import { Accordion } from '../shared/Accordion';
import { TimelineStatusBadge } from '../shared/TimelineStatusBadge';
import { KpiCard } from '../shared/KpiCard';
import { NetGrossAmount } from '../shared/NetGrossAmount';
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
  const [primary50, primary600, level250, level2600, orange600, orange800] = useToken('colors', [
    'primary.50', 'primary.600', 'level2.50', 'level2.600', 'orange.600', 'orange.800',
  ]);

  const header = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1, flexWrap: 'wrap' }}>
      <span style={{ fontSize: "sm", fontWeight: "medium", flex: 1 }}>{summary.workScheduleName}</span>
      <Badge
        text={`${summary.totalWorkItemsCount ?? '?'} zakresów`}
        bg={primary50}
        color={primary600}
        small
      />
      {summary.hasLinkedEstimate && (
        <Badge
          text="Z kosztorysem"
          bg={level250}
          color={level2600}
          small
        />
      )}
      {summary.totalCostsNet != null && (
        <NetGrossAmount
          net={summary.totalCostsNet}
          gross={summary.totalCostsGross}
          size="sm"
          align="right"
          accentColor={orange600}
        />
      )}
      <TimelineStatusBadge status={summary.timelineStatus} small />
    </div>
  );

  return (
    <Accordion header={header} headerBg={primary50}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {/* Sekcja A: KPI finansowe */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
          <KpiCard label="Koszty" netValue={summary.totalCostsNet} grossValue={summary.totalCostsGross} small />
          <KpiCard
            label="Zakresów z kosztami"
            value={`${summary.workItemsWithCostsCount} / ${summary.totalWorkItemsCount}`}
            small
          />
          <KpiCard
            label="Opóźnionych zakresów"
            value={String(summary.workItemsDelayedCount)}
            accent={summary.workItemsDelayedCount > 0 ? orange800 : undefined}
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

