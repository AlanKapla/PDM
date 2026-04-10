import { useState } from "react";
import {
  Box,
  VStack,
  HStack,
  Text,
  SimpleGrid,
  Button,
  FormControl,
  FormLabel,
  NumberInput,
  NumberInputField,
  useToast,
  Divider,
  Badge,
  Skeleton,
} from "@chakra-ui/react";
import { Edit2, Save, X, TrendingDown, TrendingUp } from "lucide-react";
import { costTrackerApi } from "../../api/costTrackerApi";
import type { CostTrackerBudgetSummary } from "../../types/costTracker.types";

interface BudgetSummarySectionProps {
  trackerId: string;
  tenantId: string;
  projectId: string;
  budgetSummary: CostTrackerBudgetSummary;
  onMutated: () => void;
}

const fmt = (v: number | null): string =>
  v === null || v === undefined
    ? "—"
    : new Intl.NumberFormat("pl-PL", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(v);

interface StatCardProps {
  label: string;
  value: string;
  sub?: string;
  accent?: "red" | "green" | "gray";
}

function StatCard({ label, value, sub, accent = "gray" }: StatCardProps) {
  const colorMap = {
    red: "red.600",
    green: "green.600",
    gray: "gray.700",
  };
  return (
    <Box bg="white" p={4} borderRadius="lg" borderWidth="1px" shadow="sm">
      <Text fontSize="xs" color="gray.500" mb={1}>
        {label}
      </Text>
      <Text fontSize="lg" fontWeight="bold" color={colorMap[accent]}>
        {value}
      </Text>
      {sub && (
        <Text fontSize="xs" color="gray.400" mt={0.5}>
          {sub}
        </Text>
      )}
    </Box>
  );
}

export default function BudgetSummarySection({
  trackerId,
  tenantId,
  projectId,
  budgetSummary,
  onMutated,
}: BudgetSummarySectionProps) {
  const toast = useToast();
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [budgetNet, setBudgetNet] = useState<string>(
    budgetSummary.totalBudgetNet !== null ? String(budgetSummary.totalBudgetNet) : ""
  );
  const [budgetGross, setBudgetGross] = useState<string>(
    budgetSummary.totalBudgetGross !== null ? String(budgetSummary.totalBudgetGross) : ""
  );

  const handleEdit = () => {
    setBudgetNet(budgetSummary.totalBudgetNet !== null ? String(budgetSummary.totalBudgetNet) : "");
    setBudgetGross(budgetSummary.totalBudgetGross !== null ? String(budgetSummary.totalBudgetGross) : "");
    setIsEditing(true);
  };

  const handleCancel = () => {
    setIsEditing(false);
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await costTrackerApi.updateBudget(tenantId, projectId, trackerId, {
        budgetNet: budgetNet !== "" ? parseFloat(budgetNet) : null,
        budgetGross: budgetGross !== "" ? parseFloat(budgetGross) : null,
      });
      toast({
        title: "Budżet zaktualizowany",
        status: "success",
        duration: 3000,
      });
      setIsEditing(false);
      onMutated();
    } catch {
      toast({
        title: "Błąd",
        description: "Nie udało się zapisać budżetu",
        status: "error",
        duration: 5000,
      });
    } finally {
      setIsSaving(false);
    }
  };

  const deviationAccent: "red" | "green" | "gray" =
    budgetSummary.totalBudgetNet === null
      ? "gray"
      : budgetSummary.isBudgetExceeded
      ? "red"
      : "green";

  return (
    <Box bg="gray.50" borderRadius="xl" borderWidth="1px" p={{ base: 4, md: 6 }}>
      <HStack justify="space-between" mb={4}>
        <HStack spacing={2}>
          <Text fontWeight="bold" fontSize="md">
            Budżet projektu (koszty dodatkowe)
          </Text>
          {budgetSummary.isBudgetExceeded && (
            <Badge colorScheme="red" fontSize="xs">
              Przekroczony
            </Badge>
          )}
        </HStack>
        {!isEditing && (
          <Button
            size="sm"
            variant="ghost"
            leftIcon={<Edit2 size={14} />}
            onClick={handleEdit}
            minH="36px"
          >
            Edytuj budżet
          </Button>
        )}
      </HStack>

      {isEditing ? (
        <VStack spacing={4} align="stretch">
          <SimpleGrid columns={{ base: 1, sm: 2 }} spacing={4}>
            <FormControl>
              <FormLabel fontSize="sm">Budżet netto</FormLabel>
              <NumberInput
                value={budgetNet}
                onChange={(val) => setBudgetNet(val)}
                min={0}
                precision={2}
              >
                <NumberInputField placeholder="np. 50000.00" />
              </NumberInput>
            </FormControl>
            <FormControl>
              <FormLabel fontSize="sm">Budżet brutto</FormLabel>
              <NumberInput
                value={budgetGross}
                onChange={(val) => setBudgetGross(val)}
                min={0}
                precision={2}
              >
                <NumberInputField placeholder="np. 61500.00" />
              </NumberInput>
            </FormControl>
          </SimpleGrid>
          <HStack spacing={2} justify="flex-end">
            <Button
              size="sm"
              variant="ghost"
              leftIcon={<X size={14} />}
              onClick={handleCancel}
              minH="36px"
            >
              Anuluj
            </Button>
            <Button
              size="sm"
              colorScheme="blue"
              leftIcon={<Save size={14} />}
              onClick={handleSave}
              isLoading={isSaving}
              minH="36px"
            >
              Zapisz
            </Button>
          </HStack>
        </VStack>
      ) : (
        <VStack spacing={4} align="stretch">
          <SimpleGrid columns={{ base: 2, md: 4 }} spacing={3}>
            <StatCard
              label="Budżet netto"
              value={fmt(budgetSummary.totalBudgetNet)}
            />
            <StatCard
              label="Koszty dodatkowe netto"
              value={fmt(budgetSummary.totalCostsNet)}
              sub={`${budgetSummary.costCount} koszt${budgetSummary.costCount === 1 ? "" : budgetSummary.costCount < 5 ? "y" : "ów"}`}
            />
            <StatCard
              label="Odchylenie netto"
              value={
                budgetSummary.totalDeviationNet !== null
                  ? `${budgetSummary.totalDeviationNet > 0 ? "+" : ""}${fmt(budgetSummary.totalDeviationNet)}`
                  : "—"
              }
              sub={
                budgetSummary.totalDeviationPercent !== null
                  ? `${budgetSummary.totalDeviationPercent.toFixed(1)}%`
                  : undefined
              }
              accent={deviationAccent}
            />
            <StatCard
              label="Budżet brutto"
              value={fmt(budgetSummary.totalBudgetGross)}
            />
          </SimpleGrid>

          {budgetSummary.totalBudgetNet !== null &&
            budgetSummary.totalCostsNet !== null && (
              <Box>
                <HStack justify="space-between" mb={1}>
                  <Text fontSize="xs" color="gray.500">
                    Realizacja budżetu netto
                  </Text>
                  <HStack spacing={1} fontSize="xs" color={deviationAccent === "red" ? "red.500" : "green.600"}>
                    {budgetSummary.isBudgetExceeded ? (
                      <TrendingUp size={12} />
                    ) : (
                      <TrendingDown size={12} />
                    )}
                    <Text>
                      {budgetSummary.totalDeviationPercent !== null
                        ? `${Math.abs(budgetSummary.totalDeviationPercent).toFixed(1)}%`
                        : "—"}
                    </Text>
                  </HStack>
                </HStack>
                <Box bg="gray.200" borderRadius="full" h="8px" overflow="hidden">
                  <Box
                    h="100%"
                    borderRadius="full"
                    bg={budgetSummary.isBudgetExceeded ? "red.400" : "green.400"}
                    w={`${Math.min(
                      budgetSummary.totalBudgetNet > 0
                        ? ((budgetSummary.totalCostsNet ?? 0) / budgetSummary.totalBudgetNet) * 100
                        : 0,
                      100
                    )}%`}
                    transition="width 0.3s ease"
                  />
                </Box>
              </Box>
            )}
        </VStack>
      )}
    </Box>
  );
}
