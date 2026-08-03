import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";
import type { ColdMailTemplateWeb } from "../types/admin.types";

/** Fetches cold-mail.html once (cached). Live preview fills placeholders locally — no per-keystroke API. */
export function useColdMailTemplate() {
  return useQuery({
    queryKey: ["coldMailTemplate"],
    queryFn: (): Promise<ColdMailTemplateWeb> => adminApi.getColdMailTemplate(),
    staleTime: Infinity,
    gcTime: Infinity,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });
}
