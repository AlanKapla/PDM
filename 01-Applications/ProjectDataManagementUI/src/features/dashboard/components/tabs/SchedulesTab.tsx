import React from 'react';
import { Box, Text } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../../types/projectDashboard.types';
import { DAYS } from '../../utils/formatters';
import { KpiCard } from '../shared/KpiCard';
import { SchedulesSection } from '../SchedulesSection';
import { ScheduleBlock } from './ScheduleBlock';

export interface SchedulesTabProps {
  data: ProjectDashboardWeb;
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

export function SchedulesTab({
  data,
  projectId,
}: SchedulesTabProps): React.ReactElement {
  const { scheduleSummaries, timelineSummary, financialSummary } = data;
  const scs = financialSummary.scheduleCostSummary;

  const scheduleCostNet = scheduleSummaries.reduce(
    (sum, s) => sum + (s.costsNet ?? s.totalCostsNet ?? 0),
    0,
  );
  const scheduleCostGross = scheduleSummaries.reduce(
    (sum, s) => sum + (s.costsGross ?? s.totalCostsGross ?? 0),
    0,
  );
  const totalSchedulesCostsNet = scs?.totalSchedulesCostsNet || scheduleCostNet || null;
  const totalSchedulesCostsGross = scs?.totalSchedulesCostsGross || scheduleCostGross || null;

  return (
    <Box w="100%">
      <Box className="dashboard-kpi-grid" mb={6}>
        <KpiCard
          label="Łączne koszty harmonogramów"
          netValue={totalSchedulesCostsNet}
          grossValue={totalSchedulesCostsGross}
          colorScheme="orange"
        />
        <KpiCard label="Harmonogramów" value={String(scheduleSummaries.length)} colorScheme="level2" />
        <KpiCard label="Zakresów łącznie" value={String(timelineSummary.totalWorkCount)} colorScheme="primary" />
        <KpiCard
          label="Opóźnione zakresy"
          value={String(timelineSummary.delayedCount)}
          colorScheme={timelineSummary.delayedCount > 0 ? 'red' : 'gray'}
        />
        <KpiCard label="W toku" value={String(timelineSummary.inProgressCount)} colorScheme="primary" />
        <KpiCard label="Nie rozpoczęto" value={String(timelineSummary.notStartedCount)} colorScheme="gray" />
        <KpiCard label="Ukończono" value={String(timelineSummary.completedCount)} colorScheme="green" />
        <KpiCard
          label="Czas projektu"
          value={timelineSummary.totalPlannedDays != null ? DAYS(timelineSummary.totalPlannedDays) : '—'}
          colorScheme="purple"
        />
      </Box>

      <Box as="section" aria-label="Harmonogramy" mb={6}>
        <Text fontSize="md" fontWeight="semibold" color="neutral.800" mb={3}>
          Harmonogramy
        </Text>
        <Box display="flex" flexDirection="column" gap={2}>
          {scheduleSummaries.map((summary) => (
            <ScheduleBlock
              key={summary.workScheduleId}
              summary={summary}
              projectId={projectId}
            />
          ))}
          {scheduleSummaries.length === 0 && (
            <Text fontSize="sm" color="neutral.600" fontStyle="italic" p={3}>
              Brak powiązanych harmonogramów
            </Text>
          )}
        </Box>
      </Box>

      <SchedulesSection data={data} />
    </Box>
  );
}

export default SchedulesTab;
