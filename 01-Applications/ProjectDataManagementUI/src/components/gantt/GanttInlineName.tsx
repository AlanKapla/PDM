import React, { useState } from "react";
import { G } from "./ganttTokens";
import { GanttTruncatedName } from "./GanttTruncatedName";

export interface GanttInlineNameProps {
  value: string;
  /** Czy Gantt jest w trybie edycji — tylko wtedy aktywność */
  isEditing: boolean;
  fontWeight?: string | number;
  fontSize?: string;
  color?: string;
  textDecoration?: string;
  /** Gdy true — nazwa zawija się w całości zamiast być ucinana wielokropkiem */
  wrap?: boolean;
  onCommit: (newName: string) => Promise<void>;
}

/**
 * Inline-edytowalny tekst.
 * Dwuklik → input; blur/Enter → commit; Escape → anuluj.
 */
export default function GanttInlineName({
  value,
  isEditing,
  fontWeight = 400,
  fontSize = "sm",
  color = G.text,
  textDecoration = "none",
  wrap = false,
  onCommit,
}: GanttInlineNameProps): React.ReactElement {
  const [editing, setEditing] = useState(false);
  const [input, setInput] = useState(value);

  const startEdit = () => {
    if (!isEditing) return;
    setInput(value);
    setEditing(true);
  };

  const commit = async () => {
    setEditing(false);
    const trimmed = input.trim();
    if (trimmed && trimmed !== value) await onCommit(trimmed);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") (e.target as HTMLInputElement).blur();
    if (e.key === "Escape") { setEditing(false); setInput(value); }
  };

  if (editing) {
    return (
      <input
        autoFocus
        value={input}
        onChange={e => setInput(e.target.value)}
        onBlur={commit}
        onKeyDown={handleKeyDown}
        style={{
          flex: 1,
          minWidth: 0,
          width: "100%",
          fontSize,
          fontWeight: String(fontWeight),
          background: G.accentLight,
          borderTop: "none",
          borderLeft: "none",
          borderRight: "none",
          borderBottom: `2px solid ${G.accent}`,
          borderRadius: 3,
          outline: "none",
          padding: "1px 4px",
          color: G.text,
        }}
      />
    );
  }

  return (
    <GanttTruncatedName
      label={value}
      fontWeight={fontWeight}
      fontSize={fontSize}
      color={color}
      textDecoration={textDecoration}
      cursor={isEditing ? "text" : "default"}
      onDoubleClick={startEdit}
      isEditingMode={isEditing}
      editHint="Dwuklik aby zmienić nazwę"
      wrap={wrap}
    />
  );
}
