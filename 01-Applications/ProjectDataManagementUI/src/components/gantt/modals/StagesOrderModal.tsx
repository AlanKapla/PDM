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
  Box,
  Button,
  useColorModeValue,
} from "@chakra-ui/react";
import { GripVertical } from "lucide-react";
import { DndContext, closestCenter, type DragEndEvent } from "@dnd-kit/core";
import { SortableContext, useSortable, verticalListSortingStrategy, arrayMove } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useGantt } from "../GanttContext";
import type { WorkScheduleStageWeb } from "../../../types/workSchedule.types";

interface SortableStageItemProps {
  stage: WorkScheduleStageWeb;
}

function SortableStageItem({ stage }: SortableStageItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: stage.id });
  const borderColor = useColorModeValue("gray.200", "gray.600");

  return (
    <Box
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      borderWidth="1px"
      borderColor={borderColor}
      borderRadius="md"
      p={2}
      bg={isDragging ? useColorModeValue("neutral.50", "neutral.800") : useColorModeValue("white", "gray.800")}
      opacity={isDragging ? 0.8 : 1}
    >
      <HStack spacing={2}>
        <Box {...attributes} {...listeners} cursor="grab" color="neutral.400">
          <GripVertical size={16} />
        </Box>
        <Text fontSize="sm">{stage.name || "(bez nazwy)"}</Text>
      </HStack>
    </Box>
  );
}

interface StagesOrderModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function StagesOrderModal({ isOpen, onClose }: StagesOrderModalProps) {
  const { schedule, reorderStages } = useGantt();
  const [stages, setStages] = useState<WorkScheduleStageWeb[]>(() =>
    [...(schedule?.stages ?? [])].sort((a, b) => a.order - b.order)
  );
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIdx = stages.findIndex(s => s.id === active.id);
    const newIdx = stages.findIndex(s => s.id === over.id);
    setStages(prev => arrayMove(prev, oldIdx, newIdx));
  };

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      await reorderStages(stages.map(s => s.id));
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Drawer isOpen={isOpen} onClose={onClose} placement="bottom" size="md">
      <DrawerOverlay />
      <DrawerContent borderTopRadius="lg" maxH="70vh">
        <DrawerCloseButton />
        <DrawerHeader borderBottomWidth="1px">Kolejność etapów</DrawerHeader>
        <DrawerBody py={4} overflowY="auto">
          <DndContext collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
            <SortableContext items={stages.map(s => s.id)} strategy={verticalListSortingStrategy}>
              <VStack spacing={2}>
                {stages.map(stage => (
                  <SortableStageItem key={stage.id} stage={stage} />
                ))}
              </VStack>
            </SortableContext>
          </DndContext>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px" gap={2}>
          <Button variant="ghost" colorScheme="gray" onClick={onClose}>Anuluj</Button>
          <Button colorScheme="primary" onClick={handleSave} isLoading={isSubmitting}>Zapisz kolejność</Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
