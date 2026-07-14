/**
 * Utility functions for common formatting operations
 */

import {
  formatDateCompactLocal,
  formatDateLocal,
  formatDateShortLocal,
  formatDateTimeCompactLocal,
  formatDateTimeLocal,
  formatTimeLocal,
  parseApiDateTime,
} from './dateTimeUtils';

export { parseApiDateTime } from './dateTimeUtils';

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
 * Format date to Polish locale format in the user's local timezone.
 * API timestamps are stored in UTC.
 */
export const formatDate = (dateString: string | Date | null | undefined, includeTime = true): string => {
  if (!dateString) return "-";

  if (includeTime) {
    return formatDateTimeLocal(dateString);
  }

  return formatDateLocal(dateString);
};

/**
 * Format date to short format (DD.MM.YYYY) in the user's local timezone.
 */
export const formatDateShort = (dateString: string | Date | null | undefined): string => {
  return formatDateShortLocal(dateString);
};

/**
 * Format time (HH:MM) in the user's local timezone.
 */
export const formatTime = (dateString: string | Date | null | undefined): string => {
  return formatTimeLocal(dateString);
};

/**
 * Compact date-time format for comments and similar UI.
 */
export const formatDateTimeCompact = (dateString: string | Date | null | undefined): string => {
  return formatDateTimeCompactLocal(dateString);
};

/**
 * Compact date format for comments and similar UI.
 */
export const formatDateCompact = (dateString: string | Date | null | undefined): string => {
  return formatDateCompactLocal(dateString);
};

/**
 * Format date for input[type="date"] (YYYY-MM-DD) using local calendar date.
 */
export const formatDateForInput = (date: Date = new Date()): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

/**
 * Get relative time (e.g., "2 godziny temu")
 */
export const getRelativeTime = (dateString: string | Date | null | undefined): string => {
  const date = parseApiDateTime(dateString);

  if (!date) return "-";

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
