import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { technicalDocumentationApi } from '../../api/technicalDocumentationApi';
import type {
  TechnicalDocumentationListItemWeb,
  TechnicalDocumentationDetailsWeb,
  CreateTechnicalDocumentationRequest,
  TechnicalDocumentationStatus,
} from '../../types/technicalDocumentation.types';
import { TechnicalDocumentationStatus as StatusEnum } from '../../types/technicalDocumentation.types';

export const technicalDocumentationKeys = {
  all: ['technicalDocumentation'] as const,
  list: (tenantId: string, projectId: string) =>
    [...technicalDocumentationKeys.all, 'list', tenantId, projectId] as const,
  detail: (tenantId: string, projectId: string, id: string) =>
    [...technicalDocumentationKeys.all, 'detail', tenantId, projectId, id] as const,
  count: (tenantId: string, projectId: string) =>
    [...technicalDocumentationKeys.all, 'count', tenantId, projectId] as const,
};

const hasActiveProcessing = (
  items: TechnicalDocumentationListItemWeb[] | undefined
): boolean => {
  if (!items) {
    return false;
  }
  return items.some(
    (item) =>
      item.status === StatusEnum.Pending || item.status === StatusEnum.Processing
  );
};

export function useTechnicalDocumentationCount(
  tenantId: string | undefined,
  projectId: string | undefined,
  enabled = true
) {
  return useQuery<number>({
    queryKey: technicalDocumentationKeys.count(tenantId ?? '', projectId ?? ''),
    queryFn: () => technicalDocumentationApi.getCount(tenantId!, projectId!),
    enabled: Boolean(enabled && tenantId && projectId),
  });
}

export function useTechnicalDocumentationList(
  tenantId: string | undefined,
  projectId: string | undefined,
  enabled = true
) {
  return useQuery<TechnicalDocumentationListItemWeb[]>({
    queryKey: technicalDocumentationKeys.list(tenantId ?? '', projectId ?? ''),
    queryFn: () => technicalDocumentationApi.getList(tenantId!, projectId!),
    enabled: Boolean(enabled && tenantId && projectId),
    refetchInterval: (query) =>
      hasActiveProcessing(query.state.data) ? 5000 : false,
  });
}

export function useTechnicalDocumentationDetails(
  tenantId: string | undefined,
  projectId: string | undefined,
  id: string | undefined
) {
  return useQuery<TechnicalDocumentationDetailsWeb>({
    queryKey: technicalDocumentationKeys.detail(
      tenantId ?? '',
      projectId ?? '',
      id ?? ''
    ),
    queryFn: () => technicalDocumentationApi.getById(tenantId!, projectId!, id!),
    enabled: Boolean(tenantId && projectId && id),
    refetchInterval: (query) => {
      const status: TechnicalDocumentationStatus | undefined = query.state.data?.status;
      if (status === StatusEnum.Pending || status === StatusEnum.Processing) {
        return 5000;
      }
      return false;
    },
  });
}

export function useCreateTechnicalDocumentation(
  tenantId: string,
  projectId: string
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTechnicalDocumentationRequest) =>
      technicalDocumentationApi.create(tenantId, projectId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: technicalDocumentationKeys.list(tenantId, projectId),
      });
      queryClient.invalidateQueries({
        queryKey: technicalDocumentationKeys.count(tenantId, projectId),
      });
    },
  });
}

export function useRetryTechnicalDocumentation(
  tenantId: string,
  projectId: string
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (documentationId: string) =>
      technicalDocumentationApi.retry(tenantId, projectId, documentationId),
    onSuccess: (_data, documentationId) => {
      queryClient.invalidateQueries({
        queryKey: technicalDocumentationKeys.detail(tenantId, projectId, documentationId),
      });
      queryClient.invalidateQueries({
        queryKey: technicalDocumentationKeys.list(tenantId, projectId),
      });
    },
  });
}
