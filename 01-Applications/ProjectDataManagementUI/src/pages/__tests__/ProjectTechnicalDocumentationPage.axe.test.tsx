import { axe } from 'vitest-axe';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderWithChakra } from '../../test/render-with-chakra';
import ProjectTechnicalDocumentationPage from '../ProjectTechnicalDocumentationPage';
import { TechnicalDocumentationStatus } from '../../types/technicalDocumentation.types';

vi.mock('../../context/AuthContext', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../context/AuthContext')>();
  return {
    ...actual,
    useAuth: () => ({
      user: { activeTenantId: 'tenant-1' },
      isAuthenticated: true,
      loading: false,
    }),
  };
});

vi.mock('../../hooks/useProjectPermissions', () => ({
  useProjectPermissions: () => ({
    canViewTechnicalDocumentation: true,
    canWriteTechnicalDocumentation: true,
    loading: false,
  }),
}));

vi.mock('../../layout/MainLayout', () => ({
  default: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

vi.mock('../../hooks/queries', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../hooks/queries')>();
  return {
    ...actual,
    useProjectDetails: () => ({
      data: { id: 'project-1', name: 'Projekt testowy' },
      isLoading: false,
    }),
    useTechnicalDocumentationList: () => ({
      data: [
        {
          id: 'doc-1',
          projectId: 'project-1',
          name: 'Dokumentacja A',
          description: 'Opis testowy',
          status: TechnicalDocumentationStatus.Completed,
          fileCount: 2,
          createdAt: '2026-06-01T10:00:00Z',
        },
      ],
      isLoading: false,
    }),
  };
});

describe('ProjectTechnicalDocumentationPage — AXE', () => {
  it('brakNaruszen_render_lista', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    const { container } = renderWithChakra(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/projects/project-1/technical-documentation']}>
          <Routes>
            <Route
              path="/projects/:projectId/technical-documentation"
              element={<ProjectTechnicalDocumentationPage />}
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
