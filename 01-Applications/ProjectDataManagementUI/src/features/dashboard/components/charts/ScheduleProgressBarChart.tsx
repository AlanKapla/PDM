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
import { chartTooltipPercent } from '../../utils/chartTooltip';
import type { ScheduleSummaryWeb } from '../../types/projectDashboard.types';
import { buildScheduleProgress } from '../../utils/chartAggregations';

export interface ScheduleProgressBarChartProps {
  summaries: ScheduleSummaryWeb[];
  title?: string;
}

export function ScheduleProgressBarChart({
  summaries,
  title = 'Postęp per harmonogram',
}: ScheduleProgressBarChartProps): React.ReactElement {
  const chartData = useMemo(() => buildScheduleProgress(summaries), [summaries]);

  return (
    <ChartCard
      title={title}
      isEmpty={chartData.length === 0}
      emptyMessage="Brak harmonogramów"
      ariaLabel="Wykres postępu per harmonogram"
    >
      <ResponsiveContainer width="100%" height={Math.max(CHART_HEIGHT, chartData.length * 36)}>
        <BarChart data={chartData} layout="vertical" margin={CHART_MARGIN}>
          <CartesianGrid strokeDasharray="3 3" horizontal={false} />
          <XAxis type="number" domain={[0, 100]} tickFormatter={(v: number) => `${v}%`} />
          <YAxis type="category" dataKey="name" width={120} tick={{ fontSize: 11 }} />
          <Tooltip formatter={chartTooltipPercent} />
          <Bar dataKey="value" name="Postęp" fill={CHART_COLORS.primary} radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default ScheduleProgressBarChart;
