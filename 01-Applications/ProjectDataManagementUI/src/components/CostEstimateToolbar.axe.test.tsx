import { axe } from 'vitest-axe';
import { beforeAll } from 'vitest';
import { renderWithChakra } from '../test/render-with-chakra';
import CostEstimateToolbar from './CostEstimateToolbar';

const noop = (): void => undefined;

beforeAll(() => {
  class ResizeObserverMock {
    observe(): void {
      return;
    }
    unobserve(): void {
      return;
    }
    disconnect(): void {
      return;
    }
  }
  globalThis.ResizeObserver = ResizeObserverMock as unknown as typeof ResizeObserver;
});

describe('CostEstimateToolbar — AXE', () => {
  it('brakNaruszen_domyslneAkcjeIWidok', async () => {
    const { container } = renderWithChakra(
      <CostEstimateToolbar
        viewMode="tree"
        onViewModeChange={noop}
        searchQuery=""
        onSearchChange={noop}
        columnVisibility={null}
        canEdit
        canShare
        canSchedule={false}
        hasSchedule={false}
        isSyncing={false}
        isRecalculating={false}
        isExportingXlsx={false}
        isExportingPdf={false}
        onExpandAll={noop}
        onCollapseAll={noop}
        onOpenSchema={noop}
        onRefresh={noop}
        onNavigateToSchedule={noop}
        onCreateSchedule={noop}
        onSyncSchedule={noop}
        onShare={noop}
        onExportXlsx={noop}
        onExportPdf={noop}
        isFullscreen={false}
        onToggleFullscreen={noop}
      />
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('brakNaruszen_podczasEksportuXlsx', async () => {
    const { container } = renderWithChakra(
      <CostEstimateToolbar
        viewMode="tree"
        onViewModeChange={noop}
        searchQuery=""
        onSearchChange={noop}
        columnVisibility={null}
        canEdit={false}
        canShare={false}
        canSchedule={false}
        hasSchedule={false}
        isSyncing={false}
        isRecalculating={false}
        isExportingXlsx
        isExportingPdf={false}
        onExpandAll={noop}
        onCollapseAll={noop}
        onOpenSchema={noop}
        onRefresh={noop}
        onNavigateToSchedule={noop}
        onCreateSchedule={noop}
        onSyncSchedule={noop}
        onShare={noop}
        onExportXlsx={noop}
        onExportPdf={noop}
        isFullscreen={false}
        onToggleFullscreen={noop}
      />
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
