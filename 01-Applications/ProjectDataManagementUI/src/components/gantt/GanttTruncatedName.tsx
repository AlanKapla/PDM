import React from "react";
import { Box, Tooltip } from "@chakra-ui/react";

export interface GanttTruncatedNameProps {
  label: string;
  fontWeight?: string | number;
  fontSize?: string;
  color?: string;
  textDecoration?: string;
  cursor?: string;
  onClick?: () => void;
  onDoubleClick?: () => void;
  /** Gdy podane — tooltip pokazuje hint zamiast pełnej nazwy (np. tryb edycji) */
  editHint?: string;
  isEditingMode?: boolean;
  /** Gdy true — nazwa zawija się w całości zamiast być ucinana wielokropkiem */
  wrap?: boolean;
}

/**
 * Ucięta nazwa z tooltipem Chakra UI pokazującym pełną treść.
 */
export function GanttTruncatedName({
  label,
  fontWeight = 400,
  fontSize = "sm",
  color,
  textDecoration = "none",
  cursor = "default",
  onClick,
  onDoubleClick,
  editHint,
  isEditingMode = false,
  wrap = false,
}: GanttTruncatedNameProps): React.ReactElement {
  const displayText = label.trim() || "Bez nazwy";
  const isPlaceholder = !label.trim();
  const tooltipLabel = isEditingMode && editHint ? editHint : displayText;

  return (
    <Tooltip
      label={tooltipLabel}
      hasArrow
      placement="top"
      openDelay={400}
      isDisabled={(isPlaceholder && !isEditingMode) || (wrap && !isEditingMode)}
    >
      <Box
        as="span"
        display="block"
        flex={1}
        minW={0}
        overflow={wrap ? "visible" : "hidden"}
        textOverflow={wrap ? "clip" : "ellipsis"}
        whiteSpace={wrap ? "normal" : "nowrap"}
        overflowWrap={wrap ? "anywhere" : undefined}
        fontSize={fontSize}
        fontWeight={fontWeight}
        color={isPlaceholder ? "gray.400" : color}
        fontStyle={isPlaceholder ? "italic" : "normal"}
        textDecoration={textDecoration}
        cursor={cursor}
        onClick={onClick}
        onDoubleClick={onDoubleClick}
        aria-label={displayText}
      >
        {displayText}
      </Box>
    </Tooltip>
  );
}
