import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";

export function useAdminUsers() {
  return useQuery({
    queryKey: ["adminUsers"],
    queryFn: () => adminApi.getUsers(),
  });
}
