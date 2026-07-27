import React from "react";
import { Box, Tooltip } from "@chakra-ui/react";
import { AlertTriangle } from "lucide-react";
import type { WorkScheduleAssignmentConflictWeb } from "../../types/workSchedule.types";
import { formatAssigneeConflictTooltip } from "../../utils/detectAssigneeConflicts";

export interface AssigneeConflictWarningIconProps {
  conflicts: WorkScheduleAssignmentConflictWeb[];
}

export function AssigneeConflictWarningIcon({
  conflicts,
}: AssigneeConflictWarningIconProps): React.ReactElement | null {
  if (conflicts.length === 0) {
    return null;
  }

  const label = formatAssigneeConflictTooltip(conflicts);

  return (
    <Tooltip
      label={label}
      whiteSpace="pre-line"
      placement="top"
      hasArrow
      openDelay={200}
      maxW="320px"
    >
      <Box
        as="span"
        display="inline-flex"
        alignItems="center"
        color="orange.500"
        flexShrink={0}
        aria-label={label}
        onClick={(e: React.MouseEvent) => e.stopPropagation()}
        onKeyDown={(e: React.KeyboardEvent) => e.stopPropagation()}
      >
        <AlertTriangle size={16} aria-hidden="true" />
      </Box>
    </Tooltip>
  );
}
