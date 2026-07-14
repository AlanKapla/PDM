export type CostDocumentType = 'TrackedCost' | 'ProjectCost';

export interface SuggestedContractor {
  name: string;
  nip?: string;
  address?: string;
}

export interface SuggestedCostCategory {
  name: string;
  code?: string;
}

export interface ParsedCostDto {
  /** Nazwa kosztu — co zostało zakupione */
  name: string;
  /** Rozszerzony opis z detalami */
  description?: string;
  /** Numer faktury/rachunku */
  number?: string;
  /** Suma netto całego dokumentu */
  net?: number;
  /** Suma brutto całego dokumentu */
  gross?: number;
  /** Data wystawienia (ISO 8601 string) */
  date?: string;
  /** GUID kontrahenta — wypełniony gdy contractorFound = true */
  contractorId?: string;
  /** Nazwa kontrahenta z dokumentu */
  contractorName?: string;
  /** NIP kontrahenta z dokumentu */
  contractorNip?: string;
  /** Adres kontrahenta z dokumentu */
  contractorAddress?: string;
  /** Czy kontrahent znaleziony w bazie */
  contractorFound: boolean;
  /** Sugestia nowego kontrahenta gdy nie znaleziono w bazie */
  suggestedContractor?: SuggestedContractor;
  /** GUID kategorii — wypełniony gdy categoryFound = true */
  categoryId?: string;
  /** Nazwa kategorii z dokumentu */
  categoryName?: string;
  /** Czy kategoria znaleziona w słowniku projektu */
  categoryFound: boolean;
  /** Sugestia nowej kategorii gdy nie znaleziono w słowniku */
  suggestedCategory?: SuggestedCostCategory;
  /** Pewność AI 0–1 */
  confidence: number;
  /** Surowy tekst z dokumentu (debug) */
  rawText?: string;
}

export interface ParseCostDocumentRequest {
  file: File;
  costType: CostDocumentType;
}

export type AICostImportItemStatus =
  | 'Queued'
  | 'Processing'
  | 'Pending'
  | 'ErrorNeedsReview'
  | 'Accepted'
  | 'Rejected'
  | 'DuplicateDetected'
  | 'ExpiredDeleted';

export interface TrackedCostContext {
  costEstimateItemId?: string;
  workScheduleStageWorkId?: string;
}

export interface SubmitAICostImportBatchRequest {
  files: File[];
  costType: CostDocumentType;
  trackedCostContext?: TrackedCostContext;
}

export interface AICostImportBatchWeb {
  id: string;
  tenantId: string;
  projectId: string;
  costDocumentType: CostDocumentType;
  status: string;
  totalFiles: number;
  processedFiles: number;
  pendingCount: number;
  errorCount: number;
  duplicateCount: number;
  createdAt: string;
  completedAt?: string | null;
}

export interface AICostImportItemWeb {
  id: string;
  batchId: string;
  tenantId: string;
  projectId: string;
  status: AICostImportItemStatus;
  costDocumentType: CostDocumentType;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  parsedData?: ParsedCostDto | null;
  lastError?: string | null;
  previewUrl?: string | null;
  analyzedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateAICostImportItemRequest {
  parsedData: ParsedCostDto;
}

export interface PendingAICostImportCountWeb {
  pendingCount: number;
  errorCount: number;
  duplicateCount: number;
}
