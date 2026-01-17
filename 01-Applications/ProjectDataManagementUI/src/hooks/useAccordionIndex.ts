import { useMemo } from 'react';

/**
 * Custom hook to calculate accordion indices from expanded IDs and files array.
 * Optimized to use O(n+m) complexity instead of O(n*m) by using a Map for lookups.
 * 
 * @param expandedIds - Set of IDs that should be expanded
 * @param files - Array of file/package objects with id property
 * @returns Array of indices corresponding to expanded items
 */
export function useAccordionIndex<T extends { id: string }>(
  expandedIds: Set<string>,
  files: T[]
): number[] {
  // Create a map from file ID to index for O(1) lookups
  const fileIdToIndex = useMemo(() => {
    return new Map(files.map((f, i) => [f.id, i]));
  }, [files]);

  // Calculate indices using the map
  const accordionIndex = useMemo(() => {
    return Array.from(expandedIds)
      .map(id => fileIdToIndex.get(id))
      .filter((i): i is number => i !== undefined);
  }, [expandedIds, fileIdToIndex]);

  return accordionIndex;
}
