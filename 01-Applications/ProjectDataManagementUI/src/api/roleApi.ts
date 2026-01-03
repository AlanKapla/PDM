import { axiosClient } from "./axiosClient";

/**
 * Interface dla roli z API
 */
export interface RoleWeb {
  id: string;
  code: string;
  name: string;
  description?: string;
  scope: string; // 'Tenant' lub 'Project'
}

/**
 * API calls dla zarządzania rolami
 */
export const roleApi = {
  /**
   * Pobierz dostępne role dla określonego scope (tenant lub project)
   * @param scope - 'tenant' lub 'project'
   * @returns Lista dostępnych ról
   */
  getAvailableRoles: async (scope: 'tenant' | 'project'): Promise<RoleWeb[]> => {
    const response = await axiosClient.get<RoleWeb[]>(`/Role/${scope}`);
    return response.data;
  }
};
