import React from 'react';
import { Box, HStack, Text, useToken } from '@chakra-ui/react';
import { TimelineStatus } from '../../types/projectDashboard.types';
import type { ScheduleStageWeb, TimelineStatsWeb } from '../../types/projectDashboard.types';
import { DATE, TIMELINE_STATUS_MAP } from '../../utils/formatters';
import { TimelineStatusBadge } from '../shared/TimelineStatusBadge';
import type { FlattenedStage } from '../../utils/chartAggregations';

export interface StageGanttLiteProps {
  stages?: ScheduleStageWeb[];
  timeline?: TimelineStatsWeb | null;
  flatStages?: FlattenedStage[];
  scheduleName?: string;
}

function flattenStagesForGantt(
  stages: ScheduleStageWeb[],
  scheduleName: string
): FlattenedStage[] {
  const result: FlattenedStage[] = [];
  const walk = (items: ScheduleStageWeb[]): void => {
    for (const stage of items) {
      result.push({
        stageId: stage.stageId,
        stageName: stage.stageName,
        scheduleName,
        plannedStart: stage.timeline?.plannedStart ?? null,
        plannedEnd: stage.timeline?.plannedEnd ?? null,
        timelineStatus: stage.timelineStatus,
        delayDays: stage.timeline?.delayDays ?? null,
        progressPercent: stage.timeline?.progressPercent ?? null,
      });
      if (stage.childStages?.length) {
        walk(stage.childStages);
      }
    }
  };
  walk(stages);
  return result;
}

/**
 * Gantt-lite — wyłącznie etapy harmonogramu (bez pojedynczych prac).
 */
export function StageGanttLite({
  stages,
  timeline,
  flatStages,
  scheduleName = '',
}: StageGanttLiteProps): React.ReactElement {
  const [
    neutral100, primary500, orange600, level1500,
    amber400, neutral400, neutral600, neutral50,
  ] = useToken('colors', [
    'neutral.100', 'primary.500', 'orange.600', 'level1.500',
    'amber.400', 'neutral.400', 'neutral.600', 'neutral.50',
  ]);

  const items: FlattenedStage[] = flatStages ?? flattenStagesForGantt(stages ?? [], scheduleName);

  const rangeStart = timeline?.plannedStart
    ? new Date(timeline.plannedStart).getTime()
    : items[0]?.plannedStart
      ? new Date(items[0].plannedStart).getTime()
      : null;
  const rangeEnd = timeline?.plannedEnd
    ? new Date(timeline.plannedEnd).getTime()
    : items.length > 0 && items[items.length - 1]?.plannedEnd
      ? new Date(items[items.length - 1].plannedEnd as string).getTime()
      : null;
  const totalMs = rangeStart != null && rangeEnd != null ? rangeEnd - rangeStart : null;

  const calcBar = (start: string | null, end: string | null) => {
    if (!totalMs || rangeStart == null || !start || !end) {
      return null;
    }
    const s = new Date(start).getTime();
    const e = new Date(end).getTime();
    const left = Math.max(0, ((s - rangeStart) / totalMs) * 100);
    const width = Math.max(1, ((e - s) / totalMs) * 100);
    return { left: `${left}%`, width: `${Math.min(width, 100 - left)}%` };
  };

  const statusColors: Record<TimelineStatus, string> = {
    [TimelineStatus.NoSchedule]: neutral100,
    [TimelineStatus.NotStarted]: neutral100,
    [TimelineStatus.InProgress]: primary500,
    [TimelineStatus.Delayed]: orange600,
    [TimelineStatus.Completed]: level1500,
    [TimelineStatus.CompletedLate]: amber400,
    [TimelineStatus.NoWorkItems]: neutral100,
  };

  if (items.length === 0) {
    return (
      <Text fontSize="sm" color="neutral.400" fontStyle="italic" py={4}>
        Brak etapów do wyświetlenia
      </Text>
    );
  }

  return (
    <Box mb={2}>
      <HStack mb={2}>
        <Box w="140px" flexShrink={0} />
        <HStack flex={1} justify="space-between" fontSize="xs" color={neutral400}>
          {rangeStart != null && <Text>{DATE(timeline?.plannedStart ?? items[0]?.plannedStart)}</Text>}
          {rangeEnd != null && <Text>{DATE(timeline?.plannedEnd ?? items[items.length - 1]?.plannedEnd)}</Text>}
        </HStack>
      </HStack>

      {items.map((item) => {
        const bar = calcBar(item.plannedStart, item.plannedEnd);
        const barColor = statusColors[item.timelineStatus];
        const label = flatStages ? `${item.scheduleName} › ${item.stageName}` : item.stageName;

        return (
          <HStack key={item.stageId} mb={1} h="22px" align="center">
            <Text
              w="140px"
              flexShrink={0}
              fontSize="xs"
              color={neutral600}
              noOfLines={1}
              pr={2}
            >
              {label}
            </Text>
            <Box flex={1} position="relative" h="10px" bg={neutral50} borderRadius="sm">
              {bar && (
                <Box
                  position="absolute"
                  left={bar.left}
                  w={bar.width}
                  h="100%"
                  bg={barColor}
                  borderRadius="sm"
                />
              )}
            </Box>
            <Box w="120px" flexShrink={0} pl={2}>
              <TimelineStatusBadge status={item.timelineStatus} small />
            </Box>
          </HStack>
        );
      })}

      <HStack gap={3} mt={3} flexWrap="wrap">
        {(
          [
            TimelineStatus.Completed,
            TimelineStatus.InProgress,
            TimelineStatus.Delayed,
            TimelineStatus.NotStarted,
          ] as const
        ).map((status) => {
          const cfg = TIMELINE_STATUS_MAP[status];
          return (
            <HStack key={status} spacing={1} align="center">
              <Box w="12px" h="6px" bg={statusColors[status]} borderRadius="sm" aria-hidden="true" />
              <Text fontSize="xs" color={neutral400}>
                {cfg.label}
              </Text>
            </HStack>
          );
        })}
      </HStack>
    </Box>
  );
}

export default StageGanttLite;
