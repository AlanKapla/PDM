import React from 'react';
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Text } from '@chakra-ui/react';
import { chartTooltipPercent } from '../../utils/chartTooltip';
import { CHART_COLORS, CHART_HEIGHT, CHART_MARGIN } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import type { SCurvePoint } from '../../utils/chartAggregations';

export interface ProgressSCurveChartProps {
  points: SCurvePoint[];
  title?: string;
  embedded?: boolean;
}

function SCurveChartBody({ points }: { points: SCurvePoint[] }): React.ReactElement {
  return (
    <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
      <LineChart data={points} margin={CHART_MARGIN}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="label" tick={{ fontSize: 11 }} />
        <YAxis domain={[0, 100]} tickFormatter={(v: number) => `${v}%`} />
        <Tooltip formatter={chartTooltipPercent} />
        <Legend />
        <Line
          type="monotone"
          dataKey="planned"
          name="Planowany"
          stroke={CHART_COLORS.neutral}
          strokeDasharray="4 4"
          dot={false}
        />
        <Line
          type="monotone"
          dataKey="actual"
          name="Rzeczywisty"
          stroke={CHART_COLORS.primary}
          strokeWidth={2}
          dot={{ r: 3 }}
        />
      </LineChart>
    </ResponsiveContainer>
  );
}

export function ProgressSCurveChart({
  points,
  title = 'Krzywa postępu (S-curve)',
  embedded = false,
}: ProgressSCurveChartProps): React.ReactElement {
  if (embedded) {
    if (points.length === 0) {
      return (
        <Text fontSize="sm" color="neutral.400" fontStyle="italic" py={4} textAlign="center">
          Brak danych do krzywej postępu
        </Text>
      );
    }
    return <SCurveChartBody points={points} />;
  }

  return (
    <ChartCard
      title={title}
      isEmpty={points.length === 0}
      emptyMessage="Brak danych do krzywej postępu"
      ariaLabel="Wykres krzywej postępu projektu"
      fullWidth
    >
      <SCurveChartBody points={points} />
    </ChartCard>
  );
}

export default ProgressSCurveChart;
