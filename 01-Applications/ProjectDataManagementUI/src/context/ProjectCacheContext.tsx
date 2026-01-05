import { createContext, useContext, useCallback, useRef, type ReactNode } from 'react';
import type { ProjectDetailsWeb } from '../types/project.types';
import { projectApi } from '../api/projectApi';

interface CacheEntry {
  data: ProjectDetailsWeb;
  timestamp: number;
  userId: string;
}

interface ProjectCacheContextType {
  getProjectDetails: (tenantId: string, projectId: string, userId: string) => Promise<ProjectDetailsWeb>;
  invalidateProject: (projectId: string, userId?: string) => void;
  invalidateAll: () => void;
}

const ProjectCacheContext = createContext<ProjectCacheContextType | null>(null);

const CACHE_TTL = 5 * 60 * 1000; // 5 minutes

export function ProjectCacheProvider({ children }: { children: ReactNode }) {
  const cacheRef = useRef<Map<string, CacheEntry>>(new Map());

  const getProjectDetails = useCallback(async (
    tenantId: string,
    projectId: string,
    userId: string
  ): Promise<ProjectDetailsWeb> => {
    const cacheKey = `${userId}-${tenantId}-${projectId}`;
    const cached = cacheRef.current.get(cacheKey);
    const now = Date.now();

    // Check if cache is valid
    if (cached && cached.userId === userId && (now - cached.timestamp) < CACHE_TTL) {
      return cached.data;
    }

    // Fetch from API
    const response = await projectApi.getProjectDetails(tenantId, projectId);
    
    // Store in cache
    cacheRef.current.set(cacheKey, {
      data: response.data,
      timestamp: now,
      userId,
    });

    return response.data;
  }, []);

  const invalidateProject = useCallback((projectId: string, userId?: string) => {
    const keysToDelete: string[] = [];
    
    cacheRef.current.forEach((_, key) => {
      if (userId) {
        // Clear specific project for specific user
        if (key.startsWith(`${userId}-`) && key.endsWith(`-${projectId}`)) {
          keysToDelete.push(key);
        }
      } else {
        // Clear specific project for all users
        if (key.endsWith(`-${projectId}`)) {
          keysToDelete.push(key);
        }
      }
    });

    keysToDelete.forEach(key => cacheRef.current.delete(key));
  }, []);

  const invalidateAll = useCallback(() => {
    cacheRef.current.clear();
  }, []);

  return (
    <ProjectCacheContext.Provider value={{ getProjectDetails, invalidateProject, invalidateAll }}>
      {children}
    </ProjectCacheContext.Provider>
  );
}

export function useProjectCache() {
  const context = useContext(ProjectCacheContext);
  if (!context) {
    throw new Error('useProjectCache must be used within ProjectCacheProvider');
  }
  return context;
}
