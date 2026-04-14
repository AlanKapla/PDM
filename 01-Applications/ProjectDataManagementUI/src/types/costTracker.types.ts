// ===== Enums =====

export enum TrackedCostItemStatus {
  NoCosts    = 0,
  NoBudget   = 1,
  InProgress = 2,
  NearLimit  = 3,
  OverBudget = 4,
}

export const ITEM_STATUS_CONFIG: Record<
  TrackedCostItemStatus,
  { label: string; color: string }
> = {
  [TrackedCostItemStatus.NoCosts]:    { label: 'Brak kosztów',   color: 'gray'   },
  [TrackedCostItemStatus.NoBudget]:   { label: 'Brak budżetu',   color: 'purple' },
  [TrackedCostItemStatus.InProgress]: { label: 'W realizacji',   color: 'green'  },
  [TrackedCostItemStatus.NearLimit]:  { label: 'Blisko limitu',  color: 'orange' },
  [TrackedCostItemStatus.OverBudget]: { label: 'Przekroczono',   color: 'red'    },
};

// ===== Core models =====

export interface TrackedCostAttachmentWeb {
  id: string;
  originalFileName: string;
  fileUrl: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
}

export interface TrackedCostWeb {
  id: string;
  trackerId: string;
  costEstimateId: string | null;
  costEstimateItemId: string | null;
  isAdditional: boolean;
  name: string;
  description: string | null;
  net: number | null;
  gross: number | null;
  vatAmount: number | null;
  vatRate: number | null;
  contractor: string | null;
  date: string | null;
  createdAt: string;
  updatedAt: string | null;
  attachments: TrackedCostAttachmentWeb[];
}

// Wspólna baza dla grupy i pozycji
export interface TrackerNodeWeb {
  budgetNet: number | null;
  budgetGross: number | null;
  costsNet: number | null;
  costsGross: number | null;
  deviationNet: number | null;
  deviationPercent: number | null;
  isBudgetExceeded: boolean;
  status: TrackedCostItemStatus;
  costCount: number;
  coveredPercent: number | null;
}

export interface TrackerItemWeb extends TrackerNodeWeb {
  costEstimateItemId: string;
  name: string;
  costs: TrackedCostWeb[];
}

export interface TrackerGroupWeb extends TrackerNodeWeb {
  groupId: string;
  groupName: string;
  order: number;
  totalItemsCount: number;
  itemsWithCostsCount: number;
  items: TrackerItemWeb[];
  childGroups: TrackerGroupWeb[];
}

export interface TrackerAdditionalCostsWeb {
  totalNet: number | null;
  totalGross: number | null;
  costsCount: number;
  costs: TrackedCostWeb[];
}

export interface ProjectAdditionalCostsWeb {
  totalNet: number | null;
  totalGross: number | null;
  costsCount: number;
  costs: TrackedCostWeb[];
}

export interface CostTrackerSummaryBaseWeb {
  totalCostsNet: number | null;
  totalCostsGross: number | null;
  totalBudgetNet: number | null;
  totalBudgetGross: number | null;
  totalDeviationNet: number | null;
  totalDeviationGross: number | null;
  totalDeviationPercent: number | null;
  isBudgetExceeded: boolean;
  additionalCostsNet: number | null;
  additionalCostsGross: number | null;
  additionalCostsCount: number;
  costCount: number;
  coveredPercent: number | null;
}

export interface CostTrackerSummaryWeb extends CostTrackerSummaryBaseWeb {
  costEstimatesCount: number;
  costEstimatesWithCostsCount: number;
}

/** Podsumowanie budżetowe wyłącznie dla kosztów dodatkowych projektu vs. budżet trackera */
export interface CostTrackerBudgetSummary extends CostTrackerSummaryBaseWeb {}

export interface CostEstimateSummaryWeb extends CostTrackerSummaryBaseWeb {
  costEstimateId: string;
  costEstimateName: string;
  totalItemsCount: number;
  itemsWithCostsCount: number;
  itemsWithoutCostsCount: number;
  itemsOverBudgetCount: number;
  itemsNearLimitCount: number;
  groups: TrackerGroupWeb[];
  additionalCosts: TrackerAdditionalCostsWeb;
}

export interface CostTrackerDetailsWeb {
  id: string;
  projectId: string;
  summary: CostTrackerSummaryWeb;
  budgetSummary: CostTrackerBudgetSummary;
  costEstimateSummaries: CostEstimateSummaryWeb[];
  projectAdditionalCosts: ProjectAdditionalCostsWeb;
}

/** Dane trackera dla pojedynczej pozycji kosztorysu */
export interface CostEstimateItemCostsWeb {
  costsCount: number;
  costsNet: number | null;
  costsGross: number | null;
  deviationNet: number | null;
  deviationPercent: number | null;
  isBudgetExceeded: boolean;
  status: TrackedCostItemStatus;
}

// ===== Form / request types (camelCase — nowe komponenty) =====

export interface CostFormValues {
  name: string;
  description?: string;
  net?: number | string;
  gross?: number | string;
  contractor?: string;
  date?: string;
  newFiles?: File[];
  existingAttachmentIds?: string[];
}

export interface CreateCostRequest {
  name: string;
  description?: string;
  net?: number;
  gross?: number;
  contractor?: string;
  date?: string;
  costEstimateId?: string | null;
  costEstimateItemId?: string | null;
  newFiles?: File[];
}

export interface UpdateCostRequest extends CreateCostRequest {
  existingAttachmentIds?: string[];
}

// ===== DTO dla starszych komponentów (PascalCase) =====

export interface TrackedCostFormValues {
  name: string;
  description: string;
  net: string;
  gross: string;
  contractor: string;
  date: string;
  costEstimateId: string | null;
  costEstimateItemId: string | null;
}

export interface CreateTrackedCostRequest {
  CostEstimateId?: string;
  CostEstimateItemId?: string;
  Name: string;
  Description?: string;
  Net?: number;
  Gross?: number;
  Contractor?: string;
  Date?: string;
  NewFiles?: File[];
}

export interface UpdateTrackedCostRequest {
  CostEstimateId?: string;
  CostEstimateItemId?: string;
  Name: string;
  Description?: string;
  Net?: number;
  Gross?: number;
  Contractor?: string;
  Date?: string;
  ExistingAttachmentIds?: string[];
  NewFiles?: File[];
}
