/**
 * Tree View State Management Hook
 * Manages expanded/collapsed state and sorting
 */

import { useState, useCallback } from 'react';

export interface TreeViewState {
  expandedGroups: Set<string>;
  sortColumn: string | null;
  sortDirection: 'asc' | 'desc';
}

export function useTreeViewState(initialGroupIds: string[]) {
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
    new Set(initialGroupIds)
  );

  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');

  const toggleGroup = useCallback((groupId: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(groupId)) {
        next.delete(groupId);
      } else {
        next.add(groupId);
      }
      return next;
    });
  }, []);

  const expandAll = useCallback((groupIds: string[]) => {
    setExpandedGroups(new Set(groupIds));
  }, []);

  const collapseAll = useCallback(() => {
    setExpandedGroups(new Set());
  }, []);

  const toggleSort = useCallback((column: string) => {
    setSortColumn((prev) => {
      if (prev === column) {
        setSortDirection((dir) => (dir === 'asc' ? 'desc' : 'asc'));
        return column;
      }
      setSortDirection('asc');
      return column;
    });
  }, []);

  return {
    expandedGroups,
    sortColumn,
    sortDirection,
    toggleGroup,
    expandAll,
    collapseAll,
    toggleSort,
  };
}
