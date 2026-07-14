import React, { useMemo } from 'react';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip, Legend } from 'recharts';
import { CHART_COLORS, CHART_HEIGHT, CHART_PALETTE } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { chartTooltipAmount } from '../../utils/chartTooltip';
import { useChartAmount } from '../../hooks/useChartAmount';

export interface BudgetCompositionChartProps {
  estimateBudget: number | null;
  reserveBudget: number | null;
}

export function BudgetCompositionChart({
  estimateBudget,
  reserveBudget,
}: BudgetCompositionChartProps): React.ReactElement {
  const { formatValue } = useChartAmount();

  const data = useMemo(() => {
    const items = [
      { name: 'Budżet kosztorysów', value: estimateBudget ?? 0 },
      { name: 'Budżet główny (rezerwa)', value: reserveBudget ?? 0 },
    ];
    return items.filter((item) => item.value > 0);
  }, [estimateBudget, reserveBudget]);

  return (
    <ChartCard
      title="Skład budżetu"
      isEmpty={data.length === 0}
      emptyMessage="Brak zdefiniowanego budżetu"
      ariaLabel="Wykres składu budżetu projektu"
    >
      <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
        <PieChart>
          <Pie data={data} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={90}>
            {data.map((entry, index) => (
              <Cell key={entry.name} fill={CHART_PALETTE[index % CHART_PALETTE.length]} />
            ))}
          </Pie>
          <Tooltip formatter={chartTooltipAmount(formatValue)} />
          <Legend />
        </PieChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default BudgetCompositionChart;
