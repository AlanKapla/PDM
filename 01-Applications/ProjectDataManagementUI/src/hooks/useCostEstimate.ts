import { useState, useEffect, useCallback } from 'react';
import { costEstimateApiNew } from '../api/costEstimateApiNew';
import type {
  CostEstimateDetailsWeb,
  CostEstimateListItemWeb,
  UpdateCostEstimateDto,
  CostEstimateGroupDto,
} from '../types/costEstimate.types.new';
import { convertDetailsWebToUpdateDto, createEmptyGroup } from '../types/costEstimate.types.new';
import { ResourceScope } from '../api/projectApi';

/**
 * Hook for loading cost estimate details
 */
export function useCostEstimateDetails(
  tenantId: string,
  projectId: string,
  costEstimateId: string
) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [details, setDetails] = useState<CostEstimateDetailsWeb | null>(null);

  const loadDetails = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const data = await costEstimateApiNew.getCostEstimateDetails(
        tenantId,
        projectId,
        costEstimateId
      );
      
      setDetails(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas ładowania kosztorysu');
      console.error('Error loading cost estimate details:', err);
    } finally {
      setLoading(false);
    }
  }, [tenantId, projectId, costEstimateId]);

  useEffect(() => {
    loadDetails();
  }, [loadDetails]);

  return {
    loading,
    error,
    details,
    reload: loadDetails,
  };
}

/**
 * Hook for loading cost estimate list
 */
export function useCostEstimateList(
  tenantId: string,
  projectId: string,
  scope: ResourceScope = ResourceScope.Mine
) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [items, setItems] = useState<CostEstimateListItemWeb[]>([]);

  const loadList = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const data = await costEstimateApiNew.getCostEstimatesByScope(
        tenantId,
        projectId,
        scope
      );
      
      setItems(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas ładowania listy kosztorysów');
      console.error('Error loading cost estimate list:', err);
    } finally {
      setLoading(false);
    }
  }, [tenantId, projectId, scope]);

  useEffect(() => {
    loadList();
  }, [loadList]);

  return {
    loading,
    error,
    items,
    reload: loadList,
  };
}

/**
 * Hook for managing cost estimate editor state
 */
export function useCostEstimateEditor(
  tenantId: string,
  projectId: string,
  costEstimateId: string
) {
  const { loading, error, details, reload } = useCostEstimateDetails(
    tenantId,
    projectId,
    costEstimateId
  );

  const [editedData, setEditedData] = useState<UpdateCostEstimateDto | null>(null);
  const [hasChanges, setHasChanges] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  // Initialize edited data when details are loaded
  useEffect(() => {
    if (details) {
      const dto = convertDetailsWebToUpdateDto(details);
      setEditedData(dto);
      setHasChanges(false);
    }
  }, [details]);

  // Update edited data
  const updateData = useCallback((updater: (prev: UpdateCostEstimateDto) => UpdateCostEstimateDto) => {
    setEditedData((prev: UpdateCostEstimateDto | null) => {
      if (!prev) return prev;
      const updated = updater(prev);
      setHasChanges(true);
      return updated;
    });
  }, []);

  // Add root group
  const addRootGroup = useCallback(() => {
    updateData((prev) => ({
      ...prev,
      rootGroups: [...prev.rootGroups, createEmptyGroup(0, prev.rootGroups.length)],
    }));
  }, [updateData]);

  // Update root group
  const updateRootGroup = useCallback((index: number, updatedGroup: CostEstimateGroupDto) => {
    updateData((prev) => {
      const newGroups = [...prev.rootGroups];
      newGroups[index] = updatedGroup;
      return { ...prev, rootGroups: newGroups };
    });
  }, [updateData]);

  // Delete root group
  const deleteRootGroup = useCallback((index: number) => {
    updateData((prev) => ({
      ...prev,
      rootGroups: prev.rootGroups.filter((_: CostEstimateGroupDto, idx: number) => idx !== index),
    }));
  }, [updateData]);

  // Save changes
  const save = useCallback(async () => {
    if (!editedData) return false;

    try {
      setSaving(true);
      setSaveError(null);

      await costEstimateApiNew.updateCostEstimate(
        tenantId,
        projectId,
        costEstimateId,
        editedData
      );

      // Reload details to get calculated values
      await reload();
      setHasChanges(false);
      
      return true;
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : 'Błąd podczas zapisywania');
      console.error('Error saving cost estimate:', err);
      return false;
    } finally {
      setSaving(false);
    }
  }, [tenantId, projectId, costEstimateId, editedData, reload]);

  // Reset changes
  const reset = useCallback(() => {
    if (details) {
      const dto = convertDetailsWebToUpdateDto(details);
      setEditedData(dto);
      setHasChanges(false);
      setSaveError(null);
    }
  }, [details]);

  return {
    // Loading state
    loading,
    error,
    details,

    // Edit state
    editedData,
    hasChanges,
    saving,
    saveError,

    // Actions
    updateData,
    addRootGroup,
    updateRootGroup,
    deleteRootGroup,
    save,
    reset,
    reload,
  };
}

/**
 * Hook for managing group hierarchy operations
 */
export function useGroupHierarchy() {
  // Find group by id in hierarchy
  const findGroup = useCallback(
    (groups: CostEstimateGroupDto[], groupId: string): CostEstimateGroupDto | null => {
      for (const group of groups) {
        if (group.id === groupId) {
          return group;
        }
        if (group.childGroups.length > 0) {
          const found = findGroup(group.childGroups, groupId);
          if (found) return found;
        }
      }
      return null;
    },
    []
  );

  // Get all groups flattened
  const flattenGroups = useCallback(
    (groups: CostEstimateGroupDto[]): CostEstimateGroupDto[] => {
      const result: CostEstimateGroupDto[] = [];
      
      const flatten = (groupList: CostEstimateGroupDto[]) => {
        for (const group of groupList) {
          result.push(group);
          if (group.childGroups.length > 0) {
            flatten(group.childGroups);
          }
        }
      };
      
      flatten(groups);
      return result;
    },
    []
  );

  // Count total groups
  const countGroups = useCallback((groups: CostEstimateGroupDto[]): number => {
    return flattenGroups(groups).length;
  }, [flattenGroups]);

  // Count total items
  const countItems = useCallback((groups: CostEstimateGroupDto[]): number => {
    return flattenGroups(groups).reduce(
      (sum, group) => sum + group.items.length,
      0
    );
  }, [flattenGroups]);

  // Get max level
  const getMaxLevel = useCallback((groups: CostEstimateGroupDto[]): number => {
    const allGroups = flattenGroups(groups);
    return allGroups.length > 0 ? Math.max(...allGroups.map(g => g.level)) : 0;
  }, [flattenGroups]);

  return {
    findGroup,
    flattenGroups,
    countGroups,
    countItems,
    getMaxLevel,
  };
}
