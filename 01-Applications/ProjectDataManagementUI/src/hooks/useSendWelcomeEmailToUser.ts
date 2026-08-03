import { useMutation, useQueryClient } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";
import { useToastNotification } from "./useToastNotification";
import type { AdminUserWeb } from "../types/admin.types";

export function useSendWelcomeEmailToUser() {
  const queryClient = useQueryClient();
  const { showSuccess, showApiError } = useToastNotification();

  return useMutation({
    mutationFn: (userId: string): Promise<AdminUserWeb> =>
      adminApi.sendWelcomeEmailToUser(userId),
    onSuccess: (user: AdminUserWeb) => {
      showSuccess(
        "Mail powitalny wysłany",
        `Wysłano do ${user.firstName} ${user.lastName} (${user.email})`
      );
      void queryClient.invalidateQueries({ queryKey: ["adminUsers"] });
    },
    onError: (error: unknown) => {
      showApiError(error);
    },
  });
}
