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
  FormControl,
  FormLabel,
  Select,
  Button,
  Text,
  NumberInput,
  NumberInputField,
  NumberInputStepper,
  NumberIncrementStepper,
  NumberDecrementStepper,
} from "@chakra-ui/react";
import { useGantt } from "../GanttContext";
import type { WorkScheduleStageWeb } from "../../../types/workSchedule.types";

function collectAllStages(stages: WorkScheduleStageWeb[]): WorkScheduleStageWeb[] {
  return stages.flatMap(s => [s, ...collectAllStages(s.childStages ?? [])]);
}

function findWorkName(stages: WorkScheduleStageWeb[], workId: string): string {
  for (const s of stages) {
    const w = s.works?.find(w => w.id === workId);
    if (w) return w.name;
    const found = findWorkName(s.childStages ?? [], workId);
    if (found) return found;
  }
  return workId;
}

interface MoveWorkModalProps {
  isOpen: boolean;
  onClose: () => void;
  stageId: string;
  workId: string;
}

export default function MoveWorkModal({ isOpen, onClose, stageId, workId }: MoveWorkModalProps) {
  const { schedule, moveWork } = useGantt();
  const [targetStageId, setTargetStageId] = useState(stageId);
  const [targetOrder, setTargetOrder] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const allStages = collectAllStages(schedule?.stages ?? []);
  const workName = findWorkName(schedule?.stages ?? [], workId);

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      await moveWork(stageId, workId, targetStageId, targetOrder);
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Drawer isOpen={isOpen} onClose={onClose} placement="bottom">
      <DrawerOverlay />
      <DrawerContent borderTopRadius="lg">
        <DrawerCloseButton />
        <DrawerHeader borderBottomWidth="1px">Przenieś zakres pracy</DrawerHeader>
        <DrawerBody py={4}>
          <VStack spacing={4}>
            <Text fontSize="sm" color="gray.600">
              Przenosisz: <strong>{workName}</strong>
            </Text>
            <FormControl isRequired>
              <FormLabel fontSize="sm">Docelowy etap</FormLabel>
              <Select
                value={targetStageId}
                onChange={e => setTargetStageId(e.target.value)}
              >
                {allStages.map(s => (
                  <option key={s.id} value={s.id}>{s.name || "(bez nazwy)"}</option>
                ))}
              </Select>
            </FormControl>
            <FormControl>
              <FormLabel fontSize="sm">Pozycja (0-based)</FormLabel>
              <NumberInput
                value={targetOrder}
                min={0}
                onChange={(_, v) => setTargetOrder(isNaN(v) ? 0 : v)}
              >
                <NumberInputField />
                <NumberInputStepper>
                  <NumberIncrementStepper />
                  <NumberDecrementStepper />
                </NumberInputStepper>
              </NumberInput>
            </FormControl>
          </VStack>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px" gap={2}>
          <Button variant="outline" onClick={onClose}>Anuluj</Button>
          <Button colorScheme="primary" onClick={handleSave} isLoading={isSubmitting}>Przenieś</Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
