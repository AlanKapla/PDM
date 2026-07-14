import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import AppModal from '../AppModal';

describe('AppModal — AXE', () => {
    it('brakNaruszen_otwartyModal_zTresciaIAkcja', async () => {
        const { container } = renderWithChakra(
            <AppModal
                isOpen
                onClose={() => undefined}
                title="Edytuj projekt"
                actionLabel="Zapisz"
                onAction={() => undefined}
            >
                <p>Treść modala z formularzem.</p>
            </AppModal>
        );
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });

    it('brakNaruszen_otwartyModal_bezStopki', async () => {
        const { container } = renderWithChakra(
            <AppModal
                isOpen
                onClose={() => undefined}
                title="Podgląd"
                hideFooter
            >
                <p>Treść tylko do odczytu.</p>
            </AppModal>
        );
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });
});
