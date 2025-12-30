import { axiosClient } from "./axiosClient";

export const projectApi = {
  // Pobierz szczegóły projektu
  getProjectDetails: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/Project/${projectId}`);
  },

  // Pobierz projekt (alias)
  getProject: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/Project/${projectId}`);
  },

  // Pobierz członków projektu
  getProjectMembers: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/Project/${projectId}/members`);
  },

  // Pobierz słownik projektów (id -> nazwa) dla tenanta
  getProjectsDictionary: async (tenantId: string): Promise<Record<string, string>> => {
    const response = await axiosClient.get<Record<string, string>>(`/tenants/${tenantId}/Project/dictionary`);
    return response.data;
  },

  // Dodaj członka do projektu
  addProjectMember: async (tenantId: string, projectId: string, userId: string) => {
    return axiosClient.post(`/tenants/${tenantId}/Project/${projectId}/members`, { 
      tenantId, projectId, userId 
    });
  },

  // Usuń członka z projektu
  removeProjectMember: async (tenantId: string, projectId: string, userId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/Project/${projectId}/members/${userId}`);
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

    return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/File/packages/create`, formData, {
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

    return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/File`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  // Pobierz pliki użytkownika w projekcie
  getMyFiles: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/projects/${projectId}/File/my`);
  },

  // Udostępnij pliki wielu użytkownikom
  shareFiles: async (
    tenantId: string,
    projectId: string,
    fileIds: string[],
    sharedWithUserIds: string[]
  ) => {
    return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/File/share`, {
      tenantId,
      projectId,
      projectFileIds: fileIds,
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
    return axiosClient.put(`/tenants/${tenantId}/projects/${projectId}/File/${fileId}/share`, {
      tenantId,
      projectId,
      fileId,
      sharedWithUserIds,
    });
  },

  // Pobierz pliki udostępnione dla użytkownika
  getSharedFiles: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/projects/${projectId}/File/shared`);
  },

  // Usuń plik z projektu
  deleteFile: async (tenantId: string, projectId: string, fileId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/projects/${projectId}/File/${fileId}`);
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

    return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/File/${fileId}/versions`, formData, {
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
    return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/File/${fileId}/versions/${versionId}/comments`, {
      comment
    });
  },

  // Zmień status projektu (aktywuj/dezaktywuj)
  toggleProjectStatus: async (tenantId: string, projectId: string, isActive: boolean) => {
    return axiosClient.patch(`/tenants/${tenantId}/Project/${projectId}/toggle-status?isActive=${isActive}`);
  },

  // Aktualizuj projekt (nazwa)
  updateProject: async (tenantId: string, projectId: string, data: { Name: string }) => {
    return axiosClient.put(`/tenants/${tenantId}/Project/${projectId}`, data);
  },

  // Utwórz harmonogram prac
  createWorkSchedule: async (
    tenantId: string,
    projectId: string,
    command: {
      name: string;
      stages: Array<{
        name: string;
        order: number;
        works: Array<{
          name: string;
          order: number;
          colorRgb: string;
          periods: Array<{
            startDate: string;
            endDate: string;
          }>;
          assignedUserIds: string[];
        }>;
      }>;
    }
  ) => {
    return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/work-schedules`, {
      tenantId,
      projectId,
      name: command.name,
      stages: command.stages,
    });
  },

  // Pobierz moje harmonogramy prac (lista podsumowań)
  getMyWorkSchedules: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/projects/${projectId}/work-schedules/my`);
  },

  // Pobierz szczegóły pojedynczego harmonogramu prac
  getWorkSchedule: async (tenantId: string, projectId: string, workScheduleId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/projects/${projectId}/work-schedules/${workScheduleId}`);
  },

  // Aktualizuj harmonogram prac
  updateWorkSchedule: async (
    tenantId: string,
    projectId: string,
    workScheduleId: string,
    command: {
      name: string;
      stages: Array<{
        id?: string;
        name: string;
        order: number;
        works: Array<{
          id?: string;
          name: string;
          order: number;
          colorRgb: string;
          isClosed: boolean;
          periods: Array<{
            id?: string;
            startDate: string;
            endDate: string;
          }>;
          assignedUserIds: string[];
        }>;
      }>;
    }
  ) => {
    return axiosClient.put(`/tenants/${tenantId}/projects/${projectId}/work-schedules/${workScheduleId}`, {
      tenantId,
      projectId,
      workScheduleId,
      name: command.name,
      stages: command.stages,
    });
  },

  // Pobierz prace przypisane do użytkownika
  getMyAssignedWorks: async (tenantId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/my-assigned-works`);
  },

  // ===== Koszty projektowe =====

  // Pobierz listę kosztów projektowych
  getProjectUserCosts: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/projects/${projectId}/costs`);
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
      netAmount?: number;
      vatRate?: number;
      grossAmount?: number;
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
    if (data.netAmount !== undefined) formData.append("NetAmount", data.netAmount.toString());
    if (data.vatRate !== undefined) formData.append("VatRate", data.vatRate.toString());
    if (data.grossAmount !== undefined) formData.append("GrossAmount", data.grossAmount.toString());
    if (data.document) formData.append("Document", data.document);

    return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/costs`, formData, {
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
      netAmount?: number;
      vatRate?: number;
      grossAmount?: number;
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
    if (data.netAmount !== undefined) formData.append("NetAmount", data.netAmount.toString());
    if (data.vatRate !== undefined) formData.append("VatRate", data.vatRate.toString());
    if (data.grossAmount !== undefined) formData.append("GrossAmount", data.grossAmount.toString());
    if (data.document) formData.append("Document", data.document);
    formData.append("RemoveDocument", data.removeDocument.toString());

    return axiosClient.put(`/tenants/${tenantId}/projects/${projectId}/costs/${costId}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  // Usuń koszt projektowy
  deleteProjectCost: async (tenantId: string, projectId: string, costId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/projects/${projectId}/costs/${costId}`);
  },

  // Pobierz udostępnione koszty projektowe
  getSharedProjectCosts: async (tenantId: string, projectId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/projects/${projectId}/costs/shared`);
  },

  // Udostępnij koszt projektowy użytkownikom (backend zarządza dodawaniem i usuwaniem)
  shareProjectCost: async (tenantId: string, projectId: string, costId: string, sharedWithUserIds: string[]) => {
    return axiosClient.post(`/tenants/${tenantId}/projects/${projectId}/costs/${costId}/share`, {
      tenantId,
      projectId,
      costId,
      sharedWithUserIds,
    });
  },

  // Pobierz dostępne role projektowe
  getProjectRoles: async (tenantId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/Project/roles`);
  },

  // Zmień rolę członka projektu
  updateProjectMemberRole: async (tenantId: string, projectId: string, userId: string, role: number) => {
    return axiosClient.patch(`/tenants/${tenantId}/Project/${projectId}/members/${userId}/role`, { role });
  },
};
