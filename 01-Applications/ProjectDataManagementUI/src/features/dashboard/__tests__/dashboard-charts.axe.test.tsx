import React from 'react';
import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import { DashboardCurrencyProvider } from '../context/DashboardCurrencyContext';
import { ChartCard } from '../components/charts/ChartCard';
import { BudgetCoverageDonut } from '../components/charts/BudgetCoverageDonut';

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
});
