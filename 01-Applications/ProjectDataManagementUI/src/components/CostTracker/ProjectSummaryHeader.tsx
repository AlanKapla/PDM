import {
  Box,
  VStack,
  HStack,
  Text,
  SimpleGrid,
  CircularProgress,
  CircularProgressLabel,
  useBreakpointValue,
  Progress,
  Divider,
} from "@chakra-ui/react";
import { TrendingDown, TrendingUp } from "lucide-react";
import type { CostTrackerSummaryWeb, CostEstimateSummaryWeb } from "../../types/costTracker.types";

interface ProjectSummaryHeaderProps {
  summary: CostTrackerSummaryWeb;
  estimates: CostEstimateSummaryWeb[];
}

const formatCurrency = (value: number | null, compact = false): string => {
  if (value === null || value === undefined) return "—";
  if (compact && Math.abs(value) >= 1_000_000)
    return `${(value / 1_000_000).toFixed(2)}M`;
  if (compact && Math.abs(value) >= 1_000)
    return `${(value / 1_000).toFixed(1)}k`;
  return new Intl.NumberFormat("pl-PL", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
};

const formatPercent = (value: number | null): string =>
  value === null ? "—" : `${value.toFixed(1)}%`;

const DEVIATION_COLOR = (isBudgetExceeded: boolean) =>
  isBudgetExceeded ? "red.500" : "green.600";

// ===== Donut SVG =====
function DonutChart({
  coveredPercent,
  isBudgetExceeded,
}: {
  coveredPercent: number | null;
  isBudgetExceeded: boolean;
}) {
  const pct = Math.min(Math.max(coveredPercent ?? 0, 0), 100);
  const r = 40;
  const circumference = 2 * Math.PI * r;
  const filled = (pct / 100) * circumference;
  const stroke = isBudgetExceeded ? "#E53E3E" : pct >= 80 ? "#DD6B20" : "#38A169";

  return (
    <svg width="100" height="100" viewBox="0 0 100 100">
      <circle cx="50" cy="50" r={r} fill="none" stroke="#EDF2F7" strokeWidth="14" />
      <circle
        cx="50"
        cy="50"
        r={r}
        fill="none"
        stroke={stroke}
        strokeWidth="14"
        strokeDasharray={`${filled} ${circumference - filled}`}
        strokeLinecap="round"
        transform="rotate(-90 50 50)"
      />
      <text x="50" y="54" textAnchor="middle" fontSize="14" fontWeight="bold" fill={stroke}>
        {pct.toFixed(0)}%
      </text>
    </svg>
  );
}

// ===== Słupki per kosztorys =====
function EstimateComparisonBars({ estimates }: { estimates: CostEstimateSummaryWeb[] }) {
  if (estimates.length === 0) return null;
  const maxVal = Math.max(
    ...estimates.map((e) => Math.max(e.totalBudgetNet ?? 0, e.totalCostsNet ?? 0))
  );
  return (
    <VStack align="stretch" spacing={2}>
      {estimates.map((est) => {
        const budgetPct = maxVal > 0 ? ((est.totalBudgetNet ?? 0) / maxVal) * 100 : 0;
        const costsPct = maxVal > 0 ? ((est.totalCostsNet ?? 0) / maxVal) * 100 : 0;
        return (
          <Box key={est.costEstimateId}>
            <Text fontSize="xs" noOfLines={1} mb={1} color="neutral.600" fontWeight="medium">
              {est.costEstimateName}
            </Text>
            <VStack spacing={1} align="stretch">
              <HStack spacing={2} align="center">
                <Text fontSize="xs" w="48px" textAlign="right" color="neutral.400">Budżet</Text>
                <Progress value={budgetPct} size="sm" colorScheme="primary" flex={1} borderRadius="full" bg="primary.100" />
              </HStack>
              <HStack spacing={2} align="center">
                <Text fontSize="xs" w="48px" textAlign="right" color="neutral.400">Koszty</Text>
                <Progress
                  value={costsPct}
                  size="sm"
                  colorScheme={est.isBudgetExceeded ? "red" : "blue"}
                  flex={1}
                  borderRadius="full"
                  bg={est.isBudgetExceeded ? "red.100" : "blue.100"}
                  sx={{ "& > div": { bg: est.isBudgetExceeded ? "blue.500" : "blue.600" } }}
                />
              </HStack>
            </VStack>
          </Box>
        );
      })}
    </VStack>
  );
}

// ===== KPI karta =====
function KpiCard({
  label,
  value,
  unit,
  accent,
  children,
}: {
  label: string;
  value?: string;
  unit?: string;
  accent?: string;
  children?: React.ReactNode;
}) {
  return (
    <Box
      bg="white"
      borderWidth="1px"
      borderColor="neutral.100"
      borderRadius="lg"
      shadow="sm"
      borderLeft={accent ? "4px solid" : undefined}
      borderLeftColor={accent}
      p={3}
      minH="80px"
      maxH="120px"
      overflow="hidden"
    >
      {children ?? (
        <VStack align="flex-start" spacing={0}>
          <Text
            fontSize="xs"
            color="neutral.500"
            fontWeight="medium"
            textTransform="uppercase"
            letterSpacing="wide"
            noOfLines={1}
          >
            {label}
          </Text>
          <Text fontSize={{ base: "md", md: "xl" }} fontWeight="bold" color="neutral.800" lineHeight="short">
            {value}
          </Text>
          {unit && <Text fontSize="xs" color="neutral.400">{unit}</Text>}
        </VStack>
      )}
    </Box>
  );
}

// ===== Główny komponent =====
export default function ProjectSummaryHeader({ summary, estimates }: ProjectSummaryHeaderProps) {
  const isMobile = useBreakpointValue({ base: true, md: false });
  const showCharts = useBreakpointValue({ base: false, md: true });
  const deviationColor = DEVIATION_COLOR(summary.isBudgetExceeded);
  const coveredPct = summary.coveredPercent ?? 0;

  return (
    <VStack align="stretch" spacing={4}>
      {/* KPI Row */}
      <SimpleGrid columns={{ base: 2, md: 4 }} spacing={3}>
        <KpiCard
          label="Łączny budżet"
          value={formatCurrency(summary.totalBudgetNet, !!isMobile)}
          unit="PLN netto"
        />

        <KpiCard
          label="Łączne koszty"
          value={formatCurrency(summary.totalCostsNet, !!isMobile)}
          unit="PLN netto"
        />

        <KpiCard
          label="Odchylenie"
          accent={deviationColor}
        >
          <VStack align="flex-start" spacing={0}>
            <Text
              fontSize="xs"
              color="neutral.500"
              fontWeight="medium"
              textTransform="uppercase"
              letterSpacing="wide"
            >
              Odchylenie
            </Text>
            <HStack spacing={1} align="center">
              {summary.isBudgetExceeded
                ? <TrendingDown size={16} color="var(--chakra-colors-red-500)" />
                : <TrendingUp size={16} color="var(--chakra-colors-green-600)" />
              }
              <Text
                fontSize={{ base: "md", md: "xl" }}
                fontWeight="bold"
                color={deviationColor}
                lineHeight="short"
              >
                {formatCurrency(summary.totalDeviationNet, !!isMobile)}
              </Text>
            </HStack>
            <Text fontSize="xs" color="neutral.400">
              {formatPercent(summary.totalDeviationPercent)}
            </Text>
          </VStack>
        </KpiCard>

        <KpiCard label="Realizacja">
          {showCharts ? (
            <HStack justify="space-between" align="center" h="100%">
              <VStack align="flex-start" spacing={0}>
                <Text
                  fontSize="xs"
                  color="neutral.500"
                  fontWeight="medium"
                  textTransform="uppercase"
                  letterSpacing="wide"
                >
                  Realizacja
                </Text>
                <Text fontSize={{ base: "md", md: "xl" }} fontWeight="bold" color="neutral.800">
                  {formatPercent(summary.coveredPercent)}
                </Text>
              </VStack>
              <CircularProgress
                value={Math.min(coveredPct, 100)}
                color={summary.isBudgetExceeded ? "red.400" : "green.400"}
                trackColor="neutral.100"
                size={{ base: "60px", md: "72px" } as any}
                thickness="10px"
              >
                <CircularProgressLabel fontSize="xs" fontWeight="bold">
                  {coveredPct.toFixed(0)}%
                </CircularProgressLabel>
              </CircularProgress>
            </HStack>
          ) : (
            <VStack align="flex-start" spacing={0}>
              <Text fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide">
                Realizacja
              </Text>
              <Text fontSize="xl" fontWeight="bold" color="neutral.800">
                {formatPercent(summary.coveredPercent)}
              </Text>
            </VStack>
          )}
        </KpiCard>
      </SimpleGrid>

      {/* Wykresy — od md */}
      {showCharts && (
        <SimpleGrid columns={{ base: 1, md: 2 }} spacing={3}>
          <Box bg="white" borderWidth="1px" borderColor="neutral.100" borderRadius="lg" shadow="sm" p={4}>
            <Text fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide" mb={3}>
              Realizacja budżetu
            </Text>
            <HStack spacing={5} align="center">
              <DonutChart coveredPercent={summary.coveredPercent} isBudgetExceeded={summary.isBudgetExceeded} />
              <VStack align="flex-start" spacing={2} fontSize="xs" color="neutral.600">
                <HStack>
                  <Box w="10px" h="10px" borderRadius="sm" bg="green.400" flexShrink={0} />
                  <Text>Koszty: {formatCurrency(summary.totalCostsNet)} PLN</Text>
                </HStack>
                <HStack>
                  <Box w="10px" h="10px" borderRadius="sm" bg="neutral.100" flexShrink={0} />
                  <Text>Pozostało: {formatCurrency((summary.totalBudgetNet ?? 0) - (summary.totalCostsNet ?? 0))} PLN</Text>
                </HStack>
              </VStack>
            </HStack>
          </Box>

          <Box bg="white" borderWidth="1px" borderColor="neutral.100" borderRadius="lg" shadow="sm" p={4}>
            <Text fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide" mb={3}>
              Budżet vs koszty per kosztorys
            </Text>
            <EstimateComparisonBars estimates={estimates} />
          </Box>
        </SimpleGrid>
      )}

      {/* Liczniki */}
      <SimpleGrid columns={{ base: 1, sm: 3 }} spacing={3}>
        <Box bg="white" borderWidth="1px" borderColor="neutral.100" borderRadius="lg" shadow="sm" p={3}>
          <VStack align="flex-start" spacing={0}>
            <Text fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide">
              Kosztorysy
            </Text>
            <HStack align="baseline" spacing={1}>
              <Text fontSize="2xl" fontWeight="bold" color="neutral.800">{summary.costEstimatesCount}</Text>
              <Text fontSize="xs" color="neutral.400">{summary.costEstimatesWithCostsCount} z kosztami</Text>
            </HStack>
          </VStack>
        </Box>
        <Box bg="white" borderWidth="1px" borderColor="neutral.100" borderRadius="lg" shadow="sm" p={3}>
          <VStack align="flex-start" spacing={0}>
            <Text fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide">
              Łączna liczba kosztów
            </Text>
            <Text fontSize="2xl" fontWeight="bold" color="neutral.800">{summary.costCount}</Text>
          </VStack>
        </Box>
        <Box bg="white" borderWidth="1px" borderColor="neutral.100" borderRadius="lg" shadow="sm" p={3}>
          <VStack align="flex-start" spacing={0}>
            <Text fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide">
              Koszty dodatkowe projektu
            </Text>
            <Text fontSize="2xl" fontWeight="bold" color="neutral.800">{summary.additionalCostsCount}</Text>
          </VStack>
        </Box>
      </SimpleGrid>
    </VStack>
  );
}
