import { useCallback } from "react";
import { useToast as useChakraToast, useBreakpointValue } from "@chakra-ui/react";
import type { UseToastOptions } from "@chakra-ui/react";
import { successMessages, type SuccessMessageKey } from "../utils/errorMessages";
import { handleApiError } from "../utils/handleApiError";

interface ToastOptions extends Omit<UseToastOptions, 'title' | 'description'> {
  title?: string;
  description?: string;
}

export const useToastNotification = () => {
  const toast = useChakraToast();
  // Na mobile toasty u góry (nie zakrywają dolnej nawigacji), na desktop – prawy górny róg.
  // Dodatkowy fallback gwarantuje przewidywalną pozycję także wtedy, gdy breakpoint nie jest jeszcze wyliczony.
  const position = (useBreakpointValue<UseToastOptions["position"]>(
    { base: "top", md: "top-right" },
    { fallback: "top-right" }
  ) ?? "top-right") as UseToastOptions["position"];

  const showSuccess = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "success",
      duration: 3000,
      isClosable: true,
      position,
      ...options,
    });
  }, [toast, position]);

  const showError = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "error",
      duration: 5000,
      isClosable: true,
      position,
      ...options,
    });
  }, [toast, position]);

  const showWarning = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "warning",
      duration: 4000,
      isClosable: true,
      position,
      ...options,
    });
  }, [toast, position]);

  const showInfo = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "info",
      duration: 3000,
      isClosable: true,
      position,
      ...options,
    });
  }, [toast, position]);

  const showApiSuccess = useCallback(
    (key: SuccessMessageKey, descriptionOverride?: string) => {
      const { title, description } = successMessages[key];
      toast({
        title,
        description: descriptionOverride ?? description,
        status: "success",
        duration: 3000,
        isClosable: true,
        position,
      });
    },
    [toast, position]
  );

  const showApiError = useCallback(
    (error: unknown, options?: ToastOptions) => {
      const { title, description, toastStatus = "error" } = handleApiError(error);

      toast({
        title,
        description,
        status: toastStatus,
        duration: toastStatus === "info" ? 4000 : 5000,
        isClosable: true,
        position,
        ...options,
      });
    },
    [toast, position]
  );

  return {
    showSuccess,
    showError,
    showWarning,
    showInfo,
    showApiSuccess,
    showApiError,
    toast, // dla bardziej zaawansowanych przypadków
  };
};

