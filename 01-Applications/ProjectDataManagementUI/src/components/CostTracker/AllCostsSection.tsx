import { useMemo, useState } from "react";
import {
  Box,
  VStack,
  HStack,
  Text,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Select,
  IconButton,
  Badge,
  Tooltip,
  useDisclosure,
  useBreakpointValue,
} from "@chakra-ui/react";
import { Edit2, Trash2 } from "lucide-react";
import CostFormDrawer from "./CostFormDrawer";
import { DeleteCostConfirm } from "./PositionsTable";
import { costTrackerApi } from "../../api/costTrackerApi";
import { useToastNotification } from "../../hooks/useToastNotification";
import { handleApiError } from "../../utils/handleApiError";
import { formatDate } from "../../utils/formatters";
import type {
  CostTrackerDetailsWeb,
  TrackedCostWeb,
  CostEstimateSummaryWeb,
  TrackerGroupWeb,
  TrackerItemWeb,
} from "../../types/costTracker.types";

type CostSource = "item" | "estimate-additional" | "project-additional";

interface FlattenedCost {
  cost: TrackedCostWeb;
  source: CostSource;
  estimateName?: string;
  estimateId?: string;
}

// Rekurencyjne zbieranie kosztów z grup
function collectGroupCosts(
  group: TrackerGroupWeb,
  estimateName: string,
  estimateId: string,
  result: FlattenedCost[]
) {
  group.items.forEach((item) => {
    item.costs.forEach((cost) => {
      result.push({ cost, source: "item", estimateName, estimateId });
    });
  });
  group.childGroups.forEach((child) => collectGroupCosts(child, estimateName, estimateId, result));
}

function aggregateAllCosts(data: CostTrackerDetailsWeb): FlattenedCost[] {
  const result: FlattenedCost[] = [];

  data.costEstimateSummaries.forEach((est) => {
    // Koszty z pozycji w grupach
    est.groups.forEach((group) =>
      collectGroupCosts(group, est.costEstimateName, est.costEstimateId, result)
    );
    // Koszty dodatkowe kosztorysu
    est.additionalCosts.costs.forEach((cost) => {
      result.push({
        cost,
        source: "estimate-additional",
        estimateName: est.costEstimateName,
        estimateId: est.costEstimateId,
      });
    });
  });

  // Koszty dodatkowe projektu
  data.projectAdditionalCosts.costs.forEach((cost) => {
    result.push({ cost, source: "project-additional" });
  });

  return result;
}

const SOURCE_LABELS: Record<CostSource, string> = {
  item: "Pozycja kosztorysu",
  "estimate-additional": "Dodatkowy kosztorysu",
  "project-additional": "Dodatkowy projektu",
};

const SOURCE_COLORS: Record<CostSource, string> = {
  item: "blue",
  "estimate-additional": "purple",
  "project-additional": "orange",
};

function fmt(value: number | null): string {
  if (value === null || value === undefined) return "—";
  return value.toLocaleString("pl-PL", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

interface AllCostsSectionProps {
  data: CostTrackerDetailsWeb;
  tenantId: string;
  projectId: string;
  onCostMutated: () => void;
}

export default function AllCostsSection({
  data,
  tenantId,
  projectId,
  onCostMutated,
}: AllCostsSectionProps) {
  const { showSuccess, showError } = useToastNotification();
  const isMobile = useBreakpointValue({ base: true, md: false });

  const allCosts = useMemo(() => aggregateAllCosts(data), [data]);

  // Filtry
  const [filterEstimateId, setFilterEstimateId] = useState<string>("all");
  const [filterSource, setFilterSource] = useState<CostSource | "all">("all");

  const filtered = useMemo(() => {
    return allCosts.filter((fc) => {
      if (filterEstimateId !== "all" && fc.estimateId !== filterEstimateId) return false;
      if (filterSource !== "all" && fc.source !== filterSource) return false;
      return true;
    });
  }, [allCosts, filterEstimateId, filterSource]);

  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [deletingCost, setDeletingCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();

  const handleDelete = async () => {
    if (!deletingCost) return;
    setIsDeleting(true);
    try {
      await costTrackerApi.deleteCost(tenantId, projectId, deletingCost.id);
      showSuccess("Koszt usunięty");
      onDeleteClose();
      setDeletingCost(null);
      onCostMutated();
    } catch (err) {
      const { title, description } = handleApiError(err);
      showError(title, description);
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <>
      <Box>
        <VStack align="stretch" spacing={4}>
          {/* Filtry */}
          <HStack spacing={3} flexWrap="wrap">
            <Select
              size="sm"
              value={filterEstimateId}
              onChange={(e) => setFilterEstimateId(e.target.value)}
              maxW="220px"
              minH="44px"
            >
              <option value="all">Wszystkie kosztorysy</option>
              {data.costEstimateSummaries.map((est) => (
                <option key={est.costEstimateId} value={est.costEstimateId}>
                  {est.costEstimateName}
                </option>
              ))}
            </Select>

            <Select
              size="sm"
              value={filterSource}
              onChange={(e) => setFilterSource(e.target.value as CostSource | "all")}
              maxW="220px"
              minH="44px"
            >
              <option value="all">Wszystkie typy</option>
              <option value="item">Pozycja kosztorysu</option>
              <option value="estimate-additional">Dodatkowy kosztorysu</option>
              <option value="project-additional">Dodatkowy projektu</option>
            </Select>

            <Text fontSize="sm" color="gray.500">
              {filtered.length} kosztów
            </Text>
          </HStack>

          {filtered.length === 0 ? (
            <Text color="gray.500" fontSize="sm" textAlign="center" py={8}>
              Brak kosztów spełniających kryteria filtrowania.
            </Text>
          ) : isMobile ? (
            <AllCostsMobileList
              costs={filtered}
              onEdit={setEditingCost}
              onDelete={(fc) => { setDeletingCost(fc.cost); onDeleteOpen(); }}
            />
          ) : (
            <AllCostsDesktopTable
              costs={filtered}
              onEdit={setEditingCost}
              onDelete={(fc) => { setDeletingCost(fc.cost); onDeleteOpen(); }}
            />
          )}
        </VStack>
      </Box>

      {/* Drawer edycji */}
      {editingCost && (
        <CostFormDrawer
          isOpen={!!editingCost}
          onClose={() => setEditingCost(null)}
          onSuccess={() => { setEditingCost(null); onCostMutated(); }}
          tenantId={tenantId}
          projectId={projectId}
          cost={editingCost}
        />
      )}

      {/* Potwierdzenie usunięcia */}
      <DeleteCostConfirm
        isOpen={isDeleteOpen}
        onClose={() => { onDeleteClose(); setDeletingCost(null); }}
        onConfirm={handleDelete}
        isLoading={isDeleting}
      />
    </>
  );
}

// ===== Desktop =====

interface TableListProps {
  costs: FlattenedCost[];
  onEdit: (cost: TrackedCostWeb) => void;
  onDelete: (fc: FlattenedCost) => void;
}

function AllCostsDesktopTable({ costs, onEdit, onDelete }: TableListProps) {
  return (
    <Box overflowX="auto">
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Źródło</Th>
            <Th>Kosztorys</Th>
            <Th>Nazwa</Th>
            <Th isNumeric>Netto</Th>
            <Th>Nr faktury</Th>
            <Th>Data</Th>
            <Th>Załączniki</Th>
            <Th>Akcje</Th>
          </Tr>
        </Thead>
        <Tbody>
          {costs.map(({ cost, source, estimateName }) => (
            <Tr key={cost.id}>
              <Td>
                <Badge colorScheme={SOURCE_COLORS[source]} fontSize="xs">
                  {SOURCE_LABELS[source]}
                </Badge>
              </Td>
              <Td fontSize="xs" maxW="120px">
                <Text noOfLines={1}>{estimateName ?? "—"}</Text>
              </Td>
              <Td fontSize="sm" maxW="180px">
                <Text noOfLines={1}>{cost.name}</Text>
              </Td>
              <Td isNumeric fontSize="sm">{fmt(cost.net)}</Td>
              <Td fontSize="sm">{cost.number ?? "—"}</Td>
              <Td fontSize="sm">{cost.date ? formatDate(cost.date, false) : "—"}</Td>
              <Td fontSize="sm">{cost.attachments.length > 0 ? cost.attachments.length : "—"}</Td>
              <Td>
                <HStack spacing={1}>
                  <Tooltip label="Edytuj">
                    <IconButton
                      aria-label="Edytuj"
                      icon={<Edit2 size={13} />}
                      size="xs"
                      variant="ghost"
                      onClick={() => onEdit(cost)}
                      minH="44px"
                    />
                  </Tooltip>
                  <Tooltip label="Usuń">
                    <IconButton
                      aria-label="Usuń"
                      icon={<Trash2 size={13} />}
                      size="xs"
                      variant="ghost"
                      colorScheme="red"
                      onClick={() => onDelete({ cost, source })}
                      minH="44px"
                    />
                  </Tooltip>
                </HStack>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
}

// ===== Mobile =====

function AllCostsMobileList({ costs, onEdit, onDelete }: TableListProps) {
  return (
    <VStack spacing={2} align="stretch">
      {costs.map((fc) => (
        <Box
          key={fc.cost.id}
          p={3}
          borderRadius="md"
          borderWidth={1}
          borderColor="gray.200"
          _dark={{ borderColor: "gray.600" }}
        >
          <HStack align="flex-start">
            <VStack align="stretch" flex={1} spacing={1}>
              <HStack flexWrap="wrap">
                <Badge colorScheme={SOURCE_COLORS[fc.source]} fontSize="xs">
                  {SOURCE_LABELS[fc.source]}
                </Badge>
                {fc.estimateName && (
                  <Text fontSize="xs" color="gray.500" noOfLines={1}>
                    {fc.estimateName}
                  </Text>
                )}
              </HStack>
              <Text fontWeight="semibold" fontSize="sm">{fc.cost.name}</Text>
              <HStack spacing={3} flexWrap="wrap">
                <Text fontSize="xs" color="gray.600">N: {fmt(fc.cost.net)} PLN</Text>
                {fc.cost.number && (
                  <Text fontSize="xs" color="gray.600">Nr: {fc.cost.number}</Text>
                )}
                {fc.cost.date && (
                  <Text fontSize="xs" color="gray.600">{formatDate(fc.cost.date, false)}</Text>
                )}
              </HStack>
            </VStack>
            <HStack spacing={1}>
              <IconButton
                aria-label="Edytuj"
                icon={<Edit2 size={14} />}
                size="sm"
                variant="ghost"
                onClick={() => onEdit(fc.cost)}
                minH="44px"
              />
              <IconButton
                aria-label="Usuń"
                icon={<Trash2 size={14} />}
                size="sm"
                variant="ghost"
                colorScheme="red"
                onClick={() => onDelete(fc)}
                minH="44px"
              />
            </HStack>
          </HStack>
        </Box>
      ))}
    </VStack>
  );
}
