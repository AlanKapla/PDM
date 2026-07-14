import React from 'react';
import { Box, Text } from '@chakra-ui/react';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import { CHART_COLORS, CHART_HEIGHT } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { chartTooltipAmount, chartTooltipPercent } from '../../utils/chartTooltip';
import { useChartAmount } from '../../hooks/useChartAmount';

export interface BudgetCoverageDonutProps {
  coveredPercent: number | null;
  isBudgetExceeded: boolean;
  totalBudget: number | null;
  totalCosts: number | null;
}

export function BudgetCoverageDonut({
  coveredPercent,
  isBudgetExceeded,
  totalBudget,
  totalCosts,
}: BudgetCoverageDonutProps): React.ReactElement {
  const { formatValue } = useChartAmount();
  const pct = Math.min(Math.max(coveredPercent ?? 0, 0), 100);
  const remaining = Math.max(0, 100 - pct);

  const data = [
    { name: 'Wykorzystane', value: pct },
    { name: 'Pozostało', value: remaining },
  ];

  const fill = isBudgetExceeded ? CHART_COLORS.red : pct >= 80 ? CHART_COLORS.orange : CHART_COLORS.level1;
  const isEmpty = totalBudget == null || totalBudget <= 0;

  return (
    <ChartCard
      title="Pokrycie budżetu"
      isEmpty={isEmpty}
      emptyMessage="Brak zdefiniowanego budżetu"
      ariaLabel={`Pokrycie budżetu: ${Math.round(pct)} procent`}
    >
      <Box position="relative" h={`${CHART_HEIGHT}px`}>
        <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
          <PieChart>
            <Pie
              data={data}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              innerRadius={60}
              outerRadius={90}
              startAngle={90}
              endAngle={-270}
            >
              <Cell fill={fill} />
              <Cell fill={CHART_COLORS.neutralLight} />
            </Pie>
            <Tooltip formatter={chartTooltipPercent} />
          </PieChart>
        </ResponsiveContainer>
        <Box position="absolute" top="50%" left="50%" transform="translate(-50%, -50%)" textAlign="center">
          <Text fontSize="xl" fontWeight="bold" color={fill}>
            {Math.round(pct)}%
          </Text>
        </Box>
      </Box>
      <Text fontSize="xs" color="neutral.500" textAlign="center" mt={2}>
        Koszty: {formatValue(totalCosts)} / {formatValue(totalBudget)}
      </Text>
    </ChartCard>
  );
}

export default BudgetCoverageDonut;
