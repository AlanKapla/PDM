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
