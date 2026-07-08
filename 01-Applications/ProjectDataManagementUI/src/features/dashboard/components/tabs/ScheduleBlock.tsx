import React from 'react';
import { Box, HStack, VStack, Text, useToken } from '@chakra-ui/react';
import { useNavigate } from 'react-router-dom';
import type { ScheduleSummaryWeb } from '../../types/projectDashboard.types';
import { DATE, DAYS } from '../../utils/formatters';
import { TimelineStatusBadge } from '../shared/TimelineStatusBadge';
import { NetGrossAmount } from '../shared/NetGrossAmount';
import { Badge } from '../shared/Badge';
import { useProjectPermissions } from '../../../../hooks/useProjectPermissions';

export interface ScheduleBlockProps {
  summary: ScheduleSummaryWeb;
  projectId: string;
}

/**
 * Karta harmonogramu — nazwa, podstawowe informacje i koszty podpięte pod
 * harmonogram. Klikalna (przejście do harmonogramu) tylko gdy użytkownik ma
 * uprawnienie do harmonogramów.
 */
export function ScheduleBlock({
  summary,
  projectId,
}: ScheduleBlockProps): React.ReactElement {
  const navigate = useNavigate();
  const { canViewSchedule } = useProjectPermissions(projectId);
  const [primary50, primary600, level250, level2600, orange600] = useToken('colors', [
    'primary.50', 'primary.600', 'level2.50', 'level2.600', 'orange.600',
  ]);

  const handleOpen = (): void => {
    if (!canViewSchedule) {
      return;
    }
    navigate(`/projects/${projectId}/schedules/${summary.workScheduleId}`);
  };

  const handleKeyDown = (event: React.KeyboardEvent): void => {
    if (!canViewSchedule) {
      return;
    }
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      handleOpen();
    }
  };

  const costsNet = summary.costsNet ?? summary.totalCostsNet;
  const costsGross = summary.costsGross ?? summary.totalCostsGross;
  const hasCosts = costsNet != null || costsGross != null || summary.costCount > 0;

  return (
    <Box
      role={canViewSchedule ? 'button' : undefined}
      tabIndex={canViewSchedule ? 0 : undefined}
      aria-label={canViewSchedule ? `Otwórz harmonogram ${summary.workScheduleName}` : undefined}
      onClick={canViewSchedule ? handleOpen : undefined}
      onKeyDown={canViewSchedule ? handleKeyDown : undefined}
      cursor={canViewSchedule ? 'pointer' : 'default'}
      borderWidth="2px"
      borderColor="neutral.200"
      borderRadius="xl"
      bg="white"
      px={4}
      py={3}
      _hover={canViewSchedule ? { bg: 'neutral.50' } : undefined}
    >
      <HStack justify="space-between" align="flex-start" spacing={3} mb={2} flexWrap="wrap">
        <VStack align="flex-start" spacing={1} flex={1} minW={0}>
          <Text fontSize="sm" fontWeight="semibold" color="neutral.800" noOfLines={1}>
            {summary.workScheduleName}
          </Text>
          <HStack spacing={2} flexWrap="wrap">
            <Badge
              text={`${summary.totalWorkItemsCount ?? '?'} zakresów`}
              bg={primary50}
              color={primary600}
              small
            />
            {summary.hasLinkedEstimate && (
              <Badge text="Z kosztorysem" bg={level250} color={level2600} small />
            )}
            <TimelineStatusBadge status={summary.timelineStatus} small />
          </HStack>
        </VStack>
        {hasCosts && (
          <Box flexShrink={0}>
            <NetGrossAmount
              net={costsNet}
              gross={costsGross}
              size="sm"
              align="right"
              accentColor={orange600}
            />
          </Box>
        )}
      </HStack>

      {summary.timeline != null && (
        <HStack spacing={4} fontSize="xs" color="neutral.600" flexWrap="wrap">
          <Text>Start: {DATE(summary.timeline.plannedStart)}</Text>
          <Text>Koniec: {DATE(summary.timeline.plannedEnd)}</Text>
          <Text>Czas: {DAYS(summary.timeline.totalPlannedDays)}</Text>
          <Text>
            Zakresów z kosztami: {summary.workItemsWithCostsCount} / {summary.totalWorkItemsCount}
          </Text>
        </HStack>
      )}
    </Box>
  );
}

export default ScheduleBlock;
