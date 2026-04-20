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
  Input,
  Select,
  Button,
} from "@chakra-ui/react";
import { useGantt, findStageInTree } from "../GanttContext";
import type { WorkScheduleStageWeb } from "../../../types/workSchedule.types";

interface StageFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  /** Tryb dodawania: id etapu nadrzędnego (undefined = poziom główny) */
  parentStageId?: string;
  /** Tryb edycji: id etapu do zmiany nazwy */
  renameStageId?: string;
  /** Tryb edycji: bieżąca nazwa etapu */
  initialName?: string;
}

function collectAllStages(stages: WorkScheduleStageWeb[]): WorkScheduleStageWeb[] {
  return stages.flatMap(s => [s, ...collectAllStages(s.childStages ?? [])]);
}

export default function StageFormModal({ isOpen, onClose, parentStageId, renameStageId, initialName }: StageFormModalProps) {
  const { addStage, renameStage, schedule } = useGantt();
  const isEditMode = Boolean(renameStageId);
  const [name, setName] = useState(initialName ?? "");
  const [parent, setParent] = useState(parentStageId ?? "");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const allStages = collectAllStages(schedule?.stages ?? []);

  const handleSubmit = async () => {
    if (!name.trim()) return;
    setIsSubmitting(true);
    try {
      if (isEditMode && renameStageId) {
        // Optimistic update + debounce obsługuje GanttContext.renameStage
        await renameStage(renameStageId, name.trim());
      } else {
        await addStage(name.trim(), parent || null);
        setName("");
        setParent("");
      }
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
        <DrawerHeader borderBottomWidth="1px">
          {isEditMode ? "Zmień nazwę etapu" : "Dodaj etap"}
        </DrawerHeader>
        <DrawerBody py={4}>
          <VStack spacing={4}>
            <FormControl isRequired>
              <FormLabel fontSize="sm">Nazwa etapu</FormLabel>
              <Input
                value={name}
                onChange={e => setName(e.target.value)}
                onKeyDown={e => e.key === "Enter" && handleSubmit()}
                placeholder={isEditMode ? "Nowa nazwa etapu" : "Nazwa nowego etapu"}
                autoFocus
              />
            </FormControl>
            {!isEditMode && (
              <FormControl>
                <FormLabel fontSize="sm">Etap nadrzędny (opcjonalnie)</FormLabel>
                <Select
                  value={parent}
                  onChange={e => setParent(e.target.value)}
                  placeholder="Poziom główny"
                >
                  {allStages.map(s => (
                    <option key={s.id} value={s.id}>{s.name || "(bez nazwy)"}</option>
                  ))}
                </Select>
              </FormControl>
            )}
          </VStack>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px" gap={2}>
          <Button variant="outline" onClick={onClose}>Anuluj</Button>
          <Button colorScheme="green" onClick={handleSubmit} isLoading={isSubmitting} isDisabled={!name.trim()}>
            {isEditMode ? "Zapisz" : "Dodaj"}
          </Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
