import { describe, it, expect, vi, beforeEach } from 'vitest';
import { aiCostApi } from './aiCostApi';
import { axiosClient } from './axiosClient';

vi.mock('./axiosClient', () => ({
  axiosClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('aiCostApi', () => {
  const tenantId = 'tenant-1';
  const projectId = 'project-1';
  const itemId = 'item-1';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('submitImportBatch_wysylaMultipartFormData', async () => {
    const file1 = new File(['a'], 'a.jpg', { type: 'image/jpeg' });
    const file2 = new File(['b'], 'b.jpg', { type: 'image/jpeg' });
    const batchResponse = { batchId: 'batch-1', totalFiles: 2, message: 'ok' };

    vi.mocked(axiosClient.post).mockResolvedValue({ data: batchResponse });

    const result = await aiCostApi.submitImportBatch(tenantId, projectId, {
      files: [file1, file2],
      costType: 'ProjectCost',
    });

    expect(result).toEqual(batchResponse);
    expect(axiosClient.post).toHaveBeenCalledWith(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/batch`,
      expect.any(FormData),
      expect.objectContaining({
        headers: { 'Content-Type': 'multipart/form-data' },
        timeout: 120_000,
      })
    );

    const formData = vi.mocked(axiosClient.post).mock.calls[0][1] as FormData;
    expect(formData.getAll('files')).toHaveLength(2);
    expect(formData.get('costDocumentType')).toBe('ProjectCost');
  });

  it('getPendingImportItems_wywolujeGetEndpoint', async () => {
    const items = [{ id: itemId, status: 'Pending' }];
    vi.mocked(axiosClient.get).mockResolvedValue({ data: items });

    const result = await aiCostApi.getPendingImportItems(tenantId, projectId);

    expect(result).toEqual(items);
    expect(axiosClient.get).toHaveBeenCalledWith(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending`
    );
  });

  it('getPendingImportCount_zwracaLicznik', async () => {
    vi.mocked(axiosClient.get).mockResolvedValue({
      data: { pendingCount: 2, errorCount: 1, duplicateCount: 3 },
    });

    const result = await aiCostApi.getPendingImportCount(tenantId, projectId);

    expect(result).toEqual({ pendingCount: 2, errorCount: 1, duplicateCount: 3 });
    expect(axiosClient.get).toHaveBeenCalledWith(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/count`
    );
  });

  it('updatePendingImportItem_wysylaParsedDataBezposrednioWBody', async () => {
    const parsedData = {
      name: 'Materiały budowlane',
      contractorFound: false,
      categoryFound: false,
      confidence: 0.9,
    };
    const itemResponse = { id: itemId, status: 'Pending', parsedData };
    vi.mocked(axiosClient.put).mockResolvedValue({ data: itemResponse });

    const result = await aiCostApi.updatePendingImportItem(tenantId, projectId, itemId, {
      parsedData,
    });

    expect(result).toEqual(itemResponse);
    expect(axiosClient.put).toHaveBeenCalledWith(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/${itemId}`,
      parsedData
    );
  });

  it('acceptPendingImportItem_wywolujePostAccept', async () => {
    vi.mocked(axiosClient.post).mockResolvedValue({ data: undefined });

    await aiCostApi.acceptPendingImportItem(tenantId, projectId, itemId);

    expect(axiosClient.post).toHaveBeenCalledWith(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/${itemId}/accept`
    );
  });

  it('rejectPendingImportItem_wywolujeDelete', async () => {
    vi.mocked(axiosClient.delete).mockResolvedValue({ data: undefined });

    await aiCostApi.rejectPendingImportItem(tenantId, projectId, itemId);

    expect(axiosClient.delete).toHaveBeenCalledWith(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/${itemId}`
    );
  });

  it('acceptAllPendingImportItems_wywolujePostAcceptAll', async () => {
    vi.mocked(axiosClient.post).mockResolvedValue({ data: undefined });

    await aiCostApi.acceptAllPendingImportItems(tenantId, projectId);

    expect(axiosClient.post).toHaveBeenCalledWith(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/accept-all`
    );
  });
});
