import { useState, useEffect, useRef, useMemo } from "react";
import { createPortal } from "react-dom";
import { Avatar } from "@chakra-ui/react";
import { G } from "./ganttTokens";
import type { GanttContractor, GanttMember } from "./GanttContext";
import { AssignmentConflictAlertDialog } from "./AssignmentConflictAlertDialog";
import { AssigneeConflictWarningIcon } from "./AssigneeConflictWarningIcon";
import {
  diffNewAssignees,
  useAssignmentConflictCheck,
} from "../../hooks/useAssignmentConflictCheck";
import { detectAssigneeConflicts } from "../../utils/detectAssigneeConflicts";
import type { WorkScheduleAssignmentConflictWeb, WorkScheduleStageWorkPeriodWeb } from "../../types/workSchedule.types";

interface GanttAssigneesPopoverProps {
  stageId: string;
  workId: string;
  workPeriods: Array<Pick<WorkScheduleStageWorkPeriodWeb, "startDate" | "endDate" | "isClosed">>;
  selectedUserIds: string[];
  selectedContractorIds: string[];
  members: GanttMember[];
  contractors: GanttContractor[];
  onClose: () => void;
  onSave: (userIds: string[], contractorIds: string[]) => void | Promise<void>;
}

/** Modal do przypisywania osób i kontahentów — wyśrodkowany na ekranie */
export default function GanttAssigneesPopover({
  stageId: _stageId,
  workId,
  workPeriods,
  selectedUserIds,
  selectedContractorIds,
  members,
  contractors,
  onClose,
  onSave,
}: GanttAssigneesPopoverProps) {
  const [selectedUsers, setSelectedUsers] = useState<string[]>(selectedUserIds);
  const [selectedContractors, setSelectedContractors] = useState<string[]>(selectedContractorIds);
  const [isSaving, setIsSaving] = useState(false);
  const [isConflictOpen, setIsConflictOpen] = useState(false);
  const [pendingUserIds, setPendingUserIds] = useState<string[]>([]);
  const [pendingContractorIds, setPendingContractorIds] = useState<string[]>([]);
  const ref = useRef<HTMLDivElement>(null);

  const { conflicts, checkConflicts, clearConflicts } = useAssignmentConflictCheck();

  const conflictsByUserId = useMemo(() => {
    const map = new Map<string, WorkScheduleAssignmentConflictWeb[]>();
    for (const m of members) {
      const name = [m.firstName, m.lastName].filter(Boolean).join(" ") || m.email;
      const found = detectAssigneeConflicts({
        workId,
        workPeriods,
        candidates: [{
          userId: m.userId,
          assigneeName: m.companyName?.trim() ? `${name} (${m.companyName.trim()})` : name,
          assignments: m.assignments ?? [],
        }],
      });
      if (found.length > 0) {
        map.set(m.userId, found);
      }
    }
    return map;
  }, [members, workId, workPeriods]);

  const conflictsByContractorId = useMemo(() => {
    const map = new Map<string, WorkScheduleAssignmentConflictWeb[]>();
    for (const c of contractors) {
      const found = detectAssigneeConflicts({
        workId,
        workPeriods,
        candidates: [{
          contractorId: c.id,
          assigneeName: c.name,
          assignments: c.assignments ?? [],
        }],
      });
      if (found.length > 0) {
        map.set(c.id, found);
      }
    }
    return map;
  }, [contractors, workId, workPeriods]);

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

  const persist = async (userIds: string[], contractorIds: string[]) => {
    setIsSaving(true);
    try {
      await onSave(userIds, contractorIds);
      onClose();
    } finally {
      setIsSaving(false);
    }
  };

  const handleSave = async () => {
    const { newUserIds, newContractorIds } = diffNewAssignees(
      selectedUsers,
      selectedContractors,
      selectedUserIds,
      selectedContractorIds
    );

    const memberById = new Map(members.map(m => [m.userId, m]));
    const contractorById = new Map(contractors.map(c => [c.id, c]));
    const candidates = [
      ...newUserIds.map(id => {
        const m = memberById.get(id);
        return {
          userId: id,
          assigneeName: m ? getMemberName(m) : id,
          assignments: m?.assignments ?? [],
        };
      }),
      ...newContractorIds.map(id => {
        const c = contractorById.get(id);
        return {
          contractorId: id,
          assigneeName: c?.name ?? id,
          assignments: c?.assignments ?? [],
        };
      }),
    ];

    const found = checkConflicts(candidates, workId, workPeriods);
    if (found.length > 0) {
      setPendingUserIds(selectedUsers);
      setPendingContractorIds(selectedContractors);
      setIsConflictOpen(true);
      return;
    }
    await persist(selectedUsers, selectedContractors);
  };

  const btnBase: React.CSSProperties = {
    padding: "5px 12px",
    fontSize: 12,
    borderRadius: 6,
    cursor: "pointer",
  };

  const renderRow = (
    key: string,
    label: string,
    avatar: React.ReactElement,
    isSelected: boolean,
    onToggle: () => void,
    rowConflicts: WorkScheduleAssignmentConflictWeb[] = [],
  ) => (
    <div
      key={key}
      onClick={onToggle}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onToggle();
        }
      }}
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
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          flexShrink: 0,
        }}
      >
        {isSelected && (
          <span style={{ color: "#fff", fontSize: 12, lineHeight: 1 }}>✓</span>
        )}
      </div>
      {avatar}
      <span style={{ fontSize: 13, color: G.text, flex: 1 }}>{label}</span>
      <AssigneeConflictWarningIcon conflicts={rowConflicts} />
    </div>
  );

  const sectionHeader = (title: string) => (
    <div
      style={{
        padding: "12px 18px 6px",
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
          aria-label="Zamknij"
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
            <Avatar
              name={[m.firstName, m.lastName].filter(Boolean).join(" ") || m.email}
              size="sm"
              flexShrink={0}
            />,
            selectedUsers.includes(m.userId),
            () => toggleUser(m.userId),
            conflictsByUserId.get(m.userId) ?? [],
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
            <Avatar name={c.name} size="sm" flexShrink={0} />,
            selectedContractors.includes(c.id),
            () => toggleContractor(c.id),
            conflictsByContractorId.get(c.id) ?? [],
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
          onClick={() => { void handleSave(); }}
          disabled={isSaving}
          style={{
            ...btnBase,
            padding: "8px 18px",
            fontSize: 13,
            border: "none",
            background: G.accent,
            color: "#fff",
            opacity: isSaving ? 0.7 : 1,
          }}
        >
          Zapisz
        </button>
      </div>
      </div>

      <AssignmentConflictAlertDialog
        isOpen={isConflictOpen}
        onClose={() => {
          setIsConflictOpen(false);
          clearConflicts();
        }}
        onConfirm={() => {
          setIsConflictOpen(false);
          clearConflicts();
          void persist(pendingUserIds, pendingContractorIds);
        }}
        conflicts={conflicts}
        isLoading={isSaving}
      />
    </>,
    document.body,
  );
}
