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
  workScheduleStageWorkId: string | null;
  isAdditional: boolean;
  name: string;
  number: string | null;
  description: string | null;
  net: number | null;
  gross: number | null;
  vatAmount: number | null;
  vatRate: number | null;
  contractorId: string | null;
  contractorName: string | null;
  date: string | null;
  createdAt: string;
  updatedAt: string | null;
  attachments: TrackedCostAttachmentWeb[];
  // Kontekst powiązania
  estimateName: string | null;
  estimateGroupName: string | null;
  estimateItemName: string | null;
  /** Pełna ścieżka pozycji kosztorysu np. "KosztorysA > Folder > Pozycja". */
  costEstimateItemPath: string | null;
  /** Pełna ścieżka zakresu pracy np. "HarmonogramA > Etap > Praca". */
  workScheduleWorkPath: string | null;
  scheduleName: string | null;
  stageName: string | null;
  workItemName: string | null;
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
  number?: string;
  contractorId?: string | null;
  date?: string;
  newFiles?: File[];
  existingAttachmentIds?: string[];
}

export interface CreateCostRequest {
  name: string;
  description?: string;
  net?: number;
  gross?: number;
  number?: string;
  contractorId?: string | null;
  date?: string;
  costEstimateId?: string | null;
  costEstimateItemId?: string | null;
  newFiles?: File[];
}

export interface UpdateCostRequest extends CreateCostRequest {
  workScheduleStageWorkId?: string | null;
  existingAttachmentIds?: string[];
}

// ===== Cost link options =====

export interface EstimateItemLinkOptionWeb {
  itemId: string;
  path: string;
  /** ID zakresu pracy spiętego z tą pozycją. Null gdy brak spięcia. */
  linkedWorkId: string | null;
}

export interface WorkLinkOptionWeb {
  workId: string;
  path: string;
  /** ID pozycji kosztorysu spiętej z tym zakresem. Null gdy brak spięcia. */
  linkedItemId: string | null;
}

export interface CostLinkOptionsWeb {
  estimateItems: EstimateItemLinkOptionWeb[];
  workItems: WorkLinkOptionWeb[];
}

