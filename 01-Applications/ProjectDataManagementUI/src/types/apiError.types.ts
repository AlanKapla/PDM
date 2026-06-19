export type ApiExceptionReason =
  | "ValidationError"
  | "NotFound"
  | "Unauthorized"
  | "Forbidden"
  | "Conflict"
  | "InvalidOperation"
  | "InternalServerError";

export interface ApiExceptionResponse {
  error: string;
  message: string;
  objectType?: string;
  objectId?: string;
}

export interface ApiErrorResult {
  title: string;
  description?: string;
  toastStatus?: "error" | "warning" | "info";
}
