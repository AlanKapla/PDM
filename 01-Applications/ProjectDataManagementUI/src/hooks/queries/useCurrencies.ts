import { useQuery } from "@tanstack/react-query";
import { dictionaryApi } from "../../api/dictionaryApi";
import type { CurrencyWeb } from "../../types/dictionary.types";

export const currencyKeys = {
  all: ["dictionary", "currencies"] as const,
};

export function useCurrencies() {
  return useQuery<CurrencyWeb[]>({
    queryKey: currencyKeys.all,
    queryFn: dictionaryApi.getCurrencies,
    staleTime: Infinity,
    gcTime: Infinity,
  });
}
