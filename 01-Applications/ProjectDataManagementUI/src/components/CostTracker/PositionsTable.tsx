import { useState, useRef } from "react";
import {
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  HStack,
  Button,
  Box,
  Stack,
  Text,
  VStack,
  Progress,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useDisclosure,
  useBreakpointValue,
} from "@chakra-ui/react";
import { Plus, List, TrendingDown, TrendingUp } from "lucide-react";
import CostTrackerStatusBadge from "./CostTrackerStatusBadge";
import CostCountBadge from "./CostCountBadge";
import CostFormDrawer from "./CostFormDrawer";
import CostListDrawer from "./CostListDrawer";
import type { CostEstimateSummaryWeb, TrackerItemWeb } from "../../types/costTracker.types";

interface PositionsTableProps {
  items: TrackerItemWeb[];
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  onCostMutated: () => void;
  estimates?: CostEstimateSummaryWeb[];
}

const fmt = (value: number | null): string => {
  if (value === null || value === undefined) return "—";
  return new Intl.NumberFormat("pl-PL", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
};

export default function PositionsTable({
  items,
  tenantId,
  projectId,
  costEstimateId,
  onCostMutated,
  estimates,
}: PositionsTableProps) {
  const isMobile = useBreakpointValue({ base: true, md: false });
  const [addTarget, setAddTarget] = useState<TrackerItemWeb | null>(null);
  const [listTarget, setListTarget] = useState<TrackerItemWeb | null>(null);

  if (items.length === 0) return null;

  return (
    <>
      {isMobile ? (
        <PositionsCardList
          items={items}
          onAddCost={setAddTarget}
          onListCosts={setListTarget}
        />
      ) : (
        <PositionsDesktopTable
          items={items}
          onAddCost={setAddTarget}
          onListCosts={setListTarget}
        />
      )}

      {addTarget && (
        <CostFormDrawer
          isOpen={!!addTarget}
          onClose={() => setAddTarget(null)}
          onSuccess={() => { setAddTarget(null); onCostMutated(); }}
          tenantId={tenantId}
          projectId={projectId}
          costEstimateId={costEstimateId}
          costEstimateItemId={addTarget.costEstimateItemId}
          title={`Dodaj koszt — ${addTarget.name}`}
        />
      )}
      {listTarget && (
        <CostListDrawer
          isOpen={!!listTarget}
          onClose={() => setListTarget(null)}
          onMutated={onCostMutated}
          tenantId={tenantId}
          projectId={projectId}
          costs={listTarget.costs}
          title={`Koszty — ${listTarget.name}`}
          estimates={estimates}
        />
      )}
    </>
  );
}

// ===== Desktop =====

type Callbacks = {
  onAddCost: (item: TrackerItemWeb) => void;
  onListCosts: (item: TrackerItemWeb) => void;
};

function PositionsDesktopTable({ items, onAddCost, onListCosts }: { items: TrackerItemWeb[] } & Callbacks) {
  return (
    <Box overflowX="auto">
      <Table size="sm" variant="simple">
        <Thead>
          <Tr bg="neutral.25">
            <Th fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide">Nazwa</Th>
            <Th fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide" isNumeric>Budżet netto</Th>
            <Th fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide" isNumeric>Koszty netto</Th>
            <Th fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide" isNumeric>Odchylenie</Th>
            <Th fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide" minW="120px">Realizacja</Th>
            <Th fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide">Status</Th>
            <Th fontSize="xs" color="neutral.500" fontWeight="medium" textTransform="uppercase" letterSpacing="wide" w="80px"></Th>
          </Tr>
        </Thead>
        <Tbody>
          {items.map((item, idx) => {
            const coveredPct = item.coveredPercent ?? 0;
            const progressColor = item.isBudgetExceeded ? "red" : coveredPct >= 80 ? "orange" : "green";
            const deviationColor = item.isBudgetExceeded ? "red.500" : "green.600";
            return (
              <Tr key={item.costEstimateItemId} bg={idx % 2 === 1 ? "neutral.25" : "white"}>
                <Td maxW="200px">
                  <Text noOfLines={2} fontSize="sm" fontWeight="medium" color="neutral.800">{item.name}</Text>
                </Td>
                <Td isNumeric fontSize="sm" color="neutral.600">{fmt(item.budgetNet)}</Td>
                <Td isNumeric fontSize="sm" color="neutral.600">{fmt(item.costsNet)}</Td>
                <Td isNumeric>
                  <HStack justify="flex-end" spacing={1}>
                    {item.isBudgetExceeded
                      ? <TrendingDown size={12} color="var(--chakra-colors-red-500)" />
                      : <TrendingUp size={12} color="var(--chakra-colors-green-600)" />
                    }
                    <Text fontSize="sm" fontWeight="semibold" color={deviationColor}>{fmt(item.deviationNet)}</Text>
                  </HStack>
                </Td>
                <Td>
                  <HStack spacing={2} align="center">
                    <Progress
                      value={Math.min(coveredPct, 100)}
                      size="sm"
                      colorScheme={progressColor}
                      w="80px"
                      borderRadius="full"
                      bg="neutral.50"
                      flexShrink={0}
                    />
                    <Text fontSize="xs" color="neutral.500" flexShrink={0}>{coveredPct.toFixed(0)}%</Text>
                  </HStack>
                </Td>
                <Td>
                  <HStack spacing={1}>
                    <CostTrackerStatusBadge status={item.status} size="xs" />
                    <CostCountBadge count={item.costCount} />
                  </HStack>
                </Td>
                <Td>
                  <HStack spacing={0}>
                    <IconButton
                      aria-label="Dodaj koszt"
                      icon={<Plus size={14} />}
                      size="sm"
                      colorScheme="primary"
                      variant="ghost"
                      onClick={() => onAddCost(item)}
                      minH="36px"
                    />
                    {item.costCount > 0 && (
                      <IconButton
                        aria-label="Lista kosztów"
                        icon={<List size={14} />}
                        size="sm"
                        variant="ghost"
                        onClick={() => onListCosts(item)}
                        minH="36px"
                      />
                    )}
                  </HStack>
                </Td>
              </Tr>
            );
          })}
        </Tbody>
      </Table>
    </Box>
  );
}

// ===== Mobile: karty =====

function PositionsCardList({ items, onAddCost, onListCosts }: { items: TrackerItemWeb[] } & Callbacks) {
  return (
    <Stack spacing={2}>
      {items.map((item) => {
        const coveredPct = item.coveredPercent ?? 0;
        const progressColor = item.isBudgetExceeded ? "red" : coveredPct >= 80 ? "orange" : "green";
        return (
          <Box
            key={item.costEstimateItemId}
            p={3}
            borderRadius="md"
            borderWidth="1px"
            borderColor="neutral.200"
            bg="white"
          >
            <VStack align="stretch" spacing={2}>
              <HStack justify="space-between">
                <Text fontWeight="semibold" fontSize="sm" flex={1} noOfLines={2} color="neutral.800">
                  {item.name}
                </Text>
                <HStack spacing={1}>
                  <CostTrackerStatusBadge status={item.status} size="xs" />
                  <CostCountBadge count={item.costCount} />
                </HStack>
              </HStack>

              <HStack spacing={2} align="center">
                <Progress
                  value={Math.min(coveredPct, 100)}
                  flex={1}
                  size="sm"
                  colorScheme={progressColor}
                  borderRadius="full"
                  bg="neutral.50"
                />
                <Text fontSize="xs" color="neutral.500" flexShrink={0}>{coveredPct.toFixed(0)}%</Text>
              </HStack>

              <HStack justify="space-between" fontSize="xs" color="neutral.500">
                <Text>Bud: <Text as="span" fontWeight="medium" color="neutral.700">{fmt(item.budgetNet)}</Text></Text>
                <Text>Koszty: <Text as="span" fontWeight="medium" color="neutral.700">{fmt(item.costsNet)}</Text></Text>
                <HStack spacing={0}>
                  <IconButton
                    aria-label="Dodaj koszt"
                    icon={<Plus size={14} />}
                    size="sm"
                    colorScheme="primary"
                    variant="ghost"
                    onClick={() => onAddCost(item)}
                    minH="44px"
                  />
                  {item.costCount > 0 && (
                    <IconButton
                      aria-label="Lista kosztów"
                      icon={<List size={14} />}
                      size="sm"
                      variant="ghost"
                      onClick={() => onListCosts(item)}
                      minH="44px"
                    />
                  )}
                </HStack>
              </HStack>
            </VStack>
          </Box>
        );
      })}
    </Stack>
  );
}

// ===== AlertDialog (eksportowany) =====

interface DeleteConfirmProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  isLoading?: boolean;
}

export function DeleteCostConfirm({ isOpen, onClose, onConfirm, isLoading }: DeleteConfirmProps) {
  const cancelRef = useRef<HTMLButtonElement>(null);
  return (
    <AlertDialog isOpen={isOpen} leastDestructiveRef={cancelRef} onClose={onClose} isCentered>
      <AlertDialogOverlay>
        <AlertDialogContent>
          <AlertDialogHeader>Usuń koszt</AlertDialogHeader>
          <AlertDialogBody>
            Czy na pewno chcesz usunąć ten koszt? Operacja jest nieodwracalna.
          </AlertDialogBody>
          <AlertDialogFooter>
            <HStack spacing={2}>
              <Button ref={cancelRef} variant="ghost" onClick={onClose} isDisabled={isLoading}>
                Anuluj
              </Button>
              <Button colorScheme="red" onClick={onConfirm} isLoading={isLoading}>
                Usuń
              </Button>
            </HStack>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialogOverlay>
    </AlertDialog>
  );
}
