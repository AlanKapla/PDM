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

  it("maps Forbidden to polish title with API message", () => {
    const error = createAxiosError(
      { error: "Forbidden", message: "You do not have permission to edit this resource." },
      403,
    );

    const result = handleApiError(error);

    expect(result.title).toBe("Brak uprawnień");
    expect(result.description).toBe("You do not have permission to edit this resource.");
  });

  it("maps NotFound", () => {
    const error = createAxiosError({ error: "NotFound", message: "Project not found." }, 404);
    expect(handleApiError(error).title).toBe("Nie znaleziono");
  });

  it("maps Conflict", () => {
    const error = createAxiosError({ error: "Conflict", message: "Version mismatch." }, 409);
    expect(handleApiError(error).title).toBe("Konflikt danych");
  });

  it("handles network error without response", () => {
    const error = new AxiosError("Network Error");
    const result = handleApiError(error);
    expect(result.title).toBe("Brak połączenia");
  });

  it("falls back to HTTP status when body has no error field", () => {
    const error = createAxiosError({ message: "oops" }, 503);
    expect(handleApiError(error).title).toBe("Usługa niedostępna");
  });

  it("handles non-axios errors", () => {
    expect(handleApiError(new Error("x")).title).toBe("Błąd");
  });
});
