import React, { useMemo } from "react";
import { Tooltip } from "@chakra-ui/react";
import { G } from "./ganttTokens";
import { getStageRange, toLocalDateStr, fmtShortDate } from "./ganttRowUtils";
import type { WorkScheduleStageWeb } from "../../types/workSchedule.types";

export interface GanttStageAggregateBarProps {
  stage: WorkScheduleStageWeb;
  dates: Date[];
  colMap: Record<string, number>;
  columnWidth: number;
  rowHeight: number;
}

function resolveColIdx(
  dateStr: string,
  colMap: Record<string, number>,
  firstDateStr: string,
  lastDateStr: string,
  datesLength: number,
  side: "start" | "end",
): number | null {
  const exact = colMap[dateStr];
  if (exact !== undefined) {
    return exact;
  }
  if (dateStr < firstDateStr) {
    return side === "start" ? 0 : null;
  }
  if (dateStr > lastDateStr) {
    return side === "end" ? datesLength - 1 : null;
  }
  const nextKey = Object.keys(colMap).find((k) => k >= dateStr);
  if (nextKey) {
    return colMap[nextKey];
  }
  return side === "start" ? 0 : datesLength - 1;
}

/**
 * Zbiorczy pasek etapu — widoczny gdy etap jest zwinięty.
 * Obejmuje zakres od najwcześniejszej do najpóźniejszej daty prac w etapie.
 */
export function GanttStageAggregateBar({
  stage,
  dates,
  colMap,
  columnWidth,
  rowHeight,
}: GanttStageAggregateBarProps): React.ReactElement | null {
  const range = useMemo(() => getStageRange(stage), [stage]);

  if (!range || dates.length === 0) {
    return null;
  }

  const firstDateStr = toLocalDateStr(dates[0]);
  const lastDateStr = toLocalDateStr(dates[dates.length - 1]);
  const startIdx = resolveColIdx(range.start, colMap, firstDateStr, lastDateStr, dates.length, "start");
  const endIdx = resolveColIdx(range.end, colMap, firstDateStr, lastDateStr, dates.length, "end");

  if (startIdx === null || endIdx === null) {
    return null;
  }

  const left = startIdx * columnWidth + 1;
  const width = Math.max((endIdx - startIdx + 1) * columnWidth - 2, 4);
  const barHeight = rowHeight * 0.55;
  const tooltipLabel = `${stage.name}: ${fmtShortDate(range.start)} – ${fmtShortDate(range.end)}`;

  return (
    <Tooltip label={tooltipLabel} hasArrow placement="top" openDelay={300}>
      <div
        role="img"
        aria-label={tooltipLabel}
        style={{
          position: "absolute",
          top: "50%",
          transform: "translateY(-50%)",
          left: `${left}px`,
          width: `${width}px`,
          height: `${barHeight}px`,
          borderRadius: 5,
          background: G.accent,
          opacity: 0.45,
          border: `2px solid ${G.accent}`,
          boxShadow: "0 1px 3px rgba(0,0,0,.15)",
          pointerEvents: "auto",
          cursor: "default",
          zIndex: 4,
        }}
      />
    </Tooltip>
  );
}
