import { useState, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import { G } from "./ganttTokens";
import type { GanttMember } from "./GanttContext";

interface GanttAssigneesPopoverProps {
  assigneeIds: string[];
  members: GanttMember[];
  onClose: () => void;
  onSave: (userIds: string[]) => void;
}

/** Modal do przypisywania osób — wyśrodkowany na ekranie */
export default function GanttAssigneesPopover({
  assigneeIds,
  members,
  onClose,
  onSave,
}: GanttAssigneesPopoverProps) {
  const [selected, setSelected] = useState<string[]>(assigneeIds);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [onClose]);

  const toggle = (userId: string) =>
    setSelected(prev =>
      prev.includes(userId) ? prev.filter(id => id !== userId) : [...prev, userId],
    );

  const getName = (m: GanttMember) =>
    [m.firstName, m.lastName].filter(Boolean).join(" ") || m.email;

  const getInitial = (m: GanttMember) =>
    (m.firstName?.[0] ?? m.email?.[0] ?? "?").toUpperCase();

  const btnBase: React.CSSProperties = {
    padding: "5px 12px",
    fontSize: 12,
    borderRadius: 6,
    cursor: "pointer",
  };

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
          width: 480,
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
          padding: "14px 18px 12px",
          borderBottom: `1px solid ${G.border}`,
        }}
      >
        <span style={{ fontSize: 15, fontWeight: 700, color: G.text }}>Przypisani do zakresu</span>
        <button
          onClick={onClose}
          style={{ background: "none", border: "none", cursor: "pointer", color: G.text3, fontSize: 20, lineHeight: 1, padding: 0 }}
        >
          ×
        </button>
      </div>

      {/* Lista */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {members.map(m => (
          <div
            key={m.userId}
            onClick={() => toggle(m.userId)}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              padding: "10px 18px",
              cursor: "pointer",
              background: "transparent",
              transition: "background .1s",
            }}
            onMouseEnter={e => (e.currentTarget.style.background = G.surface2)}
            onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
          >
            {/* Checkbox */}
            <div
              style={{
                width: 18,
                height: 18,
                borderRadius: 4,
                border: `2px solid ${selected.includes(m.userId) ? G.accent : G.borderStrong}`,
                background: selected.includes(m.userId) ? G.accent : "transparent",
                flexShrink: 0,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              {selected.includes(m.userId) && (
                <span style={{ color: "#fff", fontSize: 11, fontWeight: 700, lineHeight: 1 }}>✓</span>
              )}
            </div>
            {/* Avatar */}
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: "50%",
                background: G.accentLight,
                color: G.accent,
                fontSize: 14,
                fontWeight: 700,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                flexShrink: 0,
              }}
            >
              {getInitial(m)}
            </div>
            <span
              style={{
                fontSize: 14,
                color: G.text,
                flex: 1,
                minWidth: 0,
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
              }}
            >
              {getName(m)}
            </span>
          </div>
        ))}
        {members.length === 0 && (
          <div style={{ padding: 12, color: G.text3, fontSize: 12, textAlign: "center" }}>
            Brak członków projektu
          </div>
        )}
      </div>

      {/* Footer */}
      <div
        style={{
          display: "flex",
          gap: 10,
          padding: "12px 18px",
          borderTop: `1px solid ${G.border}`,
          justifyContent: "flex-end",
        }}
      >
        <button
          onClick={onClose}
          style={{ ...btnBase, padding: "8px 18px", fontSize: 13, border: `1px solid ${G.border}`, background: G.surface, color: G.text2 }}
        >
          Anuluj
        </button>
        <button
          onClick={() => { onSave(selected); onClose(); }}
          style={{ ...btnBase, padding: "8px 18px", fontSize: 13, border: "none", background: G.accent, color: "#fff" }}
        >
          Zapisz
        </button>
      </div>
      </div>
    </>,
    document.body,
  );
}
