import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from "axios";
import { describe, expect, it } from "vitest";
import { handleApiError } from "./handleApiError";

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

describe("handleApiError", () => {
  it("maps already tenant member validation error to friendly polish info", () => {
    const error = createAxiosError({
      error: "ValidationError",
      message:
        "Property name: , Error: User is already a member of this tenant., Severity: Error",
    });

    const result = handleApiError(error);

    expect(result.title).toBe("Użytkownik jest już w organizacji");
    expect(result.description).toBe("Ta osoba jest już członkiem tej organizacji.");
    expect(result.toastStatus).toBe("info");
  });

  it("maps already project member validation error to friendly polish info", () => {
    const error = createAxiosError({
      error: "ValidationError",
      message:
        "Property name: , Error: User is already a member of this project., Severity: Error",
    });

    const result = handleApiError(error);

    expect(result.title).toBe("Użytkownik jest już w projekcie");
    expect(result.description).toBe("Ta osoba jest już członkiem tego projektu.");
    expect(result.toastStatus).toBe("info");
  });

  it("maps multiple validation errors when first is already member", () => {
    const error = createAxiosError({
      error: "ValidationError",
      message:
        "Property name: , Error: User is already a member of this project., Severity: Error, Property name: , Error: User is already a member of this tenant., Severity: Error",
    });

    const result = handleApiError(error);

    expect(result.title).toBe("Użytkownik jest już w projekcie");
    expect(result.toastStatus).toBe("info");
  });

  it("keeps generic validation title for unknown validation messages", () => {
    const error = createAxiosError({
      error: "ValidationError",
      message:
        "Property name: Email, Error: Invalid email format, Severity: Error",
    });

    const result = handleApiError(error);

    expect(result.title).toBe("Niepoprawne dane");
    expect(result.description).toBe("Invalid email format");
    expect(result.toastStatus).toBe("error");
  });
});
