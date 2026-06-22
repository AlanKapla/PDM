import { useCallback } from "react";
import { useToast as useChakraToast } from "@chakra-ui/react";
import type { UseToastOptions } from "@chakra-ui/react";
import { successMessages, type SuccessMessageKey } from "../utils/errorMessages";
import { handleApiError } from "../utils/handleApiError";

interface ToastOptions extends Omit<UseToastOptions, 'title' | 'description'> {
  title?: string;
  description?: string;
}

const TOAST_DEFAULTS: Pick<UseToastOptions, "position" | "isClosable"> = {
  position: "top-right",
  isClosable: true,
};

export const useToastNotification = () => {
  const chakraToast = useChakraToast();

  const toast = useCallback(
    (options?: UseToastOptions) => {
      chakraToast({
        ...TOAST_DEFAULTS,
        ...options,
      });
    },
    [chakraToast]
  );

  const showSuccess = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "success",
      duration: 3000,
      ...options,
    });
  }, [toast]);

  const showError = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "error",
      duration: 5000,
      ...options,
    });
  }, [toast]);

  const showWarning = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "warning",
      duration: 4000,
      ...options,
    });
  }, [toast]);

  const showInfo = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "info",
      duration: 3000,
      ...options,
    });
  }, [toast]);

  const showApiSuccess = useCallback(
    (key: SuccessMessageKey, descriptionOverride?: string) => {
      const { title, description } = successMessages[key];
      toast({
        title,
        description: descriptionOverride ?? description,
        status: "success",
        duration: 3000,
      });
    },
    [toast]
  );

  const showApiError = useCallback(
    (error: unknown, options?: ToastOptions) => {
      const { title, description, toastStatus = "error" } = handleApiError(error);

      toast({
        title,
        description,
        status: toastStatus,
        duration: toastStatus === "info" ? 4000 : 5000,
        ...options,
      });
    },
    [toast]
  );

  return {
    showSuccess,
    showError,
    showWarning,
    showInfo,
    showApiSuccess,
    showApiError,
    toast,
  };
};

