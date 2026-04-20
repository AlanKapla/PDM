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
  Button,
  Box,
  HStack,
  Text,
} from "@chakra-ui/react";
import { useGantt } from "../GanttContext";
import type { WorkScheduleStageWorkWeb } from "../../../types/workSchedule.types";

interface WorkFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  stageId: string;
  /** Gdy podano — tryb edycji: zmiana nazwy + koloru istniejącego zakresu */
  editWork?: WorkScheduleStageWorkWeb;
}

const PRESET_COLORS = [
  "#3182CE", "#38A169", "#805AD5", "#D69E2E", "#E53E3E",
  "#00B5D8", "#DD6B20", "#319795", "#D53F8C", "#4A5568",
];

export default function WorkFormModal({ isOpen, onClose, stageId, editWork }: WorkFormModalProps) {
  const { addWork, renameWork, setWorkColor } = useGantt();
  const isEditMode = Boolean(editWork);
  const [name, setName] = useState(editWork?.name ?? "");
  const [color, setColor] = useState(editWork?.colorRgb ?? "#3182CE");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!name.trim()) return;
    setIsSubmitting(true);
    try {
      if (isEditMode && editWork) {
        // Zapisuj tylko zmienione pola — optimistic update + debounce w kontekście
        if (name.trim() !== editWork.name) {
          await renameWork(stageId, editWork.id, name.trim());
        }
        if (color !== editWork.colorRgb) {
          await setWorkColor(stageId, editWork.id, color);
        }
      } else {
        await addWork(stageId, name.trim(), color);
        setName("");
        setColor("#3182CE");
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
          {isEditMode ? "Edytuj zakres pracy" : "Dodaj zakres pracy"}
        </DrawerHeader>
        <DrawerBody py={4}>
          <VStack spacing={4}>
            <FormControl isRequired>
              <FormLabel fontSize="sm">Nazwa zakresu pracy</FormLabel>
              <Input
                value={name}
                onChange={e => setName(e.target.value)}
                onKeyDown={e => e.key === "Enter" && handleSubmit()}
                placeholder="Np. Wylewka betonu"
                autoFocus
              />
            </FormControl>

            <FormControl>
              <FormLabel fontSize="sm">Kolor</FormLabel>
              <VStack align="start" spacing={2}>
                <HStack spacing={2} flexWrap="wrap">
                  {PRESET_COLORS.map(c => (
                    <Box
                      key={c}
                      w="24px"
                      h="24px"
                      borderRadius="full"
                      bg={c}
                      cursor="pointer"
                      border={color === c ? "3px solid" : "2px solid transparent"}
                      borderColor={color === c ? "gray.800" : "transparent"}
                      onClick={() => setColor(c)}
                    />
                  ))}
                </HStack>
                <HStack spacing={2}>
                  <input
                    type="color"
                    value={color}
                    onChange={e => setColor(e.target.value)}
                    style={{ width: 32, height: 32, border: "none", padding: 0, cursor: "pointer", borderRadius: 4 }}
                  />
                  <Text fontSize="xs" color="gray.500">{color}</Text>
                </HStack>
              </VStack>
            </FormControl>
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
