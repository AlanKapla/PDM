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
  Checkbox,
} from "@chakra-ui/react";
import { Plus, Trash2, GripVertical } from "lucide-react";
import { projectApi } from "../api/projectApi";
import type { WorkScheduleDetailsWeb } from "../types/workSchedule.types";
import { handleApiError } from "../utils/handleApiError";

interface EditWorkScheduleModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  schedule: WorkScheduleDetailsWeb;
  members: any[];
  onScheduleUpdated?: () => void;
}

interface StageFormData {
  id?: string;
  tempId: string;
  name: string;
  order: number;
  works: WorkFormData[];
}

interface WorkPeriodFormData {
  id?: string;
  tempId: string;
  startDate: string;
  endDate: string;
}

interface WorkFormData {
  id?: string;
  tempId: string;
  name: string;
  order: number;
  colorRgb: string;
  isClosed: boolean;
  periods: WorkPeriodFormData[];
  assignedUserIds: string[];
}

const PRESET_COLORS = [
  "#3182CE", "#38A169", "#DD6B20", "#E53E3E", "#805AD5",
  "#D69E2E", "#00B5D8", "#D53F8C", "#319795", "#718096",
];

export default function EditWorkScheduleModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  schedule,
  members,
  onScheduleUpdated,
}: EditWorkScheduleModalProps) {
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
    if (isOpen && schedule) {
      loadScheduleData();
    }
  }, [isOpen, schedule]);

  const loadScheduleData = () => {
    setScheduleName(schedule.name);
    
    const loadedStages: StageFormData[] = schedule.stages.map((stage) => ({
      id: stage.id,
      tempId: `stage-${stage.id}`,
      name: stage.name,
      order: stage.order,
      works: stage.works.map((work) => ({
        id: work.id,
        tempId: `work-${work.id}`,
        name: work.name,
        order: work.order,
        colorRgb: work.colorRgb,
        isClosed: work.isClosed,
        periods: work.periods.map((period, idx) => ({
          tempId: `period-${work.id}-${idx}`,
          startDate: new Date(period.startDate).toISOString().split("T")[0],
          endDate: new Date(period.endDate).toISOString().split("T")[0],
        })),
        assignedUserIds: work.assignees.map((a) => a.userId),
      })),
    }));

    setStages(loadedStages);
  };

  const addStage = () => {
    const newStage: StageFormData = {
      tempId: `stage-new-${Date.now()}`,
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
            tempId: `work-new-${Date.now()}`,
            name: "",
            order: stage.works.length,
            colorRgb: PRESET_COLORS[stage.works.length % PRESET_COLORS.length],
            isClosed: false,
            periods: [{
              tempId: `period-new-${Date.now()}`,
              startDate: today.toISOString().split("T")[0],
              endDate: tomorrow.toISOString().split("T")[0],
            }],
            assignedUserIds: [],
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
                  tempId: `period-new-${Date.now()}`,
                  startDate: newStartDate,
                  endDate: newEndDate,
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

  // Drag & Drop (kopiowane z CreateWorkScheduleModal)
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

    if (stages.length === 0) {
      toast({
        title: "Błąd walidacji",
        description: "Dodaj co najmniej jeden etap",
        status: "error",
        duration: 3000,
      });
      return;
    }

    for (const stage of stages) {
      if (!stage.name.trim()) {
        toast({
          title: "Błąd walidacji",
          description: "Wszystkie etapy muszą mieć nazwę",
          status: "error",
          duration: 3000,
        });
        return;
      }

      if (stage.works.length === 0) {
        toast({
          title: "Błąd walidacji",
          description: `Etap "${stage.name}" musi mieć co najmniej jedną pracę`,
          status: "error",
          duration: 3000,
        });
        return;
      }

      for (const work of stage.works) {
        if (!work.name.trim()) {
          toast({
            title: "Błąd walidacji",
            description: `Wszystkie prace w etapie "${stage.name}" muszą mieć nazwę`,
            status: "error",
            duration: 3000,
          });
          return;
        }

        if (work.periods.length === 0) {
          toast({
            title: "Błąd walidacji",
            description: `Zakres robót "${work.name}" musi mieć co najmniej jeden okres`,
            status: "error",
            duration: 3000,
          });
          return;
        }

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

    setSubmitting(true);
    try {
      const command = {
        name: scheduleName,
        stages: stages.map((stage) => ({
          id: stage.id,
          name: stage.name,
          order: stage.order,
          works: stage.works.map((work) => ({
            id: work.id,
            name: work.name,
            order: work.order,
            colorRgb: work.colorRgb,
            isClosed: work.isClosed,
            periods: work.periods.map((period) => ({
              id: period.id,
              startDate: new Date(period.startDate).toISOString(),
              endDate: new Date(period.endDate).toISOString(),
            })),
            assignedUserIds: work.assignedUserIds,
          })),
        })),
      };

      const response = await projectApi.updateWorkSchedule(
        tenantId,
        projectId,
        schedule.id,
        command
      );

      if (response.ok) {
        toast({
          title: "Sukces",
          description: "Harmonogram został zaktualizowany",
          status: "success",
          duration: 3000,
        });
        onScheduleUpdated?.();
        onClose();
      } else {
        const errorMessage = await handleApiError(response);
        toast({
          title: "Błąd",
          description: errorMessage,
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd aktualizacji harmonogramu:", error);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="6xl" scrollBehavior="inside">
      <ModalOverlay />
      <ModalContent maxH="90vh">
        <ModalHeader>Edytuj harmonogram - {schedule.name}</ModalHeader>
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
                {stages.map((stage, stageIndex) => (
                  <Box
                    key={stage.tempId}
                    p={4}
                    borderWidth="2px"
                    borderRadius="lg"
                    borderColor={draggedStage === stage.tempId ? "blue.400" : borderColor}
                    bg={bgColor}
                    draggable
                    onDragStart={(e) => handleStageDragStart(e, stage.tempId)}
                    onDragOver={handleStageDragOver}
                    onDrop={(e) => handleStageDrop(e, stage.tempId)}
                    cursor="move"
                    transition="all 0.2s"
                    _hover={{ borderColor: "blue.300" }}
                  >
                    <HStack spacing={3} mb={3}>
                      <Box cursor="grab" _active={{ cursor: "grabbing" }}>
                        <GripVertical size={20} />
                      </Box>
                      <Badge colorScheme="blue">Etap {stageIndex + 1}</Badge>
                      <Input
                        placeholder="Nazwa etapu"
                        value={stage.name}
                        onChange={(e) => updateStageName(stage.tempId, e.target.value)}
                        flex={1}
                      />
                      <IconButton
                        aria-label="Usuń etap"
                        icon={<Trash2 size={16} />}
                        colorScheme="red"
                        size="sm"
                        variant="ghost"
                        onClick={() => removeStage(stage.tempId)}
                      />
                    </HStack>

                    <VStack spacing={3} align="stretch" pl={8}>
                      {stage.works.map((work, workIndex) => (
                        <Box
                          key={work.tempId}
                          p={3}
                          borderWidth="1px"
                          borderRadius="md"
                          borderColor={borderColor}
                          bg={useColorModeValue("gray.50", "gray.700")}
                          draggable
                          onDragStart={(e) => handleWorkDragStart(e, stage.tempId, work.tempId)}
                          onDragOver={handleStageDragOver}
                          onDrop={(e) => handleWorkDrop(e, stage.tempId, work.tempId)}
                          cursor="move"
                          _hover={{ bg: hoverBg }}
                        >
                          <HStack spacing={2} mb={2}>
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
                              flex={1}
                            />
                            <IconButton
                              aria-label="Usuń pracę"
                              icon={<Trash2 size={14} />}
                              colorScheme="red"
                              size="xs"
                              variant="ghost"
                              onClick={() => removeWork(stage.tempId, work.tempId)}
                            />
                          </HStack>

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
                                  <HStack key={period.tempId} spacing={2}>
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
                              <Checkbox
                                isChecked={work.isClosed}
                                onChange={(e) =>
                                  updateWork(stage.tempId, work.tempId, { isClosed: e.target.checked })
                                }
                                colorScheme="green"
                                size="sm"
                              >
                                <Text fontSize="xs">Prace zakończone</Text>
                              </Checkbox>
                            </FormControl>
                          </VStack>
                        </Box>
                      ))}

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
                  </Box>
                ))}

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
            loadingText="Zapisywanie..."
          >
            Zapisz zmiany
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
