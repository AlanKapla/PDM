import { axe } from 'vitest-axe';
import { renderWithChakra } from '../../../test/render-with-chakra';
import { TechnicalDocumentationDetailsView } from '../TechnicalDocumentationDetailsView';
import {
  mockGroupPipelineDetails,
  mockTechnicalDocumentationDetails,
} from './mockTechnicalDocumentationDetails';

describe('TechnicalDocumentationDetailsView — AXE', () => {
  it('brakNaruszen_render_legacyMockDetails', async () => {
    const { container } = renderWithChakra(
      <TechnicalDocumentationDetailsView details={mockTechnicalDocumentationDetails} />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('brakNaruszen_render_groupPipelineMockDetails', async () => {
    const { container } = renderWithChakra(
      <TechnicalDocumentationDetailsView details={mockGroupPipelineDetails} />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
