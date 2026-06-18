import React from 'react';
import { Box, HStack, Text, useToken } from '@chakra-ui/react';
import { CHART_COLORS } from '../../utils/chartTheme';
import { ChartCard } from './ChartCard';
import { DATE } from '../../utils/formatters';
import type { ProjectTimelineSummaryWeb } from '../../types/projectDashboard.types';

export interface ProjectTimelineSpanProps {
  data: ProjectTimelineSummaryWeb;
  referenceDate: string;
}

export function ProjectTimelineSpan({
  data,
  referenceDate,
}: ProjectTimelineSpanProps): React.ReactElement {
  const [neutral50, neutral400, primary500, orange600] = useToken('colors', [
    'neutral.50', 'neutral.400', 'primary.500', 'orange.600',
  ]);

  const rangeStart = data.earliestStart ? new Date(data.earliestStart).getTime() : null;
  const rangeEnd = data.latestEnd ? new Date(data.latestEnd).getTime() : null;
  const refTime = new Date(referenceDate).getTime();
  const totalMs = rangeStart != null && rangeEnd != null ? rangeEnd - rangeStart : null;

  const refPosition =
    totalMs != null && rangeStart != null && !Number.isNaN(refTime)
      ? Math.min(100, Math.max(0, ((refTime - rangeStart) / totalMs) * 100))
      : null;

  const isEmpty = rangeStart == null || rangeEnd == null;

  return (
    <ChartCard
      title="Oś czasu projektu"
      isEmpty={isEmpty}
      emptyMessage="Brak zdefiniowanego zakresu czasowego"
      ariaLabel="Oś czasu projektu od planowanego startu do końca"
      fullWidth
    >
      <HStack justify="space-between" mb={2}>
        <Text fontSize="xs" color="neutral.500">
          Start: {DATE(data.earliestStart)}
        </Text>
        <Text fontSize="xs" color="neutral.500">
          Koniec: {DATE(data.latestEnd)}
        </Text>
      </HStack>
      <Box position="relative" h="24px" bg={neutral50} borderRadius="md" overflow="hidden">
        <Box
          position="absolute"
          left={0}
          top={0}
          h="100%"
          w="100%"
          bg={CHART_COLORS.primaryLight}
          borderRadius="md"
        />
        {refPosition != null && (
          <Box
            position="absolute"
            left={`${refPosition}%`}
            top={0}
            h="100%"
            w="2px"
            bg={primary500}
            zIndex={2}
            aria-hidden="true"
          />
        )}
      </Box>
      <HStack justify="space-between" mt={2}>
        <Text fontSize="xs" color="neutral.400">
          Czas planowany: {data.totalPlannedDays != null ? `${Math.round(data.totalPlannedDays)} dni` : '—'}
        </Text>
        {data.delayDays != null && data.delayDays > 0 && (
          <Text fontSize="xs" color={orange600} fontWeight="medium">
            Opóźnienie: {Math.round(data.delayDays)} dni
          </Text>
        )}
        {refPosition != null && (
          <Text fontSize="xs" color={neutral400}>
            ↕ data ref.
          </Text>
        )}
      </HStack>
    </ChartCard>
  );
}

export default ProjectTimelineSpan;
