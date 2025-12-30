import { useState, useEffect } from "react";
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalCloseButton,
  ModalFooter,
  VStack,
  HStack,
  Text,
  Button,
  Input,
  FormControl,
  FormLabel,
  useColorModeValue,
  useToast,
  Box,
  IconButton,
  Divider,
  Badge,
  Flex,
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionPanel,
  AccordionIcon,
  Checkbox,
  Textarea,
} from "@chakra-ui/react";
import { Plus, Trash2, GripVertical } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";

interface CreateWorkScheduleModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  projectName: string;
  members: any[];
  onScheduleCreated?: () => void;
}

interface StageFormData {
  tempId: string;
  name: string;
  order: number;
  works: WorkFormData[];
}

interface WorkPeriodFormData {
  tempId: string;
  startDate: string;
  endDate: string;
  isClosed: boolean;
}

interface WorkCommentFormData {
  tempId: string;
  content: string;
}

interface WorkFormData {
  tempId: string;
  name: string;
  order: number;
  colorRgb: string;
  periods: WorkPeriodFormData[];
  assignedUserIds: string[];
  comments: WorkCommentFormData[];
}

const PRESET_COLORS = [
  "#3182CE", // blue
  "#38A169", // green
  "#DD6B20", // orange
  "#E53E3E", // red
  "#805AD5", // purple
  "#D69E2E", // yellow
  "#00B5D8", // cyan
  "#D53F8C", // pink
  "#319795", // teal
  "#718096", // gray
];

export default function CreateWorkScheduleModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  projectName,
  members,
  onScheduleCreated,
}: CreateWorkScheduleModalProps) {
  const toast = useToast();
  const [scheduleName, setScheduleName] = useState("");
  const [stages, setStages] = useState<StageFormData[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [draggedStage, setDraggedStage] = useState<string | null>(null);
  const [draggedWork, setDraggedWork] = useState<{ stageId: string; workId: string } | null>(null);

  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    if (isOpen) {
      resetForm();
    }
  }, [isOpen]);

  const resetForm = () => {
    setScheduleName("");
    setStages([]);
  };

  const addStage = () => {
    const newStage: StageFormData = {
      tempId: `stage-${Date.now()}`,
      name: "",
      order: stages.length,
      works: [],
    };
    setStages([...stages, newStage]);
  };

  const removeStage = (tempId: string) => {
    const updatedStages = stages
      .filter((s) => s.tempId !== tempId)
      .map((s, index) => ({ ...s, order: index }));
    setStages(updatedStages);
  };

  const updateStageName = (tempId: string, name: string) => {
    setStages(stages.map((s) => (s.tempId === tempId ? { ...s, name } : s)));
  };

  const addWork = (stageTempId: string) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          const today = new Date();
          const tomorrow = new Date(today);
          tomorrow.setDate(tomorrow.getDate() + 1);
          
          const newWork: WorkFormData = {
            tempId: `work-${Date.now()}`,
            name: "",
            order: stage.works.length,
            colorRgb: PRESET_COLORS[stage.works.length % PRESET_COLORS.length],
            periods: [{
              tempId: `period-${Date.now()}`,
              startDate: today.toISOString().split("T")[0],
              endDate: tomorrow.toISOString().split("T")[0],
              isClosed: false,
            }],
            assignedUserIds: [],
            comments: [],
          };
          return { ...stage, works: [...stage.works, newWork] };
        }
        return stage;
      })
    );
  };

  const removeWork = (stageTempId: string, workTempId: string) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          const updatedWorks = stage.works
            .filter((w) => w.tempId !== workTempId)
            .map((w, index) => ({ ...w, order: index }));
          return { ...stage, works: updatedWorks };
        }
        return stage;
      })
    );
  };

  const updateWork = (stageTempId: string, workTempId: string, updates: Partial<WorkFormData>) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          return {
            ...stage,
            works: stage.works.map((w) =>
              w.tempId === workTempId ? { ...w, ...updates } : w
            ),
          };
        }
        return stage;
      })
    );
  };

  const addPeriod = (stageTempId: string, workTempId: string) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          return {
            ...stage,
            works: stage.works.map((work) => {
              if (work.tempId === workTempId) {
                // Get last period's endDate and add 1 day for new startDate
                let newStartDate: string;
                let newEndDate: string;
                
                if (work.periods.length > 0) {
                  const lastPeriod = work.periods[work.periods.length - 1];
                  const lastEndDate = new Date(lastPeriod.endDate);
                  lastEndDate.setDate(lastEndDate.getDate() + 1);
                  newStartDate = lastEndDate.toISOString().split("T")[0];
                  
                  const endDate = new Date(lastEndDate);
                  endDate.setDate(endDate.getDate() + 1);
                  newEndDate = endDate.toISOString().split("T")[0];
                } else {
                  newStartDate = new Date().toISOString().split("T")[0];
                  const endDate = new Date();
                  endDate.setDate(endDate.getDate() + 1);
                  newEndDate = endDate.toISOString().split("T")[0];
                }
                
                const newPeriod: WorkPeriodFormData = {
                  tempId: `period-${Date.now()}`,
                  startDate: newStartDate,
                  endDate: newEndDate,
                  isClosed: false,
                };
                return { ...work, periods: [...work.periods, newPeriod] };
              }
              return work;
            }),
          };
        }
        return stage;
      })
    );
  };

  const removePeriod = (stageTempId: string, workTempId: string, periodTempId: string) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          return {
            ...stage,
            works: stage.works.map((work) => {
              if (work.tempId === workTempId) {
                return {
                  ...work,
                  periods: work.periods.filter((p) => p.tempId !== periodTempId),
                };
              }
              return work;
            }),
          };
        }
        return stage;
      })
    );
  };

  const updatePeriod = (
    stageTempId: string,
    workTempId: string,
    periodTempId: string,
    updates: Partial<WorkPeriodFormData>
  ) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          return {
            ...stage,
            works: stage.works.map((work) => {
              if (work.tempId === workTempId) {
                return {
                  ...work,
                  periods: work.periods.map((p) => {
                    if (p.tempId === periodTempId) {
                      const updated = { ...p, ...updates };
                      // Auto-adjust endDate to be startDate + 1 day when startDate changes
                      if (updates.startDate) {
                        const startDate = new Date(updates.startDate);
                        startDate.setDate(startDate.getDate() + 1);
                        updated.endDate = startDate.toISOString().split("T")[0];
                      }
                      return updated;
                    }
                    return p;
                  }),
                };
              }
              return work;
            }),
          };
        }
        return stage;
      })
    );
  };

  const toggleAssignedUser = (stageTempId: string, workTempId: string, userId: string) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          return {
            ...stage,
            works: stage.works.map((w) => {
              if (w.tempId === workTempId) {
                const isAssigned = w.assignedUserIds.includes(userId);
                return {
                  ...w,
                  assignedUserIds: isAssigned
                    ? w.assignedUserIds.filter((id) => id !== userId)
                    : [...w.assignedUserIds, userId],
                };
              }
              return w;
            }),
          };
        }
        return stage;
      })
    );
  };

  const addComment = (stageTempId: string, workTempId: string) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          return {
            ...stage,
            works: stage.works.map((work) => {
              if (work.tempId === workTempId) {
                const newComment: WorkCommentFormData = {
                  tempId: `comment-${Date.now()}`,
                  content: "",
                };
                return { ...work, comments: [...work.comments, newComment] };
              }
              return work;
            }),
          };
        }
        return stage;
      })
    );
  };

  const updateComment = (
    stageTempId: string,
    workTempId: string,
    commentTempId: string,
    content: string
  ) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          return {
            ...stage,
            works: stage.works.map((work) => {
              if (work.tempId === workTempId) {
                return {
                  ...work,
                  comments: work.comments.map((c) =>
                    c.tempId === commentTempId ? { ...c, content } : c
                  ),
                };
              }
              return work;
            }),
          };
        }
        return stage;
      })
    );
  };

  const removeComment = (stageTempId: string, workTempId: string, commentTempId: string) => {
    setStages(
      stages.map((stage) => {
        if (stage.tempId === stageTempId) {
          return {
            ...stage,
            works: stage.works.map((work) => {
              if (work.tempId === workTempId) {
                return {
                  ...work,
                  comments: work.comments.filter((c) => c.tempId !== commentTempId),
                };
              }
              return work;
            }),
          };
        }
        return stage;
      })
    );
  };

  // Drag & Drop dla etapów
  const handleStageDragStart = (e: React.DragEvent, tempId: string) => {
    setDraggedStage(tempId);
    e.dataTransfer.effectAllowed = "move";
  };

  const handleStageDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = "move";
  };

  const handleStageDrop = (e: React.DragEvent, targetTempId: string) => {
    e.preventDefault();
    if (!draggedStage || draggedStage === targetTempId) return;

    const draggedIndex = stages.findIndex((s) => s.tempId === draggedStage);
    const targetIndex = stages.findIndex((s) => s.tempId === targetTempId);

    const newStages = [...stages];
    const [removed] = newStages.splice(draggedIndex, 1);
    newStages.splice(targetIndex, 0, removed);

    const reorderedStages = newStages.map((s, index) => ({ ...s, order: index }));
    setStages(reorderedStages);
    setDraggedStage(null);
  };

  // Drag & Drop dla prac
  const handleWorkDragStart = (e: React.DragEvent, stageTempId: string, workTempId: string) => {
    setDraggedWork({ stageId: stageTempId, workId: workTempId });
    e.dataTransfer.effectAllowed = "move";
  };

  const handleWorkDrop = (
    e: React.DragEvent,
    targetStageTempId: string,
    targetWorkTempId: string
  ) => {
    e.preventDefault();
    e.stopPropagation();
    
    if (!draggedWork || 
        (draggedWork.stageId === targetStageTempId && draggedWork.workId === targetWorkTempId)) {
      return;
    }

    setStages((prevStages) => {
      const newStages = [...prevStages];
      const sourceStageIndex = newStages.findIndex((s) => s.tempId === draggedWork.stageId);
      const targetStageIndex = newStages.findIndex((s) => s.tempId === targetStageTempId);

      const sourceStage = newStages[sourceStageIndex];
      const targetStage = newStages[targetStageIndex];

      const workIndex = sourceStage.works.findIndex((w) => w.tempId === draggedWork.workId);
      const [work] = sourceStage.works.splice(workIndex, 1);

      const targetWorkIndex = targetStage.works.findIndex((w) => w.tempId === targetWorkTempId);
      targetStage.works.splice(targetWorkIndex, 0, work);

      // Aktualizuj order w obu etapach
      sourceStage.works = sourceStage.works.map((w, idx) => ({ ...w, order: idx }));
      targetStage.works = targetStage.works.map((w, idx) => ({ ...w, order: idx }));

      return newStages;
    });

    setDraggedWork(null);
  };

  const handleSubmit = async () => {
    if (!scheduleName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa harmonogramu jest wymagana",
        status: "error",
        duration: 3000,
      });
      return;
    }

    // Walidacja tylko dla wypełnionych etapów
    for (const stage of stages) {
      if (stage.name.trim() && stage.works.length > 0) {
        for (const work of stage.works) {
          if (work.name.trim() && work.periods.length > 0) {
            for (const period of work.periods) {
              if (new Date(period.startDate) > new Date(period.endDate)) {
                toast({
                  title: "Błąd walidacji",
                  description: `Data rozpoczęcia nie może być późniejsza niż data zakończenia w pracy "${work.name}"`,
                  status: "error",
                  duration: 3000,
                });
                return;
              }
            }

            // Check for overlapping periods
            for (let i = 0; i < work.periods.length; i++) {
              for (let j = i + 1; j < work.periods.length; j++) {
                const period1 = work.periods[i];
                const period2 = work.periods[j];
                const start1 = new Date(period1.startDate);
                const end1 = new Date(period1.endDate);
                const start2 = new Date(period2.startDate);
                const end2 = new Date(period2.endDate);

                // Check if periods overlap
                if (start1 <= end2 && start2 <= end1) {
                  toast({
                    title: "Błąd walidacji",
                    description: `Okresy pracy "${work.name}" nie mogą nachodzić na siebie`,
                    status: "error",
                    duration: 3000,
                  });
                  return;
                }
              }
            }
          }
        }
      }
    }

    setSubmitting(true);
    try {
      const command = {
        name: scheduleName,
        stages: stages.map((stage) => ({
          name: stage.name,
          order: stage.order,
          works: stage.works.map((work) => ({
            name: work.name,
            order: work.order,
            colorRgb: work.colorRgb,
            periods: work.periods.map((period) => ({
              startDate: new Date(period.startDate).toISOString(),
              endDate: new Date(period.endDate).toISOString(),
              isClosed: period.isClosed,
            })),
            assignedUserIds: work.assignedUserIds,
            comments: work.comments
              .filter((c) => c.content.trim())
              .map((c) => ({
                content: c.content.trim(),
              })),
          })),
        })),
      };

      const response = await projectApi.createWorkSchedule(tenantId, projectId, command);

      toast({
        title: "Sukces",
        description: "Harmonogram został utworzony",
        status: "success",
        duration: 3000,
      });
      onScheduleCreated?.();
      onClose();
    } catch (error) {
      console.error("Błąd tworzenia harmonogramu:", error);
      const { title, description } = handleApiError(error);
      toast({
        title,
        description,
        status: "error",
        duration: 3000,
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="6xl" scrollBehavior="inside">
      <ModalOverlay />
      <ModalContent maxH="90vh">
        <ModalHeader>Utwórz harmonogram prac - {projectName}</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={6} align="stretch">
            <FormControl isRequired>
              <FormLabel>Nazwa harmonogramu</FormLabel>
              <Input
                placeholder="Np. Harmonogram budowy - Q1 2025"
                value={scheduleName}
                onChange={(e) => setScheduleName(e.target.value)}
              />
            </FormControl>

            <Divider />

            <Box>
              <HStack justify="space-between" mb={4}>
                <Text fontWeight="bold" fontSize="lg">
                  Etapy i prace
                </Text>
                <Button
                  leftIcon={<Plus size={16} />}
                  colorScheme="blue"
                  size="sm"
                  onClick={addStage}
                >
                  Dodaj etap
                </Button>
              </HStack>

              <VStack spacing={4} align="stretch">
                <Accordion allowMultiple>
                  {stages.map((stage, stageIndex) => (
                    <AccordionItem
                      key={stage.tempId}
                      borderWidth="2px"
                      borderRadius="lg"
                      borderColor={draggedStage === stage.tempId ? "blue.400" : borderColor}
                      bg={bgColor}
                      mb={3}
                    >
                      <AccordionButton
                        p={4}
                        _hover={{ bg: hoverBg }}
                        draggable
                        onDragStart={(e) => handleStageDragStart(e, stage.tempId)}
                        onDragOver={handleStageDragOver}
                        onDrop={(e) => handleStageDrop(e, stage.tempId)}
                      >
                        <HStack spacing={3} flex={1}>
                          <Box cursor="grab" _active={{ cursor: "grabbing" }}>
                            <GripVertical size={20} />
                          </Box>
                          <Badge colorScheme="blue">Etap {stageIndex + 1}</Badge>
                          <Input
                            placeholder="Nazwa etapu"
                            value={stage.name}
                            onChange={(e) => updateStageName(stage.tempId, e.target.value)}
                            onClick={(e) => e.stopPropagation()}
                            flex={1}
                          />
                          <IconButton
                            aria-label="Usuń etap"
                            icon={<Trash2 size={16} />}
                            colorScheme="red"
                            size="sm"
                            variant="ghost"
                            onClick={(e) => {
                              e.stopPropagation();
                              removeStage(stage.tempId);
                            }}
                          />
                        </HStack>
                        <AccordionIcon ml={2} />
                      </AccordionButton>

                      <AccordionPanel pb={4} pt={2}>
                        <VStack spacing={3} align="stretch" pl={8}>
                          <Accordion allowMultiple>
                            {stage.works.map((work, workIndex) => (
                              <AccordionItem
                                key={work.tempId}
                                borderWidth="1px"
                                borderRadius="md"
                                borderColor={borderColor}
                                bg={useColorModeValue("gray.50", "gray.700")}
                                mb={2}
                              >
                                <AccordionButton
                                  p={3}
                                  _hover={{ bg: hoverBg }}
                                  draggable
                                  onDragStart={(e) => handleWorkDragStart(e, stage.tempId, work.tempId)}
                                  onDragOver={handleStageDragOver}
                                  onDrop={(e) => handleWorkDrop(e, stage.tempId, work.tempId)}
                                >
                                  <HStack spacing={2} flex={1}>
                                    <Box cursor="grab" _active={{ cursor: "grabbing" }}>
                                      <GripVertical size={16} />
                                    </Box>
                                    <Badge colorScheme="green" fontSize="xs">
                                      Zakres robót {workIndex + 1}
                                    </Badge>
                                    <Input
                                      placeholder="Nazwa zakresu robót"
                                      size="sm"
                                      value={work.name}
                                      onChange={(e) =>
                                        updateWork(stage.tempId, work.tempId, { name: e.target.value })
                                      }
                                      onClick={(e) => e.stopPropagation()}
                                      flex={1}
                                    />
                                    <IconButton
                                      aria-label="Usuń pracę"
                                      icon={<Trash2 size={14} />}
                                      colorScheme="red"
                                      size="xs"
                                      variant="ghost"
                                      onClick={(e) => {
                                        e.stopPropagation();
                                        removeWork(stage.tempId, work.tempId);
                                      }}
                                    />
                                  </HStack>
                                  <AccordionIcon ml={2} />
                                </AccordionButton>

                                <AccordionPanel pb={3} pt={2}>
                                  <VStack spacing={2} align="stretch">
                                    <FormControl size="sm">
                                      <HStack justify="space-between" mb={1}>
                                        <FormLabel fontSize="xs" mb={0}>Okresy pracy</FormLabel>
                                <Button
                                  size="xs"
                                  leftIcon={<Plus size={12} />}
                                  onClick={() => addPeriod(stage.tempId, work.tempId)}
                                  colorScheme="green"
                                  variant="ghost"
                                >
                                  Dodaj okres
                                </Button>
                              </HStack>
                              <VStack spacing={2} align="stretch">
                                {work.periods.map((period, periodIdx) => (
                                  <VStack key={period.tempId} spacing={1} align="stretch">
                                    <HStack spacing={2}>
                                      <Text fontSize="xs" minW="20px">{periodIdx + 1}.</Text>
                                      <Input
                                        type="date"
                                        size="sm"
                                        value={period.startDate}
                                        onChange={(e) =>
                                          updatePeriod(stage.tempId, work.tempId, period.tempId, {
                                            startDate: e.target.value,
                                          })
                                        }
                                        placeholder="Od"
                                      />
                                      <Input
                                        type="date"
                                        size="sm"
                                        value={period.endDate}
                                        onChange={(e) =>
                                          updatePeriod(stage.tempId, work.tempId, period.tempId, {
                                            endDate: e.target.value,
                                          })
                                        }
                                        placeholder="Do"
                                      />
                                      <IconButton
                                        aria-label="Usuń okres"
                                        icon={<Trash2 size={14} />}
                                        size="sm"
                                        colorScheme="red"
                                        variant="ghost"
                                        onClick={() => removePeriod(stage.tempId, work.tempId, period.tempId)}
                                        isDisabled={work.periods.length === 1}
                                      />
                                    </HStack>
                                    <Checkbox
                                      size="sm"
                                      isChecked={period.isClosed}
                                      onChange={(e) =>
                                        updatePeriod(stage.tempId, work.tempId, period.tempId, {
                                          isClosed: e.target.checked,
                                        })
                                      }
                                      ml={6}
                                    >
                                      <Text fontSize="xs">Okres wykonany</Text>
                                    </Checkbox>
                                  </VStack>
                                ))}
                              </VStack>
                            </FormControl>

                            <FormControl size="sm">
                              <FormLabel fontSize="xs" mb={1}>
                                Kolor
                              </FormLabel>
                              <HStack spacing={2} flexWrap="wrap">
                                {PRESET_COLORS.map((color) => (
                                  <Box
                                    key={color}
                                    w={8}
                                    h={8}
                                    bg={color}
                                    borderRadius="md"
                                    cursor="pointer"
                                    borderWidth="3px"
                                    borderColor={
                                      work.colorRgb === color ? "red.500" : "transparent"
                                    }
                                    onClick={() =>
                                      updateWork(stage.tempId, work.tempId, { colorRgb: color })
                                    }
                                    _hover={{ transform: "scale(1.1)" }}
                                    transition="all 0.2s"
                                    position="relative"
                                    _after={work.colorRgb === color ? {
                                      content: '""',
                                      position: "absolute",
                                      top: 0,
                                      left: 0,
                                      right: 0,
                                      bottom: 0,
                                      bg: "blackAlpha.300",
                                      borderRadius: "md",
                                    } : undefined}
                                  />
                                ))}
                                <Box
                                  position="relative"
                                  w={8}
                                  h={8}
                                  bg={work.colorRgb}
                                  borderRadius="md"
                                  borderWidth="3px"
                                  borderColor={
                                    !PRESET_COLORS.includes(work.colorRgb) ? "red.500" : "gray.300"
                                  }
                                  overflow="hidden"
                                  cursor="pointer"
                                  _hover={{ transform: "scale(1.1)" }}
                                  transition="all 0.2s"
                                  display="flex"
                                  alignItems="center"
                                  justifyContent="center"
                                >
                                  <Text
                                    fontSize="2xs"
                                    fontWeight="bold"
                                    color="white"
                                    textShadow="0 0 2px black"
                                    pointerEvents="none"
                                    position="relative"
                                    zIndex={1}
                                  >
                                    Inny
                                  </Text>
                                  <Input
                                    type="color"
                                    value={work.colorRgb}
                                    onChange={(e) =>
                                      updateWork(stage.tempId, work.tempId, { colorRgb: e.target.value })
                                    }
                                    position="absolute"
                                    top={0}
                                    left={0}
                                    w="100%"
                                    h="100%"
                                    border="none"
                                    cursor="pointer"
                                    opacity={0}
                                    sx={{
                                      '&::-webkit-color-swatch-wrapper': {
                                        padding: 0,
                                      },
                                      '&::-webkit-color-swatch': {
                                        border: 'none',
                                        borderRadius: 'md',
                                      },
                                    }}
                                  />
                                </Box>
                              </HStack>
                            </FormControl>

                            <FormControl size="sm">
                              <FormLabel fontSize="xs" mb={1}>
                                Przypisani członkowie
                              </FormLabel>
                              <Flex flexWrap="wrap" gap={2}>
                                {members.map((member) => (
                                  <Badge
                                    key={member.userId}
                                    colorScheme={
                                      work.assignedUserIds.includes(member.userId)
                                        ? "blue"
                                        : "gray"
                                    }
                                    cursor="pointer"
                                    px={2}
                                    py={1}
                                    borderRadius="md"
                                    onClick={() =>
                                      toggleAssignedUser(stage.tempId, work.tempId, member.userId)
                                    }
                                    _hover={{ transform: "scale(1.05)" }}
                                    transition="all 0.2s"
                                  >
                                    {member.firstName} {member.lastName}
                                  </Badge>
                                ))}
                              </Flex>
                            </FormControl>

                            <FormControl size="sm">
                              <HStack justify="space-between" mb={1}>
                                <FormLabel fontSize="xs" mb={0}>Komentarze</FormLabel>
                                <Button
                                  size="xs"
                                  leftIcon={<Plus size={12} />}
                                  onClick={() => addComment(stage.tempId, work.tempId)}
                                  colorScheme="purple"
                                  variant="ghost"
                                >
                                  Dodaj komentarz
                                </Button>
                              </HStack>
                              <VStack spacing={2} align="stretch">
                                {work.comments.map((comment, commentIdx) => (
                                  <HStack key={comment.tempId} spacing={2} align="flex-start">
                                    <Text fontSize="xs" minW="20px" mt={2}>{commentIdx + 1}.</Text>
                                    <Textarea
                                      size="sm"
                                      value={comment.content}
                                      onChange={(e) =>
                                        updateComment(stage.tempId, work.tempId, comment.tempId, e.target.value)
                                      }
                                      placeholder="Treść komentarza (max 2000 znaków)"
                                      maxLength={2000}
                                      resize="vertical"
                                      minH="60px"
                                    />
                                    <IconButton
                                      aria-label="Usuń komentarz"
                                      icon={<Trash2 size={14} />}
                                      size="sm"
                                      colorScheme="red"
                                      variant="ghost"
                                      onClick={() => removeComment(stage.tempId, work.tempId, comment.tempId)}
                                      mt={1}
                                    />
                                  </HStack>
                                ))}
                              </VStack>
                            </FormControl>
                          </VStack>
                          </AccordionPanel>
                        </AccordionItem>
                      ))}
                    </Accordion>

                    <Button
                      leftIcon={<Plus size={14} />}
                      size="sm"
                      variant="outline"
                      colorScheme="green"
                      onClick={() => addWork(stage.tempId)}
                    >
                      Dodaj zakres robót
                    </Button>
                  </VStack>
                </AccordionPanel>
              </AccordionItem>
            ))}
          </Accordion>

          {stages.length === 0 && (
            <Box textAlign="center" py={8} color="gray.500">
              <Text>Brak etapów. Kliknij "Dodaj etap" aby rozpocząć.</Text>
            </Box>
          )}
        </VStack>
      </Box>
    </VStack>
  </ModalBody>

        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={onClose} isDisabled={submitting}>
            Anuluj
          </Button>
          <Button
            colorScheme="blue"
            onClick={handleSubmit}
            isLoading={submitting}
            loadingText="Tworzenie..."
          >
            Utwórz harmonogram
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
