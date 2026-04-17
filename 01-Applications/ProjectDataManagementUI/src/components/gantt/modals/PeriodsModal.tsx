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
  Checkbox,
  Divider,
  Switch,
  FormControl,
  FormLabel,
  useColorModeValue,
} from "@chakra-ui/react";
import { Plus, X } from "lucide-react";
import { useGantt } from "../GanttContext";
import type { WorkScheduleStageWorkWeb, WorkScheduleStageWorkPeriodWeb } from "../../../types/workSchedule.types";

interface PeriodsModalProps {
  isOpen: boolean;
  onClose: () => void;
  stageId: string;
  work: WorkScheduleStageWorkWeb;
}

interface PeriodRow {
  tempKey: string;
  startDate: string;
  endDate: string;
  isClosed: boolean;
}

function periodToPeriodRow(p: WorkScheduleStageWorkPeriodWeb, idx: number): PeriodRow {
  return {
    tempKey: p.id ?? `new-${idx}`,
    startDate: p.startDate.slice(0, 10),
    endDate: p.endDate.slice(0, 10),
    isClosed: p.isClosed,
  };
}

export default function PeriodsModal({ isOpen, onClose, stageId, work }: PeriodsModalProps) {
  const { setPeriods, setWorkIsClosed } = useGantt();
  const [periods, setPeriodRows] = useState<PeriodRow[]>(
    (work.periods ?? []).map((p, i) => periodToPeriodRow(p, i))
  );
  const [isWorkClosed, setIsWorkClosed] = useState(work.isClosed);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const borderColor = useColorModeValue("gray.100", "gray.700");

  const addPeriod = () => {
    const today = new Date().toISOString().slice(0, 10);
    setPeriodRows(prev => [
      ...prev,
      { tempKey: `new-${Date.now()}`, startDate: today, endDate: today, isClosed: false },
    ]);
  };

  const removePeriod = (key: string) => {
    setPeriodRows(prev => prev.filter(p => p.tempKey !== key));
  };

  const updatePeriod = (key: string, field: keyof PeriodRow, value: string | boolean) => {
    setPeriodRows(prev => prev.map(p => (p.tempKey === key ? { ...p, [field]: value } : p)));
  };

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      const payloadPeriods = periods.map(p => ({
        startDate: p.startDate,
        endDate: p.endDate,
        isClosed: p.isClosed,
      }));
      await setPeriods(stageId, work.id, payloadPeriods);
      if (isWorkClosed !== work.isClosed) {
        await setWorkIsClosed(stageId, work.id, isWorkClosed);
      }
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Drawer isOpen={isOpen} onClose={onClose} placement="bottom" size="md">
      <DrawerOverlay />
      <DrawerContent borderTopRadius="lg" maxH="80vh">
        <DrawerCloseButton />
        <DrawerHeader borderBottomWidth="1px">Okresy — {work.name}</DrawerHeader>
        <DrawerBody py={4} overflowY="auto">
          <VStack spacing={4} align="stretch">
            {/* Ogólny status zamknięcia pracy */}
            <HStack justify="space-between">
              <Text fontSize="sm" fontWeight="medium">Zakres zamknięty</Text>
              <Switch
                isChecked={isWorkClosed}
                onChange={e => setIsWorkClosed(e.target.checked)}
                colorScheme="green"
              />
            </HStack>
            <Divider />

            {/* Lista okresów */}
            {periods.map((period, idx) => (
              <VStack
                key={period.tempKey}
                align="stretch"
                borderWidth="1px"
                borderColor={borderColor}
                borderRadius="md"
                p={3}
                spacing={2}
              >
                <HStack justify="space-between">
                  <Text fontSize="xs" fontWeight="semibold" color="gray.500">Okres {idx + 1}</Text>
                  <IconButton
                    aria-label="Usuń okres"
                    icon={<X size={12} />}
                    size="xs"
                    variant="ghost"
                    colorScheme="red"
                    onClick={() => removePeriod(period.tempKey)}
                  />
                </HStack>
                <HStack spacing={3}>
                  <FormControl flex={1}>
                    <FormLabel fontSize="xs">Od</FormLabel>
                    <input
                      type="date"
                      value={period.startDate}
                      onChange={e => updatePeriod(period.tempKey, "startDate", e.target.value)}
                      style={{ width: "100%", padding: "6px 8px", borderRadius: 6, border: "1px solid #CBD5E0", fontSize: 13 }}
                    />
                  </FormControl>
                  <FormControl flex={1}>
                    <FormLabel fontSize="xs">Do</FormLabel>
                    <input
                      type="date"
                      value={period.endDate}
                      min={period.startDate}
                      onChange={e => updatePeriod(period.tempKey, "endDate", e.target.value)}
                      style={{ width: "100%", padding: "6px 8px", borderRadius: 6, border: "1px solid #CBD5E0", fontSize: 13 }}
                    />
                  </FormControl>
                </HStack>
                <Checkbox
                  isChecked={period.isClosed}
                  onChange={e => updatePeriod(period.tempKey, "isClosed", e.target.checked)}
                  colorScheme="green"
                  size="sm"
                >
                  Okres zamknięty
                </Checkbox>
              </VStack>
            ))}

            <Button leftIcon={<Plus size={14} />} size="sm" variant="outline" onClick={addPeriod}>
              Dodaj okres
            </Button>
          </VStack>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px" gap={2}>
          <Button variant="outline" onClick={onClose}>Anuluj</Button>
          <Button colorScheme="primary" onClick={handleSave} isLoading={isSubmitting}>Zapisz</Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
