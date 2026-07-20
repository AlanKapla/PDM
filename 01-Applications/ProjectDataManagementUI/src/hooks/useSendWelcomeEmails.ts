import { useMutation, useQueryClient } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";
import { useToastNotification } from "./useToastNotification";
import type { SendWelcomeEmailsResultWeb } from "../types/admin.types";

export function useSendWelcomeEmails() {
  const queryClient = useQueryClient();
  const { showSuccess, showApiError } = useToastNotification();

  return useMutation({
    mutationFn: (): Promise<SendWelcomeEmailsResultWeb> => adminApi.sendWelcomeEmails(),
    onSuccess: (result: SendWelcomeEmailsResultWeb) => {
      showSuccess(
        "Maile powitalne wysłane",
        `Wysłano: ${result.sentCount}, pominięto: ${result.skippedCount}`
      );
      void queryClient.invalidateQueries({ queryKey: ["adminUsers"] });
    },
    onError: (error: unknown) => {
      showApiError(error);
    },
  });
}
