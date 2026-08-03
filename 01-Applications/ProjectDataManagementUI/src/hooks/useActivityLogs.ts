import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";
import type { UserActivityLogWeb } from "../types/activity.types";

export function useActivityLogs() {
  return useQuery({
    queryKey: ["activityLogs"],
    queryFn: (): Promise<UserActivityLogWeb[]> => adminApi.getActivityLogs(),
  });
}
