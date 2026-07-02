import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import { TechnicalDocumentationStatusBadge } from '../TechnicalDocumentationStatusBadge';
import { TechnicalDocumentationStatus } from '../../../types/technicalDocumentation.types';

describe('TechnicalDocumentationStatusBadge — AXE', () => {
  it('brakNaruszen_render_completed', async () => {
    const { container } = renderWithChakra(
      <TechnicalDocumentationStatusBadge status={TechnicalDocumentationStatus.Completed} />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('brakNaruszen_render_processing', async () => {
    const { container } = renderWithChakra(
      <TechnicalDocumentationStatusBadge status={TechnicalDocumentationStatus.Processing} />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('brakNaruszen_render_completedWithWarnings', async () => {
    const { container } = renderWithChakra(
      <TechnicalDocumentationStatusBadge status={TechnicalDocumentationStatus.CompletedWithWarnings} />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
