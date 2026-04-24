import React from 'react';
import type { CostEstimateSummaryWeb } from '../../types/projectDashboard.types';
import { PLN, PROG, DATE, DAYS } from '../../utils/formatters';
import { COLOR_PALETTE } from '../../utils/colors';
import { Accordion } from '../shared/Accordion';
import { FinancialStatusBadge } from '../shared/FinancialStatusBadge';
import { KpiCard } from '../shared/KpiCard';
import { Badge } from '../shared/Badge';
import { GroupAccordion } from '../GroupAccordion';

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
  const header = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1, flexWrap: 'wrap' }}>
      <span style={{ fontSize: 13, fontWeight: 500, flex: 1 }}>{summary.costEstimateName}</span>
      <Badge
        text={`${summary.totalItemsCount ?? '?'} poz.`}
        bg={COLOR_PALETTE.purple100}
        color={COLOR_PALETTE.purple600}
        small
      />
      {summary.itemsOverBudgetCount > 0 && (
        <Badge
          text={`${summary.itemsOverBudgetCount} przekr.`}
          bg={COLOR_PALETTE.coral50}
          color={COLOR_PALETTE.coral600}
          small
        />
      )}
      <Badge
        text={summary.hasLinkedSchedule ? 'Z harmonogramem' : 'Bez harmonogramu'}
        bg={summary.hasLinkedSchedule ? COLOR_PALETTE.purple50 : COLOR_PALETTE.gray50}
        color={summary.hasLinkedSchedule ? COLOR_PALETTE.purple600 : COLOR_PALETTE.gray600}
        small
      />
      <span style={{ fontSize: 12, color: COLOR_PALETTE.gray600 }}>{PLN(summary.budgetNet)}</span>
      <FinancialStatusBadge status={summary.financialStatus} small />
    </div>
  );

  return (
    <Accordion header={header} headerBg={COLOR_PALETTE.purple50}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {/* Sekcja A: KPI finansowe (6 kafli) */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6, 1fr)', gap: 8 }}>
          <KpiCard label="Budżet netto" value={PLN(summary.budgetNet)} small />
          <KpiCard label="Budżet brutto" value={PLN(summary.budgetGross)} small />
          <KpiCard label="Koszty" value={PLN(summary.costsNet)} small />
          <KpiCard label="Pokrycie" value={PROG(summary.coveredPercent)} small />
          <KpiCard
            label="Poz. z kosztami"
            value={`${summary.itemsWithCostsCount} / ${summary.totalItemsCount}`}
            small
          />
          <KpiCard
            label="Przekroczone"
            value={String(summary.itemsOverBudgetCount)}
            accent={summary.itemsOverBudgetCount > 0 ? COLOR_PALETTE.coral600 : undefined}
            small
          />
        </div>

        {/* Sekcja B: Oś czasowa */}
        {!summary.hasLinkedSchedule ? (
          <div
            style={{
              background: COLOR_PALETTE.gray50,
              borderRadius: 8,
              padding: '8px 12px',
              fontSize: 11,
              color: COLOR_PALETTE.gray400,
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
              fontSize: 10,
              color: COLOR_PALETTE.gray400,
              fontWeight: 500,
              borderBottom: `0.5px solid ${COLOR_PALETTE.border}`,
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
              background: COLOR_PALETTE.amber50,
              borderRadius: 8,
              padding: '10px 12px',
              fontSize: 12,
              color: COLOR_PALETTE.amber600,
            }}
          >
            Koszty dodatkowe kosztorysu: {PLN(summary.additionalCosts?.totalNet ?? null)}{' '}
            <span style={{ color: COLOR_PALETTE.gray400 }}>
              ({summary.additionalCosts?.costsCount ?? 0} pozycji)
            </span>
          </div>
        )}

        {/* Stopka kosztorysu */}
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
          <span>Suma kosztorysu — budżet / koszty:</span>
          <span style={{ fontWeight: 500 }}>
            {PLN(summary.budgetNet)} / {PLN(summary.costsNet)}
          </span>
        </div>
      </div>
    </Accordion>
  );
}

export default EstimateBlock;

