import { useEffect, useState } from "react";
import {
  Box,
  Button,
  FormControl,
  FormLabel,
  HStack,
  Select,
  Spinner,
  Text,
  VStack,
} from "@chakra-ui/react";
import { useCurrencies } from "../../hooks/queries/useCurrencies";
import { useUpdateProjectCurrency } from "../../hooks/queries/useUpdateProjectCurrency";
import { useToastNotification } from "../../hooks/useToastNotification";
import type { ProjectCurrencyWeb } from "../../types/project.types";

export interface CurrencySelectorProps {
  tenantId: string;
  projectId: string;
  currentCurrency?: ProjectCurrencyWeb;
  canEdit: boolean;
}

export default function CurrencySelector({
  tenantId,
  projectId,
  currentCurrency,
  canEdit,
}: CurrencySelectorProps) {
  const { data: currencies, isLoading: isLoadingCurrencies } = useCurrencies();
  const updateMutation = useUpdateProjectCurrency(tenantId, projectId);
  const { showSuccess, showError, showApiError } = useToastNotification();

  const [selectedCode, setSelectedCode] = useState<string>(currentCurrency?.code ?? "");

  useEffect(() => {
    setSelectedCode(currentCurrency?.code ?? "");
  }, [currentCurrency?.code]);

  if (!canEdit) {
    const displayName = currentCurrency?.name ?? "Nie ustawiono";
    const symbol = currentCurrency?.symbol;
    return (
      <VStack align="flex-start" spacing={1}>
        <Text fontSize="sm" color="neutral.600">
          Waluta projektu
        </Text>
        <Text fontWeight="semibold">
          {displayName}
          {symbol ? ` (${symbol})` : ""}
        </Text>
      </VStack>
    );
  }

  if (isLoadingCurrencies) {
    return (
      <HStack spacing={3}>
        <Spinner size="sm" color="primary.600" />
        <Text fontSize="sm" color="neutral.600">
          Ładowanie walut…
        </Text>
      </HStack>
    );
  }

  const isUnchanged =
    selectedCode === (currentCurrency?.code ?? "") || selectedCode === "";

  const handleSave = async () => {
    const selected = currencies?.find((c) => c.code === selectedCode);
    if (!selected) {
      return;
    }

    try {
      await updateMutation.mutateAsync({
        code: selected.code,
        name: selected.name,
        symbol: selected.symbol,
      });
      showSuccess("Sukces", "Waluta projektu została zapisana");
    } catch (error) {
      showApiError(error);
    }
  };

  return (
    <Box>
      <FormControl>
        <FormLabel>Waluta projektu</FormLabel>
        <Select
          value={selectedCode}
          onChange={(e) => setSelectedCode(e.target.value)}
          placeholder="Wybierz walutę"
          maxW={{ base: "100%", md: "md" }}
        >
          {(currencies ?? []).map((currency) => (
            <option key={currency.code} value={currency.code}>
              {currency.name} ({currency.symbol ?? currency.code})
            </option>
          ))}
        </Select>
      </FormControl>
      <HStack mt={4}>
        <Button
          colorScheme="primary"
          onClick={handleSave}
          isLoading={updateMutation.isPending}
          isDisabled={isUnchanged || updateMutation.isPending}
        >
          Zapisz walutę
        </Button>
      </HStack>
    </Box>
  );
}
