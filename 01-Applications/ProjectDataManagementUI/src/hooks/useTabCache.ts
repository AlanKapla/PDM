import { useState, useCallback, useEffect, useRef } from 'react';

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

// GLOBALNY cache - przetrwa odmontowanie komponentów
// Klucz: cacheKey, Wartość: CacheEntry
const globalCache = new Map<string, CacheEntry<any>>();

// GLOBALNA mapa śledzenia fetch - zapobiega wielokrotnym wywołaniom w Strict Mode
const fetchInProgress = new Map<string, boolean>();

/**
 * Hook do zarządzania cache'owaniem danych z 5-minutowym TTL
 * Cache jest globalny - przetrwa odmontowanie komponentów
 * Używany dla lazy loading danych w tabach
 */
export function useTabCache<T>(
  fetchFn: () => Promise<T>,
  cacheKey: string
): TabCacheResult<T> {
  const [data, setDataState] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);

  // Przechowuj najnowszą wersję fetchFn w ref, aby uniknąć stale zmieniającego się fetch callback
  const fetchFnRef = useRef(fetchFn);

  useEffect(() => {
    fetchFnRef.current = fetchFn;
  }, [fetchFn]);

  // Przy montowaniu sprawdź czy są dane w globalnym cache
  useEffect(() => {
    const cached = globalCache.get(cacheKey) as CacheEntry<T> | undefined;
    if (cached && Date.now() - cached.timestamp < CACHE_TTL) {
      setDataState(cached.data);
    }
  }, [cacheKey]);

  const isCacheValid = useCallback((timestamp: number): boolean => {
    return Date.now() - timestamp < CACHE_TTL;
  }, []);

  const fetch = useCallback(async () => {
    // Sprawdź czy fetch już jest w trakcie dla tego cache key
    if (fetchInProgress.get(cacheKey)) {
      return;
    }

    // Sprawdź globalny cache
    const cached = globalCache.get(cacheKey) as CacheEntry<T> | undefined;
    if (cached && isCacheValid(cached.timestamp)) {
      setDataState(cached.data);
      return;
    }

    // Oznacz że fetch jest w trakcie
    fetchInProgress.set(cacheKey, true);
    setLoading(true);
    
    try {
      const result = await fetchFnRef.current();
      const cacheEntry: CacheEntry<T> = {
        data: result,
        timestamp: Date.now(),
      };
      globalCache.set(cacheKey, cacheEntry);
      setDataState(result);
    } catch (error) {
      throw error;
    } finally {
      setLoading(false);
      // Usuń flagę fetch in progress
      fetchInProgress.delete(cacheKey);
    }
  }, [cacheKey, isCacheValid]);

  const setData = useCallback((newData: T) => {
    const cacheEntry: CacheEntry<T> = {
      data: newData,
      timestamp: Date.now(),
    };
    globalCache.set(cacheKey, cacheEntry);
    setDataState(newData);
  }, [cacheKey]);

  const clear = useCallback(() => {
    globalCache.delete(cacheKey);
    setDataState(null);
  }, [cacheKey]);

  return { data, loading, fetch, setData, clear };
}
