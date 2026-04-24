import React from 'react';
import type { TrackerGroupWeb } from '../types/projectDashboard.types';
import { PLN, PROG, DATE, DAYS } from '../utils/formatters';
import { COLOR_PALETTE } from '../utils/colors';
import { Accordion } from './shared/Accordion';
import { FinancialStatusBadge } from './shared/FinancialStatusBadge';
import { TimelineStatusBadge } from './shared/TimelineStatusBadge';
import { KpiCard } from './shared/KpiCard';
import { Badge } from './shared/Badge';
import { WorkItemAccordion } from './WorkItemAccordion';

export interface GroupAccordionProps {
  group: TrackerGroupWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
  showCosts?: boolean;
}

/**
 * Accordion grupy pozycji kosztorysu. Obsługuje rekurencję childGroups bez limitu głębokości.
 * Źródło danych: TrackerGroupWeb.
 */
export function GroupAccordion({
  group,
  tenantId,
  projectId,
  onRefetch,
  showCosts = true,
}: GroupAccordionProps): React.ReactElement {
  const header = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1, flexWrap: 'wrap' }}>
      <span style={{ fontSize: 12, fontWeight: 500, flex: 1 }}>{group.groupName}</span>
      <span style={{ fontSize: 11, color: COLOR_PALETTE.gray400 }}>
        {group.totalItemsCount} poz.
      </span>
      {group.itemsOverBudgetCount > 0 && (
        <Badge
          text={`${group.itemsOverBudgetCount} przekr.`}
          bg={COLOR_PALETTE.coral50}
          color={COLOR_PALETTE.coral600}
          small
        />
      )}
      <TimelineStatusBadge status={group.timelineStatus} small />
      <span style={{ fontSize: 12, color: COLOR_PALETTE.gray600 }}>{PLN(group.budgetNet)}</span>
      <span style={{ fontSize: 11, color: COLOR_PALETTE.gray400 }}>/</span>
      <span style={{ fontSize: 12, fontWeight: 500, color: group.costsNet != null && group.costsNet > 0 ? COLOR_PALETTE.coral400 : COLOR_PALETTE.gray600 }}>
        {PLN(group.costsNet)}
      </span>
      <FinancialStatusBadge status={group.financialStatus} small />
    </div>
  );

  return (
    <Accordion header={header} headerBg={COLOR_PALETTE.gray50}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        {/* KPI finansowe */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
          <KpiCard label="Budżet" value={PLN(group.budgetNet)} small />
          <KpiCard label="Koszty" value={PLN(group.costsNet)} small />
          <KpiCard label="Pokrycie grupy" value={PROG(group.coveredPercent)} small />
          <KpiCard
            label="Poz. bez kosztów"
            value={String(group.itemsWithoutCostsCount)}
            accent={group.itemsWithoutCostsCount > 0 ? COLOR_PALETTE.coral600 : undefined}
            small
          />
        </div>

        {/* KPI czasowe — tylko gdy jest oś czasowa */}
        {group.timelinePlannedStart != null && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
            <KpiCard label="Start" value={DATE(group.timelinePlannedStart)} small />
            <KpiCard label="Koniec" value={DATE(group.timelinePlannedEnd)} small />
            <KpiCard label="Czas trwania" value={DAYS(group.timelineTotalDays)} small />
            <KpiCard label="Postęp" value={PROG(group.timeline?.progressPercent ?? null)} small />
          </div>
        )}

        {/* Pozycje grupy */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
          {(group.items ?? []).map((item) => (
            <WorkItemAccordion
              key={item.workItemLinkId}
              item={item}
              tenantId={tenantId}
              projectId={projectId}
              onRefetch={onRefetch}
              showCosts={showCosts}
              displayMode="estimate"
            />
          ))}
        </div>

        {/* Grupy podrzędne — rekurencja */}
        {(group.childGroups ?? []).length > 0 && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 5, marginTop: 4 }}>
            {(group.childGroups ?? []).map((child) => (
              <GroupAccordion
                key={child.groupId}
                group={child}
                tenantId={tenantId}
                projectId={projectId}
                onRefetch={onRefetch}
                showCosts={showCosts}
              />
            ))}
          </div>
        )}

        {/* Koszty dodatkowe grupy */}
        {(group.additionalCosts?.costsCount ?? 0) > 0 && (
          <div
            style={{
              background: COLOR_PALETTE.amber50,
              borderRadius: 8,
              padding: '8px 10px',
              fontSize: 11,
              color: COLOR_PALETTE.amber600,
              marginTop: 4,
            }}
          >
            Koszty dodatkowe grupy: {PLN(group.additionalCosts.totalNet)}{' '}
            <span style={{ color: COLOR_PALETTE.gray400 }}>
              ({group.additionalCosts?.costsCount ?? 0} pozycji)
            </span>
          </div>
        )}

        {/* Stopka grupy */}
        <div
          style={{
            borderTop: `0.5px solid ${COLOR_PALETTE.border}`,
            paddingTop: 8,
            display: 'flex',
            justifyContent: 'space-between',
            fontSize: 11,
            color: COLOR_PALETTE.gray600,
          }}
        >
          <span>Suma grupy — budżet / koszty:</span>
          <span style={{ fontWeight: 500 }}>
            {PLN(group.budgetNet)} / {PLN(group.costsNet)}
          </span>
        </div>
      </div>
    </Accordion>
  );
}

export default GroupAccordion;

