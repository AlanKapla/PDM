import { useState } from "react";
import {
  Drawer,
  DrawerOverlay,
  DrawerContent,
  DrawerHeader,
  DrawerBody,
  DrawerFooter,
  DrawerCloseButton,
  VStack,
  HStack,
  Text,
  IconButton,
  Button,
  Select,
  NumberInput,
  NumberInputField,
  NumberInputStepper,
  NumberIncrementStepper,
  NumberDecrementStepper,
  Box,
  Badge,
  useColorModeValue,
} from "@chakra-ui/react";
import { Plus, X } from "lucide-react";
import { useGantt } from "../GanttContext";
import { WorkDependencyType, WorkDependencyTypeLabels } from "../../../types/workSchedule.types";
import type { WorkScheduleWorkDependencyWeb, WorkScheduleStageWeb } from "../../../types/workSchedule.types";

interface DependenciesModalProps {
  isOpen: boolean;
  onClose: () => void;
}

interface DependencyRow {
  tempKey: string;
  predecessorWorkId: string;
  successorWorkId: string;
  dependencyType: WorkDependencyType;
  lagDays: number;
}

function collectAllWorks(stages: WorkScheduleStageWeb[]) {
  const works: { id: string; name: string; stageName: string }[] = [];
  function walk(ss: WorkScheduleStageWeb[]) {
    for (const s of ss) {
      for (const w of s.works ?? []) {
        works.push({ id: w.id, name: w.name, stageName: s.name });
      }
      walk(s.childStages ?? []);
    }
  }
  walk(stages);
  return works;
}

export default function DependenciesModal({ isOpen, onClose }: DependenciesModalProps) {
  const { schedule, setDependencies, isMutating } = useGantt();

  const [rows, setRows] = useState<DependencyRow[]>(
    (schedule?.dependencies ?? []).map(d => ({
      tempKey: d.id,
      predecessorWorkId: d.predecessorWorkId,
      successorWorkId: d.successorWorkId,
      dependencyType: d.dependencyType,
      lagDays: d.lagDays,
    }))
  );
  const [isSubmitting, setIsSubmitting] = useState(false);

  const allWorks = collectAllWorks(schedule?.stages ?? []);
  const borderColor = useColorModeValue("gray.100", "gray.700");

  const addRow = () => {
    if (allWorks.length < 2) return;
    setRows(prev => [
      ...prev,
      {
        tempKey: `new-${Date.now()}`,
        predecessorWorkId: allWorks[0].id,
        successorWorkId: allWorks[1].id,
        dependencyType: WorkDependencyType.FinishToStart,
        lagDays: 0,
      },
    ]);
  };

  const removeRow = (key: string) => setRows(prev => prev.filter(r => r.tempKey !== key));

  const updateRow = <K extends keyof DependencyRow>(key: string, field: K, value: DependencyRow[K]) => {
    setRows(prev => prev.map(r => (r.tempKey === key ? { ...r, [field]: value } : r)));
  };

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      const payload: WorkScheduleWorkDependencyWeb[] = rows.map(r => ({
        id: r.tempKey,
        predecessorWorkId: r.predecessorWorkId,
        successorWorkId: r.successorWorkId,
        dependencyType: r.dependencyType,
        lagDays: r.lagDays,
      }));
      await setDependencies(payload);
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Drawer isOpen={isOpen} onClose={onClose} placement="bottom" size="full">
      <DrawerOverlay />
      <DrawerContent>
        <DrawerCloseButton />
        <DrawerHeader borderBottomWidth="1px">Zależności harmonogramu</DrawerHeader>
        <DrawerBody py={4} overflowY="auto">
          <VStack spacing={3} align="stretch">
            {rows.length === 0 && (
              <Text fontSize="sm" color="neutral.400" textAlign="center" py={4}>
                Brak zdefiniowanych zależności
              </Text>
            )}
            {rows.map((row, idx) => (
              <Box
                key={row.tempKey}
                borderWidth="1px"
                borderColor={borderColor}
                borderRadius="md"
                p={3}
              >
                <HStack justify="space-between" mb={2}>
                  <Badge colorScheme="gray">Zależność {idx + 1}</Badge>
                  <IconButton
                    aria-label="Usuń zależność"
                    icon={<X size={12} />}
                    size="xs"
                    variant="ghost"
                    colorScheme="red"
                    onClick={() => removeRow(row.tempKey)}
                  />
                </HStack>
                <VStack spacing={2} align="stretch">
                  <HStack spacing={2} flexWrap="wrap">
                    <Box flex={1} minW="140px">
                      <Text fontSize="xs" fontWeight="medium" mb={1}>Poprzednik</Text>
                      <Select
                        size="sm"
                        value={row.predecessorWorkId}
                        onChange={e => updateRow(row.tempKey, "predecessorWorkId", e.target.value)}
                      >
                        {allWorks.map(w => (
                          <option key={w.id} value={w.id}>{w.stageName} / {w.name}</option>
                        ))}
                      </Select>
                    </Box>
                    <Box flex={1} minW="140px">
                      <Text fontSize="xs" fontWeight="medium" mb={1}>Następnik</Text>
                      <Select
                        size="sm"
                        value={row.successorWorkId}
                        onChange={e => updateRow(row.tempKey, "successorWorkId", e.target.value)}
                      >
                        {allWorks.map(w => (
                          <option key={w.id} value={w.id}>{w.stageName} / {w.name}</option>
                        ))}
                      </Select>
                    </Box>
                  </HStack>
                  <HStack spacing={2} flexWrap="wrap">
                    <Box flex={2} minW="160px">
                      <Text fontSize="xs" fontWeight="medium" mb={1}>Typ</Text>
                      <Select
                        size="sm"
                        value={row.dependencyType}
                        onChange={e => updateRow(row.tempKey, "dependencyType", Number(e.target.value) as WorkDependencyType)}
                      >
                        {Object.entries(WorkDependencyTypeLabels).map(([val, label]) => (
                          <option key={val} value={val}>{label}</option>
                        ))}
                      </Select>
                    </Box>
                    <Box flex={1} minW="100px">
                      <Text fontSize="xs" fontWeight="medium" mb={1}>Lag (dni)</Text>
                      <NumberInput
                        size="sm"
                        value={row.lagDays}
                        min={-365}
                        max={365}
                        onChange={(_, v) => updateRow(row.tempKey, "lagDays", isNaN(v) ? 0 : v)}
                      >
                        <NumberInputField />
                        <NumberInputStepper>
                          <NumberIncrementStepper />
                          <NumberDecrementStepper />
                        </NumberInputStepper>
                      </NumberInput>
                    </Box>
                  </HStack>
                </VStack>
              </Box>
            ))}
            <Button
              leftIcon={<Plus size={14} />}
              size="sm"
              variant="ghost"
              colorScheme="gray"
              onClick={addRow}
              isDisabled={allWorks.length < 2}
            >
              Dodaj zależność
            </Button>
          </VStack>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px" gap={2}>
          <Button variant="ghost" colorScheme="gray" onClick={onClose}>Anuluj</Button>
          <Button
            colorScheme="primary"
            onClick={handleSave}
            isLoading={isSubmitting || isMutating.has("setDependencies")}
          >
            Zapisz zależności
          </Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
