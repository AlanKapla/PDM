import { useState, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import { G } from "./ganttTokens";
import type { GanttContractor, GanttMember } from "./GanttContext";

interface GanttAssigneesPopoverProps {
  selectedUserIds: string[];
  selectedContractorIds: string[];
  members: GanttMember[];
  contractors: GanttContractor[];
  onClose: () => void;
  onSave: (userIds: string[], contractorIds: string[]) => void;
}

/** Modal do przypisywania osób i kontahentów — wyśrodkowany na ekranie */
export default function GanttAssigneesPopover({
  selectedUserIds,
  selectedContractorIds,
  members,
  contractors,
  onClose,
  onSave,
}: GanttAssigneesPopoverProps) {
  const [selectedUsers, setSelectedUsers] = useState<string[]>(selectedUserIds);
  const [selectedContractors, setSelectedContractors] = useState<string[]>(selectedContractorIds);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [onClose]);

  const toggleUser = (userId: string) =>
    setSelectedUsers(prev =>
      prev.includes(userId) ? prev.filter(id => id !== userId) : [...prev, userId],
    );

  const toggleContractor = (contractorId: string) =>
    setSelectedContractors(prev =>
      prev.includes(contractorId)
        ? prev.filter(id => id !== contractorId)
        : [...prev, contractorId],
    );

  const getMemberName = (m: GanttMember) => {
    const name = [m.firstName, m.lastName].filter(Boolean).join(" ") || m.email;
    return m.companyName?.trim() ? `${name} (${m.companyName.trim()})` : name;
  };

  const getMemberInitial = (m: GanttMember) =>
    (m.firstName?.[0] ?? m.email?.[0] ?? "?").toUpperCase();

  const btnBase: React.CSSProperties = {
    padding: "5px 12px",
    fontSize: 12,
    borderRadius: 6,
    cursor: "pointer",
  };

  const renderRow = (
    key: string,
    label: string,
    initial: string,
    isSelected: boolean,
    onToggle: () => void,
  ) => (
    <div
      key={key}
      onClick={onToggle}
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
      <div
        style={{
          width: 18,
          height: 18,
          borderRadius: 4,
          border: `2px solid ${isSelected ? G.accent : G.borderStrong}`,
          background: isSelected ? G.accent : "transparent",
          flexShrink: 0,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        {isSelected && (
          <span style={{ color: "#fff", fontSize: 11, fontWeight: 700, lineHeight: 1 }}>✓</span>
        )}
      </div>
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
        {initial}
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
        {label}
      </span>
    </div>
  );

  const sectionHeader = (title: string) => (
    <div
      style={{
        padding: "10px 18px 6px",
        fontSize: 11,
        fontWeight: 700,
        letterSpacing: "0.04em",
        textTransform: "uppercase",
        color: G.text3,
      }}
    >
      {title}
    </div>
  );

  return createPortal(
    <>
      <div
        onClick={onClose}
        style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.4)", zIndex: 199 }}
      />
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

      <div style={{ flex: 1, overflowY: "auto" }}>
        {sectionHeader("Zespół projektu")}
        {members.map(m =>
          renderRow(
            m.userId,
            getMemberName(m),
            getMemberInitial(m),
            selectedUsers.includes(m.userId),
            () => toggleUser(m.userId),
          )
        )}
        {members.length === 0 && (
          <div style={{ padding: "4px 18px 12px", color: G.text3, fontSize: 12 }}>
            Brak członków projektu
          </div>
        )}

        {sectionHeader("Kontahenci")}
        {contractors.map(c =>
          renderRow(
            c.id,
            c.name,
            (c.name?.[0] ?? "?").toUpperCase(),
            selectedContractors.includes(c.id),
            () => toggleContractor(c.id),
          )
        )}
        {contractors.length === 0 && (
          <div style={{ padding: "4px 18px 12px", color: G.text3, fontSize: 12 }}>
            Brak kontahentów
          </div>
        )}
      </div>

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
          onClick={() => { onSave(selectedUsers, selectedContractors); onClose(); }}
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
