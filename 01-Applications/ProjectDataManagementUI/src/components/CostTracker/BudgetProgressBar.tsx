import { Box, Progress, HStack, Text } from "@chakra-ui/react";

interface BudgetProgressBarProps {
  coveredPercent: number | null;
  budgetNet: number | null;
  costsNet: number | null;
  isBudgetExceeded?: boolean;
  showAmounts?: boolean;
}

const formatAmount = (value: number | null): string => {
  if (value === null || value === undefined) return "—";
  return value.toLocaleString("pl-PL", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
};

export default function BudgetProgressBar({
  coveredPercent,
  budgetNet,
  costsNet,
  isBudgetExceeded = false,
  showAmounts = true,
}: BudgetProgressBarProps) {
  const percent =
    coveredPercent !== null && coveredPercent !== undefined
      ? Math.min(coveredPercent, 100)
      : 0;

  const colorScheme = isBudgetExceeded ? "red" : percent >= 80 ? "orange" : "green";

  return (
    <Box>
      <Progress
        value={percent}
        colorScheme={colorScheme}
        size="sm"
        borderRadius="full"
        hasStripe={isBudgetExceeded}
      />
      {showAmounts && (
        <HStack justify="space-between" mt={1} fontSize="xs" color="gray.500">
          <Text>
            {coveredPercent !== null ? `${coveredPercent.toFixed(1)}%` : "—"}
          </Text>
          <Text>
            {formatAmount(costsNet)} / {formatAmount(budgetNet)} PLN
          </Text>
        </HStack>
      )}
    </Box>
  );
}
