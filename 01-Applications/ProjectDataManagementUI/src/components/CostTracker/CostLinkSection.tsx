import {
  Box,
  FormControl,
  FormLabel,
  Select,
  Text,
  HStack,
  Button,
  VStack,
  Divider,
  Spinner,
  Alert,
  AlertIcon,
} from "@chakra-ui/react";
import { Link2, Link2Off } from "lucide-react";
import { useCostLinkOptions } from "../../hooks/queries";
import type {
  EstimateItemLinkOptionWeb,
  WorkLinkOptionWeb,
} from "../../types/costTracker.types";

interface CostLinkSectionProps {
  /** Pełna ścieżka bieżącej pozycji kosztorysu z web modelu (bez ładowania danych). */
  currentEstimatePath: string | null;
  /** Pełna ścieżka bieżącego zakresu pracy z web modelu (bez ładowania danych). */
  currentWorkPath: string | null;
  selectedItemId: string | null;
  selectedWorkId: string | null;
  onChange: (costEstimateItemId: string | null) => void;
  onWorkChange: (workId: string | null, relatedEstimateItemId?: string | null) => void;
  tenantId: string;
  projectId: string;
}

export default function CostLinkSection({
  currentEstimatePath,
  currentWorkPath,
  selectedItemId,
  selectedWorkId,
  onChange,
  onWorkChange,
  tenantId,
  projectId,
}: CostLinkSectionProps) {
  // Opcje ładowane zawsze — od razu po otwarciu modala
  const { data: linkOptions, isLoading } = useCostLinkOptions(tenantId, projectId, true);

  const estimateItems: EstimateItemLinkOptionWeb[] = linkOptions?.estimateItems ?? [];
  const workItems: WorkLinkOptionWeb[] = linkOptions?.workItems ?? [];

  // --- Reguły powiązania ---
  const selectedItem = estimateItems.find((i) => i.itemId === selectedItemId) ?? null;
  const selectedWork = workItems.find((w) => w.workId === selectedWorkId) ?? null;

  // Pozycja wybrana ale bez spięcia → blokuj zakres pracy
  const isWorkLockedByItem =
    selectedItemId !== null &&
    (selectedItem === null || selectedItem.linkedWorkId === null);

  // Zakres wybrany ale bez spięcia → blokuj pozycję
  const isItemLockedByWork =
    selectedWorkId !== null &&
    (selectedWork === null || selectedWork.linkedItemId === null);

  const filteredWorkItems = workItems;
  const filteredEstimateItems = estimateItems;

  // Path: preferuj dane z opcji (działa w create i edit), fallback do props
  const resolvedEstimatePath = selectedItem?.path ?? currentEstimatePath;
  const resolvedWorkPath = selectedWork?.path ?? currentWorkPath;

  const handleItemSelect = (itemId: string | null) => {
    onChange(itemId);
    if (itemId === null) {
      onWorkChange(null);
      return;
    }
    const item = estimateItems.find((i) => i.itemId === itemId) ?? null;
    if (item?.linkedWorkId) {
      onWorkChange(item.linkedWorkId, itemId);
    } else {
      onWorkChange(null);
    }
  };

  const handleWorkSelect = (workId: string | null) => {
    if (workId === null) {
      onWorkChange(null);
      onChange(null);
      return;
    }
    const work = workItems.find((w) => w.workId === workId) ?? null;
    if (work?.linkedItemId) {
      onChange(work.linkedItemId);
      onWorkChange(workId, work.linkedItemId);
    } else {
      onChange(null);
      onWorkChange(workId);
    }
  };

  const handleDetachAll = () => {
    onChange(null);
    onWorkChange(null);
  };

  const isLinked = !!selectedItemId || !!selectedWorkId;

  return (
    <Box
      border="1px solid"
      borderColor={isLinked ? "blue.200" : "neutral.200"}
      borderRadius="md"
      p={3}
      bg={isLinked ? "blue.50" : "neutral.50"}
    >
      {/* Nagłówek sekcji */}
      <HStack mb={3} justify="space-between">
        <HStack spacing={1}>
          <Link2 size={14} />
          <Text fontWeight="semibold" fontSize="sm">Powiązanie kosztu</Text>
        </HStack>
        {isLinked && (
          <Button
            size="xs"
            variant="ghost"
            colorScheme="red"
            leftIcon={<Link2Off size={12} />}
            onClick={handleDetachAll}
          >
            Odepnij wszystko
          </Button>
        )}
      </HStack>

      {isLoading ? (
        <HStack spacing={2} py={2}>
          <Spinner size="xs" />
          <Text fontSize="xs" color="neutral.500">Ładowanie opcji powiązania…</Text>
        </HStack>
      ) : (
        <VStack align="stretch" spacing={0}>
          {/* === Pozycja kosztorysu === */}
          <FormControl isDisabled={isItemLockedByWork}>
            <FormLabel fontSize="xs" mb={1} fontWeight="semibold" color="neutral.600">
              Pozycja kosztorysu
            </FormLabel>
            <Select
              size="sm"
              value={selectedItemId ?? ""}
              onChange={(e) => handleItemSelect(e.target.value || null)}
              placeholder="— brak powiązania —"
              bg="white"
            >
              {filteredEstimateItems.map((item) => (
                <option key={item.itemId} value={item.itemId}>
                  {item.path}{item.linkedWorkId ? " ⚡" : ""}
                </option>
              ))}
            </Select>
            {selectedItemId && resolvedEstimatePath && (
              <Text fontSize="xs" color="blue.700" mt={1} noOfLines={2}>
                📂 {resolvedEstimatePath}
              </Text>
            )}
            {isItemLockedByWork && !selectedItemId && (
              <Alert status="info" borderRadius="sm" py={1} px={2} mt={1}>
                <AlertIcon boxSize={3} />
                <Text fontSize="xs">Wybrany zakres nie ma spiętej pozycji — odepnij zakres, aby powiązać pozycję.</Text>
              </Alert>
            )}
            {filteredEstimateItems.length === 0 && !isItemLockedByWork && (
              <Text fontSize="xs" color="neutral.400" mt={1} fontStyle="italic">
                Brak pozycji kosztorysu w tym projekcie.
              </Text>
            )}
          </FormControl>

          <Divider my={3} />

          {/* === Zakres pracy === */}
          <FormControl isDisabled={isWorkLockedByItem}>
            <FormLabel fontSize="xs" mb={1} fontWeight="semibold" color="neutral.600">
              Zakres pracy (harmonogram)
            </FormLabel>
            <Select
              size="sm"
              value={selectedWorkId ?? ""}
              onChange={(e) => handleWorkSelect(e.target.value || null)}
              placeholder="— brak powiązania —"
              bg="white"
            >
              {filteredWorkItems.map((item) => (
                <option key={item.workId} value={item.workId}>
                  {item.path}{item.linkedItemId ? " ⚡" : ""}
                </option>
              ))}
            </Select>
            {selectedWorkId && resolvedWorkPath && (
              <Text fontSize="xs" color="purple.700" mt={1} noOfLines={2}>
                📂 {resolvedWorkPath}
              </Text>
            )}
            {isWorkLockedByItem && !selectedWorkId && (
              <Alert status="info" borderRadius="sm" py={1} px={2} mt={1}>
                <AlertIcon boxSize={3} />
                <Text fontSize="xs">Wybrana pozycja nie ma spiętego zakresu — odepnij pozycję, aby powiązać zakres.</Text>
              </Alert>
            )}
            {filteredWorkItems.length === 0 && !isWorkLockedByItem && (
              <Text fontSize="xs" color="neutral.400" mt={1} fontStyle="italic">
                Brak zakresów pracy w harmonogramach tego projektu.
              </Text>
            )}
          </FormControl>
        </VStack>
      )}

      {isLinked && (
        <Text fontSize="xs" color="neutral.500" mt={3}>
          ⚡ oznacza wzajemne spięcie pozycji z zakresem pracy.
        </Text>
      )}
    </Box>
  );
}
