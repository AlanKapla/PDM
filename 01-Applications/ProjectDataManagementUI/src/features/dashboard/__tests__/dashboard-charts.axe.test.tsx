import React from 'react';
import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import { DashboardCurrencyProvider } from '../context/DashboardCurrencyContext';
import { ChartCard } from '../components/charts/ChartCard';
import { BudgetCoverageDonut } from '../components/charts/BudgetCoverageDonut';
import { CostCategoryPieChart } from '../components/charts/CostCategoryPieChart';

function renderDashboard(ui: React.ReactElement) {
  return renderWithChakra(
    <DashboardCurrencyProvider currencySymbol="zł">{ui}</DashboardCurrencyProvider>
  );
}

describe('Dashboard charts — AXE', () => {
  it('ChartCard_brakNaruszen', async () => {
    const { container } = renderDashboard(
      <ChartCard title="Test wykresu" ariaLabel="Wykres testowy">
        <div>Wykres</div>
      </ChartCard>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('BudgetCoverageDonut_brakNaruszen', async () => {
    const { container } = renderDashboard(
      <BudgetCoverageDonut
        coveredPercent={65}
        isBudgetExceeded={false}
        totalBudget={100000}
        totalCosts={65000}
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('CostCategoryPieChart_brakNaruszen', async () => {
    const { container } = renderDashboard(
      <CostCategoryPieChart
        costByCategory={[
          {
            categoryId: 'cat-1',
            categoryName: 'Materiały',
            color: '#3182CE',
            net: 40000,
            gross: 49200,
            costsCount: 3,
          },
          {
            categoryId: null,
            categoryName: 'Bez kategorii',
            color: null,
            net: 25000,
            gross: 30750,
            costsCount: 2,
          },
        ]}
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
