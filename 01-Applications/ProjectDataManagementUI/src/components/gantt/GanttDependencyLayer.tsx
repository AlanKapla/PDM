import { useMemo } from "react";
import { Box, useColorModeValue } from "@chakra-ui/react";
import { useGantt } from "./GanttContext";
import type { WorkDependencyType } from "../../types/workSchedule.types";
import type { WorkScheduleStageWeb, WorkScheduleStageWorkWeb } from "../../types/workSchedule.types";

interface WorkPosition {
  workId: string;
  rowIndex: number;
  startColIdx: number;
  endColIdx: number;
}

interface GanttDependencyLayerProps {
  dates: Date[];
  columnWidth: number;
  treeColumnWidth: number;
  rowHeight: number;
}

/** Plansza nakładkowa SVG z liniami zależności */
export default function GanttDependencyLayer({
  dates, columnWidth, treeColumnWidth, rowHeight,
}: GanttDependencyLayerProps) {
  const { schedule, expandedStages, mode } = useGantt();

  const arrowColor = useColorModeValue("#805AD5", "#B794F4"); // level2
  const selectedColor = useColorModeValue("#E53E3E", "#FC8181");

  const dependencies = schedule?.dependencies ?? [];

  // Budujemy mapę: workId → pozycja (rząd + zakres kolumn)
  const workPositions = useMemo(() => {
    const positions = new Map<string, WorkPosition>();
    let row = 0; // liczymy od 1 (header zajmuje 0)

    const processStages = (stages: WorkScheduleStageWeb[]) => {
      const sorted = [...stages].sort((a, b) => a.order - b.order);
      for (const stage of sorted) {
        row++; // wiersz etapu
        if (expandedStages.has(stage.id)) {
          const works = [...(stage.works ?? [])].sort((a, b) => a.order - b.order);
          for (const work of works) {
            const workPeriods = work.periods ?? [];
            if (workPeriods.length > 0) {
              const startStr = workPeriods.reduce(
                (minDate, period) => {
                  const periodStart = period.startDate.slice(0, 10);
                  return periodStart < minDate ? periodStart : minDate;
                },
                workPeriods[0].startDate.slice(0, 10));
              const endStr = workPeriods.reduce(
                (maxDate, period) => {
                  const periodEnd = period.endDate.slice(0, 10);
                  return periodEnd > maxDate ? periodEnd : maxDate;
                },
                workPeriods[0].endDate.slice(0, 10));
              const startIdx = dates.findIndex(d => toLocalStr(d) === startStr);
              const endIdx = dates.findIndex(d => toLocalStr(d) === endStr);
              positions.set(work.id, {
                workId: work.id,
                rowIndex: row,
                startColIdx: startIdx >= 0 ? startIdx : 0,
                endColIdx: endIdx >= 0 ? endIdx : dates.length - 1,
              });
            }
            row++;
          }
          processStages(stage.childStages ?? []);
        }
      }
    };

    processStages(schedule?.stages ?? []);
    return positions;
  }, [schedule?.stages, expandedStages, dates]);

  if (dependencies.length === 0) return null;

  const totalRows = workPositions.size + (schedule?.stages?.length ?? 0) + 2; // approx
  const totalHeight = totalRows * rowHeight;
  const totalWidth = dates.length * columnWidth;

  const getArrowPath = (
    predPos: WorkPosition,
    succPos: WorkPosition,
    depType: number
  ) => {
    // Pozycje Y (środek wiersza)
    const predY = (predPos.rowIndex + 0.5) * rowHeight;
    const succY = (succPos.rowIndex + 0.5) * rowHeight;

    let x1: number, x2: number;

    // Typ 0: FS — od końca poprzednika do początku następnika
    // Typ 1: SS — od początku poprzednika do początku następnika
    // Typ 2: FF — od końca poprzednika do końca następnika
    // Typ 3: SF — od początku poprzednika do końca następnika
    if (depType === 0) { // FS
      x1 = (predPos.endColIdx + 1) * columnWidth;
      x2 = predPos.endColIdx < succPos.startColIdx
        ? succPos.startColIdx * columnWidth
        : succPos.startColIdx * columnWidth;
    } else if (depType === 1) { // SS
      x1 = predPos.startColIdx * columnWidth;
      x2 = succPos.startColIdx * columnWidth;
    } else if (depType === 2) { // FF
      x1 = (predPos.endColIdx + 1) * columnWidth;
      x2 = (succPos.endColIdx + 1) * columnWidth;
    } else { // SF
      x1 = predPos.startColIdx * columnWidth;
      x2 = (succPos.endColIdx + 1) * columnWidth;
    }

    const midX = (x1 + x2) / 2;

    return `M ${x1} ${predY} C ${midX} ${predY}, ${midX} ${succY}, ${x2} ${succY}`;
  };

  return (
    <Box
      position="absolute"
      top={`${rowHeight * 2}px`} // skip header rows
      left={`${treeColumnWidth}px`}
      pointerEvents="none"
      zIndex={10}
    >
      <svg
        width={totalWidth}
        height={totalHeight}
        overflow="visible"
        style={{ pointerEvents: "none" }}
      >
        <defs>
          <marker
            id="dep-arrow"
            viewBox="0 0 10 10"
            refX="9"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto"
          >
            <path d="M 0 0 L 10 5 L 0 10 z" fill={arrowColor} />
          </marker>
        </defs>

        {dependencies.map((dep, idx) => {
          const pred = workPositions.get(dep.predecessorWorkId);
          const succ = workPositions.get(dep.successorWorkId);
          if (!pred || !succ) return null;

          const path = getArrowPath(pred, succ, dep.dependencyType);
          const lagLabel = dep.lagDays !== 0 ? `${dep.lagDays > 0 ? "+" : ""}${dep.lagDays}d` : "";

          return (
            <g key={dep.id ?? idx}>
              <path
                d={path}
                stroke={arrowColor}
                strokeWidth={1.5}
                fill="none"
                markerEnd="url(#dep-arrow)"
                strokeDasharray={dep.lagDays !== 0 ? "4 2" : undefined}
                opacity={0.8}
              />
              {lagLabel && (
                <text
                  x={(workPositions.get(dep.predecessorWorkId)!.endColIdx + workPositions.get(dep.successorWorkId)!.startColIdx) / 2 * columnWidth}
                  y={((workPositions.get(dep.predecessorWorkId)!.rowIndex + workPositions.get(dep.successorWorkId)!.rowIndex) / 2 + 0.5) * rowHeight - 4}
                  fontSize="9"
                  fill={arrowColor}
                  textAnchor="middle"
                >
                  {lagLabel}
                </text>
              )}
            </g>
          );
        })}
      </svg>
    </Box>
  );
}

function toLocalStr(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}
