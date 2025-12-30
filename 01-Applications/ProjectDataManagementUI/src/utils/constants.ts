/**
 * Shared constants for roles, colors, and configuration
 */

import { ProjectRole } from "../types/project.types";
import { TenantRole } from "../types/auth.types";

/**
 * Project role names in Polish
 */
export const getProjectRoleName = (role: number): string => {
  switch (role) {
    case ProjectRole.Admin:
      return "Administrator";
    case ProjectRole.Editor:
      return "Edytor";
    case ProjectRole.Viewer:
      return "Przeglądający";
    case ProjectRole.Member:
      return "Członek";
    default:
      return "Nieznana rola";
  }
};

/**
 * Project role badge colors
 */
export const getProjectRoleColor = (role: number): string => {
  switch (role) {
    case ProjectRole.Admin:
      return "purple";
    case ProjectRole.Editor:
      return "blue";
    case ProjectRole.Viewer:
      return "green";
    case ProjectRole.Member:
      return "gray";
    default:
      return "gray";
  }
};

/**
 * Tenant role names in Polish
 */
export const getTenantRoleName = (role: number): string => {
  switch (role) {
    case TenantRole.Admin:
      return "Administrator";
    case TenantRole.Member:
      return "Członek";
    case TenantRole.Editor:
      return "Edytor";
    case TenantRole.Viewer:
      return "Przeglądający";
    default:
      return "Nieznana rola";
  }
};

/**
 * Tenant role badge colors
 */
export const getTenantRoleColor = (role: number): string => {
  switch (role) {
    case TenantRole.Admin:
      return "purple";
    case TenantRole.Editor:
      return "blue";
    case TenantRole.Viewer:
      return "green";
    case TenantRole.Member:
      return "gray";
    default:
      return "gray";
  }
};

/**
 * File upload constants
 */
export const FILE_UPLOAD = {
  ALLOWED_TYPES: ['application/pdf', 'image/jpeg', 'image/jpg'],
  MAX_FILE_SIZE: 10 * 1024 * 1024, // 10MB
  ALLOWED_TYPES_DISPLAY: 'PDF, JPG, JPEG',
} as const;

/**
 * Preset colors for work schedule
 */
export const WORK_SCHEDULE_COLORS = [
  "#3182CE", // blue
  "#38A169", // green
  "#DD6B20", // orange
  "#E53E3E", // red
  "#805AD5", // purple
  "#D69E2E", // yellow
  "#00B5D8", // cyan
  "#D53F8C", // pink
  "#319795", // teal
  "#718096", // gray
] as const;

/**
 * Toast durations
 */
export const TOAST_DURATION = {
  SHORT: 2000,
  NORMAL: 3000,
  LONG: 5000,
} as const;
