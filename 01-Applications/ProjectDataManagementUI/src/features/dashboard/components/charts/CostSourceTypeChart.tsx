import React, { useMemo } from 'react';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip, Legend } from 'recharts';
import { CHART_HEIGHT, CHART_PALETTE } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { chartTooltipAmount } from '../../utils/chartTooltip';
import { useChartAmount } from '../../hooks/useChartAmount';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { groupCostsBySourceType } from '../../utils/chartAggregations';

export interface CostSourceTypeChartProps {
  costs: TrackedCostWeb[];
  title?: string;
}

export function CostSourceTypeChart({
  costs,
  title = 'Koszty wg typu źródła',
}: CostSourceTypeChartProps): React.ReactElement {
  const { formatValue } = useChartAmount();

  const data = useMemo(
    () => groupCostsBySourceType(costs),
    [costs]
  );

  return (
    <ChartCard
      title={title}
      isEmpty={data.length === 0}
      ariaLabel="Wykres kosztów według typu źródła"
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

export default CostSourceTypeChart;
