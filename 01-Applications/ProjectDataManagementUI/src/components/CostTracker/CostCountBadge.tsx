import { Badge, Tooltip } from "@chakra-ui/react";

interface CostCountBadgeProps {
  count: number;
}

export default function CostCountBadge({ count }: CostCountBadgeProps) {
  return (
    <Tooltip label={`${count} koszt${count === 1 ? "" : count < 5 ? "y" : "ów"}`}>
      <Badge colorScheme="blue" borderRadius="full" px={2} fontSize="xs">
        {count}
      </Badge>
    </Tooltip>
  );
}
