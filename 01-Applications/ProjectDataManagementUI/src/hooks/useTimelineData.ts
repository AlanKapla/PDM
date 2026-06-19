import { useState, useMemo, useRef, useCallback, useEffect } from "react";

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

export const VISIBLE_MONTH_COUNT = 3;

const COLUMN_WIDTHS = {
  days: 34,
  weeks: 26,
  months: 18,
} as const;

function getDefaultWindowBounds(): { minDate: Date; maxDate: Date } {
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const minDate = new Date(today.getFullYear(), today.getMonth(), 1);
  const maxDate = new Date(minDate.getFullYear(), minDate.getMonth() + VISIBLE_MONTH_COUNT, 0);

  return { minDate, maxDate };
}

function getWindowBounds(weekOffset: number): { minDate: Date; maxDate: Date } {
  const { minDate: baseMin, maxDate: baseMax } = getDefaultWindowBounds();
  const shiftDays = weekOffset * 7;

  const minDate = new Date(baseMin);
  minDate.setDate(minDate.getDate() + shiftDays);
  const maxDate = new Date(baseMax);
  maxDate.setDate(maxDate.getDate() + shiftDays);

  return { minDate, maxDate };
}

function alignToMonday(date: Date): Date {
  const aligned = new Date(date);
  const day = aligned.getDay();
  aligned.setDate(aligned.getDate() - (day === 0 ? 6 : day - 1));
  return aligned;
}

/**
 * Wspólna logika timeline'u dla widoków harmonogramów.
 * Zarządza skalą czasu, oknem 3 miesięcy, ukrywaniem weekendów
 * oraz generowaniem dat/grup.
 */
export function useTimelineData(options?: UseTimelineDataOptions) {
  const { isMobile = false } = options || {};

  const [timeScale, setTimeScale] = useState<TimeScale>("weeks");
  const [hideWeekends, setHideWeekends] = useState(false);
  const [visibleWeekOffset, setVisibleWeekOffset] = useState(0);

  const todayColumnRef = useRef<HTMLTableCellElement>(null);
  const scrollContainerRef = useRef<HTMLDivElement>(null);

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

  const navigatePrev = useCallback(() => {
    setVisibleWeekOffset((offset) => offset - 1);
  }, []);

  const navigateNext = useCallback(() => {
    setVisibleWeekOffset((offset) => offset + 1);
  }, []);

  const resetToToday = useCallback(() => {
    setVisibleWeekOffset(0);
  }, []);

  const { allDates, allDateGroups } = useMemo(() => {
    const dates: Date[] = [];
    const { minDate, maxDate } = getWindowBounds(visibleWeekOffset);

    if (timeScale === "weeks") {
      const current = alignToMonday(minDate);
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

    const current = new Date(minDate);
    const dateGroups: DateGroup[] = [];
    let groupStartIdx = 0;

    while (current <= maxDate) {
      const monthStart = new Date(current.getFullYear(), current.getMonth(), 1);
      const monthLast = new Date(current.getFullYear(), current.getMonth() + 1, 0);
      const effectiveEnd = monthLast <= maxDate ? monthLast : maxDate;

      let count = 0;
      while (current <= effectiveEnd) {
        dates.push(new Date(current));
        current.setDate(current.getDate() + 1);
        count += 1;
      }

      dateGroups.push({
        label: monthStart.toLocaleDateString("pl-PL", {
          month: "long",
          year: "numeric",
        }),
        count,
        startIdx: groupStartIdx,
      });
      groupStartIdx += count;
    }

    return { allDates: dates, allDateGroups: dateGroups };
  }, [timeScale, visibleWeekOffset]);

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

  const datesRef = useRef(dates);
  datesRef.current = dates;

  const scrollToTodayColumn = useCallback(() => {
    const now = new Date();
    const columnWidth = COLUMN_WIDTHS[timeScale];
    const todayIdx = datesRef.current.findIndex(
      (d) =>
        d.getFullYear() === now.getFullYear() &&
        d.getMonth() === now.getMonth() &&
        d.getDate() === now.getDate(),
    );

    if (todayIdx < 0 || !scrollContainerRef.current) {
      return;
    }

    const el = scrollContainerRef.current;
    el.scrollLeft = todayIdx * columnWidth - el.clientWidth / 2 + columnWidth / 2;
  }, [timeScale]);

  const scrollToToday = useCallback(() => {
    setVisibleWeekOffset(0);
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        scrollToTodayColumn();
      });
    });
  }, [scrollToTodayColumn]);

  useEffect(() => {
    if (scrollContainerRef.current) {
      scrollContainerRef.current.scrollLeft = 0;
    }
  }, [visibleWeekOffset, timeScale]);

  return {
    timeScale,
    setTimeScale,
    hideWeekends,
    setHideWeekends,
    toggleWeekends: useCallback(() => setHideWeekends((h) => !h), []),
    visibleWeekOffset,

    dates,
    dateGroups,

    isToday,
    isWeekend,
    formatTimelineDate,
    isWorkInPeriod,
    getPeriodEnd,

    todayColumnRef,
    scrollContainerRef,
    scrollToToday,
    navigatePrev,
    navigateNext,
    resetToToday,
  };
}
