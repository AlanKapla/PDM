import { useState, useMemo, useRef, useCallback } from "react";

export type TimeScale = "days" | "weeks" | "months";

export interface DateGroup {
  label: string;
  count: number;
  startIdx: number;
}

interface UseTimelineDataOptions {
  /** Responsywne formatowanie dat na mobile */
  isMobile?: boolean;
}

/**
 * Wspólna logika timeline'u dla widoków harmonogramów.
 * Zarządza skalą czasu, zakresem, ukrywaniem weekendów,
 * generowaniem dat/grup oraz automatycznym scrollem do "dzisiaj".
 */
export function useTimelineData(options?: UseTimelineDataOptions) {
  const { isMobile = false } = options || {};

  // ─── Stan ────────────────────────────────────────────
  const [timeScale, setTimeScale] = useState<TimeScale>("weeks");
  const [timeRangeMonths, setTimeRangeMonths] = useState(1);
  const [hideWeekends, setHideWeekends] = useState(false);

  // ─── Refy do scrollowania ────────────────────────────
  const todayColumnRef = useRef<HTMLTableCellElement>(null);
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  // ─── Helpery ─────────────────────────────────────────

  const isToday = useCallback((date: Date): boolean => {
    const today = new Date();
    return (
      date.getDate() === today.getDate() &&
      date.getMonth() === today.getMonth() &&
      date.getFullYear() === today.getFullYear()
    );
  }, []);

  const isWeekend = useCallback((date: Date): boolean => {
    const day = date.getDay();
    return day === 0 || day === 6;
  }, []);

  const formatTimelineDate = useCallback(
    (date: Date): string => {
      if (timeScale === "days") {
        return isMobile
          ? `${date.getDate()}`
          : `${date.getDate()}.${date.getMonth() + 1}`;
      } else if (timeScale === "weeks") {
        const dayNames = ["Nd", "Pn", "Wt", "Śr", "Cz", "Pt", "So"];
        return isMobile
          ? dayNames[date.getDay()]
          : `${dayNames[date.getDay()]}\n${date.getDate()}.${date.getMonth() + 1}`;
      } else {
        return `${date.getDate()}`;
      }
    },
    [timeScale, isMobile]
  );

  const isWorkInPeriod = useCallback(
    (workStart: string, workEnd: string, periodStart: Date, periodEnd: Date): boolean => {
      const start = new Date(workStart);
      const end = new Date(workEnd);
      return start < periodEnd && end >= periodStart;
    },
    []
  );

  const getPeriodEnd = useCallback((periodStart: Date): Date => {
    const end = new Date(periodStart);
    end.setDate(end.getDate() + 1);
    return end;
  }, []);

  // ─── Scroll do dzisiejszej daty ──────────────────────

  const scrollToToday = useCallback(() => {
    if (todayColumnRef.current) {
      todayColumnRef.current.scrollIntoView({
        behavior: "smooth",
        block: "nearest",
        inline: "center",
      });
    }
  }, []);

  // ─── Generowanie dat i grup ──────────────────────────

  const { allDates, allDateGroups } = useMemo(() => {
    const dates: Date[] = [];
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const minDate = new Date(today);
    minDate.setMonth(minDate.getMonth() - timeRangeMonths);
    minDate.setDate(1);

    const maxDate = new Date(today);
    maxDate.setMonth(maxDate.getMonth() + timeRangeMonths);
    maxDate.setMonth(maxDate.getMonth() + 1);
    maxDate.setDate(0);

    const current = new Date(minDate);

    if (timeScale === "weeks") {
      // Wyrównaj do poniedziałku
      const day = current.getDay();
      current.setDate(current.getDate() - (day === 0 ? 6 : day - 1));

      const dateGroups: DateGroup[] = [];
      let groupStartIdx = 0;

      while (current <= maxDate) {
        const weekStart = new Date(current);
        const weekEnd = new Date(current);
        weekEnd.setDate(weekEnd.getDate() + 6);

        for (let i = 0; i < 7; i++) {
          dates.push(new Date(current));
          current.setDate(current.getDate() + 1);
        }

        dateGroups.push({
          label: `${weekStart.getDate()}.${weekStart.getMonth() + 1} - ${weekEnd.getDate()}.${weekEnd.getMonth() + 1}`,
          count: 7,
          startIdx: groupStartIdx,
        });
        groupStartIdx += 7;
      }

      return { allDates: dates, allDateGroups: dateGroups };
    }

    // "days" i "months" — ta sama siatka z miesięcznymi grupami
    const dateGroups: DateGroup[] = [];
    let groupStartIdx = 0;

    while (current <= maxDate) {
      const monthStart = new Date(current);
      const daysInMonth = new Date(
        current.getFullYear(),
        current.getMonth() + 1,
        0
      ).getDate();

      for (let day = 1; day <= daysInMonth; day++) {
        dates.push(new Date(current.getFullYear(), current.getMonth(), day));
      }

      dateGroups.push({
        label: monthStart.toLocaleDateString("pl-PL", {
          month: "long",
          year: "numeric",
        }),
        count: daysInMonth,
        startIdx: groupStartIdx,
      });
      groupStartIdx += daysInMonth;
      current.setMonth(current.getMonth() + 1);
    }

    return { allDates: dates, allDateGroups: dateGroups };
  }, [timeScale, timeRangeMonths]);

  // ─── Filtrowanie weekendów ───────────────────────────

  const { dates, dateGroups } = useMemo(() => {
    if (!hideWeekends) return { dates: allDates, dateGroups: allDateGroups };

    const filtered = allDates.filter((d) => !isWeekend(d));

    let startIdx = 0;
    const groups = allDateGroups
      .map((g) => {
        const originalDates = allDates.slice(g.startIdx, g.startIdx + g.count);
        const filteredCount = originalDates.filter((d) => !isWeekend(d)).length;
        const group = { ...g, count: filteredCount, startIdx };
        startIdx += filteredCount;
        return group;
      })
      .filter((g) => g.count > 0);

    return { dates: filtered, dateGroups: groups };
  }, [allDates, allDateGroups, hideWeekends, isWeekend]);

  return {
    // Stan
    timeScale,
    setTimeScale,
    timeRangeMonths,
    setTimeRangeMonths,
    hideWeekends,
    setHideWeekends,
    toggleWeekends: useCallback(() => setHideWeekends((h) => !h), []),

    // Wygenerowane dane
    dates,
    dateGroups,

    // Helpery
    isToday,
    isWeekend,
    formatTimelineDate,
    isWorkInPeriod,
    getPeriodEnd,

    // Scroll do "dzisiaj"
    todayColumnRef,
    scrollContainerRef,
    scrollToToday,
  };
}
