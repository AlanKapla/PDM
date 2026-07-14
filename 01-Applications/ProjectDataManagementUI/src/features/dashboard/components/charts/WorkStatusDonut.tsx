import React, { useMemo } from 'react';
import { Box, Text } from '@chakra-ui/react';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip, Legend } from 'recharts';
import { CHART_HEIGHT, CHART_PALETTE } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import type { ProjectTimelineSummaryWeb } from '../../types/projectDashboard.types';

export interface WorkStatusDonutProps {
  data: ProjectTimelineSummaryWeb;
}

export function WorkStatusDonut({ data }: WorkStatusDonutProps): React.ReactElement {
  const chartData = useMemo(
    () =>
      [
        { name: 'Ukończone', value: data.completedCount },
        { name: 'Ukończone późno', value: data.completedLateCount },
        { name: 'W toku', value: data.inProgressCount },
        { name: 'Opóźnione', value: data.delayedCount },
        { name: 'Nie rozpoczęto', value: data.notStartedCount },
      ].filter((item) => item.value > 0),
    [data]
  );

  const total = data.totalWorkCount;

  return (
    <ChartCard
      title="Rozkład statusów prac"
      isEmpty={total === 0}
      emptyMessage="Brak zakresów pracy w harmonogramach"
      ariaLabel="Wykres rozkładu statusów prac"
    >
      <Box position="relative" h={`${CHART_HEIGHT}px`}>
        <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
          <PieChart>
            <Pie data={chartData} dataKey="value" nameKey="name" cx="50%" cy="50%" innerRadius={55} outerRadius={90}>
              {chartData.map((entry, index) => (
                <Cell key={entry.name} fill={CHART_PALETTE[index % CHART_PALETTE.length]} />
              ))}
            </Pie>
            <Tooltip />
            <Legend />
          </PieChart>
        </ResponsiveContainer>
        <Box position="absolute" top="50%" left="50%" transform="translate(-50%, -50%)" textAlign="center">
          <Text fontSize="lg" fontWeight="bold" color="neutral.700">
            {total}
          </Text>
          <Text fontSize="xs" color="neutral.400">
            zakresów
          </Text>
        </Box>
      </Box>
    </ChartCard>
  );
}

export default WorkStatusDonut;
