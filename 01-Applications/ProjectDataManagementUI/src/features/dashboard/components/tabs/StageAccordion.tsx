import React from 'react';
import { useToken } from '@chakra-ui/react';
import type { ScheduleStageWeb } from '../../types/projectDashboard.types';
import { PLN, DATE, DAYS } from '../../utils/formatters';
import { Accordion } from '../shared/Accordion';
import { TimelineStatusBadge } from '../shared/TimelineStatusBadge';
import { KpiCard } from '../shared/KpiCard';
import { WorkItemAccordion } from '../WorkItemAccordion';
import { useDashboardCurrency } from '../../context/DashboardCurrencyContext';

export interface StageAccordionProps {
  stage: ScheduleStageWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Accordion etapu harmonogramu. Pozycje wyświetlane razem z kosztami (showCosts=true).
 * Źródło danych: ScheduleStageWeb.
 */
export function StageAccordion({
  stage,
  tenantId,
  projectId,
  onRefetch,
}: StageAccordionProps): React.ReactElement {
  const currencySymbol = useDashboardCurrency();
  const [neutral400, orange600, orange800, neutral100, neutral200, neutral600] = useToken('colors', [
    'neutral.400', 'orange.600', 'orange.800', 'neutral.100', 'neutral.200', 'neutral.600',
  ]);

  const header = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1, flexWrap: 'wrap' }}>
      <span style={{ fontSize: "xs", fontWeight: "medium", flex: 1 }}>{stage.stageName}</span>
      <TimelineStatusBadge status={stage.timelineStatus} small />
      {stage.timeline && (
        <span style={{ fontSize: "xs", color: neutral400, whiteSpace: 'nowrap' }}>
          {DATE(stage.timeline.plannedStart)} – {DATE(stage.timeline.plannedEnd)}
        </span>
      )}
      {stage.totalCostsNet != null && (
        <span style={{ fontSize: "xs", fontWeight: "medium", color: orange600, whiteSpace: 'nowrap' }}>
          {PLN(stage.totalCostsNet, currencySymbol)}
        </span>
      )}
    </div>
  );

  return (
    <Accordion header={header}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {/* KPI etapu */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
          <KpiCard label="Koszty etapu netto" value={PLN(stage.totalCostsNet, currencySymbol)} small />
          <KpiCard
            label="Opóźnionych zakresów"
            value={String(stage.delayedWorkItemsCount)}
            accent={stage.delayedWorkItemsCount > 0 ? orange800 : undefined}
            small
          />
          <KpiCard label="Start etapu" value={DATE(stage.timeline?.plannedStart ?? null)} small />
          <KpiCard label="Koniec etapu" value={DATE(stage.timeline?.plannedEnd ?? null)} small />
        </div>

        {/* Zakresy prac */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
          {(stage.workItems ?? []).map((item) => (
            <WorkItemAccordion
              key={`${item.costEstimateItemId ?? ''}-${item.workScheduleStageWorkId ?? ''}`}
              item={item}
              tenantId={tenantId}
              projectId={projectId}
              onRefetch={onRefetch}
              showCosts
              displayMode="schedule"
            />
          ))}
        </div>

        {(stage.childStages ?? []).length > 0 && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 5, paddingLeft: 16, borderLeft: `2px solid ${neutral100}` }}>
            {(stage.childStages ?? []).map((child) => (
              <StageAccordion
                key={child.stageId}
                stage={child}
                tenantId={tenantId}
                projectId={projectId}
                onRefetch={onRefetch}
              />
            ))}
          </div>
        )}

        {/* Stopka etapu */}
        {stage.totalCostsNet != null && (
          <div
            style={{
              borderTop: `0.5px solid ${neutral200}`,
              paddingTop: 8,
              display: 'flex',
              justifyContent: 'space-between',
              fontSize: "xs",
              color: neutral600,
            }}
          >
            <span>Suma kosztów etapu:</span>
            <span style={{ fontWeight: "medium", color: orange600 }}>
              {PLN(stage.totalCostsNet, currencySymbol)}
            </span>
          </div>
        )}
      </div>
    </Accordion>
  );
}

export default StageAccordion;

