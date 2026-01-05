# Project Details Cache

## Overview

The `ProjectCacheContext` provides a caching mechanism for project details (including user permissions) to reduce API calls and improve performance.

## How it works

- **Cache Key**: `{userId}-{tenantId}-{projectId}`
- **TTL (Time To Live)**: 5 minutes
- **Storage**: In-memory (React Context with useRef)
- **Auto-invalidation**: Cache expires after 5 minutes OR when manually invalidated

## Usage

### 1. Basic Usage (automatic via `useProjectPermissions`)

The `useProjectPermissions` hook automatically uses the cache:

```typescript
import { useProjectPermissions } from '../hooks/useProjectPermissions';

function MyComponent() {
  const { projectId } = useParams();
  const permissions = useProjectPermissions(projectId);

  // First call: fetches from API and caches
  // Subsequent calls (within 5 min): returns from cache
  
  if (permissions.canEdit) {
    // ...
  }
}
```

### 2. Manual Cache Invalidation

When you modify project data that affects permissions (e.g., adding/removing members, changing roles), invalidate the cache:

```typescript
import { useProjectCache } from '../hooks/useProjectCache';

function AddMemberComponent() {
  const { invalidateProject } = useProjectCache();
  
  const handleAddMember = async () => {
    await projectApi.addProjectMember(tenantId, projectId, userId);
    
    // Invalidate cache so next useProjectPermissions call fetches fresh data
    invalidateProject(projectId);
  };
}
```

### 3. Advanced Cache Control

```typescript
import { useProjectCache } from '../hooks/useProjectCache';

function MyComponent() {
  const { invalidateProject, invalidateAll, getProjectDetails } = useProjectCache();
  
  // Invalidate specific project for all users
  invalidateProject(projectId);
  
  // Invalidate specific project for specific user
  invalidateProject(projectId, userId);
  
  // Clear entire cache
  invalidateAll();
  
  // Manually fetch with caching
  const projectDetails = await getProjectDetails(tenantId, projectId, userId);
}
```

## When to Invalidate Cache

Always invalidate after operations that change:

- **Project membership**: `addProjectMember`, `removeProjectMember`
- **User roles**: `updateProjectMemberRole`
- **Project status**: `toggleProjectStatus` (inactive projects have different permissions)
- **Tenant membership**: When user joins/leaves tenant (invalidate all projects)

## Benefits

1. **Reduced API Calls**: Same project permissions reused across components
2. **Faster Loading**: No spinner flashing when switching between project pages
3. **Better UX**: Smoother navigation within project
4. **Automatic Cleanup**: Cache expires after 5 minutes to ensure fresh data

## Example Scenario

```
User navigates: 
  Projects List → Project Details → Project Members → Project Files → Back to Details

Without cache: 5+ API calls to /project/{id}
With cache: 1 API call (cached for 5 minutes)
```

## Implementation Details

The cache is implemented using:
- `React Context` for global state
- `useRef` to persist cache between renders
- `Map<string, CacheEntry>` for O(1) lookups
- TTL check on every read

```typescript
interface CacheEntry {
  data: ProjectDetailsWeb;
  timestamp: number;
  userId: string;
}
```
