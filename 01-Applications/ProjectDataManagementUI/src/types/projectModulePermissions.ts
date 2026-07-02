export const ProjectModule = {
  Settings: 0,
  Files: 2,
  Estimates: 3,
  Costs: 4,
  Schedule: 5,
  DashboardTracker: 6,
  TechnicalDocumentation: 7,
} as const;

export type ProjectModule = (typeof ProjectModule)[keyof typeof ProjectModule];

export const PROJECT_MODULE_LABELS: Record<ProjectModule, string> = {
  [ProjectModule.Settings]: "Ustawienia",
  [ProjectModule.Files]: "Pliki",
  [ProjectModule.Estimates]: "Kosztorysy",
  [ProjectModule.Costs]: "Wydatki",
  [ProjectModule.Schedule]: "Harmonogramy",
  [ProjectModule.DashboardTracker]: "Śledzenie kosztów",
  [ProjectModule.TechnicalDocumentation]: "Dokumentacja techniczna",
};

/** Domyślny preset modułów przy zaproszeniu e-mailem do projektu */
export const DEFAULT_INVITE_PROJECT_MODULES: ProjectModule[] = [
  ProjectModule.Files,
  ProjectModule.Estimates,
  ProjectModule.Schedule,
];
