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
import type { WorkScheduleDetailsWeb } from "../types/workSchedule.types";

interface WorkScheduleFormModalProps {
  mode: 'create' | 'edit';
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  projectName: string;
  members: any[];
  schedule?: WorkScheduleDetailsWeb; // Tylko dla trybu 'edit'
  onSuccess?: () => void;
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
  isClosed: boolean;
}

interface WorkCommentFormData {
  id?: string;
  tempId: string;
  content: string;
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
  comments: WorkCommentFormData[];
}

const PRESET_COLORS = [
  "#3182CE", "#38A169", "#DD6B20", "#E53E3E", "#805AD5",
  "#D69E2E", "#00B5D8", "#D53F8C", "#319795", "#718096",
];

export default function WorkScheduleFormModal({
  mode,
  isOpen,
  onClose,
  tenantId,
  projectId,
  projectName,
  members,
  schedule,
  onSuccess,
}: WorkScheduleFormModalProps) {
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
      if (mode === 'create') {
        resetForm();
      } else if (mode === 'edit' && schedule) {
        loadScheduleData();
      }
    }
  }, [isOpen, mode, schedule]);

  const resetForm = () => {
    setScheduleName("");
    setStages([]);
  };

  const loadScheduleData = () => {
    if (!schedule) return;
    
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
          id: period.id,
          tempId: `period-${work.id}-${idx}`,
          startDate: new Date(period.startDate).toISOString().split("T")[0],
          endDate: new Date(period.endDate).toISOString().split("T")[0],
          isClosed: period.isClosed,
        })),
        assignedUserIds: work.assignees.map((a) => a.userId),
        comments: work.comments.map((comment) => ({
          id: comment.id,
          tempId: `comment-${comment.id}`,
          content: comment.content,
        })),
      })),
    }));

    setStages(loadedStages);
  };

  const addStage = () => {
    const newStage: StageFormData = {
      tempId: mode === 'create' ? `stage-${Date.now()}` : `stage-new-${Date.now()}`,
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
            tempId: mode === 'create' ? `work-${Date.now()}` : `work-new-${Date.now()}`,
            name: "",
            order: stage.works.length,
            colorRgb: PRESET_COLORS[stage.works.length % PRESET_COLORS.length],
            isClosed: false,
            periods: [{
              tempId: mode === 'create' ? `period-${Date.now()}` : `period-new-${Date.now()}`,
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
                  tempId: mode === 'create' ? `period-${Date.now()}` : `period-new-${Date.now()}`,
                  startDate: newStartDate,
                  endDate: newEndDate,
                  isClosed: false,
                };
                return { ...work, periods: [...work.periods, newPeriod], isClosed: false };
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
                  tempId: mode === 'create' ? `comment-${Date.now()}` : `comment-new-${Date.now()}`,
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

  // Drag & Drop
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
    // Walidacja nazwy harmonogramu
    if (!scheduleName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa harmonogramu jest wymagana",
        status: "error",
        duration: 3000,
      });
      return;
    }

    if (scheduleName.length > 200) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa harmonogramu nie może przekraczać 200 znaków",
        status: "error",
        duration: 3000,
      });
      return;
    }

    // Walidacja etapów i prac
    for (const stage of stages) {
      // Walidacja nazwy etapu
      if (!stage.name.trim()) {
        toast({
          title: "Błąd walidacji",
          description: "Nazwa etapu jest wymagana dla wszystkich etapów",
          status: "error",
          duration: 3000,
        });
        return;
      }

      if (stage.name.length > 200) {
        toast({
          title: "Błąd walidacji",
          description: `Nazwa etapu "${stage.name}" nie może przekraczać 200 znaków`,
          status: "error",
          duration: 3000,
        });
        return;
      }

      // Walidacja prac w etapie
      if (stage.works.length > 0) {
        for (const work of stage.works) {
          // Walidacja nazwy pracy
          if (!work.name.trim()) {
            toast({
              title: "Błąd walidacji",
              description: `Nazwa zakresu robót jest wymagana w etapie "${stage.name}"`,
              status: "error",
              duration: 3000,
            });
            return;
          }

          if (work.name.length > 200) {
            toast({
              title: "Błąd walidacji",
              description: `Nazwa zakresu robót "${work.name}" nie może przekraczać 200 znaków`,
              status: "error",
              duration: 3000,
            });
            return;
          }

          // Walidacja okresów
          if (work.periods.length > 0) {
            for (const period of work.periods) {
              if (new Date(period.startDate) > new Date(period.endDate)) {
                toast({
                  title: "Błąd walidacji",
                  description: `Data rozpoczęcia nie może być późniejsza niż data zakończenia w zakresie robót "${work.name}"`,
                  status: "error",
                  duration: 3000,
                });
                return;
              }
            }

            // Sprawdź nakładające się okresy
            for (let i = 0; i < work.periods.length; i++) {
              for (let j = i + 1; j < work.periods.length; j++) {
                const period1 = work.periods[i];
                const period2 = work.periods[j];
                const start1 = new Date(period1.startDate);
                const end1 = new Date(period1.endDate);
                const start2 = new Date(period2.startDate);
                const end2 = new Date(period2.endDate);

                if (start1 <= end2 && start2 <= end1) {
                  toast({
                    title: "Błąd walidacji",
                    description: `Okresy w zakresie robót "${work.name}" nie mogą nachodzić na siebie`,
                    status: "error",
                    duration: 3000,
                  });
                  return;
                }
              }
            }
          }

          // Walidacja komentarzy
          for (const comment of work.comments) {
            if (comment.content.trim() && comment.content.length > 2000) {
              toast({
                title: "Błąd walidacji",
                description: `Komentarz w zakresie robót "${work.name}" nie może przekraczać 2000 znaków`,
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
          ...(mode === 'edit' && stage.id ? { id: stage.id } : {}),
          name: stage.name,
          order: stage.order,
          works: stage.works.map((work) => ({
            ...(mode === 'edit' && work.id ? { id: work.id } : {}),
            name: work.name,
            order: work.order,
            colorRgb: work.colorRgb,
            isClosed: work.isClosed,
            periods: work.periods.map((period) => ({
              ...(mode === 'edit' && period.id ? { id: period.id } : {}),
              startDate: new Date(period.startDate).toISOString(),
              endDate: new Date(period.endDate).toISOString(),
              isClosed: period.isClosed,
            })),
            assignedUserIds: work.assignedUserIds,
            comments: work.comments
              .filter((c) => c.content.trim())
              .map((c) => ({
                ...(mode === 'edit' && c.id ? { id: c.id } : {}),
                content: c.content.trim(),
              })),
          })),
        })),
      };

      if (mode === 'create') {
        await projectApi.createWorkSchedule(tenantId, projectId, command);
        toast({
          title: "Sukces",
          description: "Harmonogram został utworzony",
          status: "success",
          duration: 3000,
        });
      } else {
        await projectApi.updateWorkSchedule(tenantId, projectId, schedule!.id, command);
        toast({
          title: "Sukces",
          description: "Harmonogram został zaktualizowany",
          status: "success",
          duration: 3000,
        });
      }

      onSuccess?.();
      onClose();
    } catch (error) {
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

  const modalTitle = mode === 'create' 
    ? `Utwórz harmonogram prac - ${projectName}`
    : `Edytuj harmonogram - ${schedule?.name || ''}`;

  const submitButtonText = mode === 'create' ? 'Utwórz harmonogram' : 'Zapisz zmiany';
  const submitLoadingText = mode === 'create' ? 'Tworzenie...' : 'Zapisywanie...';

  return (
    <Modal isOpen={isOpen} onClose={onClose} size={{ base: "full", md: "6xl" }} scrollBehavior="inside">
      <ModalOverlay />
      <ModalContent maxH={{ base: "100vh", md: "90vh" }} mx={{ base: 0, md: "auto" }}>
        <ModalHeader fontSize={{ base: "sm", md: "lg" }}>{modalTitle}</ModalHeader>
        <ModalCloseButton />
        <ModalBody pb={20}>
          <VStack spacing={6} align="stretch">
            <FormControl isRequired>
              <FormLabel fontSize={{ base: "xs", md: "sm" }}>Nazwa harmonogramu</FormLabel>
              <Input
                placeholder="Np. Harmonogram budowy - Q1 2025"
                value={scheduleName}
                onChange={(e) => setScheduleName(e.target.value)}
                maxLength={200}
                size={{ base: "sm", md: "md" }}
              />
              <Text fontSize="xs" color="gray.500" mt={1}>
                {scheduleName.length}/200 znaków
              </Text>
            </FormControl>

            <Divider />

            <Box>
              <HStack justify="space-between" mb={4} flexWrap="wrap" gap={2}>
                <Text fontWeight="bold" fontSize={{ base: "md", md: "lg" }}>
                  Etapy i prace
                </Text>
                <Button
                  leftIcon={<Plus size={16} />}
                  colorScheme="blue"
                  size={{ base: "sm", md: "md" }}
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
                        p={{ base: 2, md: 4 }}
                        _hover={{ bg: hoverBg }}
                        draggable
                        onDragStart={(e) => handleStageDragStart(e, stage.tempId)}
                        onDragOver={handleStageDragOver}
                        onDrop={(e) => handleStageDrop(e, stage.tempId)}
                        flexDirection={{ base: "column", md: "row" }}
                        alignItems={{ base: "flex-start", md: "center" }}
                      >
                        <HStack spacing={2} flex={1} width="100%" alignItems={{ base: "flex-start", md: "center" }}>
                          <Box cursor="grab" _active={{ cursor: "grabbing" }} display={{ base: "none", md: "block" }}>
                            <GripVertical size={20} />
                          </Box>
                          <Badge colorScheme="blue" fontSize={{ base: "10px", md: "xs" }} flexShrink={0}>
                            Etap {stageIndex + 1}
                          </Badge>
                          <Input
                            placeholder="Nazwa etapu (wymagane, max 200 znaków)"
                            value={stage.name}
                            onChange={(e) => updateStageName(stage.tempId, e.target.value)}
                            onClick={(e) => e.stopPropagation()}
                            flex={1}
                            maxLength={200}
                            isInvalid={!stage.name.trim()}
                            size={{ base: "sm", md: "md" }}
                            width={{ base: "100%", md: "auto" }}
                          />
                          <Text fontSize={{ base: "9px", md: "xs" }} color="gray.500" minW={{ base: "50px", md: "60px" }} textAlign="right" flexShrink={0}>
                            {stage.name.length}/200
                          </Text>
                          <IconButton
                            aria-label="Usuń etap"
                            icon={<Trash2 size={16} />}
                            colorScheme="red"
                            size={{ base: "sm", md: "md" }}
                            variant="ghost"
                            flexShrink={0}
                            onClick={(e) => {
                              e.stopPropagation();
                              removeStage(stage.tempId);
                            }}
                          />
                        </HStack>
                        <AccordionIcon ml={2} display={{ base: "none", md: "block" }} />
                      </AccordionButton>

                      <AccordionPanel pb={4} pt={2}>
                        <VStack spacing={3} align="stretch" pl={{ base: 0, md: 8 }}>
                          <Accordion allowMultiple>
                            {stage.works.map((work, workIndex) => (
                              <AccordionItem
                                key={work.tempId}
                                borderWidth="1px"
                                borderRadius="md"
                                borderColor={borderColor}
                                bg={hoverBg}
                                mb={2}
                              >
                                <AccordionButton
                                  p={{ base: 2, md: 3 }}
                                  _hover={{ bg: hoverBg }}
                                  draggable
                                  onDragStart={(e) => handleWorkDragStart(e, stage.tempId, work.tempId)}
                                  onDragOver={handleStageDragOver}
                                  onDrop={(e) => handleWorkDrop(e, stage.tempId, work.tempId)}
                                  flexDirection={{ base: "column", md: "row" }}
                                  alignItems={{ base: "flex-start", md: "center" }}
                                >
                                  <HStack spacing={{ base: 1, md: 2 }} flex={1} width="100%" alignItems={{ base: "flex-start", md: "center" }}>
                                    <Box cursor="grab" _active={{ cursor: "grabbing" }} display={{ base: "none", md: "block" }}>
                                      <GripVertical size={16} />
                                    </Box>
                                    <Badge colorScheme="green" fontSize={{ base: "9px", md: "xs" }} flexShrink={0}>
                                      Zakres robót {workIndex + 1}
                                    </Badge>
                                    <Input
                                      placeholder="Nazwa zakresu robót (wymagane, max 200 znaków)"
                                      size={{ base: "sm", md: "md" }}
                                      value={work.name}
                                      onChange={(e) =>
                                        updateWork(stage.tempId, work.tempId, { name: e.target.value })
                                      }
                                      onClick={(e) => e.stopPropagation()}
                                      flex={1}
                                      maxLength={200}
                                      isInvalid={!work.name.trim()}
                                      width={{ base: "100%", md: "auto" }}
                                    />
                                    <Text fontSize={{ base: "9px", md: "xs" }} color="gray.500" minW={{ base: "45px", md: "50px" }} textAlign="right" flexShrink={0}>
                                      {work.name.length}/200
                                    </Text>
                                    <IconButton
                                      aria-label="Usuń pracę"
                                      icon={<Trash2 size={14} />}
                                      colorScheme="red"
                                      size={{ base: "sm", md: "xs" }}
                                      variant="ghost"
                                      flexShrink={0}
                                      onClick={(e) => {
                                        e.stopPropagation();
                                        removeWork(stage.tempId, work.tempId);
                                      }}
                                    />
                                  </HStack>
                                  <AccordionIcon ml={2} display={{ base: "none", md: "block" }} />
                                </AccordionButton>

                                <AccordionPanel pb={3} pt={2}>
                                  <VStack spacing={2} align="stretch">
                                    {/* Okresy pracy */}
                                    <FormControl size={{ base: "sm", md: "md" }}>
                                      <HStack justify="space-between" mb={2} flexWrap="wrap" gap={2}>
                                        <FormLabel fontSize={{ base: "xs", md: "sm" }} mb={0}>Okresy pracy</FormLabel>
                                        <Button
                                          size={{ base: "xs", md: "sm" }}
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
                                            <HStack spacing={1} flexWrap={{ base: "wrap", md: "nowrap" }}>
                                              <Text fontSize={{ base: "xs", md: "sm" }} minW="20px" flexShrink={0}>{periodIdx + 1}.</Text>
                                              <Input
                                                type="date"
                                                size={{ base: "sm", md: "md" }}
                                                value={period.startDate}
                                                onChange={(e) =>
                                                  updatePeriod(stage.tempId, work.tempId, period.tempId, {
                                                    startDate: e.target.value,
                                                  })
                                                }
                                                placeholder="Od"
                                                flex={1}
                                              />
                                              <Input
                                                type="date"
                                                size={{ base: "sm", md: "md" }}
                                                value={period.endDate}
                                                onChange={(e) =>
                                                  updatePeriod(stage.tempId, work.tempId, period.tempId, {
                                                    endDate: e.target.value,
                                                  })
                                                }
                                                placeholder="Do"
                                                flex={1}
                                              />
                                              <IconButton
                                                aria-label="Usuń okres"
                                                icon={<Trash2 size={14} />}
                                                size={{ base: "sm", md: "md" }}
                                                colorScheme="red"
                                                variant="ghost"
                                                onClick={() => removePeriod(stage.tempId, work.tempId, period.tempId)}
                                                isDisabled={work.periods.length === 1}
                                                flexShrink={0}
                                              />
                                            </HStack>
                                            <Checkbox
                                              size={{ base: "sm", md: "md" }}
                                              colorScheme="green"
                                              isChecked={period.isClosed}
                                              onChange={(e) => {
                                                const updatedPeriods = work.periods.map(p => 
                                                  p.tempId === period.tempId ? { ...p, isClosed: e.target.checked } : p
                                                );
                                                const allPeriodsClosed = updatedPeriods.every(p => p.isClosed);
                                                updateWork(stage.tempId, work.tempId, { 
                                                  periods: updatedPeriods,
                                                  isClosed: allPeriodsClosed 
                                                });
                                              }}
                                              ml={6}
                                            >
                                              <Text fontSize={{ base: "xs", md: "sm" }}>Okres wykonany</Text>
                                            </Checkbox>
                                          </VStack>
                                        ))}
                                      </VStack>
                                    </FormControl>

                                    {/* Kolor */}
                                    <FormControl size={{ base: "sm", md: "md" }}>
                                      <FormLabel fontSize={{ base: "xs", md: "sm" }} mb={2}>Kolor</FormLabel>
                                      <HStack spacing={1} flexWrap="wrap">
                                        {PRESET_COLORS.map((color) => (
                                          <Box
                                            key={color}
                                            w={{ base: 7, md: 8 }}
                                            h={{ base: 7, md: 8 }}
                                            bg={color}
                                            borderRadius="md"
                                            cursor="pointer"
                                            borderWidth="3px"
                                            borderColor={work.colorRgb === color ? "red.500" : "transparent"}
                                            onClick={() => updateWork(stage.tempId, work.tempId, { colorRgb: color })}
                                            _hover={{ transform: "scale(1.1)" }}
                                            transition="all 0.2s"
                                            position="relative"
                                            flexShrink={0}
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
                                          w={{ base: 7, md: 8 }}
                                          h={{ base: 7, md: 8 }}
                                          bg={work.colorRgb}
                                          borderRadius="md"
                                          borderWidth="3px"
                                          borderColor={!PRESET_COLORS.includes(work.colorRgb) ? "red.500" : "gray.300"}
                                          overflow="hidden"
                                          cursor="pointer"
                                          _hover={{ transform: "scale(1.1)" }}
                                          transition="all 0.2s"
                                          display="flex"
                                          alignItems="center"
                                          justifyContent="center"
                                          flexShrink={0}
                                        >
                                          <Text fontSize={{ base: "7px", md: "2xs" }} fontWeight="bold" color="white" textShadow="0 0 2px black" pointerEvents="none" position="relative" zIndex={1}>
                                            Inny
                                          </Text>
                                          <Input
                                            type="color"
                                            value={work.colorRgb}
                                            onChange={(e) => updateWork(stage.tempId, work.tempId, { colorRgb: e.target.value })}
                                            position="absolute"
                                            top={0}
                                            left={0}
                                            w="100%"
                                            h="100%"
                                            border="none"
                                            cursor="pointer"
                                            opacity={0}
                                            sx={{
                                              '&::-webkit-color-swatch-wrapper': { padding: 0 },
                                              '&::-webkit-color-swatch': { border: 'none', borderRadius: 'md' },
                                            }}
                                          />
                                        </Box>
                                      </HStack>
                                    </FormControl>

                                    {/* Prace zakończone */}
                                    <FormControl size={{ base: "sm", md: "md" }}>
                                      <Checkbox
                                        size={{ base: "sm", md: "md" }}
                                        colorScheme="green"
                                        isChecked={work.isClosed}
                                        onChange={(e) => {
                                          const newClosedState = e.target.checked;
                                          const updatedPeriods = work.periods.map(p => ({ ...p, isClosed: newClosedState }));
                                          updateWork(stage.tempId, work.tempId, { isClosed: newClosedState, periods: updatedPeriods });
                                        }}
                                      >
                                        <Text fontSize={{ base: "xs", md: "sm" }}>Prace zakończone</Text>
                                      </Checkbox>
                                    </FormControl>

                                    {/* Przypisani członkowie */}
                                    <FormControl size={{ base: "sm", md: "md" }}>
                                      <FormLabel fontSize={{ base: "xs", md: "sm" }} mb={2}>Przypisani członkowie</FormLabel>
                                      <Flex flexWrap="wrap" gap={2}>
                                        {members.map((member) => (
                                          <Badge
                                            key={member.userId}
                                            colorScheme={work.assignedUserIds.includes(member.userId) ? "blue" : "gray"}
                                            cursor="pointer"
                                            px={2}
                                            py={1}
                                            borderRadius="md"
                                            fontSize={{ base: "9px", md: "xs" }}
                                            onClick={() => toggleAssignedUser(stage.tempId, work.tempId, member.userId)}
                                            _hover={{ transform: "scale(1.05)" }}
                                            transition="all 0.2s"
                                          >
                                            {member.firstName} {member.lastName}
                                          </Badge>
                                        ))}
                                      </Flex>
                                    </FormControl>

                                    {/* Komentarze */}
                                    <FormControl size={{ base: "sm", md: "md" }}>
                                      <HStack justify="space-between" mb={2} flexWrap="wrap" gap={2}>
                                        <FormLabel fontSize={{ base: "xs", md: "sm" }} mb={0}>Komentarze</FormLabel>
                                        <Button
                                          size={{ base: "xs", md: "sm" }}
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
                                          <VStack key={comment.tempId} spacing={1} align="stretch">
                                            <HStack spacing={2} align="flex-start" flexWrap={{ base: "wrap", md: "nowrap" }}>
                                              <Text fontSize={{ base: "xs", md: "sm" }} minW="20px" mt={2} flexShrink={0}>{commentIdx + 1}.</Text>
                                              <Textarea
                                                size={{ base: "sm", md: "md" }}
                                                value={comment.content}
                                                onChange={(e) =>
                                                  updateComment(stage.tempId, work.tempId, comment.tempId, e.target.value)
                                                }
                                                placeholder="Treść komentarza (max 2000 znaków)"
                                                maxLength={2000}
                                                resize="vertical"
                                                minH="60px"
                                                flex={1}
                                              />
                                              <IconButton
                                                aria-label="Usuń komentarz"
                                                icon={<Trash2 size={14} />}
                                                size={{ base: "sm", md: "md" }}
                                                colorScheme="red"
                                                variant="ghost"
                                                onClick={() => removeComment(stage.tempId, work.tempId, comment.tempId)}
                                                mt={1}
                                                flexShrink={0}
                                              />
                                            </HStack>
                                            <Text fontSize={{ base: "9px", md: "xs" }} color={comment.content.length > 1900 ? "orange.500" : "gray.500"} ml={6}>
                                              {comment.content.length}/2000 znaków
                                            </Text>
                                          </VStack>
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
                            size={{ base: "sm", md: "md" }}
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
                    <Text fontSize={{ base: "xs", md: "sm" }}>Brak etapów. Kliknij "Dodaj etap" aby rozpocząć.</Text>
                  </Box>
                )}
              </VStack>
            </Box>
          </VStack>
        </ModalBody>

        <ModalFooter position={{ base: "fixed", md: "relative" }} bottom={{ base: 0, md: "auto" }} left={{ base: 0, md: "auto" }} right={{ base: 0, md: "auto" }} bg={bgColor} borderTopWidth={{ base: "1px", md: 0 }} borderColor={borderColor} p={3} gap={2}>
          <Button variant="ghost" onClick={onClose} isDisabled={submitting} size={{ base: "sm", md: "md" }}>
            Anuluj
          </Button>
          <Button
            colorScheme="blue"
            onClick={handleSubmit}
            isLoading={submitting}
            loadingText={submitLoadingText}
            size={{ base: "sm", md: "md" }}
          >
            {submitButtonText}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
