import React, { useMemo } from 'react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { CHART_COLORS, CHART_HEIGHT, CHART_MARGIN } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { chartTooltipAmount } from '../../utils/chartTooltip';
import { useChartAmount } from '../../hooks/useChartAmount';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { groupCostsByContractor } from '../../utils/chartAggregations';

export interface TopContractorsChartProps {
  costs: TrackedCostWeb[];
  limit?: number;
  title?: string;
}

export function TopContractorsChart({
  costs,
  limit = 5,
  title = 'Top wykonawcy',
}: TopContractorsChartProps): React.ReactElement {
  const { formatValue } = useChartAmount();

  const chartData = useMemo(
    () => groupCostsByContractor(costs, limit),
    [costs, limit]
  );

  return (
    <ChartCard
      title={title}
      isEmpty={chartData.length === 0}
      emptyMessage="Brak kosztów z przypisanymi wykonawcami"
      ariaLabel="Wykres top wykonawców według kosztów"
    >
      <ResponsiveContainer width="100%" height={Math.max(CHART_HEIGHT, chartData.length * 36)}>
        <BarChart data={chartData} layout="vertical" margin={CHART_MARGIN}>
          <CartesianGrid strokeDasharray="3 3" horizontal={false} />
          <XAxis type="number" tickFormatter={(v: number) => formatValue(v)} />
          <YAxis type="category" dataKey="name" width={140} tick={{ fontSize: 11 }} />
          <Tooltip formatter={chartTooltipAmount(formatValue)} />
          <Bar dataKey="value" name="Koszty" fill={CHART_COLORS.action} radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default TopContractorsChart;
