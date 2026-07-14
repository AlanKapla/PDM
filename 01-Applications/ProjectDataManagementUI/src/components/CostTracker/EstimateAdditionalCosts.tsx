import { useState } from "react";
import {
  Box,
  VStack,
  HStack,
  Text,
  Badge,
  Button,
  IconButton,
  Divider,
  Stack,
  useDisclosure,
} from "@chakra-ui/react";
import { Plus, Edit2, List } from "lucide-react";
import CostFormDrawer from "./CostFormDrawer";
import CostListDrawer from "./CostListDrawer";
import type { CostEstimateSummaryWeb, TrackerAdditionalCostsWeb } from "../../types/costTracker.types";
import { formatDateShort } from "../../utils/formatters";

const fmt = (value: number | null): string => {
  if (value === null || value === undefined) return "—";
  return new Intl.NumberFormat("pl-PL", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
};

interface EstimateAdditionalCostsProps {
  additionalCosts: TrackerAdditionalCostsWeb;
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  onCostMutated: () => void;
  estimates?: CostEstimateSummaryWeb[];
}

export default function EstimateAdditionalCosts({
  additionalCosts,
  tenantId,
  projectId,
  costEstimateId,
  onCostMutated,
  estimates,
}: EstimateAdditionalCostsProps) {
  const { isOpen: isAddOpen, onOpen: onAddOpen, onClose: onAddClose } = useDisclosure();
  const { isOpen: isListOpen, onOpen: onListOpen, onClose: onListClose } = useDisclosure();

  return (
    <>
      <Divider />
      <Box bg="neutral.25" px={3} py={3} borderRadius="md">
        {/* Nagłówek sekcji */}
        <HStack justify="space-between" mb={2} flexWrap="wrap" gap={2}>
          <HStack spacing={2}>
            <Text fontSize="xs" fontWeight="semibold" color="neutral.600" textTransform="uppercase" letterSpacing="wide">
              Koszty dodatkowe kosztorysu
            </Text>
            <Badge colorScheme="gray" borderRadius="full" fontSize="xs">
              {additionalCosts.costsCount}
            </Badge>
          </HStack>
          <HStack spacing={1}>
            {additionalCosts.costsCount > 0 && (
              <IconButton
                aria-label="Pokaż listę"
                icon={<List size={14} />}
                size="xs"
                variant="ghost"
                onClick={onListOpen}
                minH="32px"
              />
            )}
            <Button
              size="xs"
              leftIcon={<Plus size={12} />}
              colorScheme="primary"
              variant="ghost"
              onClick={onAddOpen}
              minH="32px"
            >
              Dodaj
            </Button>
          </HStack>
        </HStack>

        {/* Sumy */}
        <HStack spacing={4} fontSize="xs" color="neutral.500">
          <Box>
            <Text color="neutral.400" textTransform="uppercase" letterSpacing="wide" mb="1px">Netto</Text>
            <Text fontWeight="semibold" fontSize="sm" color="neutral.700">{fmt(additionalCosts.totalNet)} PLN</Text>
          </Box>
          <Box>
            <Text color="neutral.400" textTransform="uppercase" letterSpacing="wide" mb="1px">Brutto</Text>
            <Text fontWeight="semibold" fontSize="sm" color="neutral.700">{fmt(additionalCosts.totalGross)} PLN</Text>
          </Box>
        </HStack>

        {/* Lista kosztów (inline, max 3) */}
        {additionalCosts.costs.length > 0 && (
          <Stack spacing={1} mt={2}>
            {additionalCosts.costs.slice(0, 3).map((cost) => (
              <HStack
                key={cost.id}
                justify="space-between"
                px={2}
                py={1}
                bg="white"
                borderRadius="md"
                borderWidth="1px"
                borderColor="neutral.200"
                fontSize="xs"
              >
                <Text fontWeight="medium" color="neutral.700" noOfLines={1} flex={1}>{cost.name}</Text>
                <HStack spacing={3} flexShrink={0} color="neutral.500">
                  <Text>{fmt(cost.net)} PLN</Text>
                  {cost.date && <Text>{formatDateShort(cost.date)}</Text>}
                </HStack>
              </HStack>
            ))}
            {additionalCosts.costs.length > 3 && (
              <Button
                size="xs"
                variant="link"
                colorScheme="primary"
                onClick={onListOpen}
                alignSelf="flex-start"
              >
                + {additionalCosts.costs.length - 3} więcej
              </Button>
            )}
          </Stack>
        )}
      </Box>

      <CostFormDrawer
        isOpen={isAddOpen}
        onClose={onAddClose}
        onSuccess={() => { onAddClose(); onCostMutated(); }}
        tenantId={tenantId}
        projectId={projectId}
        costEstimateId={costEstimateId}
        costEstimateItemId={null}
        title="Dodaj koszt dodatkowy kosztorysu"
      />

      {isListOpen && (
        <CostListDrawer
          isOpen={isListOpen}
          onClose={onListClose}
          onMutated={onCostMutated}
          tenantId={tenantId}
          projectId={projectId}
          costs={additionalCosts.costs}
          title="Koszty dodatkowe kosztorysu"
          estimates={estimates}
        />
      )}
    </>
  );
}
