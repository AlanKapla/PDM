import React, { useMemo } from 'react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Link, Text } from '@chakra-ui/react';
import { CHART_COLORS, CHART_HEIGHT, CHART_MARGIN } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { chartTooltipAmount } from '../../utils/chartTooltip';
import { useChartAmount } from '../../hooks/useChartAmount';
import type { CostEstimateSummaryWeb } from '../../types/projectDashboard.types';
import { buildEstimateComparison } from '../../utils/chartAggregations';

export interface EstimateBudgetBarChartProps {
  summaries: CostEstimateSummaryWeb[];
  limit?: number;
  onShowAll?: () => void;
  title?: string;
}

export function EstimateBudgetBarChart({
  summaries,
  limit,
  onShowAll,
  title = 'Budżet vs koszty per kosztorys',
}: EstimateBudgetBarChartProps): React.ReactElement {
  const { formatValue } = useChartAmount();

  const chartData = useMemo(() => {
    const all = buildEstimateComparison(summaries);
    const sorted = [...all].sort((a, b) => b.costs - a.costs);
    return limit != null ? sorted.slice(0, limit) : sorted;
  }, [summaries, limit]);

  const hasMore = limit != null && summaries.length > limit;

  return (
    <ChartCard
      title={title}
      isEmpty={chartData.length === 0}
      emptyMessage="Brak powiązanych kosztorysów"
      ariaLabel="Wykres porównania budżetu i kosztów per kosztorys"
      fullWidth
      footer={
        hasMore && onShowAll ? (
          <Text fontSize="xs" color="primary.600" mt={2}>
            <Link onClick={onShowAll} cursor="pointer">
              Pokaż wszystkie ({summaries.length}) w zakładce Finanse →
            </Link>
          </Text>
        ) : undefined
      }
    >
      <ResponsiveContainer width="100%" height={Math.max(CHART_HEIGHT, chartData.length * 36)}>
        <BarChart data={chartData} layout="vertical" margin={CHART_MARGIN}>
          <CartesianGrid strokeDasharray="3 3" horizontal={false} />
          <XAxis type="number" tickFormatter={(v: number) => formatValue(v)} />
          <YAxis type="category" dataKey="name" width={120} tick={{ fontSize: 11 }} />
          <Tooltip formatter={chartTooltipAmount(formatValue)} />
          <Legend />
          <Bar dataKey="budget" name="Budżet" fill={CHART_COLORS.primary} radius={[0, 4, 4, 0]} />
          <Bar dataKey="costs" name="Koszty" fill={CHART_COLORS.orange} radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default EstimateBudgetBarChart;
