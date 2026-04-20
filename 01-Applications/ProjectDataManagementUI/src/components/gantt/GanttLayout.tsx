import { useRef, useCallback, useMemo, useEffect, useState, type RefObject } from "react";
import { Skeleton, VStack, HStack } from "@chakra-ui/react";
import { useGantt } from "./GanttContext";
import GanttLeftPanel from "./GanttLeftPanel";
import GanttRightGrid from "./GanttRightGrid";
import { buildFlatRows, isTodayDate } from "./ganttRowUtils";
import { G } from "./ganttTokens";
import type { DateGroup, TimeScale } from "../../hooks/useTimelineData";

interface GanttLayoutProps {
  dates: Date[];
  dateGroups: DateGroup[];
  timeScale: TimeScale;
  columnWidth: number;
  hideWeekends: boolean;
  scrollContainerRef?: RefObject<HTMLDivElement>;
  /** Wysokość kontenera Gantt — domyślnie "calc(100vh - 140px)" */
  height?: string;
  /** Automatyczny scroll do dzisiejszej kolumny po zamontowaniu */
  autoScrollToToday?: boolean;
}

const SKELETON_ROWS = 6;

export default function GanttLayout({
  dates,
  dateGroups,
  timeScale,
  columnWidth,
  scrollContainerRef: scrollContainerRefFromProps,
  height = "calc(100vh - 140px)",
  autoScrollToToday = false,
}: GanttLayoutProps) {
  const { isLoading, schedule, expandedStages, collapsedWorks, mode, isMutating } = useGantt();

  const internalScrollRef = useRef<HTMLDivElement>(null);
  const scrollContainerRef = scrollContainerRefFromProps ?? internalScrollRef;

  // Auto-scroll do dzisiaj po zamontowaniu — używane w widoku "Moje prace"
  useEffect(() => {
    if (!autoScrollToToday || !dates.length) return;
    const todayIdx = dates.findIndex(isTodayDate);
    if (todayIdx < 0) return;
    requestAnimationFrame(() => {
      if (scrollContainerRef.current) {
        const el = scrollContainerRef.current;
        el.scrollLeft = todayIdx * columnWidth - el.clientWidth / 2 + columnWidth / 2;
      }
    });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const leftBodyRef = useRef<HTMLDivElement>(null);
  const isSyncing = useRef(false);
  const [scrollbarH, setScrollbarH] = useState(0);
  const scrollPositionRef = useRef<number>(0);
  const wasMutatingRef = useRef(false);

  // Kompensacja wysokości poziomego scrollbara w prawym panelu.
  // scrollContainerRef.clientHeight jest pomniejszone o ~17px przez scrollbar poziomy,
  // co skutkuje innym scrollTop_max niż w lewym panelu (overflow: hidden).
  useEffect(() => {
    const el = scrollContainerRef.current;
    if (!el) return;
    const measure = () => setScrollbarH(el.offsetHeight - el.clientHeight);
    const obs = new ResizeObserver(measure);
    obs.observe(el);
    measure();
    return () => obs.disconnect();
  }, [scrollContainerRef]);

  // BUG 9 — Zachowaj pozycję scroll podczas mutacji/refetch
  useEffect(() => {
    const hasActiveMutation = isMutating.size > 0;
    if (hasActiveMutation && !wasMutatingRef.current) {
      // Mutacja startuje — zapisz scroll
      wasMutatingRef.current = true;
      if (scrollContainerRef.current) {
        scrollPositionRef.current = scrollContainerRef.current.scrollLeft;
      }
    }
    if (!hasActiveMutation && wasMutatingRef.current) {
      // Mutacja skończyła się — przywróć scroll
      wasMutatingRef.current = false;
      const savedLeft = scrollPositionRef.current;
      requestAnimationFrame(() => {
        if (scrollContainerRef.current) {
          scrollContainerRef.current.scrollLeft = savedLeft;
        }
      });
    }
  }, [isMutating, scrollContainerRef]);

  const flatRows = useMemo(
    () => buildFlatRows(schedule?.stages ?? [], expandedStages, mode, collapsedWorks),
    [schedule, expandedStages, mode, collapsedWorks],
  );

  /** Synchronizuje pozycję pionową lewego panelu z prawym */
  const onRightScroll = useCallback(() => {
    if (isSyncing.current || !leftBodyRef.current || !scrollContainerRef.current) return;
    isSyncing.current = true;
    leftBodyRef.current.scrollTop = scrollContainerRef.current.scrollTop;
    requestAnimationFrame(() => { isSyncing.current = false; });
  }, [scrollContainerRef]);

  if (isLoading) {
    return (
      <div style={{ background: G.surface, border: `1px solid ${G.border}`, borderRadius: 8, padding: 16 }}>
        <VStack spacing={3} align="stretch">
          {Array.from({ length: SKELETON_ROWS }).map((_, i) => (
            <HStack key={i} spacing={3}>
              <Skeleton height="32px" width="280px" borderRadius="md" />
              <Skeleton height="32px" flex={1} borderRadius="md" />
            </HStack>
          ))}
        </VStack>
      </div>
    );
  }

  return (
    <div
      style={{
        display: "flex",
        border: `1px solid ${G.border}`,
        borderRadius: 8,
        overflow: "hidden",
        background: G.surface,
        height,
        minHeight: 400,
      }}
    >
      {/* Lewy panel — stały 340px */}
      <div
        style={{
          width: G.LEFT_W,
          flexShrink: 0,
          display: "flex",
          flexDirection: "column",
          borderRight: `1px solid ${G.borderStrong}`,
          background: G.surface,
          zIndex: 10,
        }}
      >
        <GanttLeftPanel flatRows={flatRows} leftBodyRef={leftBodyRef} scrollbarH={scrollbarH} />
      </div>

      {/* Prawy panel — przewijana siatka */}
      <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0, overflow: "hidden" }}>
        <GanttRightGrid
          flatRows={flatRows}
          dates={dates}
          dateGroups={dateGroups}
          timeScale={timeScale}
          columnWidth={columnWidth}
          scrollRef={scrollContainerRef}
          onScroll={onRightScroll}
        />
      </div>
    </div>
  );
}

export { GanttLayout };
