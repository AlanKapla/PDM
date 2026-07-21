import React from 'react';
import { axe } from 'vitest-axe';
import { vi } from 'vitest';
import { renderWithChakra } from '../../../test/render-with-chakra';
import { AICostReviewItem } from '../AICostReviewItem';
import type { AICostImportItemWeb } from '../../../types/ai.types';

vi.mock('../../../hooks/usePendingAICostImports', () => ({
  useUpdatePendingAICostImportItem: () => ({
    mutateAsync: vi.fn(),
    isPending: false,
  }),
  useAcceptPendingAICostImportItem: () => ({
    mutateAsync: vi.fn(),
    isPending: false,
  }),
  useRejectPendingAICostImportItem: () => ({
    mutateAsync: vi.fn(),
    isPending: false,
  }),
}));

vi.mock('../../../hooks/useToastNotification', () => ({
  useToastNotification: () => ({
    showSuccess: vi.fn(),
    showApiError: vi.fn(),
  }),
}));

vi.mock('../../../hooks/useProjectPermissions', () => ({
  useProjectPermissions: () => ({ canEdit: true }),
}));

vi.mock('../../../hooks/useTenantPermissions', () => ({
  useTenantPermissions: () => ({ canEdit: false }),
}));

vi.mock('../AICostReviewItemForm', () => ({
  AICostReviewItemForm: (): React.ReactElement => (
    <div data-testid="ai-cost-review-item-form">Formularz</div>
  ),
}));

function createPdfItem(
  overrides?: Partial<AICostImportItemWeb>
): AICostImportItemWeb {
  return {
    id: 'item-1',
    batchId: 'batch-1',
    tenantId: 'tenant-1',
    projectId: 'project-1',
    status: 'Pending',
    costDocumentType: 'ProjectCost',
    originalFileName: 'faktura.pdf',
    contentType: 'application/pdf',
    fileSizeBytes: 1024 * 1024,
    parsedData: {
      name: 'Faktura testowa',
      contractorFound: false,
      categoryFound: false,
      confidence: 0.9,
    },
    previewUrl: 'https://example.com/sas/faktura.pdf',
    createdAt: '2026-07-21T10:00:00Z',
    updatedAt: '2026-07-21T10:00:00Z',
    ...overrides,
  };
}

function createImageItem(): AICostImportItemWeb {
  return createPdfItem({
    originalFileName: 'faktura.jpg',
    contentType: 'image/jpeg',
    previewUrl: 'https://example.com/sas/faktura.jpg',
  });
}

describe('AICostReviewItem — AXE', () => {
  it('brakNaruszen_podgladPdf', async () => {
    const { container } = renderWithChakra(
      <AICostReviewItem
        tenantId="tenant-1"
        projectId="project-1"
        item={createPdfItem()}
      />
    );
    const results = await axe(container, { iframes: false });
    expect(results).toHaveNoViolations();
  });

  it('brakNaruszen_podgladObrazu', async () => {
    const { container } = renderWithChakra(
      <AICostReviewItem
        tenantId="tenant-1"
        projectId="project-1"
        item={createImageItem()}
      />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
