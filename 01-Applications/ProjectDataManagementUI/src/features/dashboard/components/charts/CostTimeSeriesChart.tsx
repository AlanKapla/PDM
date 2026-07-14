import React, { useMemo } from 'react';
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Text } from '@chakra-ui/react';
import { CHART_COLORS, CHART_HEIGHT, CHART_MARGIN } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { useCostTimeSeries } from '../../hooks/useCostTimeSeries';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { useChartAmount } from '../../hooks/useChartAmount';

export interface CostTimeSeriesChartProps {
  costs: TrackedCostWeb[];
  title?: string;
}

export function CostTimeSeriesChart({
  costs,
  title = 'Kumulacja kosztów w czasie',
}: CostTimeSeriesChartProps): React.ReactElement {
  const { points, hasUndatedCosts } = useCostTimeSeries(costs);
  const { formatValue } = useChartAmount();

  const chartData = useMemo(
    () => points.map((point) => ({ ...point, display: point.label })),
    [points]
  );

  return (
    <ChartCard
      title={title}
      isEmpty={chartData.length === 0}
      emptyMessage="Brak kosztów z datą do wyświetlenia"
      ariaLabel="Wykres kumulacji kosztów w czasie"
      fullWidth
      footer={
        hasUndatedCosts ? (
          <Text fontSize="xs" color="neutral.400" mt={2}>
            Część kosztów nie ma daty — użyto daty utworzenia jako przybliżenia.
          </Text>
        ) : undefined
      }
    >
      <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
        <AreaChart data={chartData} margin={CHART_MARGIN}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="display" tick={{ fontSize: 11 }} />
          <YAxis tickFormatter={(v: number) => formatValue(v)} width={80} tick={{ fontSize: 11 }} />
          <Tooltip
            formatter={(value, name) => [
              formatValue(typeof value === 'number' ? value : Number(value) || 0),
              name === 'cumulative' ? 'Skumulowane' : 'Miesięczne',
            ]}
          />
          <Area
            type="monotone"
            dataKey="cumulative"
            name="cumulative"
            stroke={CHART_COLORS.primary}
            fill={CHART_COLORS.primaryLight}
            strokeWidth={2}
          />
        </AreaChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default CostTimeSeriesChart;
