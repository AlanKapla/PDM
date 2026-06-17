import { useEffect, useRef } from 'react';
import type { CostEstimateGroupWeb } from '../types/costEstimate.types.new';
import { scrollToCostEstimateGroup } from '../utils/costEstimateScrollUtils';

/** Po dodaniu etapu — rozwija go i scrolluje w kontenerze widoku. */
export function useNewRootGroupFocus(
  rootGroups: CostEstimateGroupWeb[],
  setExpandedGroups: React.Dispatch<React.SetStateAction<Set<string>>>,
  scrollContainerRef: React.RefObject<HTMLElement | null>,
): void {
  const prevRootGroupIdsRef = useRef<string[]>([]);
  const isInitialMountRef = useRef<boolean>(true);

  useEffect(() => {
    const currentIds: string[] = rootGroups.map((group) => group.id);

    if (isInitialMountRef.current) {
      isInitialMountRef.current = false;
      prevRootGroupIdsRef.current = currentIds;
      return;
    }

    const prevIds: string[] = prevRootGroupIdsRef.current;

    if (currentIds.length > prevIds.length) {
      const newIds: string[] = currentIds.filter((id) => !prevIds.includes(id));
      if (newIds.length > 0) {
        const focusId: string = newIds[newIds.length - 1];
        setExpandedGroups((prev) => {
          const next = new Set(prev);
          for (const id of newIds) {
            next.add(id);
          }
          return next;
        });
        requestAnimationFrame(() => {
          requestAnimationFrame(() => {
            scrollToCostEstimateGroup(focusId, scrollContainerRef.current);
          });
        });
      }
    } else if (currentIds.length === prevIds.length) {
      for (let index = 0; index < currentIds.length; index++) {
        const prevId: string | undefined = prevIds[index];
        const currentId: string = currentIds[index];
        if (prevId && currentId !== prevId) {
          setExpandedGroups((prev) => {
            const next = new Set(prev);
            if (next.has(prevId)) {
              next.delete(prevId);
              next.add(currentId);
            }
            return next;
          });
          break;
        }
      }
    }

    prevRootGroupIdsRef.current = currentIds;
  }, [rootGroups, scrollContainerRef, setExpandedGroups]);
}
