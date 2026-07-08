import React, { useMemo } from 'react';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip, Legend } from 'recharts';
import { CHART_COLORS, CHART_HEIGHT, CHART_PALETTE } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { chartTooltipAmount } from '../../utils/chartTooltip';
import { useChartAmount } from '../../hooks/useChartAmount';
import type { CostByCategoryWeb } from '../../types/projectDashboard.types';

const UNCATEGORIZED_LABEL = 'Bez kategorii';

export interface CostCategoryPieChartProps {
  costByCategory: CostByCategoryWeb[];
  title?: string;
}

interface ChartSegment {
  name: string;
  value: number;
  color: string;
  costsCount: number;
}

function resolveSegmentColor(
  item: CostByCategoryWeb,
  paletteIndex: number
): string {
  if (item.color) {
    return item.color;
  }
  if (item.categoryId === null) {
    return CHART_COLORS.neutral;
  }
  return CHART_PALETTE[paletteIndex % CHART_PALETTE.length];
}

export function CostCategoryPieChart({
  costByCategory,
  title = 'Koszty wg kategorii',
}: CostCategoryPieChartProps): React.ReactElement {
  const { formatValue } = useChartAmount();

  const data = useMemo((): ChartSegment[] => {
    let paletteIndex = 0;
    return costByCategory
      .filter((item) => item.net > 0 || (item.gross ?? 0) > 0)
      .map((item) => {
        const displayName =
          item.categoryId === null ? UNCATEGORIZED_LABEL : item.categoryName;
        const color =
          item.categoryId === null
            ? CHART_COLORS.neutral
            : resolveSegmentColor(item, paletteIndex++);
        return {
          name: displayName,
          value: item.net,
          color,
          costsCount: item.costsCount,
        };
      });
  }, [costByCategory]);

  return (
    <ChartCard
      title={title}
      isEmpty={data.length === 0}
      emptyMessage="Brak kosztów do wyświetlenia wg kategorii"
      ariaLabel={`Wykres: ${title}`}
      fullWidth
    >
      <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
        <PieChart>
          <Pie
            data={data}
            dataKey="value"
            nameKey="name"
            cx="50%"
            cy="50%"
            innerRadius={55}
            outerRadius={90}
          >
            {data.map((entry) => (
              <Cell key={entry.name} fill={entry.color} />
            ))}
          </Pie>
          <Tooltip
            formatter={chartTooltipAmount(formatValue)}
            labelFormatter={(label, payload) => {
              const costsCount: number | undefined = payload?.[0]?.payload?.costsCount;
              if (costsCount != null) {
                return `${label} (${costsCount} kosztów)`;
              }
              return String(label);
            }}
          />
          <Legend />
        </PieChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

export default CostCategoryPieChart;
