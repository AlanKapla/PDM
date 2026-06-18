import React from 'react';
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
import type { FlattenedStage } from '../../utils/chartAggregations';

export interface StageDelaysChartProps {
  stages: FlattenedStage[];
  title?: string;
}

export function StageDelaysChart({
  stages,
  title = 'Opóźnienia per etap',
}: StageDelaysChartProps): React.ReactElement {
  const chartData = stages.map((stage) => ({
    id: stage.stageId,
    name: stages.length > 1 && stage.scheduleName
      ? `${stage.scheduleName} › ${stage.stageName}`
      : stage.stageName,
    delayDays: stage.delayDays ?? 0,
  }));

  return (
    <ChartCard
      title={title}
      isEmpty={chartData.length === 0}
      emptyMessage="Brak opóźnionych etapów"
      ariaLabel="Wykres opóźnień per etap harmonogramu"
      fullWidth
    >
      <ResponsiveContainer width="100%" height={Math.max(CHART_HEIGHT, chartData.length * 36)}>
        <BarChart data={chartData} layout="vertical" margin={CHART_MARGIN}>
          <CartesianGrid strokeDasharray="3 3" horizontal={false} />
          <XAxis type="number" tickFormatter={(v: number) => `${v} dni`} />
          <YAxis type="category" dataKey="name" width={160} tick={{ fontSize: 11 }} />
          <Tooltip formatter={(value) => `${typeof value === 'number' ? value : Number(value) || 0} dni`} />
          <Bar dataKey="delayDays" name="Opóźnienie" fill={CHART_COLORS.orange} radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default StageDelaysChart;
