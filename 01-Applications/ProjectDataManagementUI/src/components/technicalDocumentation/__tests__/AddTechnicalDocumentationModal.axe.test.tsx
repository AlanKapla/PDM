import { axe } from 'vitest-axe';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderWithChakra } from '../../../test/render-with-chakra';
import { AddTechnicalDocumentationModal } from '../AddTechnicalDocumentationModal';

vi.mock('../../../hooks/queries', () => ({
  useCreateTechnicalDocumentation: () => ({
    mutateAsync: vi.fn(),
    isPending: false,
  }),
}));

describe('AddTechnicalDocumentationModal — AXE', () => {
  it('brakNaruszen_render_otwarty', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });

    const { container } = renderWithChakra(
      <QueryClientProvider client={queryClient}>
        <AddTechnicalDocumentationModal
          isOpen
          onClose={() => {}}
          tenantId="tenant-1"
          projectId="project-1"
        />
      </QueryClientProvider>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
