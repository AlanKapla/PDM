import React from 'react';
import { useToken } from '@chakra-ui/react';
import type { TrackerGroupWeb } from '../types/projectDashboard.types';
import { PLN, PROG, DATE, DAYS } from '../utils/formatters';
import { Accordion } from './shared/Accordion';
import { FinancialStatusBadge } from './shared/FinancialStatusBadge';
import { TimelineStatusBadge } from './shared/TimelineStatusBadge';
import { KpiCard } from './shared/KpiCard';
import { Badge } from './shared/Badge';
import { WorkItemAccordion } from './WorkItemAccordion';
import { useDashboardCurrency } from '../context/DashboardCurrencyContext';

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
  const currencySymbol = useDashboardCurrency();
  const [
    neutral400, orange50, orange600, orange800, neutral600, neutral50, amber50, amber600, neutral200,
  ] = useToken('colors', [
    'neutral.400', 'orange.50', 'orange.600', 'orange.800', 'neutral.600', 'neutral.50', 'amber.50', 'amber.600', 'neutral.200',
  ]);

  const header = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1, flexWrap: 'wrap' }}>
      <span style={{ fontSize: "xs", fontWeight: "medium", flex: 1 }}>{group.groupName}</span>
      <span style={{ fontSize: "xs", color: neutral400 }}>
        {group.totalItemsCount} poz.
      </span>
      {group.itemsOverBudgetCount > 0 && (
        <Badge
          text={`${group.itemsOverBudgetCount} przekr.`}
          bg={orange50}
          color={orange800}
          small
        />
      )}
      <TimelineStatusBadge status={group.timelineStatus} small />
      <span style={{ fontSize: "xs", color: neutral600 }}>{PLN(group.budgetNet, currencySymbol)}</span>
      <span style={{ fontSize: "xs", color: neutral400 }}>/</span>
      <span style={{ fontSize: "xs", fontWeight: "medium", color: group.costsNet != null && group.costsNet > 0 ? orange600 : neutral600 }}>
        {PLN(group.costsNet, currencySymbol)}
      </span>
      <FinancialStatusBadge status={group.financialStatus} small />
    </div>
  );

  return (
    <Accordion header={header} headerBg={neutral50}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        {/* KPI finansowe */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
          <KpiCard label="Budżet" value={PLN(group.budgetNet, currencySymbol)} small />
          <KpiCard label="Koszty" value={PLN(group.costsNet, currencySymbol)} small />
          <KpiCard label="Pokrycie grupy" value={PROG(group.coveredPercent)} small />
          <KpiCard
            label="Poz. bez kosztów"
            value={String(group.itemsWithoutCostsCount)}
            accent={group.itemsWithoutCostsCount > 0 ? orange800 : undefined}
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
              key={`${item.costEstimateItemId ?? ''}-${item.workScheduleStageWorkId ?? ''}`}
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
              background: amber50,
              borderRadius: 8,
              padding: '8px 10px',
              fontSize: "xs",
              color: amber600,
              marginTop: 4,
            }}
          >
            Koszty dodatkowe grupy: {PLN(group.additionalCosts.totalNet, currencySymbol)}{' '}
            <span style={{ color: neutral400 }}>
              ({group.additionalCosts?.costsCount ?? 0} pozycji)
            </span>
          </div>
        )}

        {/* Stopka grupy */}
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
          <span>Suma grupy — budżet / koszty:</span>
          <span style={{ fontWeight: "medium" }}>
            {PLN(group.budgetNet, currencySymbol)} / {PLN(group.costsNet, currencySymbol)}
          </span>
        </div>
      </div>
    </Accordion>
  );
}

export default GroupAccordion;

