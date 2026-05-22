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
import CostTrackerStatusBadge from "./CostTrackerStatusBadge";
import CostCountBadge from "./CostCountBadge";
import PositionsTable from "./PositionsTable";
import { TrackedCostItemStatus } from "../../types/costTracker.types";
import type { CostEstimateSummaryWeb, TrackerGroupWeb } from "../../types/costTracker.types";

interface StageAccordionProps {
  groups: TrackerGroupWeb[];
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  onCostMutated: () => void;
  depth?: number;
  estimates?: CostEstimateSummaryWeb[];
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
  estimates,
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
            borderColor="neutral.200"
            borderRadius="md"
            mb={2}
            overflow="hidden"
            borderLeft={groupLeftBorder(group)}
          >
            <AccordionButton
              px={depth > 0 ? 3 : 4}
              py={2}
              minH="44px"
              bg="neutral.25"
              _hover={{ bg: "neutral.50" }}
              _expanded={{ bg: "neutral.50" }}
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
                  bg="neutral.100"
                />
                <Text fontSize="xs" color="neutral.500" flexShrink={0}>
                  {group.itemsWithCostsCount}/{group.totalItemsCount}
                </Text>
                <CostCountBadge count={group.costCount} />
                <CostTrackerStatusBadge status={group.status} size="xs" />
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
                    estimates={estimates}
                  />
                )}

                {/* Podgrupy — rekurencja z indentacją */}
                {group.childGroups.length > 0 && (
                  <Box
                    pl={6}
                    borderLeft="2px solid"
                    borderLeftColor="neutral.200"
                  >
                    <StageAccordion
                      groups={group.childGroups}
                      tenantId={tenantId}
                      projectId={projectId}
                      costEstimateId={costEstimateId}
                      onCostMutated={onCostMutated}
                      depth={depth + 1}
                      estimates={estimates}
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
