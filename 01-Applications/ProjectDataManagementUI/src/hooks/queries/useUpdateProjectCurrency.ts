import { useMutation, useQueryClient } from "@tanstack/react-query";
import { projectApi } from "../../api/projectApi";
import { projectKeys } from "./useProjectDetails";
import type { SetProjectCurrencyRequest } from "../../types/project.types";

export function useUpdateProjectCurrency(tenantId: string, projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: SetProjectCurrencyRequest) =>
      projectApi.setProjectCurrency(tenantId, projectId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: projectKeys.detail(tenantId, projectId),
      });
    },
  });
}
