import type {
  CostEstimateAdditionalFieldWeb,
  CostEstimateDetailsWeb,
  CostEstimateGroupDto,
  CostEstimateGroupWeb,
  CostEstimateItemDto,
  CostEstimateItemWeb,
} from '../types/costEstimate.types.new';
import { AdditionalFieldType } from '../types/costEstimate.types.new';

// Aliasy dla kompatybilności wstecznej
type CostEstimateWorkScopeItemDto = CostEstimateItemDto;

/**
 * Utility functions for cost estimate hierarchy operations
 */

/**
 * Clone group with all children (deep copy)
 */
export function cloneGroup(group: CostEstimateGroupDto): CostEstimateGroupDto {
  return {
    ...group,
    id: undefined, // Reset ID for new group
    additionalFieldValues: (group.additionalFieldValues ?? []).map((fv) => ({ ...fv })),
    items: group.items.map((item) => cloneWorkScopeItem(item)),
    childGroups: group.childGroups.map((child) => cloneGroup(child)),
  };
}

/**
 * Clone work scope item (deep copy)
 */
export function cloneWorkScopeItem(item: CostEstimateWorkScopeItemDto): CostEstimateWorkScopeItemDto {
  return {
    ...item,
    id: undefined, // Reset ID for new item
    additionalFieldValues: (item.additionalFieldValues ?? []).map((fv) => ({ ...fv })),
  };
}

/**
 * Move group to different parent
 */
export function moveGroup(
  groups: CostEstimateGroupDto[],
  groupId: string,
  newParentId: string | undefined,
  newLevel: number
): CostEstimateGroupDto[] {
  // Find and remove group from current location
  let movedGroup: CostEstimateGroupDto | null = null;

  const removeGroup = (groupList: CostEstimateGroupDto[]): CostEstimateGroupDto[] => {
    return groupList.filter((g) => {
      if (g.id === groupId) {
        movedGroup = g;
        return false;
      }
      g.childGroups = removeGroup(g.childGroups);
      return true;
    });
  };

  let newGroups = removeGroup([...groups]);

  if (!movedGroup) {
    return groups; // Group not found
  }

  // Update group level recursively
  const updateLevels = (group: CostEstimateGroupDto, level: number): CostEstimateGroupDto => {
    return {
      ...group,
      level,
      parentGroupId: level === 0 ? undefined : group.parentGroupId,
      childGroups: group.childGroups.map((child) => updateLevels(child, level + 1)),
    };
  };

  movedGroup = updateLevels(movedGroup, newLevel);
  (movedGroup as CostEstimateGroupDto).parentGroupId = newParentId;

  // Insert group at new location
  if (!newParentId) {
    // Move to root
    newGroups.push(movedGroup);
  } else {
    // Move to specific parent
    const addToParent = (groupList: CostEstimateGroupDto[]): CostEstimateGroupDto[] => {
      return groupList.map((g) => {
        if (g.id === newParentId) {
          return {
            ...g,
            childGroups: [...g.childGroups, movedGroup!],
          };
        }
        return {
          ...g,
          childGroups: addToParent(g.childGroups),
        };
      });
    };

    newGroups = addToParent(newGroups);
  }

  return newGroups;
}

/**
 * Reorder groups (change order indices)
 */
export function reorderGroups(
  groups: CostEstimateGroupDto[],
  fromIndex: number,
  toIndex: number
): CostEstimateGroupDto[] {
  const result = [...groups];
  const [removed] = result.splice(fromIndex, 1);
  result.splice(toIndex, 0, removed);

  // Update order indices
  return result.map((group, index) => ({
    ...group,
    order: index,
  }));
}

/**
 * Reorder work scope items within a group
 */
export function reorderWorkScopeItems(
  items: CostEstimateWorkScopeItemDto[],
  fromIndex: number,
  toIndex: number
): CostEstimateWorkScopeItemDto[] {
  const result = [...items];
  const [removed] = result.splice(fromIndex, 1);
  result.splice(toIndex, 0, removed);

  // Update order indices
  return result.map((item, index) => ({
    ...item,
    order: index,
  }));
}

/**
 * Search groups by name or additional field value
 */
export function searchGroups(
  groups: CostEstimateGroupDto[],
  searchTerm: string,
  fieldName?: string
): CostEstimateGroupDto[] {
  const lowerSearch = searchTerm.toLowerCase();
  const results: CostEstimateGroupDto[] = [];

  const search = (groupList: CostEstimateGroupDto[]) => {
    for (const group of groupList) {
      let matches = false;

      if (fieldName === undefined || fieldName === 'name') {
        // Szukaj po name (direct property)
        if (group.name?.toLowerCase().includes(lowerSearch)) {
          matches = true;
        }
      }

      if (!matches) {
        // Szukaj w additionalFieldValues
        for (const fv of group.additionalFieldValues ?? []) {
          if (fieldName && fv.additionalFieldId !== fieldName) {
            continue;
          }
          const valueStr = fv.stringValue
            ?? (fv.decimalValue !== undefined ? String(fv.decimalValue) : undefined)
            ?? (fv.boolValue !== undefined ? String(fv.boolValue) : undefined)
            ?? fv.dateTimeValue;
          if (valueStr?.toLowerCase().includes(lowerSearch)) {
            matches = true;
            break;
          }
        }
      }

      if (matches) {
        results.push(group);
      }

      // Search in children
      search(group.childGroups);
    }
  };

  search(groups);
  return results;
}

/**
 * Get group path (breadcrumb) from root to group
 */
export function getGroupPath(
  groups: CostEstimateGroupDto[],
  targetGroupId: string
): CostEstimateGroupDto[] {
  const path: CostEstimateGroupDto[] = [];

  const findPath = (groupList: CostEstimateGroupDto[]): boolean => {
    for (const group of groupList) {
      path.push(group);

      if (group.id === targetGroupId) {
        return true;
      }

      if (group.childGroups.length > 0) {
        if (findPath(group.childGroups)) {
          return true;
        }
      }

      path.pop();
    }

    return false;
  };

  findPath(groups);
  return path;
}

/**
 * Validate group hierarchy
 */
export interface ValidationError {
  groupId?: string;
  itemId?: string;
  fieldId?: string;
  message: string;
}

export function validateGroupHierarchy(
  groups: CostEstimateGroupDto[],
  maxLevel?: number,
  canBranchGroups: boolean = true
): ValidationError[] {
  const errors: ValidationError[] = [];

  const validate = (groupList: CostEstimateGroupDto[], parentId?: string) => {
    for (const group of groupList) {
      // Check level
      if (maxLevel !== undefined && group.level > maxLevel) {
        errors.push({
          groupId: group.id,
          message: `Etap przekracza maksymalny poziom zagnieżdżenia (${maxLevel})`,
        });
      }

      // Check parent
      if (group.parentGroupId !== parentId) {
        errors.push({
          groupId: group.id,
          message: 'Nieprawidłowy rodzic etapu',
        });
      }

      // Check branching
      if (!canBranchGroups && group.childGroups.length > 0) {
        errors.push({
          groupId: group.id,
          message: 'Nie można tworzyć podetapów',
        });
      }

      // Validate children
      if (group.childGroups.length > 0) {
        validate(group.childGroups, group.id);
      }
    }
  };

  validate(groups);
  return errors;
}

/**
 * Calculate statistics for groups
 */
export interface GroupStatistics {
  totalGroups: number;
  totalItems: number;
  maxLevel: number;
  groupsByLevel: Record<number, number>;
  itemsByLevel: Record<number, number>;
}

export function calculateStatistics(groups: CostEstimateGroupDto[]): GroupStatistics {
  const stats: GroupStatistics = {
    totalGroups: 0,
    totalItems: 0,
    maxLevel: 0,
    groupsByLevel: {},
    itemsByLevel: {},
  };

  const calculate = (groupList: CostEstimateGroupDto[]) => {
    for (const group of groupList) {
      stats.totalGroups++;
      stats.totalItems += group.items.length;
      stats.maxLevel = Math.max(stats.maxLevel, group.level);

      stats.groupsByLevel[group.level] = (stats.groupsByLevel[group.level] || 0) + 1;
      stats.itemsByLevel[group.level] = (stats.itemsByLevel[group.level] || 0) + group.items.length;

      if (group.childGroups.length > 0) {
        calculate(group.childGroups);
      }
    }
  };

  calculate(groups);
  return stats;
}

/**
 * Export groups to simple array (flattened)
 */
export interface FlattenedGroup {
  group: CostEstimateGroupDto;
  level: number;
  path: string[];
  hasChildren: boolean;
}

export function flattenGroupsWithContext(groups: CostEstimateGroupDto[]): FlattenedGroup[] {
  const result: FlattenedGroup[] = [];

  const flatten = (groupList: CostEstimateGroupDto[], path: string[] = []) => {
    for (const group of groupList) {
      // Nowa architektura: name jest direct property
      const groupName = group.name || `Etap ${group.order + 1}`;

      result.push({
        group,
        level: group.level,
        path: [...path, groupName],
        hasChildren: group.childGroups.length > 0,
      });

      if (group.childGroups.length > 0) {
        flatten(group.childGroups, [...path, groupName]);
      }
    }
  };

  flatten(groups);
  return result;
}

/**
 * Bulk update field values across multiple groups
 * Zaktualizowane: operuje na additionalFieldValues zamiast fieldValues
 */
export function bulkUpdateGroupFields(
  groups: CostEstimateGroupDto[],
  fieldId: string,
  getValue: (group: CostEstimateGroupDto) => string | undefined
): CostEstimateGroupDto[] {
  return groups.map((group) => {
    const newValue = getValue(group);
    const existingIndex = (group.additionalFieldValues ?? []).findIndex(
      (fv) => fv.additionalFieldId === fieldId
    );
    const newAdditionalFieldValues = [...(group.additionalFieldValues ?? [])];

    if (newValue !== undefined && newValue !== '') {
      if (existingIndex >= 0) {
        newAdditionalFieldValues[existingIndex] = {
          ...newAdditionalFieldValues[existingIndex],
          stringValue: newValue,
        };
      } else {
        newAdditionalFieldValues.push({
          id: `temp_${Date.now()}`,
          additionalFieldId: fieldId,
          stringValue: newValue,
        });
      }
    } else if (existingIndex >= 0) {
      newAdditionalFieldValues.splice(existingIndex, 1);
    }

    return {
      ...group,
      additionalFieldValues: newAdditionalFieldValues,
      childGroups: bulkUpdateGroupFields(group.childGroups, fieldId, getValue),
    };
  });
}

export interface CostEstimateTotals {
  net: number;
  gross: number;
  vat: number;
}

function removeItemFromItems(
  items: CostEstimateItemWeb[],
  itemId: string,
): CostEstimateItemWeb[] {
  return items
    .filter((item) => item.id !== itemId)
    .map((item) => ({
      ...item,
      options: item.options ? removeItemFromItems(item.options, itemId) : item.options,
      components: item.components
        ? removeItemFromItems(item.components, itemId)
        : item.components,
    }));
}

/**
 * Usuwa pozycję/komponent/opcję z drzewa kosztorysu (wszystkie poziomy zagnieżdżenia).
 */
export function removeItemFromCostEstimateTree(
  groups: CostEstimateGroupWeb[],
  itemId: string,
): CostEstimateGroupWeb[] {
  return groups.map((group) => ({
    ...group,
    items: removeItemFromItems(group.items ?? [], itemId),
    childGroups: removeItemFromCostEstimateTree(group.childGroups ?? [], itemId),
  }));
}

/** Filtruje etapy kosztorysu po zapytaniu wyszukiwania (nazwa + pola tekstowe). */
export function filterCostEstimateGroupsBySearch(
  rootGroups: CostEstimateGroupWeb[],
  searchQuery: string,
  additionalFields: CostEstimateAdditionalFieldWeb[] = [],
): CostEstimateGroupWeb[] {
  if (!searchQuery.trim()) {
    return rootGroups;
  }

  const q = searchQuery.trim().toLowerCase();

  const groupMatches = (group: CostEstimateGroupWeb): boolean => {
    if (group.name.toLowerCase().includes(q)) {
      return true;
    }

    const additionalMatch = (group.additionalFieldValues ?? []).some((fv) => {
      const fieldDef = additionalFields.find((af) => af.id === fv.additionalFieldId);
      if (!fieldDef || fieldDef.fieldType !== AdditionalFieldType.String) {
        return false;
      }
      return (fv.stringValue ?? '').toLowerCase().includes(q);
    });
    if (additionalMatch) {
      return true;
    }

    const itemMatch = (group.items ?? []).some((item) => {
      if (item.name.toLowerCase().includes(q)) {
        return true;
      }
      return (item.additionalFieldValues ?? []).some((fv) => {
        const fieldDef = additionalFields.find((af) => af.id === fv.additionalFieldId);
        if (!fieldDef || fieldDef.fieldType !== AdditionalFieldType.String) {
          return false;
        }
        return (fv.stringValue ?? '').toLowerCase().includes(q);
      });
    });
    if (itemMatch) {
      return true;
    }

    return (group.childGroups ?? []).some(groupMatches);
  };

  return rootGroups.filter(groupMatches);
}

/** Sumy całkowite kosztorysu — z pól details lub jako suma rootGroups. */
export function getCostEstimateTotals(details: CostEstimateDetailsWeb): CostEstimateTotals {
  if (details.totalNet !== undefined && details.totalGross !== undefined) {
    return {
      net: details.totalNet ?? 0,
      gross: details.totalGross ?? 0,
      vat: details.totalVat ?? 0,
    };
  }

  return details.rootGroups.reduce(
    (acc, g) => ({
      net: acc.net + (g.totalNet ?? 0),
      gross: acc.gross + (g.totalGross ?? 0),
      vat: acc.vat + (g.totalVat ?? 0),
    }),
    { net: 0, gross: 0, vat: 0 },
  );
}
