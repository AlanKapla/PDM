import {
  Box,
  VStack,
  HStack,
  Text,
  Button,
  SimpleGrid,
  Stat,
  StatLabel,
  StatNumber,
  StatHelpText,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  Tooltip,
  useDisclosure,
  useBreakpointValue,
} from "@chakra-ui/react";
import { Plus, Edit2, Trash2 } from "lucide-react";
import { useState } from "react";
import CostFormDrawer from "./CostFormDrawer";
import CostListDrawer from "./CostListDrawer";
import { DeleteCostConfirm } from "./PositionsTable";
import { costTrackerApi } from "../../api/costTrackerApi";
import { useToastNotification } from "../../hooks/useToastNotification";
import { handleApiError } from "../../utils/handleApiError";
import { formatDate } from "../../utils/formatters";
import type { ProjectAdditionalCostsWeb, TrackedCostWeb } from "../../types/costTracker.types";

interface ProjectAdditionalCostsSectionProps {
  projectAdditionalCosts: ProjectAdditionalCostsWeb;
  tenantId: string;
  projectId: string;
  onCostMutated: () => void;
}

function fmt(value: number | null): string {
  if (value === null || value === undefined) return "—";
  return value.toLocaleString("pl-PL", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default function ProjectAdditionalCostsSection({
  projectAdditionalCosts,
  tenantId,
  projectId,
  onCostMutated,
}: ProjectAdditionalCostsSectionProps) {
  const { showSuccess, showError } = useToastNotification();
  const isMobile = useBreakpointValue({ base: true, md: false });

  const { isOpen: isAddOpen, onOpen: onAddOpen, onClose: onAddClose } = useDisclosure();
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();

  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [deletingCost, setDeletingCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

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
        <HStack justify="space-between" mb={4} flexWrap="wrap" gap={2}>
          <Text fontWeight="bold" fontSize={{ base: "md", md: "lg" }}>
            Koszty dodatkowe projektu
          </Text>
          <Button
            leftIcon={<Plus size={14} />}
            colorScheme="blue"
            variant="outline"
            size="sm"
            onClick={onAddOpen}
            minH="44px"
          >
            Dodaj koszt dodatkowy projektu
          </Button>
        </HStack>

        <SimpleGrid columns={{ base: 2, md: 3 }} spacing={4} mb={4}>
          <Stat>
            <StatLabel>Suma netto</StatLabel>
            <StatNumber fontSize="xl">{fmt(projectAdditionalCosts.totalNet)}</StatNumber>
            <StatHelpText>PLN</StatHelpText>
          </Stat>
          <Stat>
            <StatLabel>Suma brutto</StatLabel>
            <StatNumber fontSize="xl">{fmt(projectAdditionalCosts.totalGross)}</StatNumber>
            <StatHelpText>PLN</StatHelpText>
          </Stat>
          <Stat>
            <StatLabel>Liczba kosztów</StatLabel>
            <StatNumber fontSize="xl">{projectAdditionalCosts.costsCount}</StatNumber>
          </Stat>
        </SimpleGrid>

        {projectAdditionalCosts.costs.length > 0 && (
          isMobile ? (
            <CostsCardList
              costs={projectAdditionalCosts.costs}
              onEdit={setEditingCost}
              onDelete={(c) => { setDeletingCost(c); onDeleteOpen(); }}
            />
          ) : (
            <CostsTable
              costs={projectAdditionalCosts.costs}
              onEdit={setEditingCost}
              onDelete={(c) => { setDeletingCost(c); onDeleteOpen(); }}
            />
          )
        )}
      </Box>

      {/* Drawer: dodaj */}
      <CostFormDrawer
        isOpen={isAddOpen}
        onClose={onAddClose}
        onSuccess={() => { onAddClose(); onCostMutated(); }}
        tenantId={tenantId}
        projectId={projectId}
        costEstimateId={null}
        costEstimateItemId={null}
        title="Dodaj koszt dodatkowy projektu"
      />

      {/* Drawer: edytuj */}
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

// ===== Desktop table =====

interface CostActionsProps {
  costs: TrackedCostWeb[];
  onEdit: (cost: TrackedCostWeb) => void;
  onDelete: (cost: TrackedCostWeb) => void;
}

function CostsTable({ costs, onEdit, onDelete }: CostActionsProps) {
  return (
    <Box overflowX="auto">
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Nazwa</Th>
            <Th isNumeric>Netto</Th>
            <Th>Nr faktury</Th>
            <Th>Data</Th>
            <Th>Wykonawca</Th>
            <Th>Załączniki</Th>
            <Th>Akcje</Th>
          </Tr>
        </Thead>
        <Tbody>
          {costs.map((cost) => (
            <Tr key={cost.id}>
              <Td maxW="180px">
                <Text noOfLines={1} fontSize="sm">{cost.name}</Text>
              </Td>
              <Td isNumeric fontSize="sm">{fmt(cost.net)}</Td>
              <Td fontSize="sm">{cost.number ?? "—"}</Td>
              <Td fontSize="sm">{cost.date ? formatDate(cost.date, false) : "—"}</Td>
              <Td fontSize="sm" maxW="120px">
                <Text noOfLines={1}>{cost.contractor ?? "—"}</Text>
              </Td>
              <Td fontSize="sm">{cost.attachments.length > 0 ? cost.attachments.length : "—"}</Td>
              <Td>
                <HStack spacing={1}>
                  <Tooltip label="Edytuj">
                    <IconButton
                      aria-label="Edytuj koszt"
                      icon={<Edit2 size={14} />}
                      size="xs"
                      variant="ghost"
                      onClick={() => onEdit(cost)}
                      minH="44px"
                    />
                  </Tooltip>
                  <Tooltip label="Usuń">
                    <IconButton
                      aria-label="Usuń koszt"
                      icon={<Trash2 size={14} />}
                      size="xs"
                      variant="ghost"
                      colorScheme="red"
                      onClick={() => onDelete(cost)}
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

// ===== Mobile card list =====

function CostsCardList({ costs, onEdit, onDelete }: CostActionsProps) {
  return (
    <VStack spacing={2} align="stretch">
      {costs.map((cost) => (
        <Box
          key={cost.id}
          p={3}
          borderRadius="md"
          borderWidth={1}
          borderColor="gray.200"
          _dark={{ borderColor: "gray.600" }}
        >
          <HStack align="flex-start">
            <VStack align="stretch" flex={1} spacing={1}>
              <Text fontWeight="semibold" fontSize="sm">{cost.name}</Text>
              <HStack spacing={3} flexWrap="wrap">
                <Text fontSize="xs" color="gray.600">N: {fmt(cost.net)} PLN</Text>
                <Text fontSize="xs" color="gray.600">B: {fmt(cost.gross)} PLN</Text>
                {cost.date && (
                  <Text fontSize="xs" color="gray.600">{formatDate(cost.date, false)}</Text>
                )}
              </HStack>
            </VStack>
            <HStack spacing={1}>
              <IconButton
                aria-label="Edytuj"
                icon={<Edit2 size={14} />}
                size="sm"
                variant="ghost"
                onClick={() => onEdit(cost)}
                minH="44px"
              />
              <IconButton
                aria-label="Usuń"
                icon={<Trash2 size={14} />}
                size="sm"
                variant="ghost"
                colorScheme="red"
                onClick={() => onDelete(cost)}
                minH="44px"
              />
            </HStack>
          </HStack>
        </Box>
      ))}
    </VStack>
  );
}
