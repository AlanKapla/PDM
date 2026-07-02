import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import { MultiDocumentDropzone } from '../MultiDocumentDropzone';

describe('MultiDocumentDropzone — AXE', () => {
  it('brakNaruszen_render_pustyStan', async () => {
    const { container } = renderWithChakra(
      <MultiDocumentDropzone files={[]} onFilesChange={() => {}} />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
