import React, { useState, useRef, useCallback } from "react";
import { createPortal } from "react-dom";
import { useGantt } from "./GanttContext";
import { usePeriodsValidation } from "./usePeriodsValidation";
import { G } from "./ganttTokens";
import { toLocalDateStr, fmtShortDate } from "./ganttRowUtils";
import type { WorkScheduleStageWorkWeb, WorkScheduleStageWorkPeriodWeb } from "../../types/workSchedule.types";

interface GanttBarProps {
  work: WorkScheduleStageWorkWeb;
  stageId: string;
  /** Zgrupowane kolejne okresy renderowane jako jeden pasek */
  periods: WorkScheduleStageWorkPeriodWeb[];
  dates: Date[];
  colMap: Record<string, number>;
  columnWidth: number;
  rowHeight: number;
  /** Dependency drawing: mousedown na uchwycie */
  onDepHandleDown?: (side: "left" | "right", e: React.MouseEvent) => void;
  /** Dependency drawing: mouseup na uchwycie (cel połączenia) */
  onDepHandleMouseUp?: (side: "left" | "right") => void;
  /** Kliknięcie paska — rodzic decyduje co otworzyć */
  onBarClick?: (anchorRect: DOMRect) => void;
}

interface DragState {
  side: "left" | "right" | "move";
  originX: number;
  origStartIdx: number;
  origEndIdx: number;
  curStartIdx: number;
  curEndIdx: number;
}

/**
 * Zwraca indeks kolumny dla podanej daty z zachowaniem zakresu widocznych dat.
 * Jeśli data jest poza zakresem, klampuje do 0 lub dates.length-1.
 * Zwraca null gdy pasek jest całkowicie poza widokiem (należy go ukryć).
 */
function resolveColIdx(
  dateStr: string,
  colMap: Record<string, number>,
  firstDateStr: string,
  lastDateStr: string,
  datesLength: number,
  side: "start" | "end",
): number | null {
  const exact = colMap[dateStr];
  if (exact !== undefined) return exact;

  // Data wcześniejsza niż początek widoku
  if (dateStr < firstDateStr) {
    return side === "start" ? 0 : null; // start-period poza lewą krawędzią → klamp; end-period poza lewą → ukryj
  }
  // Data późniejsza niż koniec widoku
  if (dateStr > lastDateStr) {
    return side === "end" ? datesLength - 1 : null; // end-period poza prawą krawędzią → klamp; start-period poza prawą → ukryj
  }
  // Data w zakresie ale nie w mapie (np. weekend odfiltrowany) — szukaj najbliższej kolumny
  // dates jest posortowane, więc możemy porównać lexicograficznie (YYYY-MM-DD)
  for (let i = 0; i < datesLength; i++) {
    const key = Object.keys(colMap).find(k => k >= dateStr);
    if (key) return colMap[key];
  }
  return side === "start" ? 0 : datesLength - 1;
}

/**
 * Scala okresy które się stykają lub nakładają po zmianie dat.
 * Sortuje rosnąco i scala bloki z przerwą ≤ 1 dzień.
 */
function mergePeriodsAfterResize(
  periodList: Array<{ startDate: string; endDate: string; isClosed: boolean }>,
): Array<{ startDate: string; endDate: string; isClosed: boolean }> {
  if (periodList.length <= 1) return periodList;
  const sorted = [...periodList].sort((a, b) => a.startDate.localeCompare(b.startDate));
  const merged: typeof sorted = [{ ...sorted[0] }];
  for (let i = 1; i < sorted.length; i++) {
    const last = merged[merged.length - 1];
    const lastEnd = new Date(last.endDate + "T00:00:00");
    const curStart = new Date(sorted[i].startDate + "T00:00:00");
    // Scalaj gdy gap ≤ 1 dzień — ta sama logika co groupConsecutivePeriods
    const diffDays = Math.round((curStart.getTime() - lastEnd.getTime()) / 86_400_000);
    if (diffDays <= 1) {
      const laterEnd = lastEnd >= new Date(sorted[i].endDate + "T00:00:00") ? last.endDate : sorted[i].endDate;
      merged[merged.length - 1] = { ...last, endDate: laterEnd, isClosed: last.isClosed && sorted[i].isClosed };
    } else {
      merged.push({ ...sorted[i] });
    }
  }
  return merged;
}

function GanttBar({
  work,
  stageId,
  periods,
  dates,
  colMap,
  columnWidth,
  rowHeight,
  onDepHandleDown,
  onDepHandleMouseUp,
  onBarClick,
}: GanttBarProps) {
  const firstPeriod = periods[0];
  const lastPeriod = periods[periods.length - 1];

  const { mode, setPeriods: savePeriods } = useGantt();
  const { validate } = usePeriodsValidation();

  const [tooltip, setTooltip] = useState<{ x: number; y: number } | null>(null);
  const [showHandles, setShowHandles] = useState(false);
  const [dragValidationState, setDragValidationState] = useState<"valid" | "error" | "warning">("valid");
  const [dragResizeTooltip, setDragResizeTooltip] = useState<string | null>(null);
  // dragOverride — używane tylko podczas aktywnego przeciągania; null = liczymy z colMap
  const [dragOverride, setDragOverride] = useState<{ startIdx: number; endIdx: number } | null>(null);
  const [isMoveDragging, setIsMoveDragging] = useState(false);

  const barRef = useRef<HTMLDivElement>(null);
  const dragRef = useRef<DragState | null>(null);
  const isEditing = mode === "edit";

  // Wyznacz indeksy z uwzględnieniem zakresu widocznych dat
  const firstDateStr = dates.length > 0 ? toLocalDateStr(dates[0]) : "";
  const lastDateStr  = dates.length > 0 ? toLocalDateStr(dates[dates.length - 1]) : "";
  const startDateStr = firstPeriod.startDate.slice(0, 10);
  const endDateStr   = lastPeriod.endDate.slice(0, 10);

  const startIdxResolved = resolveColIdx(startDateStr, colMap, firstDateStr, lastDateStr, dates.length, "start");
  const endIdxResolved   = resolveColIdx(endDateStr,   colMap, firstDateStr, lastDateStr, dates.length, "end");

  // Pasek poza widocznym zakresem — nie renderuj
  const isOutOfView = startIdxResolved === null || endIdxResolved === null;

  const startIdx = startIdxResolved ?? 0;
  const endIdx   = endIdxResolved   ?? dates.length - 1;

  // Podczas drag używamy tymczasowych wartości, poza dragiem — zawsze świeże z colMap
  const displayStartIdx = dragOverride?.startIdx ?? startIdx;
  const displayEndIdx   = dragOverride?.endIdx   ?? endIdx;

  const left  = displayStartIdx * columnWidth + 1;
  const width = Math.max((displayEndIdx - displayStartIdx + 1) * columnWidth - 2, 4);

  const closedCount = periods.filter(p => p.isClosed).length;
  const closedState: "all" | "partial" | "none" =
    periods.length === 0 ? "none"
    : closedCount === periods.length ? "all"
    : closedCount > 0 ? "partial"
    : "none";

  const handleResizeMouseDown = useCallback(
    (e: React.MouseEvent<HTMLDivElement>, side: "left" | "right") => {
      if (!isEditing) return;
      e.stopPropagation();
      e.preventDefault();

      dragRef.current = {
        side,
        originX: e.clientX,
        origStartIdx: startIdx,
        origEndIdx: endIdx,
        curStartIdx: startIdx,
        curEndIdx: endIdx,
      };

      const handleMouseMove = (ev: MouseEvent) => {
        const drag = dragRef.current;
        if (!drag) return;
        const dx = ev.clientX - drag.originX;
        const dCols = Math.round(dx / columnWidth);
        if (drag.side === "left") {
          drag.curStartIdx = Math.max(0, Math.min(drag.origEndIdx, drag.origStartIdx + dCols));
          setDragOverride({ startIdx: drag.curStartIdx, endIdx: drag.origEndIdx });
        } else {
          drag.curEndIdx = Math.max(drag.origStartIdx, Math.min(dates.length - 1, drag.origEndIdx + dCols));
          setDragOverride({ startIdx: drag.origStartIdx, endIdx: drag.curEndIdx });
        }

        // Walidacja zależności podczas przeciągania
        const curStart = drag.side === "left"
          ? toLocalDateStr(dates[drag.curStartIdx])
          : firstPeriod.startDate.slice(0, 10);
        const curEnd = drag.side === "right"
          ? toLocalDateStr(dates[drag.curEndIdx])
          : lastPeriod.endDate.slice(0, 10);

        const groupIds = new Set(periods.map(p => p.id));
        const otherPeriodsForValidation = (work.periods ?? [])
          .filter(p => !groupIds.has(p.id))
          .map(p => ({ startDate: p.startDate.slice(0, 10), endDate: p.endDate.slice(0, 10) }));
        const periodsForValidation = [...otherPeriodsForValidation, { startDate: curStart, endDate: curEnd }];

        const result = validate(work.id, periodsForValidation);
        if (!result.valid) {
          setDragValidationState("error");
          setDragResizeTooltip(
            result.errors
              .map(e => `Min. ${e.violatedField === "startDate" ? "start" : "koniec"}: ${fmtShortDate(toLocalDateStr(e.requiredDate))}`)
              .join(", "),
          );
        } else if (result.warnings.length > 0) {
          setDragValidationState("warning");
          setDragResizeTooltip(
            result.warnings.map(w => `Przesunie "${w.successorName}" o ${w.willBeShiftedBy} dni`).join(", "),
          );
        } else {
          setDragValidationState("valid");
          setDragResizeTooltip(null);
        }
      };

      const handleMouseUp = async () => {
        const drag = dragRef.current;
        if (!drag) return;
        dragRef.current = null;
        document.removeEventListener("mousemove", handleMouseMove);
        document.removeEventListener("mouseup", handleMouseUp);
        setDragOverride(null);
        if (
          drag.curStartIdx === drag.origStartIdx &&
          drag.curEndIdx === drag.origEndIdx
        ) return;

        // Wszystkie periods z tej grupy (wizualnie scalonych) zastępujemy jednym okresem.
        // Dzięki temu drag na skalonym pasku (np. z 2 dotykających się periodów)
        // zapisuje je do API jako pojedynczy okres.
        const groupIds = new Set(periods.map(p => p.id));
        const otherPeriods = (work.periods ?? [])
          .filter(p => !groupIds.has(p.id))
          .map(p => ({ startDate: p.startDate.slice(0, 10), endDate: p.endDate.slice(0, 10), isClosed: p.isClosed }));

        const mergedGroupPeriod = {
          startDate: drag.side === "left"
            ? toLocalDateStr(dates[drag.curStartIdx])
            : firstPeriod.startDate.slice(0, 10),
          endDate: drag.side === "right"
            ? toLocalDateStr(dates[drag.curEndIdx])
            : lastPeriod.endDate.slice(0, 10),
          isClosed: periods.every(p => p.isClosed),
        };

        const combined = [...otherPeriods, mergedGroupPeriod]
          .sort((a, b) => a.startDate.localeCompare(b.startDate));
        // Dodatkowy merge na wypadek gdyby po resize pasek nachodził na inne okresy
        const finalPeriods = mergePeriodsAfterResize(combined);
        await savePeriods(stageId, work.id, finalPeriods);
        // drag override zostaje wyczyszczone powyżej; colMap zaktualizuje się po refetchu
      };

      document.addEventListener("mousemove", handleMouseMove);
      document.addEventListener("mouseup", handleMouseUp);
    },
    [isEditing, startIdx, endIdx, columnWidth, dates, firstPeriod, lastPeriod, work, stageId, savePeriods, validate],
  );

  // Obsługuje zarówno kliknięcie (otwiera popover) jak i przeciąganie (przesuwa pasek)
  const handleBarMoveMouseDown = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      e.stopPropagation();
      if (!isEditing) {
        // Poza trybem edycji: kliknięcie otwiera popover
        const rect = barRef.current?.getBoundingClientRect();
        if (rect) onBarClick?.(rect);
        return;
      }
      e.preventDefault();

      const origStart = startIdx;
      const origEnd = endIdx;
      const span = origEnd - origStart;
      let moved = false;

      dragRef.current = {
        side: "move",
        originX: e.clientX,
        origStartIdx: origStart,
        origEndIdx: origEnd,
        curStartIdx: origStart,
        curEndIdx: origEnd,
      };
      setIsMoveDragging(true);

      const handleMouseMove = (ev: MouseEvent) => {
        const drag = dragRef.current;
        if (!drag) return;
        const dx = ev.clientX - drag.originX;
        const dCols = Math.round(dx / columnWidth);
        if (dCols !== 0) moved = true;
        const newStart = Math.max(0, Math.min(dates.length - 1 - span, origStart + dCols));
        drag.curStartIdx = newStart;
        drag.curEndIdx = newStart + span;
        setDragOverride({ startIdx: drag.curStartIdx, endIdx: drag.curEndIdx });

        // Walidacja zależności podczas przesuwania
        const curStart = toLocalDateStr(dates[drag.curStartIdx]);
        const curEnd = toLocalDateStr(dates[drag.curEndIdx]);
        const groupIds = new Set(periods.map(p => p.id));
        const otherPeriods = (work.periods ?? [])
          .filter(p => !groupIds.has(p.id))
          .map(p => ({ startDate: p.startDate.slice(0, 10), endDate: p.endDate.slice(0, 10) }));
        const result = validate(work.id, [...otherPeriods, { startDate: curStart, endDate: curEnd }]);
        if (!result.valid) {
          setDragValidationState("error");
          setDragResizeTooltip(
            result.errors
              .map(err => `Min. ${err.violatedField === "startDate" ? "start" : "koniec"}: ${fmtShortDate(toLocalDateStr(err.requiredDate))}`)
              .join(", "),
          );
        } else if (result.warnings.length > 0) {
          setDragValidationState("warning");
          setDragResizeTooltip(result.warnings.map(w => `Przesunie "${w.successorName}" o ${w.willBeShiftedBy} dni`).join(", "));
        } else {
          setDragValidationState("valid");
          setDragResizeTooltip(null);
        }
      };

      const handleMouseUp = async () => {
        const drag = dragRef.current;
        if (!drag) return;
        dragRef.current = null;
        document.removeEventListener("mousemove", handleMouseMove);
        document.removeEventListener("mouseup", handleMouseUp);
        setDragOverride(null);
        setIsMoveDragging(false);
        setDragValidationState("valid");
        setDragResizeTooltip(null);

        if (!moved) {
          // Brak ruchu → traktuj jako kliknięcie: otwórz popover
          const rect = barRef.current?.getBoundingClientRect();
          if (rect) onBarClick?.(rect);
          return;
        }
        if (drag.curStartIdx === drag.origStartIdx) return;

        // Przesuń grupę periods do nowej pozycji (scala je w jeden okres)
        const groupIds = new Set(periods.map(p => p.id));
        const otherPeriods = (work.periods ?? [])
          .filter(p => !groupIds.has(p.id))
          .map(p => ({ startDate: p.startDate.slice(0, 10), endDate: p.endDate.slice(0, 10), isClosed: p.isClosed }));
        const movedPeriod = {
          startDate: toLocalDateStr(dates[drag.curStartIdx]),
          endDate: toLocalDateStr(dates[drag.curEndIdx]),
          isClosed: periods.every(p => p.isClosed),
        };
        const combined = [...otherPeriods, movedPeriod].sort((a, b) => a.startDate.localeCompare(b.startDate));
        await savePeriods(stageId, work.id, mergePeriodsAfterResize(combined));
      };

      document.addEventListener("mousemove", handleMouseMove);
      document.addEventListener("mouseup", handleMouseUp);
    },
    [isEditing, startIdx, endIdx, columnWidth, dates, work, periods, stageId, savePeriods, validate],
  );

  // Pasek poza widokiem — nic nie renderuj
  if (isOutOfView) return null;

  // Styl wrappera — pozycjonuje pasek i ustala zIndex, ale NIE ma overflow:hidden
  // (uchwyt dep-handle wystaje poza pasek i nie może być obcinany)
  const wrapperStyle: React.CSSProperties = {
    position: "absolute",
    top: "50%",
    transform: "translateY(-50%)",
    left: `${left}px`,
    width: `${width}px`,
    height: `${rowHeight * 0.65}px`,
    zIndex: 5,
    userSelect: "none",
  };

  // Styl wizualnego paska — wypełnia cały wrapper, overflow:hidden dla border-radius
  const dragValidationOutline =
    dragValidationState === "error"   ? "2px solid #dc2626" :
    dragValidationState === "warning" ? `2px solid ${G.amber}` :
    undefined;

  const innerBarStyle: React.CSSProperties = {
    position: "relative",
    width: "100%",
    height: "100%",
    borderRadius: 5,
    background: work.colorRgb,
    boxShadow: "0 1px 3px rgba(0,0,0,.2)",
    cursor: isEditing ? (isMoveDragging ? "grabbing" : "grab") : "pointer",
    display: "flex",
    alignItems: "center",
    overflow: "hidden",
    transition: "box-shadow .15s, filter .15s",
    opacity: closedState === "all" ? 0.6 : 1,
    backgroundImage: closedState === "all"
      ? "repeating-linear-gradient(-45deg, transparent, transparent 4px, rgba(0,0,0,.12) 4px, rgba(0,0,0,.12) 5px)"
      : undefined,
    outline: dragValidationOutline,
    outlineOffset: dragValidationOutline ? 1 : undefined,
  };

  const DEP_HANDLE: React.CSSProperties = {
    position: "absolute",
    top: "50%",
    transform: "translateY(-50%)",
    width: 10,
    height: 10,
    borderRadius: "50%",
    background: "#fff",
    border: `2px solid ${G.accent}`,
    cursor: "crosshair",
    zIndex: 10,
    // Zawsze widoczne w trybie edycji — pełna opacity na hover, przyciemnione poza
    opacity: showHandles ? 1 : 0.35,
    transition: "opacity .15s",
  };

  return (
    <>
      {/* Wrapper: odpowiada za pozycjonowanie; brak overflow:hidden — uchwyty dep-handle nie są obcinane */}
      <div
        style={wrapperStyle}
        onMouseEnter={e => {
          const r = e.currentTarget.getBoundingClientRect();
          setTooltip({ x: r.left + r.width / 2, y: r.top });
          setShowHandles(true);
          if (barRef.current) {
            barRef.current.style.boxShadow = "0 2px 8px rgba(0,0,0,.25)";
            barRef.current.style.filter = "brightness(1.08)";
          }
        }}
        onMouseLeave={() => {
          setTooltip(null);
          setShowHandles(false);
          if (barRef.current) {
            barRef.current.style.boxShadow = "0 1px 3px rgba(0,0,0,.2)";
            barRef.current.style.filter = "";
          }
        }}
      >
        {/* Uchwyt dependencji — lewy (poza paskiem, nie obcinany przez overflow:hidden) */}
        {isEditing && onDepHandleDown && (
          <div
            style={{ ...DEP_HANDLE, left: -5 }}
            title="Przesuń, aby dodać zależność"
            onMouseDown={e => { e.stopPropagation(); e.preventDefault(); onDepHandleDown("left", e); }}
            onMouseUp={() => onDepHandleMouseUp?.("left")}
          />
        )}

        {/* Wizualny pasek — overflow:hidden dla border-radius */}
        <div
          ref={barRef}
          style={innerBarStyle}
          onMouseDown={handleBarMoveMouseDown}
        >
          {isEditing && (
            <div
              style={{ position: "absolute", left: 0, top: 0, width: 7, height: "100%", cursor: "ew-resize", zIndex: 6, display: "flex", alignItems: "center", justifyContent: "center" }}
              onMouseDown={e => handleResizeMouseDown(e, "left")}
            >
              <div style={{ width: 2, height: 10, background: "rgba(255,255,255,.5)", borderRadius: 1 }} />
            </div>
          )}
          {/* Tooltip walidacji podczas drag resize */}
          {dragResizeTooltip && (
            <div
              style={{
                position: "absolute",
                bottom: "100%",
                left: "50%",
                transform: "translateX(-50%)",
                background: dragValidationState === "error" ? "#dc2626" : G.amber,
                color: "#fff",
                fontSize: 10,
                padding: "3px 7px",
                borderRadius: 4,
                whiteSpace: "nowrap",
                marginBottom: 4,
                zIndex: 20,
                pointerEvents: "none",
              }}
            >
              {dragResizeTooltip}
            </div>
          )}
          {closedState === "all" && (
            <span style={{ position: "absolute", right: isEditing ? 10 : 5, color: "rgba(255,255,255,.9)", fontSize: 11, fontWeight: 700 }}>✓</span>
          )}
          {closedState === "partial" && (
            <span style={{ position: "absolute", right: isEditing ? 10 : 5, color: "rgba(255,255,255,.8)", fontSize: 9 }}>◐</span>
          )}
          {/* Nazwy przypisanych użytkowników wyświetlane bezpośrednio na pasku */}
          {(work.assignees?.length ?? 0) > 0 && (
            <span
              style={{
                position: "absolute",
                left: isEditing ? 10 : 5,
                right: isEditing ? (closedState !== "none" ? 22 : 10) : (closedState !== "none" ? 18 : 5),
                fontSize: 10,
                color: "rgba(255,255,255,.92)",
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
                pointerEvents: "none",
                textShadow: "0 1px 2px rgba(0,0,0,.35)",
                fontWeight: 500,
              }}
            >
              {work.assignees.map(a => a.userName).join(", ")}
            </span>
          )}
          {isEditing && (
            <div
              style={{ position: "absolute", right: 0, top: 0, width: 7, height: "100%", cursor: "ew-resize", zIndex: 6, display: "flex", alignItems: "center", justifyContent: "center" }}
              onMouseDown={e => handleResizeMouseDown(e, "right")}
            >
              <div style={{ width: 2, height: 10, background: "rgba(255,255,255,.5)", borderRadius: 1 }} />
            </div>
          )}
        </div>

        {/* Uchwyt dependencji — prawy (poza paskiem, nie obcinany przez overflow:hidden) */}
        {isEditing && onDepHandleDown && (
          <div
            style={{ ...DEP_HANDLE, right: -5 }}
            title="Przesuń, aby dodać zależność"
            onMouseDown={e => { e.stopPropagation(); e.preventDefault(); onDepHandleDown("right", e); }}
            onMouseUp={() => onDepHandleMouseUp?.("right")}
          />
        )}
      </div>

      {tooltip && createPortal(
        <div
          style={{
            position: "fixed",
            top: tooltip.y - 8,
            left: tooltip.x,
            transform: "translate(-50%, -100%)",
            background: "#1a1917",
            color: "#fff",
            borderRadius: 8,
            padding: "10px 14px",
            fontSize: 12,
            maxWidth: 220,
            zIndex: 300,
            pointerEvents: "none",
            boxShadow: "0 4px 12px rgba(0,0,0,.3)",
          }}
        >
          <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 6 }}>{work.name}</div>
          <div style={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <div><span style={{ color: "rgba(255,255,255,.5)" }}>Start  </span><span style={{ fontFamily: "monospace" }}>{fmtShortDate(firstPeriod.startDate)}</span></div>
            <div><span style={{ color: "rgba(255,255,255,.5)" }}>Koniec </span><span style={{ fontFamily: "monospace" }}>{fmtShortDate(lastPeriod.endDate)}</span></div>
            {periods.length > 1 && <div><span style={{ color: "rgba(255,255,255,.5)" }}>Okresy </span><span style={{ fontFamily: "monospace" }}>{periods.length}</span></div>}
            <div><span style={{ color: "rgba(255,255,255,.5)" }}>Status </span><span style={{ fontFamily: "monospace" }}>{closedState === "all" ? "✓ Zamknięty" : closedState === "partial" ? "◐ Częściowy" : "Otwarty"}</span></div>
          </div>
          {(work.assignees ?? []).length > 0 && (
            <>
              <div style={{ borderTop: "1px solid rgba(255,255,255,.15)", margin: "6px 0" }} />
              <div style={{ fontSize: 11, color: "rgba(255,255,255,.7)" }}>{work.assignees.map(a => a.userName).join(", ")}</div>
            </>
          )}
          <div style={{ marginTop: 6, fontSize: 10, color: "rgba(255,255,255,.4)" }}>Klik → zarządzaj okresami</div>
        </div>,
        document.body,
      )}

    </>
  );
}

export default React.memo(GanttBar, (prev, next) => {
  if (prev.work !== next.work) return false;
  if (prev.stageId !== next.stageId) return false;
  if (prev.columnWidth !== next.columnWidth) return false;
  if (prev.rowHeight !== next.rowHeight) return false;
  if (prev.dates !== next.dates) return false;
  if (prev.colMap !== next.colMap) return false;
  if (prev.periods.length !== next.periods.length) return false;
  for (let i = 0; i < prev.periods.length; i++) {
    const p = prev.periods[i], n = next.periods[i];
    if (p.id !== n.id || p.startDate !== n.startDate || p.endDate !== n.endDate || p.isClosed !== n.isClosed) return false;
  }
  return true;
});
