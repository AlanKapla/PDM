import { useCallback, useEffect, useRef, useState, type RefObject } from "react";

const MIN_ZOOM = 0.5;
const MAX_ZOOM = 3;
const ZOOM_SENSITIVITY = 0.0015;

export interface UseGanttTimelinePanZoomOptions {
  scrollContainerRef: RefObject<HTMLDivElement | null>;
  /** Ustawia mnożnik szerokości kolumny (gęstość timeline'u). */
  setZoomFactor: (value: number) => void;
}

export interface UseGanttTimelinePanZoomResult {
  isPanning: boolean;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

/**
 * Zarządza zoomem gęstości timeline'u (Ctrl/Cmd + scroll — kolumny szersze/węższe)
 * oraz poziomym panem (przeciągnięcie środkowym przyciskiem myszy).
 *
 * Wszystkie nasłuchy rejestrujemy na `document`, bo kontener siatki montuje się
 * dopiero po załadowaniu danych — gdyby efekt przypinał listenery do
 * `scrollContainerRef.current`, w chwili jego uruchomienia ref byłby jeszcze null.
 * Przynależność kursora do timeline sprawdzamy więc w momencie zdarzenia (`contains`).
 */
export function useGanttTimelinePanZoom({
  scrollContainerRef,
  setZoomFactor,
}: UseGanttTimelinePanZoomOptions): UseGanttTimelinePanZoomResult {
  const isPanningRef = useRef(false);
  const lastPanXRef = useRef(0);
  const zoomFactorRef = useRef(1);
  const [isPanning, setIsPanning] = useState(false);

  const applyZoom = useCallback((factor: number) => {
    zoomFactorRef.current = factor;
    setZoomFactor(factor);
  }, [setZoomFactor]);

  useEffect(() => {
    const isInsideTimeline = (target: EventTarget | null): boolean => {
      const container = scrollContainerRef.current;
      return container !== null && target instanceof Node && container.contains(target);
    };

    // Ctrl/Cmd + scroll → zoom gęstości. Kolumny rosną/maleją, a punkt pod kursorem
    // pozostaje na miejscu (kotwiczenie): szerokość skaluje się liniowo z mnożnikiem,
    // więc nowy scrollLeft = (scrollLeft + offsetKursora) * ratio − offsetKursora.
    const onWheel = (e: WheelEvent) => {
      if (!e.ctrlKey && !e.metaKey) {
        return;
      }
      const container = scrollContainerRef.current;
      if (container === null || !isInsideTimeline(e.target)) {
        return;
      }
      e.preventDefault();

      let deltaY = e.deltaY;
      if (e.deltaMode === 1) {
        deltaY *= 16;
      } else if (e.deltaMode === 2) {
        deltaY *= container.clientHeight;
      }

      const oldFactor = zoomFactorRef.current;
      const newFactor = clamp(oldFactor * Math.exp(-deltaY * ZOOM_SENSITIVITY), MIN_ZOOM, MAX_ZOOM);
      if (newFactor === oldFactor) {
        return;
      }

      const rect = container.getBoundingClientRect();
      const cursorX = e.clientX - rect.left;
      const ratio = newFactor / oldFactor;
      const newScrollLeft = (container.scrollLeft + cursorX) * ratio - cursorX;

      applyZoom(newFactor);
      requestAnimationFrame(() => {
        const el = scrollContainerRef.current;
        if (el !== null) {
          el.scrollLeft = newScrollLeft;
        }
      });
    };

    // Środkowy przycisk myszy (scroll) → pan timeline w poziomie.
    const onMouseDown = (e: MouseEvent) => {
      if (e.button !== 1 || !isInsideTimeline(e.target)) {
        return;
      }
      e.preventDefault(); // blokuje autoscroll przeglądarki
      isPanningRef.current = true;
      setIsPanning(true);
      lastPanXRef.current = e.clientX;
    };

    const onMouseMove = (e: MouseEvent) => {
      if (!isPanningRef.current) {
        return;
      }
      const container = scrollContainerRef.current;
      if (container === null) {
        return;
      }
      const deltaX = e.clientX - lastPanXRef.current;
      lastPanXRef.current = e.clientX;
      container.scrollLeft -= deltaX;
    };

    const endPan = () => {
      if (!isPanningRef.current) {
        return;
      }
      isPanningRef.current = false;
      setIsPanning(false);
    };

    document.addEventListener("wheel", onWheel, { passive: false });
    document.addEventListener("mousedown", onMouseDown);
    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", endPan);
    return () => {
      document.removeEventListener("wheel", onWheel);
      document.removeEventListener("mousedown", onMouseDown);
      document.removeEventListener("mousemove", onMouseMove);
      document.removeEventListener("mouseup", endPan);
    };
  }, [scrollContainerRef, applyZoom]);

  return { isPanning };
}
