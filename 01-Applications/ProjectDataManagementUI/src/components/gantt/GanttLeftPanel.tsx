import { useState, useRef, useEffect, useMemo } from "react";
import { createPortal } from "react-dom";
import { ChevronDown, ChevronRight, Plus, Trash2, MoreHorizontal, ArrowRight, GripVertical, X, MessageCircle, Users, Link2 } from "lucide-react";
import { Button, IconButton } from "@chakra-ui/react";
import { useGantt } from "./GanttContext";
import GanttInlineName from "./GanttInlineName";
import GanttAssigneesPopover from "./GanttAssigneesPopover";
import GanttCommentPopover from "./GanttCommentPopover";
import GanttDepsPopover from "./GanttDepsPopover";
import { G } from "./ganttTokens";
import { stageProgress, workCheckState, fmtCompactDate, getStageRange, type FlatRow } from "./ganttRowUtils";
import type { WorkScheduleStageWeb, WorkScheduleStageWorkWeb } from "../../types/workSchedule.types";
import { WorkDependencyType } from "../../types/workSchedule.types";

const WORK_COLORS = [
  "#3182CE", "#E53E3E", "#38A169", "#C05621",
  "#805AD5", "#0987A0", "#D69E2E", "#1A7A4A",
];

const WORK_COLOR_PALETTE = [
  "#10b981", "#f59e0b", "#8b5cf6", "#06b6d4",
  "#ec4899", "#f97316", "#84cc16", "#ef4444",
  "#3b82f6", "#a855f7", "#14b8a6", "#f43f5e",
];

interface GanttLeftPanelProps {
  flatRows: FlatRow[];
  leftBodyRef: React.RefObject<HTMLDivElement>;
  scrollbarH: number;
}

export default function GanttLeftPanel({ flatRows, leftBodyRef, scrollbarH }: GanttLeftPanelProps) {
  const {
    mode,
    members,
    expandedStages,
    collapsedWorks,
    toggleStage,
    addStage,
    deleteStage,
    renameStage,
    addWork,
    deleteWork,
    renameWork,
    setWorkIsClosed,
    setWorkColor,
    setPeriods,
    setPeriodIsClosed,
    setAssignments,
    toggleWorkPeriods,
    schedule,
    reorderStages,
    reorderWorks,
    ganttPermissions,
  } = useGantt();

  // Mapa workId → nazwa — budowana raz ze wszystkich etapów harmonogramu
  const workNameById = useMemo(() => {
    const map: Record<string, string> = {};
    function collectWorks(stages: WorkScheduleStageWeb[]) {
      for (const s of stages) {
        for (const w of s.works ?? []) map[w.id] = w.name;
        collectWorks(s.childStages ?? []);
      }
    }
    collectWorks(schedule?.stages ?? []);
    return map;
  }, [schedule?.stages]);

  const DEP_TYPE_SHORT: Record<WorkDependencyType, string> = {
    [WorkDependencyType.FinishToStart]: "FS",
    [WorkDependencyType.StartToStart]: "SS",
    [WorkDependencyType.FinishToFinish]: "FF",
    [WorkDependencyType.StartToFinish]: "SF",
  };

  const isEditing = mode === "edit";
  const [addingWorkFor, setAddingWorkFor] = useState<string | null>(null);
  const [newWorkName, setNewWorkName] = useState("");
  const [newWorkColor, setNewWorkColor] = useState(WORK_COLORS[0]);
  // Hover łapany przez CSS :hover — nie przez stan, żeby nie powodować re-render całego panelu
  const [assigneesFor, setAssigneesFor] = useState<{
    stageId: string;
    work: WorkScheduleStageWorkWeb;
    anchor: DOMRect;
  } | null>(null);
  const [colorPickerFor, setColorPickerFor] = useState<{
    stageId: string;
    workId: string;
    currentColor: string;
    anchor: DOMRect;
  } | null>(null);
  const [commentsFor, setCommentsFor] = useState<{
    stageId: string;
    work: WorkScheduleStageWorkWeb;
    anchor: DOMRect;
  } | null>(null);
  const [depsFor, setDepsFor] = useState<{
    stageId: string;
    work: WorkScheduleStageWorkWeb;
    anchor: DOMRect;
  } | null>(null);
  // Stan menu kontekstowego zakresu pracy
  const [workMenuFor, setWorkMenuFor] = useState<{
    stageId: string;
    work: WorkScheduleStageWorkWeb;
    anchor: DOMRect;
  } | null>(null);

  const colorPickerRef = useRef<HTMLDivElement>(null);
  const commentsModalRef = useRef<HTMLDivElement>(null);
  const workMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!workMenuFor) return;
    const handler = (e: MouseEvent) => {
      if (workMenuRef.current && !workMenuRef.current.contains(e.target as Node))
        setWorkMenuFor(null);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [workMenuFor]);

  useEffect(() => {
    if (!colorPickerFor) return;
    const handler = (e: MouseEvent) => {
      if (colorPickerRef.current && !colorPickerRef.current.contains(e.target as Node))
        setColorPickerFor(null);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [colorPickerFor]);

  useEffect(() => {
    if (!commentsFor) return;
    const handler = (e: MouseEvent) => {
      if (commentsModalRef.current && !commentsModalRef.current.contains(e.target as Node))
        setCommentsFor(null);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [commentsFor]);

  // ── Przeciąganie wierszy etapów/zakresów (drag-to-reorder) ──────────────────

  interface RowDragSource {
    kind: "stage" | "work";
    /** Dla stage: id etapu. Dla work: id etapu nadrzędnego */
    stageId: string;
    sourceId: string;
    /** Głębokość źródłowego wiersza — reordering tylko wśród braci tego samego depth */
    depth: number;
  }

  const rowDragRef = useRef<RowDragSource | null>(null);
  const rowDragInsertRef = useRef<number | null>(null);
  const [rowDragInsert, setRowDragInsert] = useState<number | null>(null);

  const handleRowGripMouseDown = (e: React.MouseEvent, row: FlatRow) => {
    if (mode !== "edit") return;
    e.preventDefault();
    e.stopPropagation();

    rowDragRef.current = {
      kind: row.kind === "stageHeader" ? "stage" : "work",
      stageId: row.stage.id,
      sourceId: row.kind === "stageHeader" ? row.stage.id : row.work!.id,
      depth: row.depth,
    };

    const onMove = (ev: MouseEvent) => {
      const container = leftBodyRef.current;
      if (!container) return;
      const rect = container.getBoundingClientRect();
      const offsetY = ev.clientY - rect.top + container.scrollTop;

      // Wyznacz indeks wstawienia na podstawie Y kursora
      let accumulated = 0;
      let targetIdx = 0;
      for (let i = 0; i < flatRows.length; i++) {
        const midY = accumulated + flatRows[i].height / 2;
        if (offsetY <= midY) { targetIdx = i; break; }
        accumulated += flatRows[i].height;
        targetIdx = i + 1;
      }
      rowDragInsertRef.current = targetIdx;
      setRowDragInsert(targetIdx);
    };

    const onUp = async () => {
      document.removeEventListener("mousemove", onMove);
      document.removeEventListener("mouseup", onUp);

      const drag = rowDragRef.current;
      const insertIdx = rowDragInsertRef.current;
      rowDragRef.current = null;
      rowDragInsertRef.current = null;
      setRowDragInsert(null);

      if (drag === null || insertIdx === null) return;

      if (drag.kind === "stage") {
        // Pobierz etapy będące braćmi na tej samej głębokości (ten sam parent)
        const siblings = flatRows.filter(r => r.kind === "stageHeader" && r.depth === drag.depth);
        const siblingIds = siblings.map(r => r.stage.id);
        const fromFlatIdx = flatRows.findIndex(r => r.kind === "stageHeader" && r.stage.id === drag.sourceId);
        if (fromFlatIdx < 0) return;

        // Oblicz nowy indeks wśród braci — biorąc pod uwagę, że insertIdx jest w flatRows
        const siblingFlatIdxs = siblings.map(r => flatRows.indexOf(r));
        const fromSiblingIdx = siblingIds.indexOf(drag.sourceId);
        // Znajdź, po którym bracie chcemy wstawić
        let toSiblingIdx = 0;
        for (let si = 0; si < siblingFlatIdxs.length; si++) {
          if (insertIdx > siblingFlatIdxs[si]) toSiblingIdx = si + 1;
          else break;
        }
        if (toSiblingIdx === fromSiblingIdx || toSiblingIdx === fromSiblingIdx + 1) return;

        const reordered = [...siblingIds];
        reordered.splice(fromSiblingIdx, 1);
        const adjustedTo = toSiblingIdx > fromSiblingIdx ? toSiblingIdx - 1 : toSiblingIdx;
        reordered.splice(adjustedTo, 0, drag.sourceId);
        await reorderStages(reordered);
      } else {
        // Reorder zakresów wewnątrz etapu
        const workRows = flatRows.filter(r => r.kind === "work" && r.stage.id === drag.stageId);
        const workIds = workRows.map(r => r.work!.id);
        const fromFlatIdx = flatRows.findIndex(r => r.kind === "work" && r.work?.id === drag.sourceId);
        if (fromFlatIdx < 0) return;

        const workFlatIdxs = workRows.map(r => flatRows.indexOf(r));
        const fromWorkIdx = workIds.indexOf(drag.sourceId);
        let toWorkIdx = 0;
        for (let wi = 0; wi < workFlatIdxs.length; wi++) {
          if (insertIdx > workFlatIdxs[wi]) toWorkIdx = wi + 1;
          else break;
        }
        if (toWorkIdx === fromWorkIdx || toWorkIdx === fromWorkIdx + 1) return;

        const reordered = [...workIds];
        reordered.splice(fromWorkIdx, 1);
        const adjustedTo = toWorkIdx > fromWorkIdx ? toWorkIdx - 1 : toWorkIdx;
        reordered.splice(adjustedTo, 0, drag.sourceId);
        await reorderWorks(drag.stageId, reordered);
      }
    };

    document.addEventListener("mousemove", onMove);
    document.addEventListener("mouseup", onUp);
  };

  const handleAddWork = async (stageId: string) => {
    const trimmed = newWorkName.trim();
    if (!trimmed) return;
    await addWork(stageId, trimmed, newWorkColor);
    setAddingWorkFor(null);
    setNewWorkName("");
    setNewWorkColor(WORK_COLORS[0]);
  };

  const handleWorkCheckbox = async (stageId: string, work: WorkScheduleStageWorkWeb) => {
    if ((work.periods ?? []).length === 0) return;
    const state = workCheckState(work);
    await setWorkIsClosed(stageId, work.id, state !== "checked");
  };

  const openAssignees = (e: React.MouseEvent<HTMLElement>, stageId: string, work: WorkScheduleStageWorkWeb) => {
    setAssigneesFor({ stageId, work, anchor: e.currentTarget.getBoundingClientRect() });
  };

  /* ── renderery per rodzaj wiersza ── */

  const renderStageHeader = (row: FlatRow) => {
    const { stage, depth } = row;
    const { done, total } = stageProgress(stage);
    const isExpanded = expandedStages.has(stage.id);
    const pct = total > 0 ? Math.round((done / total) * 100) : 0;

    return (
      <div
        key={row.id}
        className="gantt-row"
        style={{
          height: row.height,
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: `0 10px 0 ${10 + depth * G.DEPTH_INDENT}px`,
          background: G.stageBg,
          borderBottom: `1px solid ${G.border}`,
          userSelect: "none",
        }}
      >
        {isEditing && (
          <div
            onMouseDown={e => handleRowGripMouseDown(e, row)}
            style={{ cursor: "grab", color: G.text3, width: 16, height: 16, flexShrink: 0, display: "flex", alignItems: "center", justifyContent: "center", marginRight: 4 }}
            title="Przeciągnij, aby zmienić kolejność"
          >
            <GripVertical size={14} />
          </div>
        )}
        <IconButton
          size="xs"
          variant="ghost"
          colorScheme="gray"
          aria-label={isExpanded ? "Zwiń etap" : "Rozwiń etap"}
          icon={isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
          onClick={() => toggleStage(stage.id)}
        />

        <div style={{ flex: 1, minWidth: 0, display: "flex", alignItems: "center" }}>
          <GanttInlineName
            value={stage.name}
            isEditing={isEditing}
            fontWeight={600}
            fontSize="13px"
            color={G.text}
            onCommit={name => renameStage(stage.id, name)}
          />
        </div>

        {total > 0 && (
          <div
            style={{
              flexShrink: 0,
              padding: "1px 6px",
              borderRadius: 10,
              background: pct === 100 ? G.greenLight : G.accentLight,
              color: pct === 100 ? G.green : G.accent,
              fontSize: 10,
              fontWeight: 700,
            }}
          >
            {done}/{total}
          </div>
        )}

        {/* Zakres dat etapu */}
        {(() => {
          const range = getStageRange(stage);
          if (!range) return null;
          return (
            <span style={{
              fontSize: 10, fontFamily: "monospace", fontWeight: 500,
              color: pct === 100 ? G.green : G.text2,
              whiteSpace: "nowrap", flexShrink: 0,
              padding: "0 4px", background: "#f0ede8", borderRadius: 4,
            }}>
              {fmtCompactDate(range.start)}–{fmtCompactDate(range.end)}
            </span>
          );
        })()}

        {isEditing && (
          <>
            <IconButton
              size="xs"
              variant="ghost"
              colorScheme="gray"
              aria-label="Dodaj podetap"
              title="Dodaj podetap"
              icon={<Plus size={12} />}
              onClick={() => addStage("Nowy podetap", stage.id)}
            />
            <IconButton
              size="xs"
              variant="ghost"
              colorScheme="gray"
              aria-label="Usuń etap"
              title="Usuń etap"
              icon={<Trash2 size={13} />}
              onClick={() => deleteStage(stage.id)}
            />
          </>
        )}
      </div>
    );
  };

  const renderStageDetail = (row: FlatRow) => {
    const { stage, depth } = row;
    const { done, total } = stageProgress(stage);
    const pct = total > 0 ? Math.round((done / total) * 100) : 0;

    return (
      <div
        key={row.id}
        style={{
          height: row.height,
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          gap: 2,
          padding: `0 12px 0 ${16 + depth * G.DEPTH_INDENT}px`,
          background: G.surface,
          borderBottom: `1px solid ${G.border}`,
        }}
      >
        <div style={{ height: 4, background: G.border, borderRadius: 2, overflow: "hidden" }}>
          <div style={{ width: `${pct}%`, height: "100%", background: G.greenMid, borderRadius: 2, transition: "width .3s" }} />
        </div>
        <div style={{ fontSize: 10, color: G.text3 }}>
          Ukończone: {done} / {total} ({pct}%)
        </div>
      </div>
    );
  };

  const renderWorkRow = (row: FlatRow) => {
    const { stage, work, depth } = row;
    if (!work) return null;

    const checkState = workCheckState(work);
    const assignees = work.assignees ?? [];
    const periods = work.periods ?? [];
    const hasMultiplePeriods = periods.length > 1;
    const periodsCollapsed = collapsedWorks.has(work.id);
    const commentCount = work.comments?.length ?? 0;

    // BUG 11: używaj setPeriodIsClosed (PATCH) zamiast setPeriods (PUT)
    const togglePeriodClosed = async (periodId: string, isClosed: boolean) => {
      await setPeriodIsClosed(stage.id, work.id, periodId, isClosed);
    };

const noPeriods = (work.periods ?? []).length === 0;
        const checkBorderColor = noPeriods ? G.border : checkState === "unchecked" ? G.borderStrong : checkState === "indeterminate" ? G.amber : G.green;
        const checkBg = noPeriods ? G.surface2 : checkState === "unchecked" ? "#fff" : checkState === "indeterminate" ? G.amberLight : G.green;

    return (
      <div
        key={row.id}
        className="gantt-row"
        style={{
          height: row.height,
          overflow: "hidden",
          display: "flex",
          flexDirection: "column",
          background: work.isClosed ? G.closedBg : G.surface,
          borderBottom: `1px solid ${G.border}`,
        }}
      >
        {/* Nagłówek wiersza pracy */}
        <div style={{ height: G.ROW_H, display: "flex", alignItems: "center", gap: 5, padding: `0 8px 0 ${8 + depth * G.DEPTH_INDENT}px` }}>
        {/* Uchwyt przeciągania */}
        {isEditing && (
          <div
            onMouseDown={e => handleRowGripMouseDown(e, row)}
            style={{ cursor: "grab", color: G.text3, width: 16, height: 16, flexShrink: 0, display: "flex", alignItems: "center", justifyContent: "center", marginRight: 4 }}
            title="Przeciągnij, aby zmienić kolejność"
          >
            <GripVertical size={14} />
          </div>
        )}
        {/* Przycisk zwijania/rozwijania okresów */}
        {hasMultiplePeriods ? (
          <IconButton
            size="xs"
            variant="ghost"
            colorScheme="gray"
            aria-label={periodsCollapsed ? "Rozwiń okresy" : "Zwiń okresy"}
            title={periodsCollapsed ? "Rozwiń okresy" : "Zwiń okresy"}
            icon={periodsCollapsed ? <ChevronRight size={13} /> : <ChevronDown size={13} />}
            onClick={() => toggleWorkPeriods(work.id)}
          />
        ) : (
          <div style={{ width: 20, flexShrink: 0 }} />
        )}
        {/* Checkbox */}
        <div
          onClick={() => !noPeriods && handleWorkCheckbox(stage.id, work)}
          title={noPeriods ? "Dodaj okres, aby zamknąć zakres" : undefined}
          style={{
            width: 18, height: 18, borderRadius: 4, flexShrink: 0,
            border: `1.5px solid ${checkBorderColor}`,
            background: checkBg,
            display: "flex", alignItems: "center", justifyContent: "center",
            cursor: noPeriods ? "not-allowed" : "pointer",
            opacity: noPeriods ? 0.5 : 1,
          }}
        >
          {!noPeriods && checkState === "checked" && <span style={{ color: "#fff", fontSize: 12, fontWeight: 700, lineHeight: 1 }}>✓</span>}
          {!noPeriods && checkState === "indeterminate" && <span style={{ color: G.amber, fontSize: 13, fontWeight: 700, lineHeight: 1 }}>–</span>}
        </div>

        {/* Kolor dot — w trybie edycji klikalny (otwiera color picker) */}
        <div
          onClick={isEditing ? (e) => {
            const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
            setColorPickerFor({ stageId: stage.id, workId: work.id, currentColor: work.colorRgb, anchor: rect });
          } : undefined}
          style={{
            width: 10, height: 10, borderRadius: "50%", background: work.colorRgb, flexShrink: 0,
            cursor: isEditing ? "pointer" : "default",
          }}
        />

        {/* Nazwa */}
        <div style={{ flex: 1, minWidth: 0, display: "flex", alignItems: "center" }}>
          <GanttInlineName
            value={work.name}
            isEditing={isEditing}
            fontSize="13px"
            color={work.isClosed ? G.text3 : G.text}
            textDecoration={work.isClosed ? "line-through" : "none"}
            onCommit={name => renameWork(stage.id, work.id, name)}
          />
        </div>

        {/* Zakres dat — lepsza widoczność (BUG 3) */}
        {periods.length > 0 && (
          <span style={{
            fontSize: 11, fontFamily: "monospace", fontWeight: 500,
            color: checkState === "checked" ? G.green : G.text2,
            whiteSpace: "nowrap", flexShrink: 0,
            padding: "0 4px", background: "#f0ede8", borderRadius: 4,
          }}>
            {fmtCompactDate(periods[0].startDate)}–{fmtCompactDate(periods[periods.length - 1].endDate)}
          </span>
        )}

        {/* Zależności — poprzednicy i następniki tego zakresu */}
        {(() => {
          const deps = schedule?.dependencies ?? [];
          const predecessors = deps.filter(d => d.successorWorkId === work.id);
          const successors   = deps.filter(d => d.predecessorWorkId === work.id);
          if (predecessors.length === 0 && successors.length === 0) return null;

          const allDepTitle = [
            ...predecessors.map(d => `← ${workNameById[d.predecessorWorkId] ?? "?"} (${DEP_TYPE_SHORT[d.dependencyType]})`),
            ...successors.map(d =>   `→ ${workNameById[d.successorWorkId]   ?? "?"} (${DEP_TYPE_SHORT[d.dependencyType]})`),
          ].join("\n");

          return (
            <div
              title={allDepTitle}
              style={{
                display: "flex", alignItems: "center", gap: 2, flexShrink: 0,
                height: 24, padding: "0 6px", borderRadius: 4, cursor: "default",
                background: G.accentLight, color: G.accent, fontSize: 11,
              }}
            >
              <ArrowRight size={10} />
              <span style={{ fontSize: 10, fontWeight: 700 }}>{predecessors.length + successors.length}</span>
            </div>
          );
        })()}

        {/* Ikona komentarzy — zawsze widoczna gdy zakres ma komentarze */}
        {commentCount > 0 && (
          <div
            style={{
              display: "flex", alignItems: "center", gap: 2,
              background: G.accentLight,
              borderRadius: 4,
              height: 24, padding: "0 6px",
              color: G.accent,
              fontSize: 10, flexShrink: 0,
              pointerEvents: "none",
            }}
          >
            <span style={{ fontSize: 10 }}>💬</span>
            <span style={{ fontWeight: 700 }}>{commentCount}</span>
          </div>
        )}

        {/* Avatary */}
        {assignees.length > 0 && (
          <div style={{ display: "flex", alignItems: "center", flexShrink: 0 }}>
            {assignees.slice(0, 3).map((a, i) => (
              <div
                key={a.userId}
                title={a.userName}
                style={{
                  width: 22, height: 22, borderRadius: "50%",
                  background: G.accentLight, color: G.accent,
                  fontSize: 9, fontWeight: 700,
                  display: "flex", alignItems: "center", justifyContent: "center",
                  border: `1.5px solid ${G.surface}`,
                  marginLeft: i > 0 ? -6 : 0,
                  zIndex: assignees.length - i,
                  position: "relative",
                }}
              >
                {a.userName?.[0]?.toUpperCase() ?? "?"}
              </div>
            ))}
          </div>
        )}

        {/* Menu kontekstowe zakresu (⋯) — zastępuje osobne guziki akcji */}
        <IconButton
          size="xs"
          variant="ghost"
          colorScheme="gray"
          aria-label="Akcje"
          title="Akcje"
          icon={<MoreHorizontal size={14} />}
          onClick={e => {
            e.stopPropagation();
            const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
            setWorkMenuFor({ stageId: stage.id, work, anchor: rect });
          }}
        />
        </div>{/* koniec nagłówka wiersza pracy */}

        {/* Lista okresów */}
        {!periodsCollapsed && periods.map(period => (
          <div
            key={period.id}
            style={{
              height: G.PERIOD_ROW_H,
              display: "flex",
              alignItems: "center",
              gap: 8,
              padding: `0 10px 0 ${32 + depth * G.DEPTH_INDENT}px`,
              background: period.isClosed ? G.greenLight : "transparent",
              borderTop: `1px solid ${G.border}`,
            }}
          >
            {/* Checkbox okresu */}
            <div
              onClick={() => togglePeriodClosed(period.id, !period.isClosed)}
              style={{
                width: 16, height: 16, borderRadius: 3, flexShrink: 0,
                border: `2px solid ${period.isClosed ? G.green : G.borderStrong}`,
                background: period.isClosed ? G.green : "#fff",
                display: "flex", alignItems: "center", justifyContent: "center",
                cursor: "pointer",
              }}
            >
              {period.isClosed && <span style={{ color: "#fff", fontSize: 11, fontWeight: 700, lineHeight: 1 }}>✓</span>}
            </div>
            <span style={{
              fontSize: 12,
              fontFamily: "monospace",
              color: period.isClosed ? G.green : G.text2,
              textDecoration: period.isClosed ? "line-through" : "none",
              whiteSpace: "nowrap",
              flex: 1,
            }}>
              {fmtCompactDate(period.startDate)} – {fmtCompactDate(period.endDate)}
            </span>
            {isEditing && (
              <IconButton
                size="xs"
                variant="ghost"
                colorScheme="gray"
                aria-label="Usuń okres"
                title="Usuń okres"
                icon={<Trash2 size={13} />}
                onClick={() => {
                  const remaining = periods
                    .filter(p => p.id !== period.id)
                    .map(p => ({ startDate: p.startDate.slice(0, 10), endDate: p.endDate.slice(0, 10), isClosed: p.isClosed }));
                  setPeriods(stage.id, work.id, remaining);
                }}
              />
            )}
          </div>
        ))}
      </div>
    );
  };

  const renderAddWorkRow = (row: FlatRow) => {
    const { stage, depth } = row;
    const isOpen = addingWorkFor === stage.id;

    return (
      <div
        key={row.id}
        style={{
          height: row.height,
          display: "flex",
          alignItems: "center",
          padding: `0 10px 0 ${10 + depth * G.DEPTH_INDENT}px`,
          background: G.surface2,
          borderBottom: `1px solid ${G.border}`,
        }}
      >
        {isOpen ? (
          <form
            onSubmit={e => { e.preventDefault(); handleAddWork(stage.id); }}
            style={{ display: "flex", alignItems: "center", gap: 4, flex: 1, minWidth: 0 }}
          >
            <div style={{ display: "flex", gap: 2, flexShrink: 0 }}>
              {WORK_COLORS.slice(0, 5).map(c => (
                <div
                  key={c}
                  onClick={() => setNewWorkColor(c)}
                  style={{
                    width: 10, height: 10, borderRadius: "50%", background: c,
                    cursor: "pointer", flexShrink: 0,
                    outline: newWorkColor === c ? `2px solid ${G.text}` : "none",
                    outlineOffset: 1,
                  }}
                />
              ))}
            </div>
            <input
              autoFocus
              value={newWorkName}
              onChange={e => setNewWorkName(e.target.value)}
              onKeyDown={e => e.key === "Escape" && setAddingWorkFor(null)}
              placeholder="Nazwa zakresu..."
              style={{
                flex: 1, minWidth: 0, fontSize: 12,
                border: `1px solid ${G.border}`, borderRadius: 4,
                padding: "2px 6px", outline: "none",
                background: G.surface, color: G.text,
              }}
            />
            <Button
              size="xs"
              variant="solid"
              colorScheme="primary"
              type="submit"
              flexShrink={0}
            >
              Dodaj
            </Button>
            <IconButton
              size="xs"
              variant="ghost"
              colorScheme="gray"
              aria-label="Anuluj"
              icon={<X size={14} />}
              onClick={() => setAddingWorkFor(null)}
              type="button"
              flexShrink={0}
            />
          </form>
        ) : (
          <Button
            size="xs"
            variant="ghost"
            colorScheme="gray"
            leftIcon={<Plus size={14} />}
            onClick={() => setAddingWorkFor(stage.id)}
          >
            zakres
          </Button>
        )}
      </div>
    );
  };

  return (
    <>

      {/* Nagłówek panelu — dwupoziomowy, dopasowany do nagłówka prawej siatki */}
      <div
        style={{
          height: G.HEADER_H,
          flexShrink: 0,
          borderBottom: `1px solid ${G.borderStrong}`,
          background: G.surface,
          overflow: "hidden",
        }}
      >
        {/* Górna warstwa — wysokość HEADER_WEEKS, odpowiada wierszowi miesięcy/tygodni */}
        <div
          style={{
            height: G.HEADER_WEEKS,
            display: "flex",
            alignItems: "flex-end",
            padding: "0 12px 8px",
            borderBottom: `1px solid ${G.border}`,
          }}
        >
          <span style={{ flex: 1, fontSize: 11, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".06em", color: G.text2 }}>
            Etap / Zakres robót
          </span>
        </div>
        {/* Dolna warstwa — wysokość HEADER_DAYS, odpowiada wierszowi dni */}
        <div
          style={{
            height: G.HEADER_DAYS,
            display: "flex",
            alignItems: "center",
            padding: "0 12px",
            justifyContent: "flex-end",
          }}
        >
          <span style={{ fontSize: 10, fontWeight: 600, textTransform: "uppercase", letterSpacing: ".06em", color: G.text3 }}>
            Postęp
          </span>
        </div>
      </div>

      {/* Wiersze — przewijane przez GanttLayout */}
      <div ref={leftBodyRef} style={{ flex: 1, overflowY: "hidden", overflowX: "hidden" }}>
        {(() => {
          const insertLineStyle: React.CSSProperties = {
            height: 2, background: G.accent, margin: "0 4px", borderRadius: 1, pointerEvents: "none",
          };
          const nodes: React.ReactNode[] = [];
          flatRows.forEach((row, rowIndex) => {
            if (rowDragInsert === rowIndex) {
              nodes.push(<div key={`insert-${rowIndex}`} style={insertLineStyle} />);
            }
            switch (row.kind) {
              case "stageHeader": nodes.push(renderStageHeader(row)); break;
              case "stageDetail": nodes.push(renderStageDetail(row)); break;
              case "work":        nodes.push(renderWorkRow(row));     break;
              case "addWork":     nodes.push(renderAddWorkRow(row));  break;
            }
          });
          if (rowDragInsert === flatRows.length) {
            nodes.push(<div key="insert-last" style={insertLineStyle} />);
          }
          return nodes;
        })()}

        {/* Przycisk dodawania nowego etapu — wewnątrz leftBodyRef (w obszarze scrollowanym),
            żeby wysokość treści lewego panelu była identyczna z prawą siatką. */}
        {isEditing && (
          <div
            onClick={() => addStage("Nowy etap")}
            style={{
              height: G.STAGE_ROW_H,
              flexShrink: 0,
              display: "flex",
              alignItems: "center",
              gap: 6,
              padding: "0 12px",
              cursor: "pointer",
              color: G.text3,
              borderTop: `1px solid ${G.border}`,
              userSelect: "none",
            }}
            onMouseEnter={e => { (e.currentTarget as HTMLDivElement).style.background = G.surface2; (e.currentTarget as HTMLDivElement).style.color = G.accent; }}
            onMouseLeave={e => { (e.currentTarget as HTMLDivElement).style.background = "transparent"; (e.currentTarget as HTMLDivElement).style.color = G.text3; }}
          >
            <Plus size={14} />
            <span style={{ fontSize: 12 }}>Dodaj etap</span>
          </div>
        )}
        {/* Spacer wyrównujący scrollTop_max lewego panelu z prawym.
            Prawy panel ma poziomy scrollbar (~17px) który pomniejsza clientHeight,
            dzięki czemu scrollTop_max prawego > scrollTop_max lewego o scrollbarH px. */}
        {scrollbarH > 0 && <div style={{ height: scrollbarH, flexShrink: 0 }} />}
      </div>

      {/* Popover przypisanych */}
      {assigneesFor && (
        <GanttAssigneesPopover
          assigneeIds={assigneesFor.work.assignees.map(a => a.userId)}
          members={members}
          onClose={() => setAssigneesFor(null)}
          onSave={async userIds => {
            await setAssignments(assigneesFor.stageId, assigneesFor.work.id, userIds);
          }}
        />
      )}

      {/* Color picker popover (BUG 6) */}
      {colorPickerFor && createPortal(
        <div
          ref={colorPickerRef}
          style={{
            position: "fixed",
            top: colorPickerFor.anchor.bottom + 4,
            left: Math.max(8, colorPickerFor.anchor.left - 60),
            zIndex: 200,
            background: G.surface,
            border: `1px solid ${G.border}`,
            borderRadius: 10,
            padding: 10,
            boxShadow: "0 4px 12px rgba(0,0,0,.12)",
            display: "grid",
            gridTemplateColumns: "repeat(6, 1fr)",
            gap: 6,
          }}
        >
          {WORK_COLOR_PALETTE.map(color => (
            <div
              key={color}
              onClick={async () => {
                await setWorkColor(colorPickerFor.stageId, colorPickerFor.workId, color);
                setColorPickerFor(null);
              }}
              style={{
                width: 24, height: 24, borderRadius: "50%",
                background: color, cursor: "pointer",
                outline: colorPickerFor.currentColor === color ? `2px solid ${G.accent}` : "none",
                outlineOffset: 2,
              }}
            />
          ))}
        </div>,
        document.body,
      )}

      {/* Modal komentarzy */}
      {commentsFor && createPortal(
        <>
          {/* Backdrop */}
          <div
            onClick={() => setCommentsFor(null)}
            style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.4)", zIndex: 199 }}
          />
          {/* Modal */}
          <div
            ref={commentsModalRef}
            style={{
              position: "fixed",
              top: "50%",
              left: "50%",
              transform: "translate(-50%, -50%)",
              width: 580,
              maxWidth: "calc(100vw - 32px)",
              maxHeight: "85vh",
              zIndex: 200,
              background: G.surface,
              border: `1px solid ${G.borderStrong}`,
              borderRadius: 12,
              boxShadow: "0 8px 40px rgba(0,0,0,.28)",
              display: "flex",
              flexDirection: "column",
              overflow: "hidden",
            }}
          >
            <div style={{
              display: "flex", alignItems: "center", justifyContent: "space-between",
              padding: "14px 18px 12px", borderBottom: `1px solid ${G.border}`, flexShrink: 0,
            }}>
              <span style={{ fontSize: 15, fontWeight: 700, color: G.text }}>
                💬 {commentsFor.work.name}
              </span>
              <IconButton
                size="sm"
                variant="ghost"
                colorScheme="gray"
                aria-label="Zamknij"
                icon={<X size={16} />}
                onClick={() => setCommentsFor(null)}
              />
            </div>
            <div style={{ padding: "14px 18px", flex: 1, overflowY: "auto" }}>
              <GanttCommentPopover
                workId={commentsFor.work.id}
                stageId={commentsFor.stageId}
                isReadOnly={!ganttPermissions.canAddComments}
              />
            </div>
          </div>
        </>,
        document.body,
      )}

      {/* Popover zależności */}
      {depsFor && (
        <GanttDepsPopover
          work={depsFor.work}
          stageId={depsFor.stageId}
          onClose={() => setDepsFor(null)}
        />
      )}

      {/* Menu kontekstowe zakresu pracy (⋯) */}
      {workMenuFor && createPortal(
        <div
          ref={workMenuRef}
          style={{
            position: "fixed",
            top: (() => {
              const spaceBelow = window.innerHeight - workMenuFor.anchor.bottom - 8;
              const spaceAbove = workMenuFor.anchor.top - 8;
              const MENU_H = 170;
              return spaceBelow < MENU_H && spaceAbove > spaceBelow
                ? Math.max(8, workMenuFor.anchor.top - MENU_H - 4)
                : Math.min(workMenuFor.anchor.bottom + 4, window.innerHeight - MENU_H - 8);
            })(),
            left: Math.min(workMenuFor.anchor.left, window.innerWidth - 188),
            zIndex: 250,
            background: G.surface,
            border: `1px solid ${G.borderStrong}`,
            borderRadius: 8,
            boxShadow: "0 4px 16px rgba(0,0,0,.15)",
            minWidth: 180,
            overflow: "hidden",
            fontSize: 12,
          }}
          onClick={() => setWorkMenuFor(null)}
        >
          {/* Komentarze */}
          <Button
            size="sm"
            variant="ghost"
            colorScheme="gray"
            w="full"
            justifyContent="flex-start"
            onClick={() => {
              const anchor = workMenuFor.anchor;
              setCommentsFor({ stageId: workMenuFor.stageId, work: workMenuFor.work, anchor });
            }}
            leftIcon={<MessageCircle size={14} />}
          >
            Komentarze
            {(workMenuFor.work.comments?.length ?? 0) > 0 && (
              <span style={{ marginLeft: "auto", fontSize: 10, background: G.accentLight, color: G.accent, borderRadius: 10, padding: "0 6px" }}>
                {workMenuFor.work.comments.length}
              </span>
            )}
          </Button>

          {/* Przypisz osoby */}
          <Button
            size="sm"
            variant="ghost"
            colorScheme="gray"
            w="full"
            justifyContent="flex-start"
            leftIcon={<Users size={14} />}
            onClick={() => {
              const anchor = workMenuFor.anchor;
              setAssigneesFor({ stageId: workMenuFor.stageId, work: workMenuFor.work, anchor });
            }}
          >
            Przypisz osoby
          </Button>

          {/* Zależności */}
          <Button
            size="sm"
            variant="ghost"
            colorScheme="gray"
            w="full"
            justifyContent="flex-start"
            leftIcon={<Link2 size={14} />}
            onClick={() => {
              const anchor = workMenuFor.anchor;
              setDepsFor({ stageId: workMenuFor.stageId, work: workMenuFor.work, anchor });
            }}
          >
            Zależności
          </Button>

          {/* Usuń — tylko tryb edycji */}
          {isEditing && (
            <>
              <div style={{ height: 1, background: G.border, margin: "2px 0" }} />
              <Button
                size="sm"
                variant="ghost"
                colorScheme="red"
                w="full"
                justifyContent="flex-start"
                leftIcon={<Trash2 size={12} />}
                onClick={() => deleteWork(workMenuFor.stageId, workMenuFor.work.id)}
              >
                Usuń zakres
              </Button>
            </>
          )}
        </div>,
        document.body,
      )}
    </>
  );
}
