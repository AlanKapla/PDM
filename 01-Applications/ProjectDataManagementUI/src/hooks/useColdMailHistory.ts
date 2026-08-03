import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";
import type { ColdMailHistoryWeb } from "../types/admin.types";

/** Max liczby polli przy statusie Queued — chroni przed nieskończonym pollingiem (np. demo stub). */
const MAX_QUEUED_POLLS = 20;
const QUEUED_POLL_INTERVAL_MS = 3000;

export function useColdMailHistory(emailFilter?: string) {
  const normalizedFilter: string | undefined = emailFilter?.trim()
    ? emailFilter.trim()
    : undefined;

  return useQuery({
    queryKey: ["coldMailHistory", normalizedFilter ?? ""],
    queryFn: (): Promise<ColdMailHistoryWeb[]> =>
      adminApi.getColdMails(normalizedFilter),
    refetchInterval: (query) => {
      const items: ColdMailHistoryWeb[] | undefined = query.state.data;
      const hasQueued: boolean =
        items?.some((item: ColdMailHistoryWeb) => item.status === "Queued") ??
        false;

      if (!hasQueued) {
        return false;
      }

      // dataUpdateCount obejmuje udane fetche; po limicie przestajemy pollować.
      if (query.state.dataUpdateCount >= MAX_QUEUED_POLLS) {
        return false;
      }

      return QUEUED_POLL_INTERVAL_MS;
    },
  });
}
