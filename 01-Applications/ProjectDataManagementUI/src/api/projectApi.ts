import { axiosClient } from "./axiosClient";

// Resource scope enum matching backend
export enum ResourceScope {
  All = 0,
  Mine = 1,
  Shared = 2,
}

// Helper to convert enum to route string
const resourceScopeToRoute = (scope: ResourceScope): string => {
  switch (scope) {
    case ResourceScope.All:
      return "all";
    case ResourceScope.Mine:
      return "mine";
    case ResourceScope.Shared:
      return "shared";
    default:
      return "mine";
  }
};

export const projectApi = {
  // Pobierz wszystkie projekty w tenancie
  getTenantProjects: async (tenantId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/project`);
  },

  // Pobierz szczegóły projektu
  getProjectDetails: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}`);
  },

  // Pobierz projekt (alias)
  getProject: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}`);
  },

  // Utwórz nowy projekt
  createProject: async (tenantId: string, name: string) => {
    return axiosClient.post(`/tenants/${tenantId}/project`, { 
      tenantId, 
      name 
    });
  },

  // Pobierz członków projektu
  getProjectMembers: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/members`);
  },

  // Pobierz słownik projektów (id -> nazwa) dla tenanta
  getProjectsDictionary: async (tenantId: string): Promise<Record<string, string>> => {
    const response = await axiosClient.get<Record<string, string>>(`/tenants/${tenantId}/project/dictionary`);
    return response.data;
  },

  // Dodaj członka do projektu
  addProjectMember: async (tenantId: string, projectId: string, userId: string) => {
    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/members`, { 
      tenantId, projectId, userId 
    });
  },

  // Usuń członka z projektu
  removeProjectMember: async (tenantId: string, projectId: string, userId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/project/${projectId}/members/${userId}`);
  },

  // Utwórz paczkę i upload plików
  createPackageAndUploadFiles: async (
    tenantId: string, 
    projectId: string, 
    packageName: string, 
    files: Array<{ file: File; displayName?: string; comment?: string }>
  ) => {
    const formData = new FormData();
    formData.append('PackageName', packageName);
    
    files.forEach((item, index) => {
      formData.append(`Files[${index}].File`, item.file);
      if (item.displayName) {
        formData.append(`Files[${index}].DisplayName`, item.displayName);
      }
      if (item.comment) {
        formData.append(`Files[${index}].Comment`, item.comment);
      }
    });

    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/file/packages/create`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  // Dodaj pliki do istniejącej paczki
  addFilesToPackage: async (
    tenantId: string, 
    projectId: string, 
    packageId: string,
    files: Array<{ file: File; displayName?: string; comment?: string }>
  ) => {
    const formData = new FormData();
    formData.append('ProjectFilePackageId', packageId);
    
    files.forEach((item, index) => {
      formData.append(`Files[${index}].File`, item.file);
      if (item.displayName) {
        formData.append(`Files[${index}].DisplayName`, item.displayName);
      }
      if (item.comment) {
        formData.append(`Files[${index}].Comment`, item.comment);
      }
    });

    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/file`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  // 🆕 Hierarchiczne endpointy dla plików (API v2.0)
  
  // 1. Pobierz listę paczek plików
  getProjectFilePackages: async (tenantId: string, projectId: string, scope: ResourceScope) => {
    const scopeRoute = resourceScopeToRoute(scope);
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/file/packages/${scopeRoute}`);
  },

  // 2. Pobierz pliki w konkretnej paczce
  getPackageFiles: async (tenantId: string, projectId: string, packageId: string, scope: ResourceScope) => {
    const scopeRoute = resourceScopeToRoute(scope);
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/file/packages/${packageId}/files/${scopeRoute}`);
  },

  // 3. Pobierz wersje konkretnego pliku
  getFileVersions: async (tenantId: string, projectId: string, fileId: string, scope: ResourceScope) => {
    const scopeRoute = resourceScopeToRoute(scope);
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/file/files/${fileId}/versions/${scopeRoute}`);
  },

  // 4. Pobierz komentarze do konkretnej wersji
  getVersionComments: async (tenantId: string, projectId: string, fileId: string, versionId: string, scope: ResourceScope) => {
    const scopeRoute = resourceScopeToRoute(scope);
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/file/files/${fileId}/versions/${versionId}/comments/${scopeRoute}`);
  },

  // Udostępnij paczki wielu użytkownikom
  sharePackages: async (
    tenantId: string,
    projectId: string,
    packageIds: string[],
    sharedWithUserIds: string[]
  ) => {
    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/file/packages/share`, {
      tenantId,
      projectId,
      packageIds,
      sharedWithUserIds,
    });
  },

  // Zaktualizuj udostępnienie konkretnego pliku
  updateFileShare: async (
    tenantId: string,
    projectId: string,
    fileId: string,
    sharedWithUserIds: string[]
  ) => {
    return axiosClient.put(`/tenants/${tenantId}/project/${projectId}/file/${fileId}/share`, {
      tenantId,
      projectId,
      fileId,
      sharedWithUserIds,
    });
  },

  // Usuń plik z projektu
  deleteFile: async (tenantId: string, projectId: string, fileId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/project/${projectId}/file/${fileId}`);
  },

  // Upload nowej wersji pliku
  uploadNewVersion: async (
    tenantId: string,
    projectId: string,
    fileId: string,
    file: File,
    comment?: string
  ) => {
    const formData = new FormData();
    formData.append("File", file);
    if (comment) {
      formData.append("Comment", comment);
    }

    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/file/${fileId}/versions`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  // Dodaj komentarz do wersji pliku
  addFileVersionComment: async (
    tenantId: string,
    projectId: string,
    fileId: string,
    versionId: string,
    comment: string
  ) => {
    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/file/${fileId}/versions/${versionId}/comments`, {
      comment
    });
  },

  // Zmień status projektu (aktywuj/dezaktywuj)
  toggleProjectStatus: async (tenantId: string, projectId: string, isActive: boolean) => {
    return axiosClient.patch(`/tenants/${tenantId}/project/${projectId}/status?isActive=${isActive}`);
  },

  // Aktualizuj projekt (nazwa)
  updateProject: async (tenantId: string, projectId: string, data: { Name: string }) => {
    return axiosClient.put(`/tenants/${tenantId}/project/${projectId}`, data);
  },

  // Utwórz harmonogram prac
  createWorkSchedule: async (
    tenantId: string,
    projectId: string,
    command: {
      name: string;
      costEstimateId?: string | null;
      stages?: Array<{
        name: string;
        order: number;
        works: Array<{
          tempId?: string;
          name: string;
          order: number;
          colorRgb: string;
          isClosed: boolean;
          periods: Array<{
            startDate: string;
            endDate: string;
            isClosed: boolean;
          }>;
          assignedUserIds: string[];
          comments: Array<{
            content: string;
          }>;
        }>;
        children?: any[];
      }>;
      dependencies?: Array<{
        predecessorDbId?: string;
        predecessorTempId?: string;
        successorDbId?: string;
        successorTempId?: string;
        dependencyType: number;
        lagDays: number;
      }>;
    }
  ) => {
    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/work-schedule`, {
      tenantId,
      projectId,
      name: command.name,
      costEstimateId: command.costEstimateId ?? null,
      stages: command.stages,
      dependencies: command.dependencies,
    });
  },

  // ===== Harmonogramy prac =====

  // UNIFIED endpoint for getting work schedules based on scope
  getWorkSchedules: async (tenantId: string, projectId: string, scope: ResourceScope) => {
    const scopeRoute = resourceScopeToRoute(scope);
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/work-schedule/${scopeRoute}`);
  },

  // DEPRECATED - use getWorkSchedules with ResourceScope.Mine
  getMyWorkSchedules: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/work-schedule/mine`);
  },

  // Pobierz szczegóły pojedynczego harmonogramu prac
  getWorkSchedule: async (tenantId: string, projectId: string, workScheduleId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/work-schedule/details/${workScheduleId}`);
  },

  // Aktualizuj harmonogram prac
  updateWorkSchedule: async (
    tenantId: string,
    projectId: string,
    workScheduleId: string,
    command: {
      name: string;
      stages?: Array<{
        id?: string;
        name: string;
        order: number;
        works: Array<{
          id?: string;
          tempId?: string;
          name: string;
          order: number;
          colorRgb: string;
          isClosed: boolean;
          periods: Array<{
            id?: string;
            startDate: string;
            endDate: string;
            isClosed: boolean;
          }>;
          assignedUserIds: string[];
          comments: Array<{
            id?: string;
            content: string;
          }>;
        }>;
        children?: any[];
      }>;
      dependencies?: Array<{
        predecessorDbId?: string;
        predecessorTempId?: string;
        successorDbId?: string;
        successorTempId?: string;
        dependencyType: number;
        lagDays: number;
      }>;
    }
  ) => {
    return axiosClient.put(`/tenants/${tenantId}/project/${projectId}/work-schedule/${workScheduleId}`, {
      tenantId,
      projectId,
      workScheduleId,
      name: command.name,
      stages: command.stages,
      dependencies: command.dependencies,
    });
  },

  // Usuń harmonogram prac
  deleteWorkSchedule: async (tenantId: string, projectId: string, workScheduleId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/project/${projectId}/work-schedule/${workScheduleId}`);
  },

  // Synchronizuj harmonogram z powiązanym kosztorysem
  syncWorkScheduleWithEstimate: async (tenantId: string, projectId: string, workScheduleId: string) => {
    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/work-schedule/${workScheduleId}/sync-with-estimate`);
  },

  // Pobierz prace przypisane do użytkownika (cross-tenant)
  getMyAssignedWorks: async () => {
    return axiosClient.get(`/user/assigned-works`);
  },

  // ===== Koszty projektowe =====

  // UNIFIED endpoint for getting costs based on scope
  getProjectCosts: async (tenantId: string, projectId: string, scope: ResourceScope) => {
    const scopeRoute = resourceScopeToRoute(scope);
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/cost/${scopeRoute}`);
  },

  // DEPRECATED - use getProjectCosts with ResourceScope.Mine
  getProjectUserCosts: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/cost/mine`);
  },

  // DEPRECATED - use getProjectCosts with ResourceScope.Shared
  getSharedProjectCosts: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/project/${projectId}/cost/shared`);
  },

  // Utwórz nowy koszt projektowy
  createProjectCost: async (
    tenantId: string,
    projectId: string,
    data: {
      name: string;
      place?: string;
      date: Date;
      description?: string;
      netAmount?: number | null;
      vatRate?: number | null;
      grossAmount?: number | null;
      isClosed?: boolean;
      document?: File;
    }
  ) => {
    const formData = new FormData();
    formData.append("TenantId", tenantId);
    formData.append("ProjectId", projectId);
    formData.append("Name", data.name);
    if (data.place) formData.append("Place", data.place);
    formData.append("Date", data.date.toISOString());
    if (data.description) formData.append("Description", data.description);
    if (data.netAmount !== undefined && data.netAmount !== null) formData.append("NetAmount", data.netAmount.toString().replace('.', ','));
    if (data.vatRate !== undefined && data.vatRate !== null) formData.append("VatRate", data.vatRate.toString().replace('.', ','));
    if (data.grossAmount !== undefined && data.grossAmount !== null) formData.append("GrossAmount", data.grossAmount.toString().replace('.', ','));
    if (data.isClosed !== undefined) formData.append("IsClosed", data.isClosed.toString());
    if (data.document) formData.append("Document", data.document);

    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/cost`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  // Aktualizuj koszt projektowy
  updateProjectCost: async (
    tenantId: string,
    projectId: string,
    costId: string,
    data: {
      name: string;
      place?: string;
      date: Date;
      description?: string;
      netAmount?: number | null;
      vatRate?: number | null;
      grossAmount?: number | null;
      isClosed: boolean;
      document?: File;
      removeDocument: boolean;
    }
  ) => {
    const formData = new FormData();
    formData.append("TenantId", tenantId);
    formData.append("ProjectId", projectId);
    formData.append("CostId", costId);
    formData.append("Name", data.name);
    if (data.place) formData.append("Place", data.place);
    formData.append("Date", data.date.toISOString());
    if (data.description) formData.append("Description", data.description);
    if (data.netAmount !== undefined && data.netAmount !== null) formData.append("NetAmount", data.netAmount.toString().replace('.', ','));
    if (data.vatRate !== undefined && data.vatRate !== null) formData.append("VatRate", data.vatRate.toString().replace('.', ','));
    if (data.grossAmount !== undefined && data.grossAmount !== null) formData.append("GrossAmount", data.grossAmount.toString().replace('.', ','));
    formData.append("IsClosed", data.isClosed.toString());
    if (data.document) formData.append("Document", data.document);
    formData.append("RemoveDocument", data.removeDocument.toString());

    return axiosClient.put(`/tenants/${tenantId}/project/${projectId}/cost/${costId}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  // Usuń koszt projektowy
  deleteProjectCost: async (tenantId: string, projectId: string, costId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/project/${projectId}/cost/${costId}`);
  },

  // Udostępnij wiele kosztów wybranym członkom (grupowe udostępnianie)
  shareProjectCosts: async (
    tenantId: string,
    projectId: string,
    data: {
      projectCostIds: string[];
      sharedWithUserIds: string[];
    }
  ) => {
    return axiosClient.post(`/tenants/${tenantId}/project/${projectId}/cost/share`, data);
  },

  // Aktualizuj udostępnienie pojedynczego kosztu
  updateCostShare: async (
    tenantId: string,
    projectId: string,
    costId: string,
    sharedWithUserIds: string[]
  ) => {
    return axiosClient.put(`/tenants/${tenantId}/project/${projectId}/cost/${costId}/share`, {
      sharedWithUserIds,
    });
  },

  // Zmień rolę członka projektu
  updateProjectMemberRole: async (tenantId: string, projectId: string, userId: string, roleId: string) => {
      return axiosClient.patch(`/tenants/${tenantId}/project/${projectId}/members/${userId}/role`, { roleId });
  },
};
