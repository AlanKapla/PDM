import { useMutation } from '@tanstack/react-query';
import { aiCostApi } from '../api/aiCostApi';
import type { ParsedCostDto, ParseCostDocumentRequest } from '../types/ai.types';

interface UseAICostDocumentParserParams {
  tenantId: string;
  projectId: string;
}

export function useAICostDocumentParser({
  tenantId,
  projectId,
}: UseAICostDocumentParserParams) {
  return useMutation<ParsedCostDto, Error, ParseCostDocumentRequest>({
    mutationFn: (data: ParseCostDocumentRequest) =>
      data.costType === 'ProjectCost'
        ? aiCostApi.parseProjectCostDocument(tenantId, projectId, data)
        : aiCostApi.parseTrackedCostDocument(tenantId, projectId, data),
  });
}
