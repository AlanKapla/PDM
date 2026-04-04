import { WorkDependencyType } from '../types/workSchedule.types';

/**
 * Normalizuje string daty do formatu `yyyy-MM-dd`.
 * API może zwracać pełne ISO datetime (np. "2026-03-15T00:00:00") — obcinamy do 10 znaków.
 */
function normalizeDate(dateStr: string): string {
  return dateStr.slice(0, 10);
}

/** Dodaje `days` dni kalendarzowych do stringa daty (akceptuje też pełne ISO datetime). */
export function addDays(dateStr: string, days: number): string {
  // slice(0,10) — obsługa API zwracającego DateTime zamiast DateOnly ("2026-03-15T00:00:00")
  const d = new Date(normalizeDate(dateStr) + 'T00:00:00');
  d.setDate(d.getDate() + days);
  // Używamy lokalnych getterów zamiast toISOString() — toISOString konwertuje do UTC
  // i dla stref UTC+x zwraca poprzedni dzień (błąd off-by-one).
  return [
    d.getFullYear(),
    String(d.getMonth() + 1).padStart(2, '0'),
    String(d.getDate()).padStart(2, '0'),
  ].join('-');
}

/** Różnica w dniach: dateA - dateB (może być ujemna). */
export function daysDiff(dateA: string, dateB: string): number {
  const a = new Date(normalizeDate(dateA) + 'T00:00:00').getTime();
  const b = new Date(normalizeDate(dateB) + 'T00:00:00').getTime();
  return Math.round((a - b) / 86_400_000);
}

/** Zwraca najwcześniejszy startDate i najpóźniejszy endDate ze wszystkich okresów pracy. */
export function getWorkEffectiveDates(
  periods: { startDate: string; endDate: string }[]
): { startDate: string | undefined; endDate: string | undefined } {
  const valid = (periods ?? []).filter(p => p.startDate && p.endDate);
  if (valid.length === 0) return { startDate: undefined, endDate: undefined };
  // normalizeDate — eliminuje komponent czasu zwracany przez .NET DateTime
  const starts = valid.map(p => normalizeDate(p.startDate)).sort();
  const ends   = valid.map(p => normalizeDate(p.endDate)).sort();
  return { startDate: starts[0], endDate: ends[ends.length - 1] };
}

/** Uproszczona reprezentacja zależności — niezależna od tego czy używamy tempId czy dbId. */
export interface GenericDependency {
  predecessorId: string;
  successorId: string;
  dependencyType: number;
  lagDays: number;
}

/** Ograniczenia dat dla jednego zakresu robót wynikające z jego zależności. */
export interface DateConstraints {
  /** Minimalna dozwolona data START (zakres jest następnikiem) */
  minStartDate?: string;
  /** Czytelne dla człowieka wyjaśnienie skąd pochodzi ograniczenie minStartDate */
  minStartDateReason?: string;
  /** Minimalna dozwolona data END (zakres jest następnikiem) */
  minEndDate?: string;
  minEndDateReason?: string;
  /** Maksymalna dozwolona data END (zakres jest poprzednikiem) */
  maxEndDate?: string;
  maxEndDateReason?: string;
  /** Maksymalna dozwolona data START (zakres jest poprzednikiem) */
  maxStartDate?: string;
  maxStartDateReason?: string;
}

const DEP_TYPE_PL: Record<number, string> = {
  0: 'FS – zakończ→zacznij',
  1: 'SS – zacznij→zacznij',
  2: 'FF – zakończ→zakończ',
  3: 'SF – zacznij→zakończ',
};

/**
 * Oblicza ograniczenia dat dla zakresu robót na podstawie jego zależności.
 * Działa zarówno z tempId (modal) jak i dbId (widok inline).
 *
 * @param workId         - identyfikator zakresu (tempId lub DB id)
 * @param dependencies   - lista zależności używająca tego samego typu identyfikatora
 * @param workDateRanges - mapa workId → { startDate?, endDate? }
 * @param workNames      - opcjonalna mapa workId → czytelna nazwa (dla komunikatów powodu)
 */
export function computeDateConstraints(
  workId: string,
  dependencies: GenericDependency[],
  workDateRanges: Map<string, { startDate?: string; endDate?: string }>,
  workNames?: Map<string, string>
): DateConstraints {
  const c: DateConstraints = {};

  const name = (id: string) => workNames?.get(id) ?? id;
  const lagSuffix = (lag: number) => lag > 0 ? ` +${lag} dni` : '';

  for (const dep of dependencies) {
    if (!dep.predecessorId || !dep.successorId) continue;

    const predDates = workDateRanges.get(dep.predecessorId);
    const succDates = workDateRanges.get(dep.successorId);

    // Ten zakres jest NASTĘPNIKIEM — obliczamy minima
    if (dep.successorId === workId && predDates) {
      const typ = DEP_TYPE_PL[dep.dependencyType] ?? '';
      const ls = lagSuffix(dep.lagDays);
      switch (dep.dependencyType) {
        case WorkDependencyType.FinishToStart:
          if (predDates.endDate) {
            const min = addDays(predDates.endDate, dep.lagDays);
            if (!c.minStartDate || min > c.minStartDate) {
              c.minStartDate = min;
              c.minStartDateReason = `Nie wcześniej niż ${min} – zakres „${name(dep.predecessorId)}" kończy się ${predDates.endDate} (${typ}${ls})`;
            }
          }
          break;
        case WorkDependencyType.StartToStart:
          if (predDates.startDate) {
            const min = addDays(predDates.startDate, dep.lagDays);
            if (!c.minStartDate || min > c.minStartDate) {
              c.minStartDate = min;
              c.minStartDateReason = `Nie wcześniej niż ${min} – zakres „${name(dep.predecessorId)}" zaczyna się ${predDates.startDate} (${typ}${ls})`;
            }
          }
          break;
        case WorkDependencyType.FinishToFinish:
          if (predDates.endDate) {
            const min = addDays(predDates.endDate, dep.lagDays);
            if (!c.minEndDate || min > c.minEndDate) {
              c.minEndDate = min;
              c.minEndDateReason = `Nie wcześniej niż ${min} – zakres „${name(dep.predecessorId)}" kończy się ${predDates.endDate} (${typ}${ls})`;
            }
          }
          break;
        case WorkDependencyType.StartToFinish:
          if (predDates.startDate) {
            const min = addDays(predDates.startDate, dep.lagDays);
            if (!c.minEndDate || min > c.minEndDate) {
              c.minEndDate = min;
              c.minEndDateReason = `Nie wcześniej niż ${min} – zakres „${name(dep.predecessorId)}" zaczyna się ${predDates.startDate} (${typ}${ls})`;
            }
          }
          break;
      }
    }

    // Ten zakres jest POPRZEDNIKIEM — obliczamy maksima
    if (dep.predecessorId === workId && succDates) {
      const typ = DEP_TYPE_PL[dep.dependencyType] ?? '';
      const ls = lagSuffix(dep.lagDays);
      switch (dep.dependencyType) {
        case WorkDependencyType.FinishToStart:
          if (succDates.startDate) {
            const max = addDays(succDates.startDate, -dep.lagDays);
            if (!c.maxEndDate || max < c.maxEndDate) {
              c.maxEndDate = max;
              c.maxEndDateReason = `Nie później niż ${max} – zakres „${name(dep.successorId)}" musi zacząć się ${succDates.startDate} (${typ}${ls})`;
            }
          }
          break;
        case WorkDependencyType.StartToStart:
          if (succDates.startDate) {
            const max = addDays(succDates.startDate, -dep.lagDays);
            if (!c.maxStartDate || max < c.maxStartDate) {
              c.maxStartDate = max;
              c.maxStartDateReason = `Nie później niż ${max} – zakres „${name(dep.successorId)}" musi zacząć się ${succDates.startDate} (${typ}${ls})`;
            }
          }
          break;
        case WorkDependencyType.FinishToFinish:
          if (succDates.endDate) {
            const max = addDays(succDates.endDate, -dep.lagDays);
            if (!c.maxEndDate || max < c.maxEndDate) {
              c.maxEndDate = max;
              c.maxEndDateReason = `Nie później niż ${max} – zakres „${name(dep.successorId)}" musi skończyć się ${succDates.endDate} (${typ}${ls})`;
            }
          }
          break;
        case WorkDependencyType.StartToFinish:
          if (succDates.endDate) {
            const max = addDays(succDates.endDate, -dep.lagDays);
            if (!c.maxStartDate || max < c.maxStartDate) {
              c.maxStartDate = max;
              c.maxStartDateReason = `Nie później niż ${max} – zakres „${name(dep.successorId)}" musi skończyć się ${succDates.endDate} (${typ}${ls})`;
            }
          }
          break;
      }
    }
  }

  return c;
}

/**
 * Sprawdza czy konkretna zależność jest spełniona przez aktualne daty zresków.
 * Zwraca opis naruszenia lub null jeśli wszystko OK.
 */
export function checkDependencyViolation(
  dep: GenericDependency,
  workDateRanges: Map<string, { startDate?: string; endDate?: string }>,
  predName: string,
  succName: string
): string | null {
  const pred = workDateRanges.get(dep.predecessorId);
  const succ = workDateRanges.get(dep.successorId);
  if (!pred || !succ) return null;

  const lagText = dep.lagDays > 0 ? ` (+${dep.lagDays} dni)` : '';

  // Normalizuj daty do YYYY-MM-DD przed porównaniem (API może zwracać pełne ISO datetime)
  const predStart = pred.startDate ? normalizeDate(pred.startDate) : undefined;
  const predEnd   = pred.endDate   ? normalizeDate(pred.endDate)   : undefined;
  const succStart = succ.startDate ? normalizeDate(succ.startDate) : undefined;
  const succEnd   = succ.endDate   ? normalizeDate(succ.endDate)   : undefined;

  switch (dep.dependencyType) {
    case WorkDependencyType.FinishToStart: {
      if (predEnd && succStart) {
        const minSuccStart = addDays(predEnd, dep.lagDays);
        if (succStart < minSuccStart)
          return `FS${lagText}: „${predName}" kończy ${predEnd} → „${succName}" musi startować ≥ ${minSuccStart} (aktualnie ${succStart})`;
      }
      break;
    }
    case WorkDependencyType.StartToStart: {
      if (predStart && succStart) {
        const minSuccStart = addDays(predStart, dep.lagDays);
        if (succStart < minSuccStart)
          return `SS${lagText}: „${predName}" startuje ${predStart} → „${succName}" musi startować ≥ ${minSuccStart} (aktualnie ${succStart})`;
      }
      break;
    }
    case WorkDependencyType.FinishToFinish: {
      if (predEnd && succEnd) {
        const minSuccEnd = addDays(predEnd, dep.lagDays);
        if (succEnd < minSuccEnd)
          return `FF${lagText}: „${predName}" kończy ${predEnd} → „${succName}" musi kończyć ≥ ${minSuccEnd} (aktualnie ${succEnd})`;
      }
      break;
    }
    case WorkDependencyType.StartToFinish: {
      if (predStart && succEnd) {
        const minSuccEnd = addDays(predStart, dep.lagDays);
        if (succEnd < minSuccEnd)
          return `SF${lagText}: „${predName}" startuje ${predStart} → „${succName}" musi kończyć ≥ ${minSuccEnd} (aktualnie ${succEnd})`;
      }
      break;
    }
  }
  return null;
}

/**
 * Przesuwa wszystkie okresy pracy o `shiftDays` dni.
 */
export function shiftPeriods<T extends { startDate: string; endDate: string }>(
  periods: T[],
  shiftDays: number
): T[] {
  return periods.map(p => ({
    ...p,
    startDate: addDays(p.startDate, shiftDays),
    endDate: addDays(p.endDate, shiftDays),
  }));
}

/**
 * Automatycznie przesuwa okresy następnika o minimalną wymaganą liczbę dni,
 * aby spełnić ograniczenia wynikające z zależności.
 *
 * @returns { periods, shiftedBy } — nowe okresy i liczba przesuniętych dni (0 jeśli nie trzeba)
 */
export function autoAdjustSuccessorPeriods<T extends { startDate: string; endDate: string }>(
  periods: T[],
  constraints: DateConstraints
): { periods: T[]; shiftedBy: number } {
  if (!periods || periods.length === 0) return { periods, shiftedBy: 0 };

  const { startDate: currentStart, endDate: currentEnd } = getWorkEffectiveDates(periods);
  if (!currentStart || !currentEnd) return { periods, shiftedBy: 0 };

  let shiftDays = 0;

  // Ograniczenie na minimalny start (FS lub SS)
  if (constraints.minStartDate && currentStart < constraints.minStartDate) {
    shiftDays = Math.max(shiftDays, daysDiff(constraints.minStartDate, currentStart));
  }

  // Ograniczenie na minimalny end (FF lub SF) — bierzemy pod uwagę już wyliczony shift
  if (constraints.minEndDate) {
    const projectedEnd = addDays(currentEnd, shiftDays);
    if (projectedEnd < constraints.minEndDate) {
      shiftDays += daysDiff(constraints.minEndDate, projectedEnd);
    }
  }

  if (shiftDays <= 0) return { periods, shiftedBy: 0 };
  return { periods: shiftPeriods(periods, shiftDays), shiftedBy: shiftDays };
}

/**
 * Kaskadowe przesunięcie dat: zaczyna od previoussorId (którego daty lub lag właśnie się zmieniły)
 * i propaguje przesunięcia w dół grafu zależności (BFS po topologicznym porządku).
 *
 * Tryb A – okresy istniały przed zdefiniowaniem zależności:
 *   wywoływana gdy użytkownik zmienia lagDays lub dodaje nową zależność.
 *
 * @param predecessorIds - zbiór workId, których daty właśnie się zmieniły (źródła kaskady)
 * @param allDeps        - wszystkie zależności (generic, niezależnie od tempId/dbId)
 * @param workPeriodsMap - mapa workId → aktualne okresy
 * @param workNames      - opcjonalna mapa do czytelnych nazw (tylko dla logów/toastów)
 *
 * @returns mapa workId → { periods: nowe okresy, shiftedBy: ile dni; tylko zmienione}
 */
export function cascadeAutoAdjust<T extends { startDate: string; endDate: string }>(
  predecessorIds: string[],
  allDeps: GenericDependency[],
  workPeriodsMap: Map<string, T[]>,
  workNames?: Map<string, string>
): Map<string, { periods: T[]; shiftedBy: number }> {
  const result = new Map<string, { periods: T[]; shiftedBy: number }>();

  // Pracująca kopia zakresów dat — aktualizowana w trakcie kaskady
  const currentDateRanges = new Map<string, { startDate?: string; endDate?: string }>();
  for (const [id, periods] of workPeriodsMap) {
    currentDateRanges.set(id, getWorkEffectiveDates(periods));
  }

  // BFS: zbierz wszystkich następników i przypisz im głębokość (dłuższa ścieżka = większa głębokość)
  // Sortowanie po głębokości malejąco zapewnia topologiczny porządek przetwarzania.
  const depthMap = new Map<string, number>();
  const bfsQueue: Array<{ id: string; depth: number }> = predecessorIds.map(id => ({ id, depth: 0 }));

  while (bfsQueue.length > 0) {
    const { id, depth } = bfsQueue.shift()!;
    for (const dep of allDeps) {
      if (dep.predecessorId !== id) continue;
      const succId = dep.successorId;
      const newDepth = depth + 1;
      if (!depthMap.has(succId) || depthMap.get(succId)! < newDepth) {
        depthMap.set(succId, newDepth);
        bfsQueue.push({ id: succId, depth: newDepth });
      }
    }
  }

  // Przetwarzaj kolejno od najniższej głębokości (bezpośredni następnicy pierwsza)
  const orderedSuccessors = Array.from(depthMap.entries())
    .sort((a, b) => a[1] - b[1])
    .map(([id]) => id);

  for (const succId of orderedSuccessors) {
    // Nie przesuwamy źródeł kaskady – tylko ich następniki
    if (predecessorIds.includes(succId)) continue;

    const succPeriods = result.get(succId)?.periods ?? workPeriodsMap.get(succId);
    if (!succPeriods || succPeriods.length === 0) continue; // brak okresów → tryb B, blokujemy tylko datepicker

    const constraints = computeDateConstraints(succId, allDeps, currentDateRanges, workNames);
    const adjusted = autoAdjustSuccessorPeriods(succPeriods, constraints);

    if (adjusted.shiftedBy > 0) {
      result.set(succId, { periods: adjusted.periods, shiftedBy: adjusted.shiftedBy });
      // Zaktualizuj robocze daty — kolejne przetwarzania kaskadowe korzystają z nowych dat
      currentDateRanges.set(succId, getWorkEffectiveDates(adjusted.periods));
    }
  }

  return result;
}

/**
 * Sprawdza czy data `dateStr` mieści się w dozwolonym przedziale constraints.
 * Używane do blokowania kliknięcia w Gantt — sprawdza 'start' lub 'end' zakresu.
 */
export function isDateAllowed(
  dateStr: string,
  role: 'start' | 'end',
  constraints: DateConstraints
): boolean {
  if (role === 'start') {
    if (constraints.minStartDate && dateStr < constraints.minStartDate) return false;
    if (constraints.maxStartDate && dateStr > constraints.maxStartDate) return false;
  }
  if (role === 'end') {
    if (constraints.minEndDate && dateStr < constraints.minEndDate) return false;
    if (constraints.maxEndDate && dateStr > constraints.maxEndDate) return false;
  }
  return true;
}
