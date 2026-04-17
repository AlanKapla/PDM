import { useState, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import { Trash2, X, ChevronDown, ChevronRight, HelpCircle } from "lucide-react";
import { useGantt } from "./GanttContext";
import { G } from "./ganttTokens";
import { WorkDependencyType, WorkDependencyTypeLabels } from "../../types/workSchedule.types";
import type { WorkScheduleStageWorkWeb, WorkScheduleWorkDependencyWeb, WorkScheduleStageWeb } from "../../types/workSchedule.types";

interface GanttDepsPopoverProps {
  work: WorkScheduleStageWorkWeb;
  stageId: string;
  onClose: () => void;
}

function collectAllWorks(stages: WorkScheduleStageWeb[]): WorkScheduleStageWorkWeb[] {
  const works: WorkScheduleStageWorkWeb[] = [];
  for (const s of stages) {
    works.push(...(s.works ?? []));
    works.push(...collectAllWorks(s.childStages ?? []));
  }
  return works;
}

export default function GanttDepsPopover({ work, onClose }: GanttDepsPopoverProps) {
  const { schedule, setDependencies, mode } = useGantt();
  const isEditing = mode === "edit";

  const allDeps = schedule?.dependencies ?? [];
  const allWorks = collectAllWorks(schedule?.stages ?? []).filter(w => w.id !== work.id);

  // Lokalna kopia wszystkich zależności harmonogramu do stagowania przed zapisem
  const [staged, setStaged] = useState<WorkScheduleWorkDependencyWeb[]>(() => [...allDeps]);
  const [isSaving, setIsSaving] = useState(false);

  // Pola formularza dodawania
  const [newWorkId, setNewWorkId] = useState("");
  const [newDepType, setNewDepType] = useState<WorkDependencyType>(WorkDependencyType.FinishToStart);
  const [newRole, setNewRole] = useState<"predecessor" | "successor">("predecessor");
  const [newLagDays, setNewLagDays] = useState(0);

  const ref = useRef<HTMLDivElement>(null);
  const [helpOpen, setHelpOpen] = useState(false);

  const hasChanges = JSON.stringify(
    staged.map(d => ({ id: d.id, predecessorWorkId: d.predecessorWorkId, successorWorkId: d.successorWorkId, dependencyType: d.dependencyType, lagDays: d.lagDays })).sort((a, b) => a.id.localeCompare(b.id))
  ) !== JSON.stringify(
    allDeps.map(d => ({ id: d.id, predecessorWorkId: d.predecessorWorkId, successorWorkId: d.successorWorkId, dependencyType: d.dependencyType, lagDays: d.lagDays })).sort((a, b) => a.id.localeCompare(b.id))
  );

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [onClose]);

  const predecessors = staged.filter(d => d.successorWorkId === work.id);
  const successors   = staged.filter(d => d.predecessorWorkId === work.id);

  const workName = (id: string) => allWorks.find(w => w.id === id)?.name ?? id;

  const stageDep = () => {
    if (!newWorkId) return;
    const predecessorWorkId = newRole === "predecessor" ? newWorkId : work.id;
    const successorWorkId   = newRole === "successor"   ? newWorkId : work.id;
    const isDuplicate = staged.some(d =>
      d.predecessorWorkId === predecessorWorkId &&
      d.successorWorkId === successorWorkId &&
      d.dependencyType === newDepType,
    );
    if (isDuplicate) return;
    const newDep: WorkScheduleWorkDependencyWeb = {
      id: crypto.randomUUID(),
      predecessorWorkId,
      successorWorkId,
      dependencyType: newDepType,
      lagDays: newLagDays,
    };
    setStaged(prev => [...prev, newDep]);
    setNewWorkId("");
    setNewLagDays(0);
  };

  const removeStagedDep = (id: string) => {
    setStaged(prev => prev.filter(d => d.id !== id));
  };

  const save = async () => {
    setIsSaving(true);
    try {
      await setDependencies(staged);
      onClose();
    } finally {
      setIsSaving(false);
    }
  };

  const select: React.CSSProperties = {
    fontSize: 12,
    border: `1px solid ${G.border}`,
    borderRadius: 5,
    padding: "4px 6px",
    background: G.surface,
    color: G.text,
  };

  const sectionTitle: React.CSSProperties = {
    fontSize: 10,
    fontWeight: 700,
    color: G.text3,
    textTransform: "uppercase" as const,
    letterSpacing: ".06em",
    marginBottom: 6,
  };

  const depRow = (d: WorkScheduleWorkDependencyWeb, color: string, nameKey: "predecessorWorkId" | "successorWorkId") => (
    <div key={d.id} style={{
      display: "flex", alignItems: "center", gap: 8,
      padding: "7px 10px", borderRadius: 5, background: G.surface2,
      marginBottom: 4,
    }}>
      <div style={{ width: 8, height: 8, borderRadius: "50%", background: color, flexShrink: 0 }} />

      {/* Zakres pracy — edytowalny w trybie edit */}
      {isEditing ? (
        <select
          value={d[nameKey]}
          onChange={e => {
            const newId = e.target.value;
            setStaged(prev => prev.map(s =>
              s.id === d.id ? { ...s, [nameKey]: newId } : s
            ));
          }}
          style={{ ...select, flex: 1, minWidth: 0 }}
        >
          {allWorks.map(w => (
            <option key={w.id} value={w.id}>{w.name}</option>
          ))}
        </select>
      ) : (
        <span style={{ flex: 1, fontSize: 12, color: G.text, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
          {workName(d[nameKey])}
        </span>
      )}

      {/* Typ zależności — edytowalny w trybie edit */}
      {isEditing ? (
        <select
          value={d.dependencyType}
          onChange={e => {
            const val = Number(e.target.value) as WorkDependencyType;
            setStaged(prev => prev.map(s =>
              s.id === d.id ? { ...s, dependencyType: val } : s
            ));
          }}
          style={{ ...select, flexShrink: 0 }}
        >
          {Object.entries(WorkDependencyTypeLabels).map(([val, label]) => (
            <option key={val} value={val}>{label}</option>
          ))}
        </select>
      ) : (
        <span style={{
          fontSize: 10, color: G.accent, background: G.accentLight,
          padding: "1px 5px", borderRadius: 10, flexShrink: 0, fontWeight: 600,
        }}>
          {Object.keys(WorkDependencyType).find(k => (WorkDependencyType as unknown as Record<string, number>)[k] === d.dependencyType) ?? "?"}
        </span>
      )}

      {/* Lag days */}
      <div style={{ display: "flex", alignItems: "center", gap: 3, flexShrink: 0 }}>
        {isEditing ? (
          <input
            type="number"
            value={d.lagDays}
            onChange={e => {
              const val = parseInt(e.target.value, 10);
              setStaged(prev => prev.map(s => s.id === d.id ? { ...s, lagDays: isNaN(val) ? 0 : val } : s));
            }}
            style={{
              width: 52, fontSize: 11, textAlign: "right",
              border: `1px solid ${G.border}`, borderRadius: 4,
              padding: "2px 4px", background: G.surface, color: G.text,
            }}
            title="Przesunięcie (dni). Wartość ujemna = lead"
          />
        ) : (
          <span style={{ fontSize: 10, color: d.lagDays !== 0 ? G.amber : G.text3, fontWeight: d.lagDays !== 0 ? 600 : 400, minWidth: 24, textAlign: "right" }}>
            {d.lagDays > 0 ? `+${d.lagDays}` : d.lagDays}
          </span>
        )}
        <span style={{ fontSize: 10, color: G.text3 }}>dni</span>
      </div>
      {isEditing && (
        <button
          onClick={() => removeStagedDep(d.id)}
          title="Usuń"
          style={{ background: "none", border: "none", cursor: "pointer", color: G.text3, padding: 2, display: "flex", alignItems: "center", flexShrink: 0 }}
        >
          <X size={12} />
        </button>
      )}
    </div>
  );

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
          width: 700,
          maxWidth: "calc(100vw - 32px)",
          maxHeight: "85vh",
          background: G.surface,
          border: `1px solid ${G.borderStrong}`,
          borderRadius: 12,
          boxShadow: "0 8px 40px rgba(0,0,0,.28)",
          zIndex: 200,
          display: "flex",
          flexDirection: "column",
          fontSize: 13,
          overflow: "hidden",
        }}
      >
      {/* Nagłówek popovera */}
      <div style={{
        display: "flex", alignItems: "center", justifyContent: "space-between",
        padding: "12px 14px 10px",
        borderBottom: `1px solid ${G.border}`,
      }}>
        <div>
          <div style={{ fontWeight: 700, fontSize: 14, color: G.text }}>Zależności</div>
          <div style={{ fontSize: 11, color: G.text3, marginTop: 1 }}>{work.name}</div>
        </div>
        <button
          onClick={onClose}
          style={{ background: "none", border: "none", cursor: "pointer", color: G.text3, padding: 4, display: "flex", alignItems: "center" }}
        >
          <X size={16} />
        </button>
      </div>

      {/* Ciało — przewijalne */}
      <div style={{ flex: 1, overflowY: "auto", padding: "12px 14px" }}>
        {/* Akordion — pomoc o typach zależności */}
        <div style={{ marginBottom: 14, borderRadius: 7, border: `1px solid ${G.border}`, overflow: "hidden" }}>
          <button
            onClick={() => setHelpOpen(o => !o)}
            style={{
              width: "100%", display: "flex", alignItems: "center", gap: 7,
              padding: "8px 12px", background: G.surface2,
              border: "none", cursor: "pointer", textAlign: "left",
            }}
          >
            <HelpCircle size={13} color={G.accent} />
            <span style={{ flex: 1, fontSize: 12, fontWeight: 600, color: G.text }}>Co oznaczają typy zależności?</span>
            {helpOpen ? <ChevronDown size={13} color={G.text3} /> : <ChevronRight size={13} color={G.text3} />}
          </button>
          {helpOpen && (
            <div style={{ padding: "10px 14px 12px", background: G.surface, borderTop: `1px solid ${G.border}` }}>
              {([
                {
                  label: "FS – Koniec → Start",
                  color: G.accent,
                  desc: "Następnik może się zacząć dopiero po zakończeniu poprzednika. Najczęstszy typ.",
                  diagram: "A [===]\n         B      [===]",
                },
                {
                  label: "SS – Start → Start",
                  color: "#38a169",
                  desc: "Następnik może się zacząć dopiero gdy poprzednik się zacznie. Prace równoległe z początkiem.",
                  diagram: "A [======]\nB [===]",
                },
                {
                  label: "FF – Koniec → Koniec",
                  color: "#d97706",
                  desc: "Następnik może się zakończyć dopiero gdy poprzednik się zakończy. Prace kończą się razem.",
                  diagram: "A [======]\n     B [===]",
                },
                {
                  label: "SF – Start → Koniec",
                  color: "#7c3aed",
                  desc: "Następnik może się zakończyć dopiero gdy poprzednik się zacznie. Rzadki typ — np. przełączenie duty.",
                  diagram: "      A [===]\nB [===]",
                },
              ] as const).map(({ label, color, desc, diagram }) => (
                <div key={label} style={{ marginBottom: 10, display: "flex", gap: 12, alignItems: "flex-start" }}>
                  <div style={{ width: 10, height: 10, borderRadius: "50%", background: color, flexShrink: 0, marginTop: 3 }} />
                  <div>
                    <div style={{ fontSize: 12, fontWeight: 700, color: G.text, marginBottom: 2 }}>{label}</div>
                    <div style={{ fontSize: 11, color: G.text2, marginBottom: 4 }}>{desc}</div>
                    <pre style={{
                      fontSize: 10, fontFamily: "monospace", color: G.text3,
                      background: G.surface2, borderRadius: 4, padding: "4px 8px",
                      margin: 0, whiteSpace: "pre",
                    }}>{diagram}</pre>
                  </div>
                </div>
              ))}
              <div style={{ marginTop: 4, padding: "6px 10px", background: G.accentLight, borderRadius: 5, fontSize: 11, color: G.accent }}>
                <strong>Przesunięcie (lag):</strong> wartość dodatnia = opóźnienie, ujemna = nakładanie się (lead).
              </div>
            </div>
          )}
        </div>
        {/* Poprzednicy */}
        <div style={sectionTitle}>← Poprzednicy <span style={{ fontWeight: 400, textTransform: "none" }}>(ten zakres zaczyna się po nich)</span></div>
        {predecessors.length === 0 ? (
          <div style={{ fontSize: 11, color: G.text3, marginBottom: 10, fontStyle: "italic" }}>Brak poprzedników</div>
        ) : (
          <div style={{ marginBottom: 10 }}>
            {predecessors.map(d => depRow(d, G.accent, "predecessorWorkId"))}
          </div>
        )}

        {/* Następniki */}
        <div style={{ ...sectionTitle, marginTop: 4 }}>→ Następniki <span style={{ fontWeight: 400, textTransform: "none" }}>(te zakresy zaczynają się po tym)</span></div>
        {successors.length === 0 ? (
          <div style={{ fontSize: 11, color: G.text3, marginBottom: 10, fontStyle: "italic" }}>Brak następników</div>
        ) : (
          <div style={{ marginBottom: 10 }}>
            {successors.map(d => depRow(d, "#38a169", "successorWorkId"))}
          </div>
        )}

        {/* Formularz dodawania — tylko w trybie edycji */}
        {isEditing && allWorks.length > 0 && (
          <>
            <div style={{ borderTop: `1px solid ${G.border}`, margin: "8px 0 12px" }} />
            <div style={{ ...sectionTitle, marginBottom: 10 }}>Dodaj nową zależność</div>

            {/* Wiersz 1: rola i typ */}
            <div style={{ display: "flex", gap: 8, marginBottom: 8, alignItems: "center" }}>
              <div style={{ display: "flex", gap: 0, borderRadius: 5, overflow: "hidden", border: `1px solid ${G.border}`, flexShrink: 0 }}>
                {(["predecessor", "successor"] as const).map(role => (
                  <button
                    key={role}
                    onClick={() => setNewRole(role)}
                    style={{
                      padding: "5px 10px", fontSize: 11, border: "none", cursor: "pointer",
                      background: newRole === role ? G.accent : G.surface2,
                      color: newRole === role ? "#fff" : G.text2,
                      fontWeight: newRole === role ? 600 : 400,
                    }}
                  >
                    {role === "predecessor" ? "← Poprzednik" : "Następnik →"}
                  </button>
                ))}
              </div>
              <select
                value={newDepType}
                onChange={e => setNewDepType(Number(e.target.value) as WorkDependencyType)}
                style={{ ...select, flex: 1 }}
              >
                {Object.entries(WorkDependencyTypeLabels).map(([val, label]) => (
                  <option key={val} value={val}>{label}</option>
                ))}
              </select>
              {/* Lag days dla nowej zależności */}
              <div style={{ display: "flex", alignItems: "center", gap: 4, flexShrink: 0 }}>
                <input
                  type="number"
                  value={newLagDays}
                  onChange={e => setNewLagDays(parseInt(e.target.value, 10) || 0)}
                  style={{ ...select, width: 52, textAlign: "right" }}
                  title="Przesunięcie (dni). Ujemna wartość = lead"
                />
                <span style={{ fontSize: 11, color: G.text3, whiteSpace: "nowrap" }}>dni</span>
              </div>
            </div>

            {/* Wiersz 2: wybór zakresu + przycisk dodaj */}
            <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <select
                value={newWorkId}
                onChange={e => setNewWorkId(e.target.value)}
                style={{ ...select, flex: 1 }}
              >
                <option value="">— wybierz zakres pracy —</option>
                {allWorks.map(w => (
                  <option key={w.id} value={w.id}>{w.name}</option>
                ))}
              </select>
              <button
                onClick={stageDep}
                disabled={!newWorkId}
                style={{
                  padding: "5px 14px", fontSize: 12, fontWeight: 600,
                  background: newWorkId ? G.accent : G.surface2,
                  color: newWorkId ? "#fff" : G.text3,
                  border: "none", borderRadius: 5, cursor: newWorkId ? "pointer" : "not-allowed",
                  flexShrink: 0,
                  transition: "background .15s",
                }}
              >
                + Dodaj
              </button>
            </div>
          </>
        )}
      </div>

      {/* Stopka z przyciskiem zapisu */}
      {isEditing && (
        <div style={{
          padding: "10px 14px",
          borderTop: `1px solid ${G.border}`,
          display: "flex", alignItems: "center", justifyContent: "flex-end", gap: 8,
        }}>
          {hasChanges && (
            <span style={{ flex: 1, fontSize: 11, color: G.amber }}>
              Niezapisane zmiany
            </span>
          )}
          <button
            onClick={onClose}
            style={{
              padding: "6px 14px", fontSize: 12,
              background: "none", border: `1px solid ${G.border}`,
              borderRadius: 5, cursor: "pointer", color: G.text2,
            }}
          >
            Anuluj
          </button>
          <button
            onClick={save}
            disabled={isSaving}
            style={{
              padding: "6px 16px", fontSize: 12, fontWeight: 600,
              background: G.accent, color: "#fff",
              border: "none", borderRadius: 5,
              cursor: isSaving ? "wait" : "pointer",
              opacity: isSaving ? 0.7 : 1,
            }}
          >
            {isSaving ? "Zapisuję…" : "Zapisz"}
          </button>
        </div>
      )}
    </div>
    </>,
    document.body,
  );
}
