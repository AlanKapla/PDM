import { AxiosError, isAxiosError } from "axios";
import type { ApiExceptionReason, ApiExceptionResponse } from "../types/apiError.types";
import { handleApiError } from "./handleApiError";

const API_EXCEPTION_REASONS = new Set<string>([
  "ValidationError",
  "NotFound",
  "Unauthorized",
  "Forbidden",
  "Conflict",
  "InvalidOperation",
  "InternalServerError",
]);

export function isAxiosApiError(error: unknown): error is AxiosError {
  return isAxiosError(error);
}

export function getApiExceptionResponse(error: unknown): ApiExceptionResponse | null {
  if (!isAxiosApiError(error) || !error.response?.data) {
    return null;
  }

  const data = error.response.data;
  if (
    typeof data === "object" &&
    data !== null &&
    "error" in data &&
    typeof (data as ApiExceptionResponse).error === "string"
  ) {
    return data as ApiExceptionResponse;
  }

  return null;
}

function hasReason(error: unknown, reason: ApiExceptionReason): boolean {
  const response = getApiExceptionResponse(error);
  return response?.error === reason;
}

export function isValidationError(error: unknown): boolean {
  return hasReason(error, "ValidationError");
}

export function isNotFoundError(error: unknown): boolean {
  return hasReason(error, "NotFound");
}

export function isForbiddenError(error: unknown): boolean {
  return hasReason(error, "Forbidden");
}

export function isConflictError(error: unknown): boolean {
  return hasReason(error, "Conflict");
}

export function isUnauthorizedError(error: unknown): boolean {
  return hasReason(error, "Unauthorized") || (isAxiosApiError(error) && error.response?.status === 401);
}

export function isApiExceptionReason(value: string): value is ApiExceptionReason {
  return API_EXCEPTION_REASONS.has(value);
}

/** Zwraca komunikat do wyświetlenia inline (bez toastu). */
export function getApiErrorMessage(error: unknown): string {
  const { title, description } = handleApiError(error);
  return description ?? title;
}
