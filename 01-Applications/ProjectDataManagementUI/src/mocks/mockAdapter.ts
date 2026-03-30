/**
 * Niestandardowy adapter axios dla trybu demo.
 * Zastępuje rzeczywiste żądanie HTTP odpowiedzią z danych mockowych.
 *
 * Podpinany jako `adapter` przy tworzeniu instancji axios (nie w interceptorze),
 * dzięki czemu żadne żądanie HTTP nigdy nie wychodzi do sieci.
 */

import type { InternalAxiosRequestConfig, AxiosResponse } from "axios";
import { resolveHandler } from "./handlers";

/**
 * Właściwy adapter axios – przyjmuje config jako argument (kontrakt axios).
 * URL może być ścieżką relatywną ("/user/me") lub pełnym URL – oba przypadki obsługiwane.
 */
export async function demoAxiosAdapter(
  config: InternalAxiosRequestConfig
): Promise<AxiosResponse> {
  // Wytnij baseURL i prefix /api, bo handlery operują na ścieżkach relatywnych
  const rawUrl = config.url ?? "";
  // Usuń pełny baseURL jeśli axios zmontował cały URL w config.url
  const pathOnly = rawUrl
    .replace(/^https?:\/\/[^/]+/, "") // usuń schemat + host
    .replace(/^\/api/, "");           // usuń prefix /api

  const result = resolveHandler(
    config.method?.toUpperCase() ?? "GET",
    pathOnly,
    config.data ? tryParse(config.data) : undefined
  );

  // Symulacja opóźnienia sieciowego (50–150ms) dla realistycznego UX
  await delay(50 + Math.random() * 100);

  return {
    data: result?.data ?? null,
    status: result?.status ?? 200,
    statusText: "OK",
    headers: { "content-type": "application/json" },
    config,
    request: {},
  };
}

function tryParse(data: unknown): unknown {
  if (typeof data === "string") {
    try {
      return JSON.parse(data);
    } catch {
      return data;
    }
  }
  return data;
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
