import { axiosClient } from './axiosClient';
import type {
  AICostImportBatchWeb,
  AICostImportItemWeb,
  ParsedCostDto,
  ParseCostDocumentRequest,
  PendingAICostImportCountWeb,
  SubmitAICostImportBatchRequest,
  UpdateAICostImportItemRequest,
} from '../types/ai.types';

const parseRequest = async (
  tenantId: string,
  projectId: string,
  file: File,
  endpoint: 'project-cost' | 'tracked-cost'
): Promise<ParsedCostDto> => {
  const form = new FormData();
  form.append('file', file);

  const response = await axiosClient.post<ParsedCostDto>(
    `/tenants/${tenantId}/projects/${projectId}/ai/cost/parse/${endpoint}`,
    form,
    {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 60_000,
    }
  );
  return response.data;
};

const buildBatchFormData = (data: SubmitAICostImportBatchRequest): FormData => {
  const form = new FormData();
  data.files.forEach((file: File) => {
    form.append('files', file);
  });
  form.append('costDocumentType', data.costType);
  if (data.trackedCostContext) {
    form.append('trackedCostContextJson', JSON.stringify(data.trackedCostContext));
  }
  return form;
};

export const aiCostApi = {
  /**
   * Parsuje dokument kosztowy przez AI dla ProjectCost.
   * POST /api/tenants/{tenantId}/projects/{projectId}/ai/cost/parse/project-cost
   */
  parseProjectCostDocument: (
    tenantId: string,
    projectId: string,
    data: ParseCostDocumentRequest
  ): Promise<ParsedCostDto> =>
    parseRequest(tenantId, projectId, data.file, 'project-cost'),

  /**
   * Parsuje dokument kosztowy przez AI dla TrackedCost.
   * POST /api/tenants/{tenantId}/projects/{projectId}/ai/cost/parse/tracked-cost
   */
  parseTrackedCostDocument: (
    tenantId: string,
    projectId: string,
    data: ParseCostDocumentRequest
  ): Promise<ParsedCostDto> =>
    parseRequest(tenantId, projectId, data.file, 'tracked-cost'),

  /**
   * Wysyła wiele plików do analizy w tle.
   * POST /api/tenants/{tenantId}/projects/{projectId}/ai/cost/import/batch
   */
  submitImportBatch: async (
    tenantId: string,
    projectId: string,
    data: SubmitAICostImportBatchRequest
  ): Promise<AICostImportBatchWeb> => {
    const response = await axiosClient.post<AICostImportBatchWeb>(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/batch`,
      buildBatchFormData(data),
      {
        headers: { 'Content-Type': 'multipart/form-data' },
        timeout: 120_000,
      }
    );
    return response.data;
  },

  /**
   * Lista pozycji oczekujących na akceptację.
   * GET /api/tenants/{tenantId}/projects/{projectId}/ai/cost/import/pending
   */
  getPendingImportItems: async (
    tenantId: string,
    projectId: string
  ): Promise<AICostImportItemWeb[]> => {
    const response = await axiosClient.get<AICostImportItemWeb[]>(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending`
    );
    return response.data;
  },

  /**
   * Szczegóły pojedynczej pozycji importu.
   * GET /api/tenants/{tenantId}/projects/{projectId}/ai/cost/import/pending/{itemId}
   */
  getPendingImportItem: async (
    tenantId: string,
    projectId: string,
    itemId: string
  ): Promise<AICostImportItemWeb> => {
    const response = await axiosClient.get<AICostImportItemWeb>(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/${itemId}`
    );
    return response.data;
  },

  /**
   * Aktualizuje dane sparsowane przed akceptacją.
   * PUT /api/tenants/{tenantId}/projects/{projectId}/ai/cost/import/pending/{itemId}
   */
  updatePendingImportItem: async (
    tenantId: string,
    projectId: string,
    itemId: string,
    data: UpdateAICostImportItemRequest
  ): Promise<AICostImportItemWeb> => {
    const response = await axiosClient.put<AICostImportItemWeb>(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/${itemId}`,
      data.parsedData
    );
    return response.data;
  },

  /**
   * Akceptuje pozycję i zapisuje koszt.
   * POST /api/tenants/{tenantId}/projects/{projectId}/ai/cost/import/pending/{itemId}/accept
   */
  acceptPendingImportItem: async (
    tenantId: string,
    projectId: string,
    itemId: string
  ): Promise<void> => {
    await axiosClient.post(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/${itemId}/accept`
    );
  },

  /**
   * Akceptuje wszystkie oczekujące pozycje.
   * POST /api/tenants/{tenantId}/projects/{projectId}/ai/cost/import/pending/accept-all
   */
  acceptAllPendingImportItems: async (
    tenantId: string,
    projectId: string
  ): Promise<void> => {
    await axiosClient.post(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/accept-all`
    );
  },

  /**
   * Odrzuca pozycję (hard delete).
   * DELETE /api/tenants/{tenantId}/projects/{projectId}/ai/cost/import/pending/{itemId}
   */
  rejectPendingImportItem: async (
    tenantId: string,
    projectId: string,
    itemId: string
  ): Promise<void> => {
    await axiosClient.delete(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/${itemId}`
    );
  },

  /**
   * Licznik pozycji do weryfikacji (badge).
   * GET /api/tenants/{tenantId}/projects/{projectId}/ai/cost/import/pending/count
   */
  getPendingImportCount: async (
    tenantId: string,
    projectId: string
  ): Promise<PendingAICostImportCountWeb> => {
    const response = await axiosClient.get<PendingAICostImportCountWeb>(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/import/pending/count`
    );
    return response.data;
  },
};
