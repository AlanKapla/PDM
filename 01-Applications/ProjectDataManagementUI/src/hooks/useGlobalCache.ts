import { useState, useEffect, useCallback, useRef } from 'react';

/**
 * Globalny cache dla zasobów współdzielonych (my-tenants, invitations, project details)
 * Zapobiega duplikacji requestów gdy wiele komponentów potrzebuje tych samych danych
 */

interface CacheEntry<T> {
  data: T;
  timestamp: number;
}

const CACHE_TTL = 5 * 60 * 1000; // 5 minut

// Globalne cache mapy (przetrwają odmontowanie komponentów)
const globalCache = new Map<string, CacheEntry<any>>();
const fetchInProgress = new Map<string, Promise<any>>();

/**
 * Hook do globalnego cache zasobów
 * @param cacheKey - unikalny klucz cache (np. "my-tenants", "invitations", "project-{id}")
 * @param fetchFn - funkcja pobierająca dane z API
 * @returns { data, loading, fetch, clear }
 */
export function useGlobalCache<T>(
  cacheKey: string,
  fetchFn: () => Promise<T>
) {
  const [dataState, setDataState] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);
  
  // Przechowuj fetchFn w ref aby uniknąć stale closure
  const fetchFnRef = useRef(fetchFn);
  
  // Aktualizuj ref przy każdym renderze
  useEffect(() => {
    fetchFnRef.current = fetchFn;
  }, [fetchFn]);

  const isCacheValid = useCallback((timestamp: number): boolean => {
    return Date.now() - timestamp < CACHE_TTL;
  }, []);

  // Przy montowaniu przywróć dane z globalnego cache jeśli są świeże
  useEffect(() => {
    const cached = globalCache.get(cacheKey) as CacheEntry<T> | undefined;
    if (cached && isCacheValid(cached.timestamp)) {
      setDataState(cached.data);
    }
  }, [cacheKey, isCacheValid]);

  const fetch = useCallback(async (): Promise<T> => {
    // Sprawdź cache
    const cached = globalCache.get(cacheKey) as CacheEntry<T> | undefined;
    if (cached && isCacheValid(cached.timestamp)) {
      setDataState(cached.data);
      return cached.data;
    }

    // Sprawdź czy fetch już jest w trakcie (synchroniczne sprawdzenie przed race condition)
    const inProgress = fetchInProgress.get(cacheKey);
    if (inProgress) {
      console.log(`⏳ Fetch already in progress for ${cacheKey}, waiting...`);
      const result = await inProgress;
      setDataState(result);
      return result;
    }

    // Utwórz i natychmiast zapisz Promise aby zapobiec race condition w React Strict Mode
    const fetchPromise = (async () => {
      try {
        setLoading(true);
        const result = await fetchFnRef.current();
        const cacheEntry: CacheEntry<T> = {
          data: result,
          timestamp: Date.now(),
        };
        globalCache.set(cacheKey, cacheEntry);
        setDataState(result);
        return result;
      } catch (error) {
        console.error(`Error fetching ${cacheKey}:`, error);
        throw error;
      } finally {
        setLoading(false);
        fetchInProgress.delete(cacheKey);
      }
    })();

    // Zapisz Promise PRZED rozpoczęciem wykonywania (zapobiega race condition)
    fetchInProgress.set(cacheKey, fetchPromise);
    
    // Sprawdź ponownie czy w międzyczasie inny fetch nie wystartował
    const potentialDuplicate = fetchInProgress.get(cacheKey);
    if (potentialDuplicate !== fetchPromise) {
      console.log(`⚠️ Race condition detected for ${cacheKey}, using existing fetch`);
      const result = await potentialDuplicate;
      setDataState(result);
      return result;
    }

    return fetchPromise;
  }, [cacheKey, isCacheValid]);

  const clear = useCallback(() => {
    globalCache.delete(cacheKey);
    setDataState(null);
  }, [cacheKey]);

  return {
    data: dataState,
    loading,
    fetch,
    clear,
  };
}

/**
 * Wyczyść cały globalny cache (użyj przy wylogowaniu)
 */
export function clearAllGlobalCache() {
  globalCache.clear();
  fetchInProgress.clear();
}
