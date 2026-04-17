import { useState, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import { Trash2, AlertTriangle, Info } from "lucide-react";
import { G } from "./ganttTokens";
import { usePeriodsValidation, type PeriodsValidationResult } from "./usePeriodsValidation";
import { WorkDependencyTypeLabels } from "../../types/workSchedule.types";
import type { WorkScheduleStageWorkWeb } from "../../types/workSchedule.types";
import { fmtShortDate, toLocalDateStr } from "./ganttRowUtils";

interface PeriodLocal {
  id: string;
  startDate: string;
  endDate: string;
  isClosed: boolean;
}

export interface PeriodsSaveResult {
  /** Pełna lista okresów po zmianach — używana przy PUT (usunięcia) */
  finalList: { startDate: string; endDate: string; isClosed: boolean }[];
  /** Tylko zmieniony isClosed na istniejących okresach — używany przy PATCH */
  closedToggles: { id: string; isClosed: boolean }[];
  /** true gdy usunięto przynajmniej jeden okres — wtedy potrzebny PUT */
  hasStructuralChange: boolean;
}

interface GanttPeriodsPopoverProps {
  work: WorkScheduleStageWorkWeb;
  onClose: () => void;
  onSave: (result: PeriodsSaveResult) => Promise<void>;
}

/** Modal do zarządzania okresami — wyśrodkowany na ekranie */
export default function GanttPeriodsPopover({
  work,
  onClose,
  onSave,
}: GanttPeriodsPopoverProps) {
  const [periods, setPeriods] = useState<PeriodLocal[]>(
    (work.periods ?? []).map(p => ({ ...p })),
  );
  const [validation, setValidation] = useState<PeriodsValidationResult | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  const { validate } = usePeriodsValidation();

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [onClose]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setValidation(validate(work.id, periods));
    }, 300);
    return () => clearTimeout(timer);
  }, [periods, validate, work.id]);

  const togglePeriod = (id: string) =>
    setPeriods(prev => prev.map(p => p.id === id ? { ...p, isClosed: !p.isClosed } : p));

  const removePeriod = (id: string) =>
    setPeriods(prev => prev.filter(p => p.id !== id));

  const selectAll = () => setPeriods(prev => prev.map(p => ({ ...p, isClosed: true })));
  const deselectAll = () => setPeriods(prev => prev.map(p => ({ ...p, isClosed: false })));

  const allClosed = periods.length > 0 && periods.every(p => p.isClosed);

  return createPortal(
    <>
      {/* Backdrop */}
      <div
        onClick={onClose}
        style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.4)", zIndex: 199 }}
      />
      {/* Modal */}
      <div
        ref={ref}
        style={{
          position: "fixed",
          top: "50%",
          left: "50%",
          transform: "translate(-50%, -50%)",
          width: 420,
          maxWidth: "calc(100vw - 32px)",
          maxHeight: "85vh",
          background: G.surface,
          border: `1px solid ${G.borderStrong}`,
          borderRadius: 12,
          boxShadow: "0 8px 40px rgba(0,0,0,.28)",
          zIndex: 200,
          display: "flex",
          flexDirection: "column",
          overflow: "hidden",
        }}
      >
      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "10px 12px 8px",
          borderBottom: `1px solid ${G.border}`,
        }}
      >
        <span style={{ fontSize: 12, fontWeight: 600, color: G.text }}>
          Okresy — {work.name}
        </span>
        <button
          onClick={onClose}
          style={{ background: "none", border: "none", cursor: "pointer", color: G.text3, fontSize: 16, lineHeight: 1, padding: 0 }}
        >
          ×
        </button>
      </div>

      {/* Subtitle */}
      <div
        style={{
          padding: "8px 12px 4px",
          fontSize: 10,
          fontWeight: 700,
          textTransform: "uppercase",
          letterSpacing: ".06em",
          color: G.text3,
        }}
      >
        Zaznacz ukończone okresy
      </div>

      {/* Lista okresów */}
      <div style={{ padding: "0 12px", maxHeight: 200, overflowY: "auto" }}>
        {periods.map(p => (
          <div
            key={p.id}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              padding: "5px 0",
              borderBottom: `1px solid ${G.border}`,
            }}
          >
            <div style={{ width: 8, height: 8, borderRadius: "50%", background: work.colorRgb, flexShrink: 0 }} />
            <span style={{ flex: 1, fontSize: 11, fontFamily: "monospace", color: G.text2 }}>
              {fmtShortDate(p.startDate)} – {fmtShortDate(p.endDate)}
            </span>
            {/* Toggle switch */}
            <div
              onClick={() => togglePeriod(p.id)}
              title={p.isClosed ? "Odznacz jako nieukończony" : "Oznacz jako ukończony"}
              style={{
                position: "relative",
                width: 32,
                height: 18,
                borderRadius: 9,
                background: p.isClosed ? G.green : G.borderStrong,
                cursor: "pointer",
                flexShrink: 0,
                transition: "background .2s",
              }}
            >
              <div
                style={{
                  position: "absolute",
                  top: 2,
                  left: p.isClosed ? 16 : 2,
                  width: 14,
                  height: 14,
                  borderRadius: "50%",
                  background: "#fff",
                  transition: "left .2s",
                  boxShadow: "0 1px 3px rgba(0,0,0,.2)",
                }}
              />
            </div>
            {/* Usuń okres */}
            <button
              onClick={() => removePeriod(p.id)}
              title="Usuń okres"
              style={{ background: "none", border: "none", cursor: "pointer", color: G.text3, padding: 0, display: "flex", alignItems: "center", flexShrink: 0 }}
            >
              <Trash2 size={12} />
            </button>
          </div>
        ))}
        {periods.length === 0 && (
          <div style={{ padding: "12px 0", color: G.text3, fontSize: 12, textAlign: "center" }}>
            Brak zdefiniowanych okresów
          </div>
        )}
      </div>

      {/* Naruszenia zależności — blokujące */}
      {validation && validation.errors.length > 0 && (
        <div
          style={{
            margin: "0 12px 8px",
            padding: "8px 10px",
            background: "#fef2f2",
            border: "1px solid #fecaca",
            borderRadius: 6,
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 5, marginBottom: 4 }}>
            <AlertTriangle size={11} color="#dc2626" />
            <span style={{ fontSize: 10, fontWeight: 700, color: "#b91c1c", textTransform: "uppercase", letterSpacing: ".05em" }}>
              Naruszenie zależności
            </span>
          </div>
          {validation.errors.map((err, i) => (
            <div key={i} style={{ fontSize: 11, color: "#b91c1c", marginBottom: i < validation.errors.length - 1 ? 4 : 0 }}>
              <span style={{ fontWeight: 600 }}>{err.predecessorName}</span>
              {" "}({WorkDependencyTypeLabels[err.dependencyType]})
              <div style={{ color: "#dc2626", marginTop: 1 }}>
                {err.violatedField === "startDate" ? "Rozpoczęcie" : "Zakończenie"} musi być
                {" "}≥ <strong>{fmtShortDate(toLocalDateStr(err.requiredDate))}</strong>
                {err.lagDays !== 0 && (
                  <span style={{ opacity: .75 }}>
                    {" "}(przesunięcie: {err.lagDays > 0 ? "+" : ""}{err.lagDays} dni)
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Ostrzeżenia — następniki zostaną przesunięte */}
      {validation && validation.warnings.length > 0 && (
        <div
          style={{
            margin: "0 12px 8px",
            padding: "8px 10px",
            background: G.amberLight,
            border: `1px solid #fcd34d`,
            borderRadius: 6,
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 5, marginBottom: 4 }}>
            <Info size={11} color={G.amber} />
            <span style={{ fontSize: 10, fontWeight: 700, color: G.amber, textTransform: "uppercase", letterSpacing: ".05em" }}>
              Automatyczne przesunięcie
            </span>
          </div>
          {validation.warnings.map((w, i) => (
            <div key={i} style={{ fontSize: 11, color: G.amber }}>
              <span style={{ fontWeight: 600 }}>{w.successorName}</span>
              {" "}zostanie przesunięty o{" "}
              <strong>{w.willBeShiftedBy} dni</strong>
            </div>
          ))}
        </div>
      )}

      {/* Footer */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "8px 12px",
          borderTop: `1px solid ${G.border}`,
        }}
      >
        <button
          onClick={allClosed ? deselectAll : selectAll}
          style={{ background: "none", border: "none", fontSize: 11, color: G.accent, cursor: "pointer", padding: 0 }}
        >
          {allClosed ? "Odznacz wszystkie" : "Zaznacz wszystkie"}
        </button>
        <button
          onClick={async () => {
            const originalPeriods = work.periods ?? [];
            const originalIds = new Set(originalPeriods.map(p => p.id));
            const currentIds = new Set(periods.map(p => p.id));
            const hasStructuralChange = [...originalIds].some(id => !currentIds.has(id));

            const closedToggles = originalPeriods
              .filter(orig => currentIds.has(orig.id))
              .flatMap(orig => {
                const current = periods.find(p => p.id === orig.id);
                if (!current || current.isClosed === orig.isClosed) return [];
                return [{ id: orig.id, isClosed: current.isClosed }];
              });

            await onSave({
              finalList: periods.map(p => ({
                startDate: p.startDate.slice(0, 10),
                endDate: p.endDate.slice(0, 10),
                isClosed: p.isClosed,
              })),
              closedToggles,
              hasStructuralChange,
            });
            onClose();
          }}
          style={{
            padding: "5px 12px",
            fontSize: 12,
            borderRadius: 6,
            border: "none",
            background: validation && !validation.valid ? G.borderStrong : G.accent,
            color: "#fff",
            cursor: validation && !validation.valid ? "not-allowed" : "pointer",
            opacity: validation && !validation.valid ? 0.6 : 1,
          }}
          disabled={validation ? !validation.valid : false}
          title={validation && !validation.valid ? "Napraw naruszenia zależności, aby zapisać" : undefined}
        >
          Zapisz
        </button>
      </div>
      </div>
    </>,
    document.body,
  );
}
