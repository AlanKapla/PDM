import { useState, useCallback } from "react";
import { handleApiError } from "../utils/handleApiError";

interface UseFetchState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

interface UseFetchOptions {
  onSuccess?: (data: any) => void;
  onError?: (error: string) => void;
  initialLoading?: boolean;
}

export const useFetch = <T = any>(options?: UseFetchOptions) => {
  const [state, setState] = useState<UseFetchState<T>>({
    data: null,
    loading: options?.initialLoading ?? false,
    error: null,
  });

  const execute = useCallback(async (
    fetchFn: () => Promise<Response>,
    parseResponse: boolean = true
  ) => {
    setState(prev => ({ ...prev, loading: true, error: null }));

    try {
      const response = await fetchFn();

      if (!response.ok) {
        const { title, description } = await handleApiError(response);
        const errorMessage = description ? `${title}: ${description}` : title;
        setState({ data: null, loading: false, error: errorMessage });
        options?.onError?.(errorMessage);
        return { success: false, error: errorMessage };
      }

      const data = parseResponse ? await response.json() : null;
      setState({ data, loading: false, error: null });
      options?.onSuccess?.(data);
      return { success: true, data };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : "Nieoczekiwany błąd";
      setState({ data: null, loading: false, error: errorMessage });
      options?.onError?.(errorMessage);
      return { success: false, error: errorMessage };
    }
  }, [options]);

  const reset = useCallback(() => {
    setState({ data: null, loading: false, error: null });
  }, []);

  return {
    ...state,
    execute,
    reset,
  };
};
