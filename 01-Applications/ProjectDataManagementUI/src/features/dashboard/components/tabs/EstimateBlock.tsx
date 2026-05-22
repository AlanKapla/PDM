import React from 'react';
import { useToken } from '@chakra-ui/react';
import type { CostEstimateSummaryWeb } from '../../types/projectDashboard.types';
import { PLN, PROG, DATE, DAYS } from '../../utils/formatters';
import { Accordion } from '../shared/Accordion';
import { FinancialStatusBadge } from '../shared/FinancialStatusBadge';
import { KpiCard } from '../shared/KpiCard';
import { Badge } from '../shared/Badge';
import { GroupAccordion } from '../GroupAccordion';
import { useDashboardCurrency } from '../../context/DashboardCurrencyContext';

export interface EstimateBlockProps {
  summary: CostEstimateSummaryWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Accordion kosztorysu — wyświetla grupy, koszty i powiązanie z harmonogramem.
 * Źródło danych: CostEstimateSummaryWeb.
 */
export function EstimateBlock({
  summary,
  tenantId,
  projectId,
  onRefetch,
}: EstimateBlockProps): React.ReactElement {
  const currencySymbol = useDashboardCurrency();
  const [
    level2100, level2600, orange50, orange800,
    level250, neutral50, neutral600,
    neutral400, neutral200, amber50, amber600,
  ] = useToken('colors', [
    'level2.100', 'level2.600', 'orange.50', 'orange.800',
    'level2.50', 'neutral.50', 'neutral.600',
    'neutral.400', 'neutral.200', 'amber.50', 'amber.600',
  ]);

  const header = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1, flexWrap: 'wrap' }}>
      <span style={{ fontSize: "sm", fontWeight: "medium", flex: 1 }}>{summary.costEstimateName}</span>
      <Badge
        text={`${summary.totalItemsCount ?? '?'} poz.`}
        bg={level2100}
        color={level2600}
        small
      />
      {summary.itemsOverBudgetCount > 0 && (
        <Badge
          text={`${summary.itemsOverBudgetCount} przekr.`}
          bg={orange50}
          color={orange800}
          small
        />
      )}
      <Badge
        text={summary.hasLinkedSchedule ? 'Z harmonogramem' : 'Bez harmonogramu'}
        bg={summary.hasLinkedSchedule ? level250 : neutral50}
        color={summary.hasLinkedSchedule ? level2600 : neutral600}
        small
      />
      <span style={{ fontSize: "xs", color: neutral600 }}>{PLN(summary.budgetNet, currencySymbol)}</span>
      <FinancialStatusBadge status={summary.financialStatus} small />
    </div>
  );

  return (
    <Accordion header={header} headerBg={level250}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {/* Sekcja A: KPI finansowe (6 kafli) */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6, 1fr)', gap: 8 }}>
          <KpiCard label="Budżet netto" value={PLN(summary.budgetNet, currencySymbol)} small />
          <KpiCard label="Budżet brutto" value={PLN(summary.budgetGross, currencySymbol)} small />
          <KpiCard label="Koszty" value={PLN(summary.costsNet, currencySymbol)} small />
          <KpiCard label="Pokrycie" value={PROG(summary.coveredPercent)} small />
          <KpiCard
            label="Poz. z kosztami"
            value={`${summary.itemsWithCostsCount} / ${summary.totalItemsCount}`}
            small
          />
          <KpiCard
            label="Przekroczone"
            value={String(summary.itemsOverBudgetCount)}
            accent={summary.itemsOverBudgetCount > 0 ? orange800 : undefined}
            small
          />
        </div>

        {/* Sekcja B: Oś czasowa */}
        {!summary.hasLinkedSchedule ? (
          <div
            style={{
              background: neutral50,
              borderRadius: 8,
              padding: '8px 12px',
              fontSize: "xs",
              color: neutral400,
              fontStyle: 'italic',
            }}
          >
            Ten kosztorys nie jest powiązany z harmonogramem — brak danych czasowych.
          </div>
        ) : (
          summary.timelinePlannedStart != null && (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
              <KpiCard label="Start" value={DATE(summary.timelinePlannedStart)} small />
              <KpiCard label="Koniec" value={DATE(summary.timelinePlannedEnd)} small />
              <KpiCard label="Czas trwania" value={DAYS(summary.timelineTotalDays)} small />
              <KpiCard label="Postęp" value={PROG(summary.timeline?.progressPercent ?? null)} small />
            </div>
          )
        )}

        {/* Nagłówek tabeli pozycji */}
        {summary.groups.length > 0 && (
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: '3fr 1.5fr 1fr 1fr 1fr 1fr',
              gap: 8,
              padding: '4px 0',
              fontSize: "xs",
              color: neutral400,
              fontWeight: "medium",
              borderBottom: `0.5px solid ${neutral200}`,
            }}
          >
            {['Pozycja', 'Czas', 'Budżet netto', 'Koszty', 'Odchylenie', 'Status'].map((col) => (
              <div key={col} style={{ textAlign: col === 'Pozycja' || col === 'Czas' ? 'left' : 'right' }}>
                {col}
              </div>
            ))}
          </div>
        )}

        {/* Grupy */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
          {summary.groups.map((group) => (
            <GroupAccordion
              key={group.groupId}
              group={group}
              tenantId={tenantId}
              projectId={projectId}
              onRefetch={onRefetch}
              showCosts
            />
          ))}
        </div>

        {(summary.additionalCosts?.costsCount ?? 0) > 0 && (
          <div
            style={{
              background: amber50,
              borderRadius: 8,
              padding: '10px 12px',
              fontSize: "xs",
              color: amber600,
            }}
          >
            Koszty dodatkowe kosztorysu: {PLN(summary.additionalCosts?.totalNet ?? null, currencySymbol)}{' '}
            <span style={{ color: neutral400 }}>
              ({summary.additionalCosts?.costsCount ?? 0} pozycji)
            </span>
          </div>
        )}

        {/* Stopka kosztorysu */}
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
          <span>Suma kosztorysu — budżet / koszty:</span>
          <span style={{ fontWeight: "medium" }}>
            {PLN(summary.budgetNet, currencySymbol)} / {PLN(summary.costsNet, currencySymbol)}
          </span>
        </div>
      </div>
    </Accordion>
  );
}

export default EstimateBlock;

