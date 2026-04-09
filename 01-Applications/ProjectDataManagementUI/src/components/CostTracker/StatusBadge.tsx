import { Badge } from "@chakra-ui/react";
import { TrackedCostItemStatus } from "../../types/costTracker.types";

const STATUS_CONFIG: Record<
  TrackedCostItemStatus,
  { label: string; colorScheme: string }
> = {
  [TrackedCostItemStatus.NoCosts]:    { label: "Brak kosztów",   colorScheme: "gray"   },
  [TrackedCostItemStatus.NoBudget]:   { label: "Brak budżetu",   colorScheme: "purple" },
  [TrackedCostItemStatus.InProgress]: { label: "W realizacji",   colorScheme: "green"  },
  [TrackedCostItemStatus.NearLimit]:  { label: "Blisko limitu",  colorScheme: "orange" },
  [TrackedCostItemStatus.OverBudget]: { label: "Przekroczono",   colorScheme: "red"    },
};

interface StatusBadgeProps {
  status: TrackedCostItemStatus;
  size?: string;
}

export default function StatusBadge({ status, size }: StatusBadgeProps) {
  const config = STATUS_CONFIG[status] ?? { label: String(status), colorScheme: "gray" };
  return (
    <Badge colorScheme={config.colorScheme} fontSize={size} borderRadius="md" px={2}>
      {config.label}
    </Badge>
  );
}
