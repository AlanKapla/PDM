import React, { useMemo } from 'react';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip, Legend } from 'recharts';
import { CHART_HEIGHT, CHART_PALETTE } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { chartTooltipAmount } from '../../utils/chartTooltip';
import { useChartAmount } from '../../hooks/useChartAmount';

export interface CostSourcesDonutProps {
  linkedCosts?: number | null;
  additionalCosts: number | null;
  title?: string;
  reserveBudget?: number | null;
}

export function CostSourcesDonut({
  linkedCosts,
  additionalCosts,
  title = 'Źródła kosztów',
  reserveBudget,
}: CostSourcesDonutProps): React.ReactElement {
  const { formatValue } = useChartAmount();

  const data = useMemo(() => {
    if (reserveBudget != null) {
      return [
        { name: 'Budżet główny', value: reserveBudget },
        { name: 'Koszty główne', value: additionalCosts ?? 0 },
      ].filter((item) => item.value > 0);
    }
    return [
      { name: 'Koszty powiązane', value: linkedCosts ?? 0 },
      { name: 'Koszty dodatkowe', value: additionalCosts ?? 0 },
    ].filter((item) => item.value > 0);
  }, [linkedCosts, additionalCosts, reserveBudget]);

  return (
    <ChartCard
      title={title}
      isEmpty={data.length === 0}
      ariaLabel={`Wykres: ${title}`}
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

export default CostSourcesDonut;
