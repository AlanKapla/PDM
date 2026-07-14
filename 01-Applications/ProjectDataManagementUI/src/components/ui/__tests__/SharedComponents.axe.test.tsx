import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import EmptyState from '../EmptyState';
import ErrorState from '../ErrorState';
import LoadingState from '../LoadingState';
import StatusBadge from '../StatusBadge';

describe('EmptyState — AXE', () => {
    it('brakNaruszen_render_zTytulem', async () => {
        const { container } = renderWithChakra(
            <EmptyState title="Brak projektów" />
        );
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });
});

describe('ErrorState — AXE', () => {
    it('brakNaruszen_render_zKomunikatemBledu', async () => {
        const { container } = renderWithChakra(
            <ErrorState description="Nie udało się załadować danych." />
        );
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });
});

describe('LoadingState — AXE', () => {
    it('brakNaruszen_render', async () => {
        const { container } = renderWithChakra(<LoadingState />);
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });
});

describe('StatusBadge — AXE', () => {
    it('brakNaruszen_render', async () => {
        const { container } = renderWithChakra(
            <StatusBadge status="active" />
        );
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });
});
