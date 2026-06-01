import { axiosClient } from './axiosClient';
import type { ParsedCostDto, ParseCostDocumentRequest } from '../types/ai.types';

const parseRequest = async (
  tenantId: string,
  projectId: string,
  file: File,
  endpoint: 'project-cost' | 'tracked-cost'
): Promise<ParsedCostDto> => {
  const form = new FormData();
  form.append('file', file);

  const res = await axiosClient.post<ParsedCostDto>(
    `/tenants/${tenantId}/projects/${projectId}/ai/cost/parse/${endpoint}`,
    form,
    {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 60_000,
    }
  );
  return res.data;
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
};
