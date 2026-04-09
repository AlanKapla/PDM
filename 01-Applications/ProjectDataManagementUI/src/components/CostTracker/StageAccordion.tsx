import {
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionPanel,
  AccordionIcon,
  HStack,
  Text,
  VStack,
  Box,
  Progress,
  useBreakpointValue,
} from "@chakra-ui/react";
import StatusBadge from "./StatusBadge";
import CostCountBadge from "./CostCountBadge";
import PositionsTable from "./PositionsTable";
import { TrackedCostItemStatus } from "../../types/costTracker.types";
import type { TrackerGroupWeb } from "../../types/costTracker.types";

interface StageAccordionProps {
  groups: TrackerGroupWeb[];
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  onCostMutated: () => void;
  depth?: number;
}

function groupLeftBorder(group: TrackerGroupWeb): string {
  if (group.isBudgetExceeded) return "3px solid var(--chakra-colors-red-400)";
  if (group.status === TrackedCostItemStatus.NearLimit) return "3px solid var(--chakra-colors-orange-400)";
  return "3px solid transparent";
}

export default function StageAccordion({
  groups,
  tenantId,
  projectId,
  costEstimateId,
  onCostMutated,
  depth = 0,
}: StageAccordionProps) {
  const isMobile = useBreakpointValue({ base: true, md: false });

  if (groups.length === 0) return null;

  const defaultIndex = isMobile ? [] : [0];

  return (
    <Accordion allowMultiple defaultIndex={defaultIndex as number[]}>
      {groups.map((group) => {
        const coveredPct = group.coveredPercent ?? 0;
        const progressColor = group.isBudgetExceeded
          ? "red"
          : group.status === TrackedCostItemStatus.NearLimit
          ? "orange"
          : "green";

        return (
          <AccordionItem
            key={group.groupId}
            borderWidth="1px"
            borderColor="gray.200"
            borderRadius="md"
            mb={2}
            overflow="hidden"
            borderLeft={groupLeftBorder(group)}
          >
            <AccordionButton
              px={depth > 0 ? 3 : 4}
              py={2}
              minH="44px"
              bg="gray.50"
              _hover={{ bg: "gray.100" }}
              _expanded={{ bg: "gray.100" }}
            >
              <HStack flex={1} spacing={2} textAlign="left" overflow="hidden">
                <Text
                  fontWeight={depth === 0 ? "semibold" : "medium"}
                  fontSize={depth === 0 ? "sm" : "xs"}
                  flexShrink={0}
                  maxW={{ base: "120px", md: "200px" }}
                  noOfLines={1}
                >
                  {group.groupName}
                </Text>
                <Progress
                  value={Math.min(coveredPct, 100)}
                  size="xs"
                  colorScheme={progressColor}
                  flex={1}
                  borderRadius="full"
                  bg="gray.200"
                />
                <Text fontSize="xs" color="gray.500" flexShrink={0}>
                  {group.itemsWithCostsCount}/{group.totalItemsCount}
                </Text>
                <CostCountBadge count={group.costCount} />
                <StatusBadge status={group.status} size="xs" />
              </HStack>
              <AccordionIcon ml={2} flexShrink={0} />
            </AccordionButton>

            <AccordionPanel pb={3} px={depth > 0 ? 2 : 4} bg="white">
              <VStack align="stretch" spacing={3}>
                {group.items.length > 0 && (
                  <PositionsTable
                    items={group.items}
                    tenantId={tenantId}
                    projectId={projectId}
                    costEstimateId={costEstimateId}
                    onCostMutated={onCostMutated}
                  />
                )}

                {/* Podgrupy — rekurencja z indentacją */}
                {group.childGroups.length > 0 && (
                  <Box
                    pl={6}
                    borderLeft="2px solid"
                    borderLeftColor="gray.200"
                  >
                    <StageAccordion
                      groups={group.childGroups}
                      tenantId={tenantId}
                      projectId={projectId}
                      costEstimateId={costEstimateId}
                      onCostMutated={onCostMutated}
                      depth={depth + 1}
                    />
                  </Box>
                )}
              </VStack>
            </AccordionPanel>
          </AccordionItem>
        );
      })}
    </Accordion>
  );
}
