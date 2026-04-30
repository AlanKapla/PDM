import {
  Box,
  VStack,
  HStack,
  Text,
  Badge,
  Divider,
  useBreakpointValue,
  Progress,
  Tooltip,
} from "@chakra-ui/react";
import { ChevronDown, ChevronUp, ExternalLink } from "lucide-react";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import CostCountBadge from "./CostCountBadge";
import StageAccordion from "./StageAccordion";
import EstimateAdditionalCosts from "./EstimateAdditionalCosts";
import type { CostEstimateSummaryWeb } from "../../types/costTracker.types";

const formatCurrency = (value: number | null): string => {
  if (value === null || value === undefined) return "—";
  return new Intl.NumberFormat("pl-PL", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
};

interface EstimateCardProps {
  estimate: CostEstimateSummaryWeb;
  tenantId: string;
  projectId: string;
  onCostMutated: () => void;
}

export default function EstimateCard({
  estimate,
  tenantId,
  projectId,
  onCostMutated,
}: EstimateCardProps) {
  const [expanded, setExpanded] = useState(false);
  const isMobile = useBreakpointValue({ base: true, md: false });
  const navigate = useNavigate();

  const leftBorderColor = estimate.isBudgetExceeded
    ? "red.400"
    : estimate.itemsNearLimitCount > 0
    ? "orange.400"
    : "green.400";

  const deviationColor = estimate.isBudgetExceeded ? "red.500" : "green.600";
  const coveredPct = estimate.coveredPercent ?? 0;
  const progressColor = estimate.isBudgetExceeded ? "red" : coveredPct >= 80 ? "orange" : "green";

  return (
    <Box
      borderLeft="4px solid"
      borderLeftColor={leftBorderColor}
      bg="white"
      borderRadius="lg"
      shadow="sm"
      borderWidth="1px"
      borderColor="neutral.100"
      mb={4}
      overflow="hidden"
      width="100%"
    >
      {/* Nagłówek karty */}
      <Box
        px={{ base: 3, md: 5 }}
        pt={{ base: 3, md: 4 }}
        pb={3}
        cursor="pointer"
        onClick={() => setExpanded((v) => !v)}
        _hover={{ bg: "neutral.25" }}
        transition="background 0.15s"
      >
        {/* Rząd 1: nazwa + badge + ikona */}
        <HStack justify="space-between" mb={1}>
          <HStack spacing={1} flex={1} minW={0} align="center">
            <Text fontWeight="bold" fontSize={{ base: "sm", md: "md" }} noOfLines={2} color="neutral.800">
              {estimate.costEstimateName}
            </Text>
            <Tooltip label="Otwórz kosztorys" hasArrow>
              <Box
                as="span"
                color="neutral.400"
                _hover={{ color: "blue.500" }}
                cursor="pointer"
                flexShrink={0}
                onClick={(e: React.MouseEvent) => {
                  e.stopPropagation();
                  navigate(`/projects/${projectId}/cost-estimates/${estimate.costEstimateId}`);
                }}
              >
                <ExternalLink size={13} />
              </Box>
            </Tooltip>
          </HStack>
          <HStack spacing={2} flexShrink={0}>
            <CostCountBadge count={estimate.costCount} />
            <Box color="neutral.400">
              {expanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            </Box>
          </HStack>
        </HStack>

        {/* Rząd 2: metryki kompaktowe */}
        <HStack
          spacing={isMobile ? 2 : 4}
          flexWrap="wrap"
          fontSize="xs"
          color="neutral.500"
          mb={2}
        >
          <Text>{estimate.itemsWithCostsCount}/{estimate.totalItemsCount} poz. z kosztami</Text>
          {!isMobile && (
            <>
              <Text color="neutral.300">·</Text>
              <Text>Budżet: <Text as="span" fontWeight="semibold" color="neutral.700">{formatCurrency(estimate.totalBudgetNet)}</Text> PLN</Text>
              <Text color="neutral.300">·</Text>
              <Text>Koszty: <Text as="span" fontWeight="semibold" color="neutral.700">{formatCurrency(estimate.totalCostsNet)}</Text> PLN</Text>
              <Text color="neutral.300">·</Text>
              <Text>
                Odchylenie:{" "}
                <Text as="span" fontWeight="semibold" color={deviationColor}>
                  {formatCurrency(estimate.totalDeviationNet)}
                </Text>
                {" "}PLN
              </Text>
            </>
          )}
          {estimate.itemsOverBudgetCount > 0 && (
            <Badge colorScheme="red" fontSize="xs" borderRadius="full" px={2}>{estimate.itemsOverBudgetCount} przekroczone</Badge>
          )}
          {estimate.itemsNearLimitCount > 0 && (
            <Badge colorScheme="orange" fontSize="xs" borderRadius="full" px={2}>{estimate.itemsNearLimitCount} blisko limitu</Badge>
          )}
        </HStack>

        {/* Na mobile: kwoty */}
        {isMobile && (
          <HStack spacing={4} fontSize="xs" color="neutral.500" mb={2} flexWrap="wrap">
            <Text>Budżet: <Text as="span" fontWeight="semibold" color="neutral.700">{formatCurrency(estimate.totalBudgetNet)}</Text> PLN</Text>
            <Text>Koszty: <Text as="span" fontWeight="semibold" color={deviationColor}>{formatCurrency(estimate.totalCostsNet)}</Text> PLN</Text>
          </HStack>
        )}

        {/* Rząd 3: pasek postępu */}
        <HStack spacing={2} align="center">
          <Progress
            value={Math.min(coveredPct, 100)}
            flex={1}
            size="sm"
            colorScheme={progressColor}
            borderRadius="full"
            bg="neutral.50"
          />
          <Text fontSize="xs" fontWeight="semibold" color="neutral.600" flexShrink={0} minW="36px" textAlign="right">
            {coveredPct.toFixed(1)}%
          </Text>
        </HStack>
      </Box>

      {/* Treść rozwijana */}
      {expanded && (
        <>
          <Divider />
          <Box px={{ base: 3, md: 5 }} py={{ base: 3, md: 4 }}>
            <VStack align="stretch" spacing={4}>
              {estimate.groups.length > 0 && (
                <StageAccordion
                  groups={estimate.groups}
                  tenantId={tenantId}
                  projectId={projectId}
                  costEstimateId={estimate.costEstimateId}
                  onCostMutated={onCostMutated}
                />
              )}
              <EstimateAdditionalCosts
                additionalCosts={estimate.additionalCosts}
                tenantId={tenantId}
                projectId={projectId}
                costEstimateId={estimate.costEstimateId}
                onCostMutated={onCostMutated}
              />
            </VStack>
          </Box>
        </>
      )}
    </Box>
  );
}
