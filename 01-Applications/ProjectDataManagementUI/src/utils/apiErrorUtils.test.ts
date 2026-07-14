import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from "axios";
import { describe, expect, it } from "vitest";
import {
  getApiErrorMessage,
  getApiExceptionResponse,
  isConflictError,
  isForbiddenError,
  isValidationError,
} from "./apiErrorUtils";

function createAxiosError(data: unknown, status = 400): AxiosError {
  const response: AxiosResponse = {
    data,
    status,
    statusText: "Bad Request",
    headers: {},
    config: {} as InternalAxiosRequestConfig,
  };

  return new AxiosError("Request failed", "ERR_BAD_REQUEST", response.config, {}, response);
}

describe("apiErrorUtils", () => {
  it("parses ApiExceptionResponse from axios error", () => {
    const error = createAxiosError({ error: "Forbidden", message: "Denied" }, 403);
    expect(getApiExceptionResponse(error)).toEqual({
      error: "Forbidden",
      message: "Denied",
    });
  });

  it("detects error reasons", () => {
    expect(isForbiddenError(createAxiosError({ error: "Forbidden", message: "x" }, 403))).toBe(true);
    expect(isConflictError(createAxiosError({ error: "Conflict", message: "x" }, 409))).toBe(true);
    expect(isValidationError(createAxiosError({ error: "ValidationError", message: "x" }))).toBe(true);
  });

  it("returns api error message for inline display", () => {
    const error = createAxiosError({ error: "NotFound", message: "Missing item" }, 404);
    expect(getApiErrorMessage(error)).toBe("Missing item");
  });
});
