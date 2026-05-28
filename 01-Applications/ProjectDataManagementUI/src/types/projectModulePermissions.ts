export const ProjectModule = {
  Settings: 0,
  Files: 2,
  Estimates: 3,
  Costs: 4,
  Schedule: 5,
  DashboardTracker: 6,
} as const;

export type ProjectModule = (typeof ProjectModule)[keyof typeof ProjectModule];

export const PROJECT_MODULE_LABELS: Record<ProjectModule, string> = {
  [ProjectModule.Settings]: "Ustawienia",
  [ProjectModule.Files]: "Pliki",
  [ProjectModule.Estimates]: "Kosztorysy",
  [ProjectModule.Costs]: "Wydatki",
  [ProjectModule.Schedule]: "Harmonogramy",
  [ProjectModule.DashboardTracker]: "Śledzenie kosztów",
};
