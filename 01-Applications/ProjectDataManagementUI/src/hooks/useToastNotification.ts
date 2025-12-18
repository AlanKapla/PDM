import { useCallback } from "react";
import { useToast as useChakraToast } from "@chakra-ui/react";
import type { UseToastOptions } from "@chakra-ui/react";

interface ToastOptions extends Omit<UseToastOptions, 'title' | 'description'> {
  title?: string;
  description?: string;
}

export const useToastNotification = () => {
  const toast = useChakraToast();

  const showSuccess = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "success",
      duration: 3000,
      isClosable: true,
      ...options,
    });
  }, [toast]);

  const showError = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "error",
      duration: 5000,
      isClosable: true,
      ...options,
    });
  }, [toast]);

  const showWarning = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "warning",
      duration: 4000,
      isClosable: true,
      ...options,
    });
  }, [toast]);

  const showInfo = useCallback((title: string, description?: string, options?: ToastOptions) => {
    toast({
      title,
      description,
      status: "info",
      duration: 3000,
      isClosable: true,
      ...options,
    });
  }, [toast]);

  return {
    showSuccess,
    showError,
    showWarning,
    showInfo,
    toast, // dla bardziej zaawansowanych przypadków
  };
};
