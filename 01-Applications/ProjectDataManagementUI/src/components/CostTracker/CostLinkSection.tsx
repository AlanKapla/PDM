import {
  Box,
  FormControl,
  FormLabel,
  Select,
  Text,
  HStack,
  Badge,
  Button,
  VStack,
  Divider,
  Spinner,
  Alert,
  AlertIcon,
} from "@chakra-ui/react";
import { Link2, Link2Off, Pencil } from "lucide-react";
import { useState } from "react";
import { useCostLinkOptions } from "../../hooks/queries";
import type {
  EstimateItemLinkOptionWeb,
  WorkLinkOptionWeb,
} from "../../types/costTracker.types";

// Minimalne interfejsy strukturalne — USUNIĘTE: zastąpione przez CostLinkOptionsWeb z API

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
  const [isChangingEstimate, setIsChangingEstimate] = useState(false);
  const [isChangingWork, setIsChangingWork] = useState(false);

  const isChangingAny = isChangingEstimate || isChangingWork;

  const { data: linkOptions, isLoading } = useCostLinkOptions(
    tenantId,
    projectId,
    isChangingAny
  );

  const estimateItems: EstimateItemLinkOptionWeb[] = linkOptions?.estimateItems ?? [];
  const workItems: WorkLinkOptionWeb[] = linkOptions?.workItems ?? [];

  // --- Reguły powiązania ---
  const selectedItem = estimateItems.find((i) => i.itemId === selectedItemId) ?? null;
  const selectedWork = workItems.find((w) => w.workId === selectedWorkId) ?? null;

  // Pozycja wybrana ale bez spięcia → blokuj zakres
  const isWorkLockedByItem =
    selectedItemId !== null &&
    (selectedItem === null || selectedItem.linkedWorkId === null);

  // Zakres wybrany ale bez spięcia → blokuj pozycję
  const isItemLockedByWork =
    selectedWorkId !== null &&
    (selectedWork === null || selectedWork.linkedItemId === null);

  // Gdy pozycja ma spięcie → w dropdownie zakresu pokazuj tylko ten jeden zakres
  const filteredWorkItems: WorkLinkOptionWeb[] =
    selectedItemId !== null && selectedItem?.linkedWorkId != null
      ? workItems.filter((w) => w.workId === selectedItem!.linkedWorkId)
      : workItems;

  // Gdy zakres ma spięcie → w dropdownie pozycji pokazuj tylko tę jedną pozycję
  const filteredEstimateItems: EstimateItemLinkOptionWeb[] =
    selectedWorkId !== null && selectedWork?.linkedItemId != null
      ? estimateItems.filter((i) => i.itemId === selectedWork!.linkedItemId)
      : estimateItems;

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
    setIsChangingEstimate(false);
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
    setIsChangingWork(false);
  };

  const handleDetachAll = () => {
    onChange(null);
    onWorkChange(null);
    setIsChangingEstimate(false);
    setIsChangingWork(false);
  };

  const isLinked = !!selectedItemId || !!selectedWorkId;

  return (
    <Box border="1px solid" borderColor="neutral.200" borderRadius="md" p={3} bg={isLinked ? "blue.50" : "neutral.50"}>
      <HStack mb={3} justify="space-between">
        <HStack spacing={1}>
          <Link2 size={14} />
          <Text fontWeight="medium" fontSize="sm">Powiązanie kosztu</Text>
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

      {/* === Sekcja: Pozycja kosztorysu === */}
      <VStack align="stretch" spacing={1} mb={3}>
        <HStack justify="space-between">
          <FormLabel fontSize="xs" mb={0} fontWeight="semibold" color="neutral.600">
            Pozycja kosztorysu
          </FormLabel>
          {selectedItemId && !isChangingEstimate && (
            <HStack spacing={1}>
              {!isItemLockedByWork && (
                <Button size="xs" variant="link" colorScheme="blue" leftIcon={<Pencil size={10} />}
                  onClick={() => setIsChangingEstimate(true)}>
                  Zmień
                </Button>
              )}
              <Button size="xs" variant="link" colorScheme="red"
                onClick={() => handleItemSelect(null)}>
                Odepnij
              </Button>
            </HStack>
          )}
          {!selectedItemId && !isChangingEstimate && !isItemLockedByWork && (
            <Button size="xs" variant="link" colorScheme="blue"
              onClick={() => setIsChangingEstimate(true)}>
              Powiąż
            </Button>
          )}
          {isChangingEstimate && (
            <Button size="xs" variant="link" colorScheme="gray"
              onClick={() => setIsChangingEstimate(false)}>
              Anuluj
            </Button>
          )}
        </HStack>

        {isItemLockedByWork && !selectedItemId && (
          <Alert status="info" borderRadius="sm" py={1} px={2}>
            <AlertIcon boxSize={3} />
            <Text fontSize="xs">Wybrany zakres nie ma spiętej pozycji — odepnij zakres, aby powiązać pozycję.</Text>
          </Alert>
        )}

        {selectedItemId && !isChangingEstimate && (
          currentEstimatePath
            ? <Badge colorScheme="blue" fontSize="xs" alignSelf="flex-start">{currentEstimatePath}</Badge>
            : <Badge colorScheme="orange" fontSize="xs" alignSelf="flex-start">Powiązana pozycja (ścieżka niedostępna)</Badge>
        )}

        {isChangingEstimate && (
          <FormControl>
            {isLoading ? (
              <HStack spacing={2}>
                <Spinner size="xs" />
                <Text fontSize="xs" color="neutral.500">Ładowanie pozycji…</Text>
              </HStack>
            ) : (
              <Select
                size="sm"
                value={selectedItemId ?? ""}
                onChange={(e) => handleItemSelect(e.target.value || null)}
                placeholder="— wybierz pozycję —"
                autoFocus
              >
                {filteredEstimateItems.map((item) => (
                  <option key={item.itemId} value={item.itemId}>
                    {item.path}{item.linkedWorkId ? " ⚡" : ""}
                  </option>
                ))}
              </Select>
            )}
          </FormControl>
        )}
      </VStack>

      <Divider />

      {/* === Sekcja: Zakres pracy === */}
      <VStack align="stretch" spacing={1} mt={3}>
        <HStack justify="space-between">
          <FormLabel fontSize="xs" mb={0} fontWeight="semibold" color="neutral.600">
            Zakres pracy (harmonogram)
          </FormLabel>
          {selectedWorkId && !isChangingWork && (
            <HStack spacing={1}>
              {!isWorkLockedByItem && (
                <Button size="xs" variant="link" colorScheme="blue" leftIcon={<Pencil size={10} />}
                  onClick={() => setIsChangingWork(true)}>
                  Zmień
                </Button>
              )}
              <Button size="xs" variant="link" colorScheme="red"
                onClick={() => handleWorkSelect(null)}>
                Odepnij
              </Button>
            </HStack>
          )}
          {!selectedWorkId && !isChangingWork && !isWorkLockedByItem && (
            <Button size="xs" variant="link" colorScheme="blue"
              onClick={() => setIsChangingWork(true)}>
              Powiąż
            </Button>
          )}
          {isChangingWork && (
            <Button size="xs" variant="link" colorScheme="gray"
              onClick={() => setIsChangingWork(false)}>
              Anuluj
            </Button>
          )}
        </HStack>

        {isWorkLockedByItem && !selectedWorkId && (
          <Alert status="info" borderRadius="sm" py={1} px={2}>
            <AlertIcon boxSize={3} />
            <Text fontSize="xs">Wybrana pozycja nie ma spiętego zakresu — odepnij pozycję, aby powiązać zakres.</Text>
          </Alert>
        )}

        {selectedWorkId && !isChangingWork && (
          currentWorkPath
            ? <Badge colorScheme="purple" fontSize="xs" alignSelf="flex-start">{currentWorkPath}</Badge>
            : <Badge colorScheme="orange" fontSize="xs" alignSelf="flex-start">Powiązany zakres (ścieżka niedostępna)</Badge>
        )}

        {isChangingWork && (
          <FormControl>
            {isLoading ? (
              <HStack spacing={2}>
                <Spinner size="xs" />
                <Text fontSize="xs" color="neutral.500">Ładowanie zakresów pracy…</Text>
              </HStack>
            ) : (
              <Select
                size="sm"
                value={selectedWorkId ?? ""}
                onChange={(e) => handleWorkSelect(e.target.value || null)}
                placeholder="— wybierz zakres —"
                autoFocus
              >
                {filteredWorkItems.map((item) => (
                  <option key={item.workId} value={item.workId}>
                    {item.path}{item.linkedItemId ? " ⚡" : ""}
                  </option>
                ))}
              </Select>
            )}
          </FormControl>
        )}
      </VStack>
    </Box>
  );
}
