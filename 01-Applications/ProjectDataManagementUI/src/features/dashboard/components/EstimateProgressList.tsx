import React from 'react';
import { Box, HStack, Text, VStack, useToken } from '@chakra-ui/react';
import type { CostEstimateSummaryWeb } from '../types/projectDashboard.types';
import { NetGrossAmount } from './shared/NetGrossAmount';
import { MiniProgressBar } from './shared/MiniProgressBar';
import { Badge } from './shared/Badge';
import { TimelineStatusBadge } from './shared/TimelineStatusBadge';

export interface EstimateProgressListProps {
  summaries: CostEstimateSummaryWeb[];
  onSelect: (estimateId: string) => void;
  /** Czy kosztorys można otworzyć (uprawnienie do kosztorysów). Domyślnie true. */
  canOpen?: boolean;
}

interface EstimateProgressRowProps {
  summary: CostEstimateSummaryWeb;
  onSelect: (estimateId: string) => void;
  canOpen: boolean;
  accentColor: string;
  warningColor: string;
  countBg: string;
  countColor: string;
  noScheduleBg: string;
  noScheduleColor: string;
}

function computePercent(summary: CostEstimateSummaryWeb): number | null {
  if (summary.budgetNet == null || summary.budgetNet === 0) {
    return null;
  }
  return ((summary.costsNet ?? 0) / summary.budgetNet) * 100;
}

function EstimateProgressRow({
  summary,
  onSelect,
  canOpen,
  accentColor,
  warningColor,
  countBg,
  countColor,
  noScheduleBg,
  noScheduleColor,
}: EstimateProgressRowProps): React.ReactElement {
  const percent = computePercent(summary);
  const hasBudget = summary.budgetNet != null && summary.budgetNet !== 0;
  const barColor = hasBudget ? accentColor : warningColor;

  const handleKeyDown = (event: React.KeyboardEvent): void => {
    if (!canOpen) {
      return;
    }
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      onSelect(summary.costEstimateId);
    }
  };

  return (
    <Box
      role={canOpen ? 'button' : undefined}
      tabIndex={canOpen ? 0 : undefined}
      aria-label={canOpen ? `Otwórz kosztorys ${summary.costEstimateName}` : undefined}
      onClick={canOpen ? () => onSelect(summary.costEstimateId) : undefined}
      onKeyDown={canOpen ? handleKeyDown : undefined}
      cursor={canOpen ? 'pointer' : 'default'}
      borderWidth="2px"
      borderColor="neutral.200"
      borderRadius="xl"
      bg="white"
      px={4}
      py={3}
      _hover={canOpen ? { bg: 'neutral.50' } : undefined}
    >
      <HStack justify="space-between" align="flex-start" spacing={3} mb={2}>
        <VStack align="flex-start" spacing={1} flex={1} minW={0}>
          <Text fontSize="sm" fontWeight="semibold" color="neutral.800" noOfLines={1}>
            {summary.costEstimateName}
          </Text>
          <HStack spacing={2}>
            <Badge
              text={`${summary.totalItemsCount} poz.`}
              bg={countBg}
              color={countColor}
              small
            />
            {summary.hasLinkedSchedule ? (
              <TimelineStatusBadge status={summary.timelineStatus} small />
            ) : (
              <Badge
                text="Bez harmonogramu"
                bg={noScheduleBg}
                color={noScheduleColor}
                small
              />
            )}
          </HStack>
        </VStack>
        <HStack spacing={1.5} align="baseline" flexShrink={0}>
          <NetGrossAmount
            net={summary.costsNet}
            gross={summary.costsGross}
            size="sm"
            align="right"
            accentColor="orange.600"
          />
          <Text fontSize="xs" color="neutral.400">
            /
          </Text>
          <NetGrossAmount
            net={summary.budgetNet}
            gross={summary.budgetGross}
            size="sm"
            align="right"
          />
        </HStack>
      </HStack>
      <MiniProgressBar percent={percent} color={barColor} exceeded={summary.isBudgetExceeded} />
    </Box>
  );
}

/**
 * Lista kosztorysów z paskiem postępu budżetu. Zastępuje wykresy budżet/koszt
 * oraz dolny akordeon — koszty/budżet renderowane tylko raz. Wiersz klikalny
 * prowadzi do szczegółów kosztorysu.
 */
export function EstimateProgressList({
  summaries,
  onSelect,
  canOpen = true,
}: EstimateProgressListProps): React.ReactElement {
  const [accentColor, warningColor, countBg, countColor, noScheduleBg, noScheduleColor] = useToken(
    'colors',
    ['level1.500', 'amber.500', 'level2.100', 'level2.600', 'neutral.50', 'neutral.600']
  );

  if (summaries.length === 0) {
    return (
      <Text fontSize="sm" color="neutral.600" fontStyle="italic" p={3}>
        Brak powiązanych kosztorysów.
      </Text>
    );
  }

  return (
    <VStack align="stretch" spacing={2}>
      {summaries.map((summary) => (
        <EstimateProgressRow
          key={summary.costEstimateId}
          summary={summary}
          onSelect={onSelect}
          canOpen={canOpen}
          accentColor={accentColor}
          warningColor={warningColor}
          countBg={countBg}
          countColor={countColor}
          noScheduleBg={noScheduleBg}
          noScheduleColor={noScheduleColor}
        />
      ))}
    </VStack>
  );
}

export default EstimateProgressList;
