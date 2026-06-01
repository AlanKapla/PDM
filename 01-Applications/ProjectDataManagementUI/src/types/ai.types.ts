export type CostDocumentType = 'TrackedCost' | 'ProjectCost';

export interface SuggestedContractor {
  name: string;
  nip?: string;
  address?: string;
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
  /** Pewność AI 0–1 */
  confidence: number;
  /** Surowy tekst z dokumentu (debug) */
  rawText?: string;
}

export interface ParseCostDocumentRequest {
  file: File;
  costType: CostDocumentType;
}
