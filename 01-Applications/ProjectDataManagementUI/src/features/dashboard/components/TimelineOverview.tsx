import React from 'react';
import { Box, Text } from '@chakra-ui/react';
import { TimelineStatus } from '../types/projectDashboard.types';
import type { ProjectTimelineSummaryWeb } from '../types/projectDashboard.types';
import { PROG, DAYS, TIMELINE_STATUS_MAP } from '../utils/formatters';
import { KpiCard } from './shared/KpiCard';
import { MiniProgressBar } from './shared/MiniProgressBar';
import { TimelineStatusBadge } from './shared/TimelineStatusBadge';
import { Badge } from './shared/Badge';

export interface TimelineOverviewProps {
  data: ProjectTimelineSummaryWeb;
}

export function TimelineOverview({ data }: TimelineOverviewProps): React.ReactElement {
  const progressColor = (() => {
    switch (data.overallStatus) {
      case TimelineStatus.Completed:
      case TimelineStatus.CompletedLate:
        return 'level1.500';
      case TimelineStatus.Delayed:
        return 'orange.600';
      default:
        return 'primary.500';
    }
  })();

  return (
    <Box
      bg="white"
      borderWidth="2px"
      borderColor="neutral.200"
      borderRadius="xl"
      p={{ base: 4, md: 5 }}
      w="100%"
    >
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Text fontSize="sm" fontWeight="medium" color="neutral.800">
          Postęp projektu
        </Text>
        <TimelineStatusBadge status={data.overallStatus} small />
      </Box>

      <Box display="grid" gridTemplateColumns="1fr 1fr" gap={3} mb={3}>
        <KpiCard
          label="Opóźnione"
          value={String(data.delayedCount)}
          colorScheme={data.delayedCount > 0 ? 'red' : 'gray'}
          small
        />
        <KpiCard label="W toku" value={String(data.inProgressCount)} colorScheme="primary" small />
        <KpiCard
          label="Czas projektu"
          value={DAYS(data.totalPlannedDays ?? null)}
          colorScheme="purple"
          small
        />
      </Box>

      <MiniProgressBar percent={data.progressPercent} color={progressColor} height={8} />
      <Text fontSize="xs" color="neutral.600" mt={1} mb={3}>
        {PROG(data.progressPercent)} ukończenia
      </Text>

      <Box display="flex" flexWrap="wrap" gap={1} mb={3}>
        {(
          [
            { label: `Ukończone ${data.completedCount}`, status: TimelineStatus.Completed },
            { label: `W toku ${data.inProgressCount}`, status: TimelineStatus.InProgress },
            { label: `Opóźnione ${data.delayedCount}`, status: TimelineStatus.Delayed },
            { label: `Nie rozpoczęto ${data.notStartedCount}`, status: TimelineStatus.NotStarted },
          ] as const
        ).map(({ label, status }) => {
          const cfg = TIMELINE_STATUS_MAP[status];
          return <Badge key={label} text={label} bg={cfg.bg} color={cfg.color} small />;
        })}
      </Box>

      <Text fontSize="xs" color="neutral.600">
        Harmonogramów: {data.workSchedulesCount}
        {data.activeSchedulesCount > 0 && (
          <Text as="span" ml={2}>
            (aktywnych: {data.activeSchedulesCount})
          </Text>
        )}
      </Text>
    </Box>
  );
}

export default TimelineOverview;
