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
} from "@chakra-ui/react";
import { useGantt } from "../GanttContext";
import type { WorkScheduleStageWeb } from "../../../types/workSchedule.types";

interface MoveStageModalProps {
  isOpen: boolean;
  onClose: () => void;
  stageId: string;
}

function collectAllStages(stages: WorkScheduleStageWeb[], excludeId: string): WorkScheduleStageWeb[] {
  return stages.flatMap(s => {
    if (s.id === excludeId) return [];
    return [s, ...collectAllStages(s.childStages ?? [], excludeId)];
  });
}

function getStageById(stages: WorkScheduleStageWeb[], id: string): WorkScheduleStageWeb | null {
  for (const s of stages) {
    if (s.id === id) return s;
    const found = getStageById(s.childStages ?? [], id);
    if (found) return found;
  }
  return null;
}

export default function MoveStageModal({ isOpen, onClose, stageId }: MoveStageModalProps) {
  const { schedule, moveStage } = useGantt();
  const [newParentId, setNewParentId] = useState<string>("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const stage = getStageById(schedule?.stages ?? [], stageId);
  const availableParents = collectAllStages(schedule?.stages ?? [], stageId);

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      await moveStage(stageId, newParentId || null);
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
        <DrawerHeader borderBottomWidth="1px">Przenieś etap</DrawerHeader>
        <DrawerBody py={4}>
          <VStack spacing={4}>
            <Text fontSize="sm" color="neutral.600">
              Przenosisz: <strong>{stage?.name ?? stageId}</strong>
            </Text>
            <FormControl>
              <FormLabel fontSize="sm">Nowy etap nadrzędny</FormLabel>
              <Select
                value={newParentId}
                onChange={e => setNewParentId(e.target.value)}
                placeholder="Poziom główny (brak rodzica)"
              >
                {availableParents.map(s => (
                  <option key={s.id} value={s.id}>{s.name || "(bez nazwy)"}</option>
                ))}
              </Select>
            </FormControl>
          </VStack>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px" gap={2}>
          <Button variant="ghost" colorScheme="gray" onClick={onClose}>Anuluj</Button>
          <Button colorScheme="primary" onClick={handleSave} isLoading={isSubmitting}>Przenieś</Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
