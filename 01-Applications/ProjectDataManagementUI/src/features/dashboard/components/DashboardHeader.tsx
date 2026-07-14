import React from 'react';
import { Box, SimpleGrid, Text } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { NetGrossAmount } from './shared/NetGrossAmount';
import { DashboardSummaryCard } from './shared/DashboardSummaryCard';
import { FINANCIAL_STATUS_CONFIG, TIMELINE_STATUS_CONFIG, PROG } from '../utils/formatters';

export interface DashboardHeaderProps {
  data: ProjectDashboardWeb;
}

export function DashboardHeader({ data }: DashboardHeaderProps): React.ReactElement {
  const { financialSummary, timelineSummary } = data;

  const financialStatus = FINANCIAL_STATUS_CONFIG(financialSummary.financialStatus);
  const timelineStatus = TIMELINE_STATUS_CONFIG(timelineSummary.overallStatus);

  return (
    <Box as="header" mb={6} w="100%">
      <SimpleGrid
        className="dashboard-summary-grid"
        columns={{ base: 1, sm: 2, lg: 5 }}
        spacing={3}
        w="100%"
      >
        <DashboardSummaryCard label="Budżet łączny" accentColor="primary.500">
          <NetGrossAmount
            net={financialSummary.totalBudgetNet}
            gross={financialSummary.totalBudgetGross}
            size="md"
            align="left"
            accentColor="primary.700"
          />
        </DashboardSummaryCard>

        <DashboardSummaryCard label="Koszty łączne" accentColor="orange.500">
          <NetGrossAmount
            net={financialSummary.totalCostsNet}
            gross={financialSummary.totalCostsGross}
            size="md"
            align="left"
            accentColor="orange.700"
          />
        </DashboardSummaryCard>

        <DashboardSummaryCard label="Postęp prac" accentColor="level1.500">
          <Text fontSize={{ base: 'lg', md: 'xl' }} fontWeight="semibold" color="level1.700">
            {PROG(timelineSummary.progressPercent)}
          </Text>
          <Text fontSize="xs" color="neutral.600" mt={1}>
            {timelineSummary.completedCount} z {timelineSummary.totalWorkCount} zakończone
          </Text>
        </DashboardSummaryCard>

        <DashboardSummaryCard label="Status finansowy" accentColor={financialStatus.color}>
          <Box
            display="inline-flex"
            alignItems="center"
            px={3}
            py={1.5}
            borderRadius="md"
            bg={financialStatus.bg}
            alignSelf="flex-start"
          >
            <Text fontSize="md" fontWeight="semibold" color={financialStatus.color}>
              {financialStatus.label}
            </Text>
          </Box>
        </DashboardSummaryCard>

        <DashboardSummaryCard label="Status harmonogramu" accentColor={timelineStatus.color}>
          <Box
            display="inline-flex"
            alignItems="center"
            px={3}
            py={1.5}
            borderRadius="md"
            bg={timelineStatus.bg}
            alignSelf="flex-start"
          >
            <Text fontSize="md" fontWeight="semibold" color={timelineStatus.color}>
              {timelineStatus.label}
            </Text>
          </Box>
        </DashboardSummaryCard>
      </SimpleGrid>
    </Box>
  );
}

export default DashboardHeader;
