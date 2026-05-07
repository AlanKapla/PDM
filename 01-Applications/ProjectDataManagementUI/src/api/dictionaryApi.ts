import { axiosClient } from "./axiosClient";
import type { CurrencyWeb } from "../types/dictionary.types";

export const dictionaryApi = {
  // Pobierz słownik dostępnych walut
  getCurrencies: async (): Promise<CurrencyWeb[]> => {
    const response = await axiosClient.get<CurrencyWeb[]>("/dictionary/currencies");
    return response.data;
  },
};
