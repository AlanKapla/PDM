import type {
  CostEstimateGroupDto,
  CostEstimateWorkScopeItemDto,
  CostEstimateGroupFieldValueDto,
  CostEstimateWorkScopeItemFieldValueDto,
} from '../types/costEstimate.types.new';

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
    fieldValues: group.fieldValues.map(fv => ({ ...fv })),
    workScopeItems: group.workScopeItems.map(item => cloneWorkScopeItem(item)),
    childGroups: group.childGroups.map(child => cloneGroup(child)),
  };
}

/**
 * Clone work scope item (deep copy)
 */
export function cloneWorkScopeItem(item: CostEstimateWorkScopeItemDto): CostEstimateWorkScopeItemDto {
  return {
    ...item,
    id: undefined, // Reset ID for new item
    fieldValues: item.fieldValues.map(fv => ({ ...fv })),
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
    return groupList.filter(g => {
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
      childGroups: group.childGroups.map(child => updateLevels(child, level + 1)),
    };
  };

  movedGroup = updateLevels(movedGroup, newLevel);
  movedGroup.parentGroupId = newParentId;

  // Insert group at new location
  if (!newParentId) {
    // Move to root
    newGroups.push(movedGroup);
  } else {
    // Move to specific parent
    const addToParent = (groupList: CostEstimateGroupDto[]): CostEstimateGroupDto[] => {
      return groupList.map(g => {
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
 * Search groups by field value
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

      // Search in field values
      for (const fieldValue of group.fieldValues) {
        if (fieldName && fieldValue.fieldDefinitionId !== fieldName) {
          continue;
        }
        
        if (fieldValue.value?.toLowerCase().includes(lowerSearch)) {
          matches = true;
          break;
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
          message: `Grupa przekracza maksymalny poziom zagnieżdżenia (${maxLevel})`,
        });
      }

      // Check parent
      if (group.parentGroupId !== parentId) {
        errors.push({
          groupId: group.id,
          message: 'Nieprawidłowy rodzic grupy',
        });
      }

      // Check branching
      if (!canBranchGroups && group.childGroups.length > 0) {
        errors.push({
          groupId: group.id,
          message: 'Szablon nie pozwala na tworzenie podgrup',
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
  totalWorkScopeItems: number;
  maxLevel: number;
  groupsByLevel: Record<number, number>;
  itemsByLevel: Record<number, number>;
}

export function calculateStatistics(groups: CostEstimateGroupDto[]): GroupStatistics {
  const stats: GroupStatistics = {
    totalGroups: 0,
    totalWorkScopeItems: 0,
    maxLevel: 0,
    groupsByLevel: {},
    itemsByLevel: {},
  };

  const calculate = (groupList: CostEstimateGroupDto[]) => {
    for (const group of groupList) {
      stats.totalGroups++;
      stats.totalWorkScopeItems += group.workScopeItems.length;
      stats.maxLevel = Math.max(stats.maxLevel, group.level);
      
      stats.groupsByLevel[group.level] = (stats.groupsByLevel[group.level] || 0) + 1;
      stats.itemsByLevel[group.level] = (stats.itemsByLevel[group.level] || 0) + group.workScopeItems.length;

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
      const groupName = group.fieldValues.find(fv => 
        fv.fieldDefinitionId.toLowerCase().includes('name')
      )?.value || `Grupa ${group.order + 1}`;

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
 */
export function bulkUpdateGroupFields(
  groups: CostEstimateGroupDto[],
  fieldId: string,
  getValue: (group: CostEstimateGroupDto) => string | undefined
): CostEstimateGroupDto[] {
  return groups.map(group => {
    const newValue = getValue(group);
    const existingIndex = group.fieldValues.findIndex(fv => fv.fieldDefinitionId === fieldId);
    const newFieldValues = [...group.fieldValues];

    if (newValue !== undefined && newValue !== '') {
      if (existingIndex >= 0) {
        newFieldValues[existingIndex] = {
          ...newFieldValues[existingIndex],
          value: newValue,
        };
      } else {
        newFieldValues.push({
          fieldDefinitionId: fieldId,
          value: newValue,
        });
      }
    } else if (existingIndex >= 0) {
      newFieldValues.splice(existingIndex, 1);
    }

    return {
      ...group,
      fieldValues: newFieldValues,
      childGroups: bulkUpdateGroupFields(group.childGroups, fieldId, getValue),
    };
  });
}
