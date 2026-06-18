import React, { useMemo } from 'react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { CHART_COLORS, CHART_HEIGHT, CHART_MARGIN } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { chartTooltipAmount } from '../../utils/chartTooltip';
import { useChartAmount } from '../../hooks/useChartAmount';
import type { CostEstimateSummaryWeb } from '../../types/projectDashboard.types';
import { buildEstimateDeviations } from '../../utils/chartAggregations';

export interface EstimateDeviationChartProps {
  summaries: CostEstimateSummaryWeb[];
  title?: string;
}

export function EstimateDeviationChart({
  summaries,
  title = 'Odchylenia budżetowe per kosztorys',
}: EstimateDeviationChartProps): React.ReactElement {
  const { formatValue } = useChartAmount();

  const chartData = useMemo(
    () => buildEstimateDeviations(summaries),
    [summaries]
  );

  return (
    <ChartCard
      title={title}
      isEmpty={chartData.length === 0}
      emptyMessage="Brak danych o odchyleniach"
      ariaLabel="Wykres odchyleń budżetowych per kosztorys"
      fullWidth
    >
      <ResponsiveContainer width="100%" height={Math.max(CHART_HEIGHT, chartData.length * 36)}>
        <BarChart data={chartData} layout="vertical" margin={CHART_MARGIN}>
          <CartesianGrid strokeDasharray="3 3" horizontal={false} />
          <ReferenceLine x={0} stroke={CHART_COLORS.neutral} />
          <XAxis type="number" tickFormatter={(v: number) => formatValue(v)} />
          <YAxis type="category" dataKey="name" width={120} tick={{ fontSize: 11 }} />
          <Tooltip formatter={chartTooltipAmount(formatValue)} />
          <Bar dataKey="deviation" name="Odchylenie">
            {chartData.map((entry) => (
              <Cell
                key={entry.id}
                fill={entry.deviation < 0 ? CHART_COLORS.red : CHART_COLORS.level1}
              />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default EstimateDeviationChart;
