import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import DeleteAlertDialog from '../DeleteAlertDialog';

describe('DeleteAlertDialog — AXE', () => {
    it('brakNaruszen_otwartyDialog_bezNazwyElementu', async () => {
        const { container } = renderWithChakra(
            <DeleteAlertDialog
                isOpen
                onClose={() => undefined}
                onConfirm={() => undefined}
            />
        );
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });

    it('brakNaruszen_otwartyDialog_zNazwaElementu', async () => {
        const { container } = renderWithChakra(
            <DeleteAlertDialog
                isOpen
                onClose={() => undefined}
                onConfirm={() => undefined}
                itemName="Projekt alfa"
            />
        );
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });
});
