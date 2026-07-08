import React from 'react';
import { axe } from 'vitest-axe';
import { Box, TabPanel } from '@chakra-ui/react';
import { renderWithChakra } from '../../../test/render-with-chakra';
import { DashboardCurrencyProvider } from '../context/DashboardCurrencyContext';
import { DashboardHeader } from '../components/DashboardHeader';
import { DashboardMainTabs } from '../components/DashboardMainTabs';
import { FinanceTab } from '../components/tabs/FinanceTab';
import { CostsTab } from '../components/tabs/CostsTab';
import {
  FinancialStatus,
  TimelineStatus,
  type ProjectDashboardWeb,
  type ProjectFinancialSummaryWeb,
  type ProjectTimelineSummaryWeb,
  type TrackedCostWeb,
} from '../types/projectDashboard.types';

vi.mock('../../../hooks/useProjectPermissions', () => ({
  useProjectPermissions: () => ({ canViewEstimates: true }),
}));

vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
}));

const mockFinancialSummary: ProjectFinancialSummaryWeb = {
  totalBudgetNet: 100000,
  totalBudgetGross: 123000,
  estimateBudgetNet: 100000,
  estimateBudgetGross: 123000,
  projectReserveBudgetNet: 0,
  projectReserveBudgetGross: 0,
  totalCostsNet: 65000,
  totalCostsGross: 79950,
  linkedCostsNet: 65000,
  linkedCostsGross: 79950,
  additionalCostsNet: 0,
  additionalCostsGross: 0,
  deviationNet: 35000,
  deviationGross: 43050,
  deviationPercent: 35,
  coveredPercent: 65,
  isBudgetExceeded: false,
  financialStatus: FinancialStatus.InProgress,
  totalCostCount: 2,
  linkedCostCount: 2,
  additionalCostCount: 0,
  costEstimatesCount: 1,
  costEstimatesWithCostsCount: 1,
  costEstimatesOverBudgetCount: 0,
  workSchedulesCount: 1,
  scheduleCostSummary: {
    totalSchedulesCostsNet: 0,
    totalSchedulesCostsGross: 0,
    schedulesWithCostsCount: 0,
    schedulesWithoutCostsCount: 1,
  },
};

const mockTimelineSummary: ProjectTimelineSummaryWeb = {
  earliestStart: '2026-01-01',
  latestEnd: '2026-12-31',
  totalPlannedDays: 365,
  totalWorkCount: 10,
  completedCount: 4,
  completedLateCount: 0,
  inProgressCount: 3,
  notStartedCount: 3,
  delayedCount: 0,
  progressPercent: 40,
  delayDays: 0,
  overallStatus: TimelineStatus.InProgress,
  isDelayed: false,
  isCompleted: false,
  workSchedulesCount: 1,
  activeSchedulesCount: 1,
  completedSchedulesCount: 0,
};

const mockData: ProjectDashboardWeb = {
  projectId: 'project-1',
  referenceDate: '2026-07-07',
  generatedAt: '2026-07-07T10:00:00Z',
  financialSummary: mockFinancialSummary,
  timelineSummary: mockTimelineSummary,
  costEstimateSummaries: [],
  scheduleSummaries: [],
  projectAdditionalCosts: {
    totalNet: 0,
    totalGross: 0,
    costsCount: 0,
    costs: [],
  },
  allCosts: [],
  costByCategory: [],
};

function makeCost(id: string, name: string): TrackedCostWeb {
  return {
    id,
    costEstimateItemId: null,
    workScheduleStageWorkId: null,
    isAdditional: true,
    name,
    description: null,
    net: 1000,
    gross: 1230,
    vatRate: 23,
    contractorId: null,
    contractorName: 'Wykonawca Sp. z o.o.',
    categoryId: null,
    categoryName: null,
    categoryColor: null,
    date: '2026-06-01',
    number: `FV/${id}`,
    attachments: [],
    createdAt: '2026-06-01T10:00:00Z',
    updatedAt: null,
    sourceType: 'ProjectAdditional',
    scheduleName: null,
    stageName: null,
    workItemName: null,
    estimateName: null,
    estimateGroupName: null,
    estimateItemName: null,
    costEstimateItemPath: null,
    workScheduleWorkPath: null,
  };
}

function renderDashboard(ui: React.ReactElement) {
  return renderWithChakra(
    <DashboardCurrencyProvider currencySymbol="zł">{ui}</DashboardCurrencyProvider>
  );
}

describe('Dashboard main — AXE', () => {
  it('DashboardHeader_brakNaruszen', async () => {
    const { container } = renderDashboard(<DashboardHeader data={mockData} />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('DashboardMainTabs_brakNaruszen', async () => {
    const { container } = renderDashboard(
      <DashboardMainTabs
        tabIndex={0}
        onTabChange={() => undefined}
        estimatesCount={1}
        schedulesCount={1}
        costsCount={2}
      >
        <TabPanel><Box /></TabPanel>
        <TabPanel><Box /></TabPanel>
        <TabPanel><Box /></TabPanel>
        <TabPanel><Box /></TabPanel>
      </DashboardMainTabs>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('FinanceTab_brakNaruszen', async () => {
    const { container } = renderDashboard(
      <FinanceTab
        data={mockData}
        tenantId="tenant-1"
        projectId="project-1"
        onRefetch={() => undefined}
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('CostsTab_brakNaruszen_pustaLista', async () => {
    const { container } = renderDashboard(
      <CostsTab
        costs={[]}
        tenantId="tenant-1"
        projectId="project-1"
        onRefetch={() => undefined}
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('CostsTab_brakNaruszen_zKosztami', async () => {
    const { container } = renderDashboard(
      <CostsTab
        costs={[makeCost('1', 'Koszt A'), makeCost('2', 'Koszt B')]}
        tenantId="tenant-1"
        projectId="project-1"
        onRefetch={() => undefined}
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
