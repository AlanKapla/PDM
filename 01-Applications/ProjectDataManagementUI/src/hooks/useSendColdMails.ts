import { useMutation, useQueryClient } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";
import { useToastNotification } from "./useToastNotification";
import type {
  SendColdMailsRequest,
  SendColdMailsResultWeb,
} from "../types/admin.types";

export function useSendColdMails() {
  const queryClient = useQueryClient();
  const { showSuccess, showApiError } = useToastNotification();

  return useMutation({
    mutationFn: (
      request: SendColdMailsRequest
    ): Promise<SendColdMailsResultWeb> => adminApi.sendColdMails(request),
    onSuccess: (result: SendColdMailsResultWeb) => {
      showSuccess(
        "Cold maile zakolejkowane",
        `Zakolejkowano: ${result.queuedCount}, błędów: ${result.failedCount}`
      );
      void queryClient.invalidateQueries({ queryKey: ["coldMailHistory"] });
    },
    onError: (error: unknown) => {
      showApiError(error);
    },
  });
}
