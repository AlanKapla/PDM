import React from 'react';
import { Box, SimpleGrid, Text } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { NetGrossAmount } from './shared/NetGrossAmount';
import { DashboardSummaryCard } from './shared/DashboardSummaryCard';
import { FINANCIAL_STATUS_CONFIG, TIMELINE_STATUS_CONFIG } from '../utils/formatters';

export interface DashboardHeaderProps {
  data: ProjectDashboardWeb;
}

export function DashboardHeader({ data }: DashboardHeaderProps): React.ReactElement {
  const { financialSummary, timelineSummary } = data;
  const additionalNet = financialSummary.additionalCostsNet;
  const additionalGross = financialSummary.additionalCostsGross;
  const showAdditional = additionalNet != null && additionalNet > 0;

  const financialStatus = FINANCIAL_STATUS_CONFIG(financialSummary.financialStatus);
  const timelineStatus = TIMELINE_STATUS_CONFIG(timelineSummary.overallStatus);

  return (
    <Box as="header" mb={6} w="100%">
      <SimpleGrid
        className="dashboard-summary-grid"
        columns={{ base: 1, sm: 2, lg: showAdditional ? 5 : 4 }}
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

        {showAdditional && (
          <DashboardSummaryCard label="Koszty dodatkowe" accentColor="amber.500">
            <NetGrossAmount
              net={additionalNet}
              gross={additionalGross}
              size="md"
              align="left"
              accentColor="amber.700"
            />
          </DashboardSummaryCard>
        )}

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
