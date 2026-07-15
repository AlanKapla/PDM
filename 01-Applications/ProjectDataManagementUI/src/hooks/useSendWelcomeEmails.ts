import { useMutation } from "@tanstack/react-query";
import { userApi } from "../api/userApi";
import { useToastNotification } from "./useToastNotification";
import type { SendWelcomeEmailsResultWeb } from "../types/user.types";

export function useSendWelcomeEmails() {
  const { showSuccess, showApiError } = useToastNotification();

  return useMutation({
    mutationFn: (): Promise<SendWelcomeEmailsResultWeb> => userApi.sendWelcomeEmails(),
    onSuccess: (result: SendWelcomeEmailsResultWeb) => {
      showSuccess(
        "Maile powitalne wysłane",
        `Wysłano: ${result.sentCount}, pominięto: ${result.skippedCount}`
      );
    },
    onError: (error: unknown) => {
      showApiError(error);
    },
  });
}
