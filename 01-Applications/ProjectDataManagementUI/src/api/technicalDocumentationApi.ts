import { axiosClient } from './axiosClient';
import type {
  TechnicalDocumentationListItemWeb,
  TechnicalDocumentationDetailsWeb,
  CreateTechnicalDocumentationRequest,
} from '../types/technicalDocumentation.types';

const MAX_FILE_SIZE_BYTES = 52_428_800; // 50 MB

const ALLOWED_MIME_TYPES = new Set(['application/pdf', 'image/jpeg']);

const validateFiles = (files: File[]): void => {
  for (const file of files) {
    if (!ALLOWED_MIME_TYPES.has(file.type)) {
      throw new Error(`Niedozwolony typ pliku: ${file.name}. Dozwolone: PDF, JPG.`);
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
      throw new Error(`Plik ${file.name} przekracza maksymalny rozmiar 50 MB.`);
    }
  }
};

const getCount = async (tenantId: string, projectId: string): Promise<number> => {
  const response = await axiosClient.get<number>(
    `/tenants/${tenantId}/projects/${projectId}/technical-documentation/count`
  );
  return response.data;
};

const getList = async (
  tenantId: string,
  projectId: string
): Promise<TechnicalDocumentationListItemWeb[]> => {
  const response = await axiosClient.get<TechnicalDocumentationListItemWeb[]>(
    `/tenants/${tenantId}/projects/${projectId}/technical-documentation`
  );
  return response.data;
};

const getById = async (
  tenantId: string,
  projectId: string,
  id: string
): Promise<TechnicalDocumentationDetailsWeb> => {
  const response = await axiosClient.get<TechnicalDocumentationDetailsWeb>(
    `/tenants/${tenantId}/projects/${projectId}/technical-documentation/${id}`
  );
  return response.data;
};

const create = async (
  tenantId: string,
  projectId: string,
  data: CreateTechnicalDocumentationRequest
): Promise<{ id: string }> => {
  validateFiles(data.files);

  const form = new FormData();
  form.append('name', data.name);
  if (data.description) {
    form.append('description', data.description);
  }
  data.files.forEach((file) => form.append('files', file));

  const response = await axiosClient.post<{ id: string }>(
    `/tenants/${tenantId}/projects/${projectId}/technical-documentation`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );
  return response.data;
};

const retry = async (
  tenantId: string,
  projectId: string,
  id: string
): Promise<void> => {
  await axiosClient.post(
    `/tenants/${tenantId}/projects/${projectId}/technical-documentation/${id}/retry`
  );
};

export const technicalDocumentationApi = {
  getCount,
  getList,
  getById,
  create,
  retry,
};
