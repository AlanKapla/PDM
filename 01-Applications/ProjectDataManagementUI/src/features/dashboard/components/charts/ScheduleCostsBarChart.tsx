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
import type { ScheduleSummaryWeb } from '../../types/projectDashboard.types';
import { buildScheduleCosts } from '../../utils/chartAggregations';

export interface ScheduleCostsBarChartProps {
  summaries: ScheduleSummaryWeb[];
  title?: string;
}

export function ScheduleCostsBarChart({
  summaries,
  title = 'Koszty per harmonogram',
}: ScheduleCostsBarChartProps): React.ReactElement {
  const { formatValue } = useChartAmount();

  const chartData = useMemo(
    () => buildScheduleCosts(summaries),
    [summaries]
  );

  return (
    <ChartCard
      title={title}
      isEmpty={chartData.length === 0}
      emptyMessage="Brak kosztów w harmonogramach"
      ariaLabel="Wykres kosztów per harmonogram"
    >
      <ResponsiveContainer width="100%" height={Math.max(CHART_HEIGHT, chartData.length * 36)}>
        <BarChart data={chartData} layout="vertical" margin={CHART_MARGIN}>
          <CartesianGrid strokeDasharray="3 3" horizontal={false} />
          <XAxis type="number" tickFormatter={(v: number) => formatValue(v)} />
          <YAxis type="category" dataKey="name" width={120} tick={{ fontSize: 11 }} />
          <Tooltip formatter={chartTooltipAmount(formatValue)} />
          <Bar dataKey="value" name="Koszty" fill={CHART_COLORS.level2} radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default ScheduleCostsBarChart;
