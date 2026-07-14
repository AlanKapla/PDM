import { useCallback } from 'react';
import { useDashboardCurrency } from '../context/DashboardCurrencyContext';
import { PLN } from '../utils/formatters';

export interface UseChartAmountResult {
  formatValue: (value: number | null) => string;
}

export function useChartAmount(): UseChartAmountResult {
  const currencySymbol = useDashboardCurrency();

  const formatValue = useCallback(
    (value: number | null): string => PLN(value, currencySymbol),
    [currencySymbol]
  );

  return { formatValue };
}
