import { useState, useCallback, useRef } from 'react';

interface CacheEntry<T> {
  data: T;
  timestamp: number;
}

interface TabCacheResult<T> {
  data: T | null;
  loading: boolean;
  fetch: () => Promise<void>;
  setData: (data: T) => void;
  clear: () => void;
}

const CACHE_TTL = 5 * 60 * 1000; // 5 minutes in milliseconds

/**
 * Hook do zarządzania cache'owaniem danych z 5-minutowym TTL
 * Używany dla lazy loading danych w tabach
 */
export function useTabCache<T>(
  fetchFn: () => Promise<T>,
  cacheKey: string
): TabCacheResult<T> {
  const [data, setDataState] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);
  const cacheRef = useRef<Map<string, CacheEntry<T>>>(new Map());

  const isCacheValid = useCallback((timestamp: number): boolean => {
    return Date.now() - timestamp < CACHE_TTL;
  }, []);

  const fetch = useCallback(async () => {
    // Sprawdź cache
    const cached = cacheRef.current.get(cacheKey);
    if (cached && isCacheValid(cached.timestamp)) {
      setDataState(cached.data);
      return;
    }

    // Cache nieważny lub brak danych - pobierz z API
    setLoading(true);
    try {
      const result = await fetchFn();
      const cacheEntry: CacheEntry<T> = {
        data: result,
        timestamp: Date.now(),
      };
      cacheRef.current.set(cacheKey, cacheEntry);
      setDataState(result);
    } catch (error) {
      console.error('Error fetching data:', error);
      throw error;
    } finally {
      setLoading(false);
    }
  }, [cacheKey, fetchFn, isCacheValid]);

  const setData = useCallback((newData: T) => {
    const cacheEntry: CacheEntry<T> = {
      data: newData,
      timestamp: Date.now(),
    };
    cacheRef.current.set(cacheKey, cacheEntry);
    setDataState(newData);
  }, [cacheKey]);

  const clear = useCallback(() => {
    cacheRef.current.delete(cacheKey);
    setDataState(null);
  }, [cacheKey]);

  return { data, loading, fetch, setData, clear };
}
