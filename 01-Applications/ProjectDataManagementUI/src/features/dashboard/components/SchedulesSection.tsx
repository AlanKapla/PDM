import React, { useState } from 'react';
import {
  Box,
  Collapse,
  HStack,
  IconButton,
  Text,
} from '@chakra-ui/react';
import { ChevronDown, ChevronUp } from 'lucide-react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';
import { useFlattenedStages } from '../hooks/useFlattenedStages';
import { WorkStatusDonut } from './charts/WorkStatusDonut';
import { ScheduleProgressBarChart } from './charts/ScheduleProgressBarChart';
import { ProjectTimelineSpan } from './charts/ProjectTimelineSpan';
import { StageGanttLite } from './charts/StageGanttLite';
import { StageDelaysChart } from './charts/StageDelaysChart';
import { ProgressSCurveChart } from './charts/ProgressSCurveChart';
import { ChartCard } from './charts/ChartCard';

export interface SchedulesSectionProps {
  data: ProjectDashboardWeb;
}

export function SchedulesSection({ data }: SchedulesSectionProps): React.ReactElement {
  const [sCurveOpen, setSCurveOpen] = useState(false);
  const { stages, delayedStages, sCurvePoints } = useFlattenedStages(data.scheduleSummaries);

  return (
    <Box as="section" mb={6} aria-label="Sekcja wykresów harmonogramów">
      <Text fontSize="md" fontWeight="semibold" color="neutral.800" mb={3}>
        Wykresy harmonogramów
      </Text>
      <div className="dashboard-schedules-grid">
        <WorkStatusDonut data={data.timelineSummary} />
        <ScheduleProgressBarChart summaries={data.scheduleSummaries} />
        <div className="dashboard-chart-row-full">
          <ProjectTimelineSpan
            data={data.timelineSummary}
            referenceDate={data.referenceDate}
          />
        </div>
        <div className="dashboard-chart-row-full">
          <ChartCard
            title="Gantt etapów (wszystkie harmonogramy)"
            isEmpty={stages.length === 0}
            emptyMessage="Brak etapów harmonogramu"
            ariaLabel="Wykres Gantt etapów harmonogramu"
            fullWidth
          >
            <StageGanttLite flatStages={stages} />
          </ChartCard>
        </div>
        <div className="dashboard-chart-row-full">
          <StageDelaysChart stages={delayedStages} />
        </div>
        <div className="dashboard-chart-row-full">
          <Box
            border="0.5px solid"
            borderColor="neutral.200"
            borderRadius="12px"
            bg="white"
            overflow="hidden"
          >
            <HStack
              px={4}
              py={3}
              justify="space-between"
              cursor="pointer"
              onClick={() => setSCurveOpen((prev) => !prev)}
              role="button"
              tabIndex={0}
              aria-expanded={sCurveOpen}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  setSCurveOpen((prev) => !prev);
                }
              }}
            >
              <Text fontSize="sm" fontWeight="medium" color="neutral.800">
                Krzywa postępu (S-curve)
              </Text>
              <IconButton
                aria-label={sCurveOpen ? 'Zwiń wykres S-curve' : 'Rozwiń wykres S-curve'}
                icon={sCurveOpen ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                size="xs"
                variant="ghost"
                onClick={(e) => {
                  e.stopPropagation();
                  setSCurveOpen((prev) => !prev);
                }}
              />
            </HStack>
            <Collapse in={sCurveOpen} animateOpacity>
              <Box px={4} pb={4}>
                <ProgressSCurveChart points={sCurvePoints} embedded />
              </Box>
            </Collapse>
          </Box>
        </div>
      </div>
    </Box>
  );
}

export default SchedulesSection;
