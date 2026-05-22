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
import type { WorkScheduleStageWeb, WorkScheduleStageWorkWeb } from "../../../types/workSchedule.types";

function findStage(stages: WorkScheduleStageWeb[], stageId: string): WorkScheduleStageWeb | null {
  for (const s of stages) {
    if (s.id === stageId) return s;
    const found = findStage(s.childStages ?? [], stageId);
    if (found) return found;
  }
  return null;
}

interface SortableWorkItemProps {
  work: WorkScheduleStageWorkWeb;
}

function SortableWorkItem({ work }: SortableWorkItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: work.id });
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
        <Box w="10px" h="10px" borderRadius="full" bg={work.colorRgb} />
        <Text fontSize="sm">{work.name || "(bez nazwy)"}</Text>
      </HStack>
    </Box>
  );
}

interface WorksOrderModalProps {
  isOpen: boolean;
  onClose: () => void;
  stageId: string;
}

export default function WorksOrderModal({ isOpen, onClose, stageId }: WorksOrderModalProps) {
  const { schedule, reorderWorks } = useGantt();
  const stage = findStage(schedule?.stages ?? [], stageId);
  const [works, setWorks] = useState<WorkScheduleStageWorkWeb[]>(() =>
    [...(stage?.works ?? [])].sort((a, b) => a.order - b.order)
  );
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIdx = works.findIndex(w => w.id === active.id);
    const newIdx = works.findIndex(w => w.id === over.id);
    setWorks(prev => arrayMove(prev, oldIdx, newIdx));
  };

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      await reorderWorks(stageId, works.map(w => w.id));
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
        <DrawerHeader borderBottomWidth="1px">Kolejność zakresów pracy</DrawerHeader>
        <DrawerBody py={4} overflowY="auto">
          <DndContext collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
            <SortableContext items={works.map(w => w.id)} strategy={verticalListSortingStrategy}>
              <VStack spacing={2}>
                {works.map(work => (
                  <SortableWorkItem key={work.id} work={work} />
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
