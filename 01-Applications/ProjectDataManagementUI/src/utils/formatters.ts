/**
 * Utility functions for common formatting operations
 */

/**
 * Format file size from bytes to human readable format
 */
export const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
};

/**
 * Format date to Polish locale format
 */
export const formatDate = (dateString: string | Date | null | undefined, includeTime = true): string => {
  if (!dateString) return "-";
  
  const date = typeof dateString === "string" ? new Date(dateString) : dateString;
  
  if (isNaN(date.getTime())) return "-";
  
  const options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "long",
    day: "numeric",
  };

  if (includeTime) {
    options.hour = "2-digit";
    options.minute = "2-digit";
  }

  return date.toLocaleDateString("pl-PL", options);
};

/**
 * Format date to short format (DD.MM.YYYY)
 */
export const formatDateShort = (dateString: string | Date | null | undefined): string => {
  if (!dateString) return "-";
  
  const date = typeof dateString === "string" ? new Date(dateString) : dateString;
  
  if (isNaN(date.getTime())) return "-";
  
  return date.toLocaleDateString("pl-PL", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  });
};

/**
 * Format date for input[type="date"] (YYYY-MM-DD)
 */
export const formatDateForInput = (date: Date = new Date()): string => {
  return date.toISOString().split('T')[0];
};

/**
 * Get relative time (e.g., "2 godziny temu")
 */
export const getRelativeTime = (dateString: string | Date | null | undefined): string => {
  if (!dateString) return "-";
  
  const date = typeof dateString === "string" ? new Date(dateString) : dateString;
  
  if (isNaN(date.getTime())) return "-";
  
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return "przed chwilą";
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)} min temu`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)} godz. temu`;
  if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)} dni temu`;
  
  return formatDate(date, false);
};

/**
 * Truncate text to specified length
 */
export const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text;
  return text.substring(0, maxLength) + "...";
};

/**
 * Get file extension from filename
 */
export const getFileExtension = (filename: string): string => {
  const parts = filename.split(".");
  return parts.length > 1 ? parts[parts.length - 1].toLowerCase() : "";
};

/**
 * Check if file is an image
 */
export const isImageFile = (filename: string): boolean => {
  const imageExtensions = ["jpg", "jpeg", "png", "gif", "bmp", "webp", "svg"];
  return imageExtensions.includes(getFileExtension(filename));
};

/**
 * Check if file is a PDF
 */
export const isPdfFile = (filename: string): boolean => {
  return getFileExtension(filename) === "pdf";
};

/**
 * Format currency amount with locale format
 */
export const formatCurrency = (amount: number | null | undefined, currency: string = 'PLN'): string => {
  if (amount === undefined || amount === null) return `0,00 ${currency}`;
  return `${amount.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currency}`;
};
