import { type RefObject, useMemo, useRef, useState, useCallback } from "react";
import GanttPeriodsPopover, { type PeriodsSaveResult } from "./GanttPeriodsPopover";
import GanttBar from "./GanttBar";
import { G } from "./ganttTokens";
import { isWeekendDate, isTodayDate, makeDateColMap, toLocalDateStr, groupConsecutivePeriods, type FlatRow } from "./ganttRowUtils";
import { useGantt } from "./GanttContext";
import type { DateGroup, TimeScale } from "../../hooks/useTimelineData";
import type { WorkScheduleStageWorkWeb, WorkScheduleWorkDependencyWeb } from "../../types/workSchedule.types";
import { WorkDependencyType } from "../../types/workSchedule.types";

const DAY_ABBREV = ["Nd", "Pn", "Wt", "Śr", "Cz", "Pt", "So"];

interface DraftPeriod {
  rowId: string;
  stageId: string;
  work: WorkScheduleStageWorkWeb;
  startColIdx: number;
  endColIdx: number;
}

interface GanttRightGridProps {
  flatRows: FlatRow[];
  dates: Date[];
  dateGroups: DateGroup[];
  timeScale: TimeScale;
  columnWidth: number;
  scrollRef: RefObject<HTMLDivElement>;
  onScroll: () => void;
}

export default function GanttRightGrid({
  flatRows,
  dates,
  dateGroups,
  timeScale,
  columnWidth,
  scrollRef,
  onScroll,
}: GanttRightGridProps) {
  const { setPeriods, setPeriodIsClosed, mode, schedule, setDependencies, showDependencies } = useGantt();
  const isEditing = mode === "edit";
  const colMap = useMemo(() => makeDateColMap(dates), [dates]);

  interface OpenBarInfo { key: string; work: WorkScheduleStageWorkWeb; stageId: string; anchorRect: DOMRect; }
  const [openBarInfo, setOpenBarInfo] = useState<OpenBarInfo | null>(null);

  // Indeksy kolumn weekendowych — obliczane raz, nie per-wierszowo
  const weekendIdxs = useMemo(
    () => dates.reduce<number[]>((acc, d, i) => { if (isWeekendDate(d)) acc.push(i); return acc; }, []),
    [dates],
  );

  const totalWidth = dates.length * columnWidth;
  const totalHeight = flatRows.reduce((s, r) => s + r.height, 0);

  const todayIdx = dates.findIndex(d => isTodayDate(d));
  const todayLeft = todayIdx >= 0 ? todayIdx * columnWidth + Math.floor(columnWidth / 2) : -1;

  const bodyRef = useRef<HTMLDivElement>(null);
  // Osobny ref dla kontenera nagłówka — scrollLeft synchronizowany z body przez JS,
  // zamiast position:sticky który tworzy osobny compositing layer GPU mogący dryfować
  const headerScrollRef = useRef<HTMLDivElement>(null);
  const draftRef = useRef<DraftPeriod | null>(null);
  const [draftDisplay, setDraftDisplay] = useState<DraftPeriod | null>(null);

  // ─── Dependency drawing state (Feature 10) ─────────────────────────────────
  interface DrawingLine {
    fromWorkId: string;
    fromSide: "left" | "right";
    toX: number;
    toY: number;
  }
  const [drawingLine, setDrawingLine] = useState<DrawingLine | null>(null);
  const drawingLineRef = useRef<DrawingLine | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<{ depId: string; x: number; y: number } | null>(null);

  // Mapa: workId → Y środka wiersza pracy w siatce
  const workRowYMap = useMemo(() => {
    const map: Record<string, number> = {};
    let y = 0;
    for (const row of flatRows) {
      if (row.kind === "work" && row.work) {
        map[row.work.id] = y + G.ROW_H / 2;
      }
      y += row.height;
    }
    return map;
  }, [flatRows]);

  // Mapa: workId → praca
  const workById = useMemo(() => {
    const map: Record<string, WorkScheduleStageWorkWeb> = {};
    for (const row of flatRows) {
      if (row.work) map[row.work.id] = row.work;
    }
    return map;
  }, [flatRows]);

  const getWorkEdgeX = useCallback((workId: string, side: "left" | "right"): number | null => {
    const w = workById[workId];
    if (!w || !w.periods?.length) return null;
    if (side === "left") {
      const sIdx = colMap[w.periods[0].startDate.slice(0, 10)];
      return sIdx !== undefined ? sIdx * columnWidth : null;
    } else {
      const lastP = w.periods[w.periods.length - 1];
      const eIdx = colMap[lastP.endDate.slice(0, 10)];
      return eIdx !== undefined ? (eIdx + 1) * columnWidth : null;
    }
  }, [workById, colMap, columnWidth]);

  const handleDepHandleDown = useCallback((
    workId: string, side: "left" | "right", e: React.MouseEvent,
  ) => {
    if (!isEditing) return;
    const bodyRect = bodyRef.current?.getBoundingClientRect();
    if (!bodyRect) return;

    const line: DrawingLine = {
      fromWorkId: workId, fromSide: side,
      toX: e.clientX - bodyRect.left,
      toY: e.clientY - bodyRect.top,
    };
    setDrawingLine(line);
    drawingLineRef.current = line;

    const onMove = (ev: MouseEvent) => {
      const rect = bodyRef.current?.getBoundingClientRect();
      if (!rect) return;
      const updated = { ...drawingLineRef.current!, toX: ev.clientX - rect.left, toY: ev.clientY - rect.top };
      drawingLineRef.current = updated;
      setDrawingLine({ ...updated });
    };
    const onUp = () => {
      document.removeEventListener("mousemove", onMove);
      document.removeEventListener("mouseup", onUp);
      // Jeśli mouseup trafił w uchwyt innej belki, handleDepHandleMouseUp go obsłuży
      // W przeciwnym razie anuluj rysowanie
      setTimeout(() => {
        if (drawingLineRef.current) {
          drawingLineRef.current = null;
          setDrawingLine(null);
        }
      }, 50);
    };
    document.addEventListener("mousemove", onMove);
    document.addEventListener("mouseup", onUp);
  }, [isEditing]);

  const handleDepHandleMouseUp = useCallback(async (toWorkId: string, toSide: "left" | "right") => {
    const dl = drawingLineRef.current;
    if (!dl) return;
    drawingLineRef.current = null;
    setDrawingLine(null);

    const { fromWorkId, fromSide } = dl;
    if (fromWorkId === toWorkId) return;

    // Wyznacz typ zależności na podstawie uchwytów (prawy=Finish, lewy=Start)
    let depType: WorkDependencyType;
    if (fromSide === "right" && toSide === "left") depType = WorkDependencyType.FinishToStart;
    else if (fromSide === "left" && toSide === "left") depType = WorkDependencyType.StartToStart;
    else if (fromSide === "right" && toSide === "right") depType = WorkDependencyType.FinishToFinish;
    else depType = WorkDependencyType.StartToFinish;

    const existing = schedule?.dependencies ?? [];
    const isDuplicate = existing.some(d =>
      d.predecessorWorkId === fromWorkId &&
      d.successorWorkId === toWorkId &&
      d.dependencyType === depType,
    );
    if (isDuplicate) return;

    const newDep: WorkScheduleWorkDependencyWeb = {
      id: crypto.randomUUID(),
      predecessorWorkId: fromWorkId,
      successorWorkId: toWorkId,
      dependencyType: depType,
      lagDays: 0,
    };
    await setDependencies([...existing, newDep]);
  }, [drawingLineRef, schedule, setDependencies]);

  const handleDepLineClick = useCallback((depId: string, x: number, y: number) => {
    if (!isEditing) return;
    setDeleteConfirm({ depId, x, y });
  }, [isEditing]);

  const confirmDeleteDependency = useCallback(async (depId: string) => {
    const updated = (schedule?.dependencies ?? []).filter(d => d.id !== depId);
    await setDependencies(updated);
    setDeleteConfirm(null);
  }, [schedule, setDependencies]);

  const getColIdx = useCallback((clientX: number) => {
    const bodyRect = bodyRef.current?.getBoundingClientRect();
    if (!bodyRect) return 0;
    return Math.max(0, Math.min(dates.length - 1, Math.floor((clientX - bodyRect.left) / columnWidth)));
  }, [dates.length, columnWidth]);

  const handleWorkRowMouseDown = useCallback((
    e: React.MouseEvent<HTMLDivElement>,
    row: FlatRow,
  ) => {
    if (!isEditing || !row.work) return;
    e.preventDefault();

    const colIdx = getColIdx(e.clientX);
    const draft: DraftPeriod = {
      rowId: row.id,
      stageId: row.stage.id,
      work: row.work,
      startColIdx: colIdx,
      endColIdx: colIdx,
    };
    draftRef.current = draft;
    setDraftDisplay({ ...draft });

    const handleMove = (ev: MouseEvent) => {
      if (!draftRef.current) return;
      const ci = getColIdx(ev.clientX);
      draftRef.current = { ...draftRef.current, endColIdx: ci };
      setDraftDisplay({ ...draftRef.current });
    };

    const handleUp = async () => {
      document.removeEventListener("mousemove", handleMove);
      document.removeEventListener("mouseup", handleUp);
      const d = draftRef.current;
      draftRef.current = null;
      setDraftDisplay(null);
      if (!d) return;

      const sIdx = Math.min(d.startColIdx, d.endColIdx);
      // endDate włączywny — minimum sIdx + 1, bo backend wymaga EndDate > StartDate
      const eIdx = Math.max(d.startColIdx, d.endColIdx, sIdx + 1);
      if (eIdx >= dates.length) return;

      const startDate = toLocalDateStr(dates[sIdx]);
      const endDate = toLocalDateStr(dates[eIdx]);

      const hasOverlap = (d.work.periods ?? []).some(
        p => startDate <= p.endDate.slice(0, 10) && endDate >= p.startDate.slice(0, 10),
      );
      if (hasOverlap) return;

      const newPeriods = [
        ...(d.work.periods ?? []).map(p => ({
          startDate: p.startDate.slice(0, 10),
          endDate: p.endDate.slice(0, 10),
          isClosed: p.isClosed,
        })),
        { startDate, endDate, isClosed: false },
      ];
      await setPeriods(d.stageId, d.work.id, newPeriods);
    };

    document.addEventListener("mousemove", handleMove);
    document.addEventListener("mouseup", handleUp);
  }, [isEditing, getColIdx, dates, setPeriods]);

  const formatDay = (d: Date) => {
    if (timeScale === "weeks") return DAY_ABBREV[d.getDay()];
    return String(d.getDate());
  };

  // Synchronizacja poziomego scrollu nagłówka z body — wywoływana synchronicznie
  // w zdarzeniu scroll przed repaintem, więc brak visual lag
  const handleBodyScroll = useCallback(() => {
    if (headerScrollRef.current && scrollRef.current) {
      headerScrollRef.current.scrollLeft = scrollRef.current.scrollLeft;
    }
    onScroll();
  }, [scrollRef, onScroll]);

  return (
    <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0, overflow: "hidden" }}>
      {/* Nagłówek — osobny kontener overflow:hidden, scrollLeft synchronizowany z body przez JS.
          Brak position:sticky = brak osobnego GPU compositing layer = zero subpixel drift. */}
      <div
        ref={headerScrollRef}
        style={{
          overflow: "hidden",
          flexShrink: 0,
          height: G.HEADER_H,
          background: G.surface,
          borderBottom: `1px solid ${G.borderStrong}`,
          zIndex: 20,
          position: "relative",
        }}
      >
      <div style={{ width: totalWidth, height: "100%" }}>
        {/* Wiersz grup — position:absolute, left = g.startIdx * columnWidth.
            Każdy element niezależny od sąsiadów → zero kumulacji błędów flex. */}
        <div
          style={{
            position: "relative",
            height: G.HEADER_WEEKS,
            borderBottom: `1px solid ${G.border}`,
          }}
        >
          {dateGroups.map(g => (
            <div
              key={g.startIdx}
              style={{
                position: "absolute",
                left: g.startIdx * columnWidth,
                top: 0,
                width: g.count * columnWidth,
                height: "100%",
                boxSizing: "border-box",
                borderRight: `1px solid ${G.border}`,
                padding: "0 8px",
                overflow: "hidden",
                fontSize: 11,
                fontWeight: 600,
                color: G.text2,
                whiteSpace: "nowrap",
                textOverflow: "ellipsis",
                lineHeight: `${G.HEADER_WEEKS}px`,
              }}
            >
              {g.label}
            </div>
          ))}
        </div>

        {/* Wiersz dni — position:absolute, left = i * columnWidth.
            Identyczna formuła co linie siatki body → zero możliwości dryftu. */}
        <div style={{
          position: "relative",
          height: G.HEADER_DAYS,
        }}>
          {dates.map((d, i) => {
            const isToday = isTodayDate(d);
            const isWknd = isWeekendDate(d);
            return (
              <div
                key={i}
                style={{
                  position: "absolute",
                  left: i * columnWidth,
                  top: 0,
                  width: columnWidth,
                  height: "100%",
                  boxSizing: "border-box",
                  borderRight: `1px solid ${G.border}`,
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  backgroundColor: isToday ? G.todayBg : undefined,
                  fontSize: 10,
                  fontWeight: isToday ? 700 : 400,
                  color: isToday ? G.today : isWknd ? G.text3 : G.text2,
                }}
              >
                {formatDay(d)}
                {isToday && (
                  <div
                    style={{
                      position: "absolute",
                      bottom: 2,
                      width: 4,
                      height: 4,
                      borderRadius: "50%",
                      background: G.today,
                    }}
                  />
                )}
              </div>
            );
          })}
        </div>
      </div>
      </div>{/* /headerScrollRef */}

      {/* Body — właściwy kontener scrolla; scrollRef przekazany z GanttLayout do sync left-panel */}
      <div
        ref={scrollRef}
        onScroll={handleBodyScroll}
        style={{ flex: 1, overflow: "auto", minWidth: 0 }}
      >
      {/* Ciało siatki */}
      <div
        ref={bodyRef}
        style={{
          position: "relative",
          width: totalWidth,
          minHeight: totalHeight,
        }}
      >
        {/* Linia dzisiejsza */}
        {todayLeft > 0 && (
          <div
            style={{
              position: "absolute",
              top: 0,
              left: todayLeft,
              width: 2,
              height: totalHeight,
              background: G.today,
              boxShadow: "0 0 8px rgba(91,141,239,.4)",
              zIndex: 10,
              pointerEvents: "none",
            }}
          />
        )}

        {weekendIdxs.map(i => (
          <div
            key={i}
            style={{
              position: "absolute",
              top: 0,
              left: i * columnWidth,
              width: columnWidth,
              height: totalHeight,
              background: "rgba(0,0,0,.018)",
              pointerEvents: "none",
              zIndex: 2,
            }}
          />
        ))}

        {/* Pionowe linie siatki — left = (i+1)*columnWidth - 1, dokładnie pod prawą krawędzią
            każdego elementu nagłówka (te same mnożniki * columnWidth). Zero akumulacji błędów. */}
        {dates.map((_, i) => (
          <div
            key={i}
            style={{
              position: "absolute",
              top: 0,
              left: (i + 1) * columnWidth - 1,
              width: 1,
              height: totalHeight,
              background: G.border,
              pointerEvents: "none",
              zIndex: 1,
            }}
          />
        ))}

        {/* Wiersze */}
        {flatRows.map(row => {
          const rowBg =
            row.kind === "stageHeader" ? G.stageBg
            : row.kind === "addWork" ? G.surface2
            : row.work?.isClosed ? G.closedBg
            : G.surface;

          return (
            <div
              key={row.id}
              style={{
                position: "relative",
                height: row.height,
                display: "flex",
                borderBottom: `1px solid ${G.border}`,
                background: rowBg,
                cursor: isEditing && row.kind === "work" ? "crosshair" : "default",
              }}
              onMouseDown={row.kind === "work" ? e => handleWorkRowMouseDown(e, row) : undefined}
            >
              {/* Paski dla zakresów pracy — kolejne okresy scalane w jeden pasek */}
              {row.kind === "work" && row.work &&
                groupConsecutivePeriods(row.work.periods ?? []).map(group => {
                  const barKey = `${row.stage.id}-${row.work!.id}-${group[0].id}`;
                  return (
                    <GanttBar
                      key={group[0].id}
                      work={row.work!}
                      stageId={row.stage.id}
                      periods={group}
                      dates={dates}
                      colMap={colMap}
                      columnWidth={columnWidth}
                      rowHeight={row.height}
                      onBarClick={anchorRect => setOpenBarInfo({ key: barKey, work: row.work!, stageId: row.stage.id, anchorRect })}
                      onDepHandleDown={isEditing ? (side, e) => handleDepHandleDown(row.work!.id, side, e) : undefined}
                      onDepHandleMouseUp={isEditing ? (side) => handleDepHandleMouseUp(row.work!.id, side) : undefined}
                    />
                  );
                })}

              {/* Szkic nowego okresu podczas ciągnięcia */}
              {row.kind === "work" && draftDisplay?.rowId === row.id && (() => {
                const sIdx = Math.min(draftDisplay.startColIdx, draftDisplay.endColIdx);
                const eIdx = Math.max(draftDisplay.startColIdx, draftDisplay.endColIdx);
                return (
                  <div
                    style={{
                      position: "absolute",
                      top: "50%",
                      transform: "translateY(-50%)",
                      left: sIdx * columnWidth + 1,
                      width: Math.max((eIdx - sIdx + 1) * columnWidth - 2, 4),
                      height: row.height * 0.65,
                      borderRadius: 5,
                      background: row.work?.colorRgb ?? G.accent,
                      opacity: 0.35,
                      border: `2px dashed ${row.work?.colorRgb ?? G.accent}`,
                      pointerEvents: "none",
                      zIndex: 4,
                    }}
                  />
                );
              })()}
            </div>
          );
        })}
        {/* SVG warstwa zależności (Feature 10) */}
        <svg
          style={{ position: "absolute", inset: 0, width: totalWidth, height: totalHeight, pointerEvents: "none", zIndex: 20 }}
          overflow="visible"
        >
          {showDependencies && (schedule?.dependencies ?? []).map(dep => {
            const fromX = getWorkEdgeX(dep.predecessorWorkId,
              dep.dependencyType === WorkDependencyType.FinishToStart || dep.dependencyType === WorkDependencyType.FinishToFinish ? "right" : "left");
            const toX = getWorkEdgeX(dep.successorWorkId,
              dep.dependencyType === WorkDependencyType.FinishToStart || dep.dependencyType === WorkDependencyType.StartToStart ? "left" : "right");
            const fromY = workRowYMap[dep.predecessorWorkId];
            const toY = workRowYMap[dep.successorWorkId];
            if (fromX == null || toX == null || fromY == null || toY == null) return null;

            const mx = (fromX + toX) / 2;
            const d = `M ${fromX} ${fromY} C ${mx} ${fromY}, ${mx} ${toY}, ${toX} ${toY}`;
            const midX = mx, midY = (fromY + toY) / 2;

            return (
              <g key={dep.id}>
                <path
                  d={d}
                  fill="none"
                  stroke={G.accent}
                  strokeWidth={2}
                  opacity={0.65}
                  style={{ pointerEvents: isEditing ? "stroke" : "none", cursor: isEditing ? "pointer" : "default" }}
                  onClick={isEditing ? () => handleDepLineClick(dep.id, midX, midY) : undefined}
                />
                {/* Strzałka na końcu */}
                <circle cx={toX} cy={toY} r={3} fill={G.accent} opacity={0.65} />
              </g>
            );
          })}

          {/* Linia rysowana podczas tworzenia nowej zależności */}
          {drawingLine && (() => {
            const fromX = getWorkEdgeX(drawingLine.fromWorkId, drawingLine.fromSide);
            const fromY = workRowYMap[drawingLine.fromWorkId];
            if (fromX == null || fromY == null) return null;
            const mx = (fromX + drawingLine.toX) / 2;
            return (
              <path
                d={`M ${fromX} ${fromY} C ${mx} ${fromY}, ${mx} ${drawingLine.toY}, ${drawingLine.toX} ${drawingLine.toY}`}
                fill="none"
                stroke={G.accent}
                strokeWidth={2}
                strokeDasharray="6 3"
                opacity={0.7}
              />
            );
          })()}
        </svg>

        {/* Spacer wyrównujący wysokość ciała siatki z lewym panelem w trybie edycji.
            Lewy panel ma przycisk "Dodaj etap" (G.ROW_H) na dole obszaru scrollowanego
            — ta sama wysokość musi być uwzględniona tutaj, inaczej scrollTop_max się różni. */}
        {isEditing && <div style={{ height: G.ROW_H }} />}

        {/* Potwierdzenie usunięcia zależności */}
        {deleteConfirm && isEditing && (
          <div
            onClick={e => e.stopPropagation()}
            style={{
              position: "absolute",
              top: deleteConfirm.y - 24,
              left: deleteConfirm.x - 60,
              zIndex: 30,
              background: G.surface,
              border: `1px solid ${G.border}`,
              borderRadius: 6,
              padding: "4px 8px",
              boxShadow: "0 2px 8px rgba(0,0,0,.15)",
              display: "flex",
              alignItems: "center",
              gap: 6,
              fontSize: 11,
            }}
          >
            <span style={{ color: G.text2 }}>Usuń zależność?</span>
            <button
              onClick={() => confirmDeleteDependency(deleteConfirm.depId)}
              style={{ background: "#e53e3e", color: "#fff", border: "none", borderRadius: 4, padding: "1px 7px", cursor: "pointer", fontSize: 11 }}
            >
              Tak
            </button>
            <button
              onClick={() => setDeleteConfirm(null)}
              style={{ background: "none", border: `1px solid ${G.border}`, borderRadius: 4, padding: "1px 7px", cursor: "pointer", fontSize: 11, color: G.text2 }}
            >
              Nie
            </button>
          </div>
        )}
      </div>
      </div>

      {openBarInfo && (
        <GanttPeriodsPopover
          work={openBarInfo.work}
          onClose={() => setOpenBarInfo(null)}
          onSave={async ({ finalList, closedToggles, hasStructuralChange }: PeriodsSaveResult) => {
            if (hasStructuralChange) {
              await setPeriods(openBarInfo.stageId, openBarInfo.work.id, finalList);
            } else {
              for (const { id, isClosed } of closedToggles) {
                await setPeriodIsClosed(openBarInfo.stageId, openBarInfo.work.id, id, isClosed);
              }
            }
          }}
        />
      )}
    </div>
  );
}
