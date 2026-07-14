/**
 * Hook zwracający wspólny stan pól pozycji kosztorysu (flagi kalkulacji, blokady).
 * Używany przez Tree View i Card View.
 */

import { useMemo } from 'react';
import type { CostEstimateItemWeb } from '../types/costEstimate.types.new';
import {
  getCostEstimateItemFieldState,
  type CostEstimateItemFieldState,
} from '../utils/costEstimateItemFlags';

export function useCostEstimateItemFieldState(
  item: CostEstimateItemWeb,
): CostEstimateItemFieldState {
  return useMemo(() => getCostEstimateItemFieldState(item), [item]);
}
