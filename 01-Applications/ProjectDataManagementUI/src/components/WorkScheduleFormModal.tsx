import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
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
  Select,
  Radio,
  RadioGroup,
  Stack,
  Spinner,
  Alert,
  AlertIcon,
  AlertDescription,
} from "@chakra-ui/react";
import { Plus, Trash2, GripVertical, FolderPlus } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { ResourceScope } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import type { WorkScheduleDetailsWeb, WorkScheduleStageWeb } from "../types/workSchedule.types";
import type { CostEstimateListItemWeb } from "../types/costEstimate.types.new";

interface WorkScheduleFormModalProps {
  mode: 'create' | 'edit';
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  projectName: string;
  members: any[];
  schedule?: WorkScheduleDetailsWeb;
  onSuccess?: () => void;
  /** Gdy podane, modal otwiera się w trybie 'linked' z tym kosztorysem już wybranym */
  initialCostEstimateId?: string;
  /** Nazwa kosztorysu potrzebna do wyświetlenia w dropdownie */
  initialCostEstimateName?: string;
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

interface StageFormData {
  id?: string;
  tempId: string;
  name: string;
  order: number;
  works: WorkFormData[];
  children: StageFormData[];
  costEstimateGroupId?: string | null;
}

const PRESET_COLORS = [
  "#3182CE", "#38A169", "#DD6B20", "#E53E3E", "#805AD5",
  "#D69E2E", "#00B5D8", "#D53F8C", "#319795", "#718096",
];

// ——— Rekurencyjne pomocniki drzewa etapów ———

function updateStageInTree(
  stages: StageFormData[],
  tempId: string,
  updater: (stage: StageFormData) => StageFormData
): StageFormData[] {
  return stages.map(stage => {
    if (stage.tempId === tempId) return updater(stage);
    const newChildren = updateStageInTree(stage.children, tempId, updater);
    if (newChildren === stage.children) return stage;
    return { ...stage, children: newChildren };
  });
}

function removeStageFromTree(stages: StageFormData[], tempId: string): StageFormData[] {
  return stages
    .filter(s => s.tempId !== tempId)
    .map(s => ({ ...s, children: removeStageFromTree(s.children, tempId) }));
}

function addChildToTree(
  stages: StageFormData[],
  parentTempId: string,
  child: StageFormData
): StageFormData[] {
  return updateStageInTree(stages, parentTempId, parent => ({
    ...parent,
    children: [...parent.children, { ...child, order: parent.children.length }],
  }));
}

function validateStagesTree(stages: StageFormData[]): string | null {
  for (const stage of stages) {
    if (!stage.name.trim()) return "Nazwa etapu jest wymagana dla wszystkich etapów";
    if (stage.name.length > 200) return `Nazwa etapu "${stage.name}" nie może przekraczać 200 znaków`;
    for (const work of stage.works) {
      if (!work.name.trim()) return `Nazwa zakresu robót jest wymagana w etapie "${stage.name}"`;
      if (work.name.length > 200) return `Nazwa zakresu robót "${work.name}" nie może przekraczać 200 znaków`;
      for (const period of work.periods) {
        if (new Date(period.startDate) > new Date(period.endDate))
          return `Data rozpoczęcia nie może być późniejsza niż data zakończenia w zakresie robót "${work.name}"`;
      }
      for (let i = 0; i < work.periods.length; i++) {
        for (let j = i + 1; j < work.periods.length; j++) {
          const s1 = new Date(work.periods[i].startDate), e1 = new Date(work.periods[i].endDate);
          const s2 = new Date(work.periods[j].startDate), e2 = new Date(work.periods[j].endDate);
          if (s1 <= e2 && s2 <= e1)
            return `Okresy w zakresie robót "${work.name}" nie mogą nachodzić na siebie`;
        }
      }
      for (const comment of work.comments) {
        if (comment.content.trim() && comment.content.length > 2000)
          return `Komentarz w zakresie robót "${work.name}" nie może przekraczać 2000 znaków`;
      }
    }
    const childError = validateStagesTree(stage.children);
    if (childError) return childError;
  }
  return null;
}

function loadStageFromApi(stage: WorkScheduleStageWeb): StageFormData {
  return {
    id: stage.id,
    tempId: `stage-${stage.id}`,
    name: stage.name,
    order: stage.order,
    works: stage.works.sort((a, b) => a.order - b.order).map(work => ({
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
      assignedUserIds: work.assignees.map(a => a.userId),
      comments: work.comments.map(c => ({
        id: c.id,
        tempId: `comment-${c.id}`,
        content: c.content,
      })),
    })),
    children: (stage.childStages || [])
      .sort((a, b) => a.order - b.order)
      .map(child => loadStageFromApi(child)),
    costEstimateGroupId: stage.costEstimateGroupId,
  };
}

function mapStageToDto(stage: StageFormData, isEdit: boolean): any {
  return {
    ...(isEdit && stage.id ? { id: stage.id } : {}),
    name: stage.name,
    order: stage.order,
    works: stage.works.map(work => ({
      ...(isEdit && work.id ? { id: work.id } : {}),
      name: work.name,
      order: work.order,
      colorRgb: work.colorRgb,
      isClosed: work.isClosed,
      periods: work.periods.map(period => ({
        ...(isEdit && period.id ? { id: period.id } : {}),
        startDate: new Date(period.startDate).toISOString(),
        endDate: new Date(period.endDate).toISOString(),
        isClosed: period.isClosed,
      })),
      assignedUserIds: work.assignedUserIds,
      comments: work.comments
        .filter(c => c.content.trim())
        .map(c => ({
          ...(isEdit && c.id ? { id: c.id } : {}),
          content: c.content.trim(),
        })),
    })),
    children: stage.children.map(child => mapStageToDto(child, isEdit)),
  };
}

// ——— Komponent ———

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
  initialCostEstimateId,
  initialCostEstimateName,
}: WorkScheduleFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const [scheduleName, setScheduleName] = useState("");
  const [stages, setStages] = useState<StageFormData[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [draggedStage, setDraggedStage] = useState<string | null>(null);
  const [draggedWork, setDraggedWork] = useState<{ stageId: string; workId: string } | null>(null);

  const [scheduleMode, setScheduleMode] = useState<'manual' | 'linked'>('manual');
  const [selectedCostEstimateId, setSelectedCostEstimateId] = useState<string>("");
  const [costEstimates, setCostEstimates] = useState<CostEstimateListItemWeb[]>([]);
  const [loadingCostEstimates, setLoadingCostEstimates] = useState(false);

  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const childStageBorderColor = useColorModeValue("blue.200", "blue.700");

  const isCostEstimateSynced = mode === 'edit' && !!schedule?.costEstimateId;

  useEffect(() => {
    if (isOpen) {
      if (mode === 'create') {
        resetForm();
      } else if (mode === 'edit' && schedule) {
        loadScheduleData();
      }
    }
  }, [isOpen, mode, schedule]);

  useEffect(() => {
    if (isOpen && mode === 'create' && scheduleMode === 'linked') {
      if (initialCostEstimateId) {
        // Lista dropdownu: tylko wstępnie wybrany kosztorys (nie trzeba pobierać wszystkich)
        setCostEstimates(
          initialCostEstimateName
            ? [{ id: initialCostEstimateId, name: initialCostEstimateName } as CostEstimateListItemWeb]
            : []
        );
      } else {
        fetchCostEstimates();
      }
    }
  }, [isOpen, mode, scheduleMode, initialCostEstimateId]);

  const resetForm = () => {
    setScheduleName("");
    setStages([]);
    // Jeśli podano initialCostEstimateId, otwórz od razu w trybie linked
    if (initialCostEstimateId) {
      setScheduleMode('linked');
      setSelectedCostEstimateId(initialCostEstimateId);
      setCostEstimates(
        initialCostEstimateName
          ? [{ id: initialCostEstimateId, name: initialCostEstimateName } as CostEstimateListItemWeb]
          : []
      );
    } else {
      setScheduleMode('manual');
      setSelectedCostEstimateId("");
      setCostEstimates([]);
    }
  };

  const loadScheduleData = () => {
    if (!schedule) return;
    setScheduleName(schedule.name);
    const loadedStages = schedule.stages
      .sort((a, b) => a.order - b.order)
      .map(stage => loadStageFromApi(stage));
    setStages(loadedStages);
  };

  const fetchCostEstimates = async () => {
    setLoadingCostEstimates(true);
    try {
      const [mine, shared] = await Promise.all([
        costEstimateApi.getCostEstimatesByScope(tenantId, projectId, ResourceScope.Mine),
        costEstimateApi.getCostEstimatesByScope(tenantId, projectId, ResourceScope.Shared),
      ]);
      const allMap = new Map<string, CostEstimateListItemWeb>();
      [...mine, ...shared].forEach(ce => allMap.set(ce.id, ce));
      setCostEstimates(Array.from(allMap.values()));
    } catch {
      toast({ title: "Błąd", description: "Nie udało się pobrać listy kosztorysów", status: "error", duration: 3000 });
    } finally {
      setLoadingCostEstimates(false);
    }
  };

  // ——— Operacje na etapach ———

  const addStage = () => {
    const newStage: StageFormData = {
      tempId: `stage-${Date.now()}`,
      name: "",
      order: stages.length,
      works: [],
      children: [],
    };
    setStages(prev => [...prev, newStage]);
  };

  const addChildStage = (parentTempId: string) => {
    const newStage: StageFormData = {
      tempId: `stage-child-${Date.now()}-${Math.random().toString(36).slice(2)}`,
      name: "",
      order: 0,
      works: [],
      children: [],
    };
    setStages(prev => addChildToTree(prev, parentTempId, newStage));
  };

  const removeStage = (tempId: string) => {
    setStages(prev =>
      removeStageFromTree(prev, tempId).map((s, idx) => ({ ...s, order: idx }))
    );
  };

  const updateStageName = (tempId: string, name: string) => {
    setStages(prev => updateStageInTree(prev, tempId, s => ({ ...s, name })));
  };

  // ——— Operacje na pracach (rekurencyjne przez updateStageInTree) ———

  const addWork = (stageTempId: string) => {
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => {
        const newWork: WorkFormData = {
          tempId: `work-${Date.now()}`,
          name: "",
          order: stage.works.length,
          colorRgb: PRESET_COLORS[stage.works.length % PRESET_COLORS.length],
          isClosed: false,
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
      })
    );
  };

  const removeWork = (stageTempId: string, workTempId: string) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.filter(w => w.tempId !== workTempId).map((w, idx) => ({ ...w, order: idx })),
      }))
    );
  };

  const updateWork = (stageTempId: string, workTempId: string, updates: Partial<WorkFormData>) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.map(w => w.tempId === workTempId ? { ...w, ...updates } : w),
      }))
    );
  };

  const addPeriod = (stageTempId: string, workTempId: string) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.map(work => {
          if (work.tempId !== workTempId) return work;
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
          return { ...work, periods: [...work.periods, newPeriod], isClosed: false };
        }),
      }))
    );
  };

  const removePeriod = (stageTempId: string, workTempId: string, periodTempId: string) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.map(work => {
          if (work.tempId !== workTempId) return work;
          return { ...work, periods: work.periods.filter(p => p.tempId !== periodTempId) };
        }),
      }))
    );
  };

  const updatePeriod = (
    stageTempId: string,
    workTempId: string,
    periodTempId: string,
    updates: Partial<WorkPeriodFormData>
  ) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.map(work => {
          if (work.tempId !== workTempId) return work;
          return {
            ...work,
            periods: work.periods.map(p => {
              if (p.tempId !== periodTempId) return p;
              const updated = { ...p, ...updates };
              if (updates.startDate) {
                const startDate = new Date(updates.startDate);
                startDate.setDate(startDate.getDate() + 1);
                updated.endDate = startDate.toISOString().split("T")[0];
              }
              return updated;
            }),
          };
        }),
      }))
    );
  };

  const toggleAssignedUser = (stageTempId: string, workTempId: string, userId: string) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.map(w => {
          if (w.tempId !== workTempId) return w;
          const isAssigned = w.assignedUserIds.includes(userId);
          return {
            ...w,
            assignedUserIds: isAssigned
              ? w.assignedUserIds.filter(id => id !== userId)
              : [...w.assignedUserIds, userId],
          };
        }),
      }))
    );
  };

  const addComment = (stageTempId: string, workTempId: string) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.map(work => {
          if (work.tempId !== workTempId) return work;
          const newComment: WorkCommentFormData = {
            tempId: `comment-${Date.now()}`,
            content: "",
          };
          return { ...work, comments: [...work.comments, newComment] };
        }),
      }))
    );
  };

  const updateComment = (
    stageTempId: string,
    workTempId: string,
    commentTempId: string,
    content: string
  ) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.map(work => {
          if (work.tempId !== workTempId) return work;
          return {
            ...work,
            comments: work.comments.map(c =>
              c.tempId === commentTempId ? { ...c, content } : c
            ),
          };
        }),
      }))
    );
  };

  const removeComment = (stageTempId: string, workTempId: string, commentTempId: string) => {
    setStages(prev =>
      updateStageInTree(prev, stageTempId, stage => ({
        ...stage,
        works: stage.works.map(work => {
          if (work.tempId !== workTempId) return work;
          return { ...work, comments: work.comments.filter(c => c.tempId !== commentTempId) };
        }),
      }))
    );
  };

  // D&D etapów (tylko na poziomie głównym)
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
      let movedWork: WorkFormData | null = null;
      const removeWorkFromTree = (stageList: StageFormData[]): StageFormData[] =>
        stageList.map((s) => {
          if (s.tempId === draggedWork!.stageId) {
            const workIdx = s.works.findIndex((w) => w.tempId === draggedWork!.workId);
            if (workIdx !== -1) {
              movedWork = s.works[workIdx];
              return {
                ...s,
                works: s.works.filter((_, i) => i !== workIdx).map((w, idx) => ({ ...w, order: idx })),
              };
            }
          }
          return { ...s, children: removeWorkFromTree(s.children) };
        });

      const addWorkToTree = (stageList: StageFormData[]): StageFormData[] =>
        stageList.map((s) => {
          if (s.tempId === targetStageTempId && movedWork) {
            const targetIdx = s.works.findIndex((w) => w.tempId === targetWorkTempId);
            const newWorks = [...s.works];
            newWorks.splice(targetIdx, 0, movedWork!);
            return {
              ...s,
              works: newWorks.map((w, idx) => ({ ...w, order: idx })),
            };
          }
          return { ...s, children: addWorkToTree(s.children) };
        });

      const stagesAfterRemove = removeWorkFromTree(prevStages);
      if (!movedWork) return prevStages;
      return addWorkToTree(stagesAfterRemove);
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

    if (scheduleName.length > 200) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa harmonogramu nie może przekraczać 200 znaków",
        status: "error",
        duration: 3000,
      });
      return;
    }

    if (scheduleMode === 'linked' && !selectedCostEstimateId) {
      toast({
        title: "Błąd walidacji",
        description: "Wybierz kosztorys, z którego ma zostać wygenerowany harmonogram",
        status: "error",
        duration: 3000,
      });
      return;
    }

    const validationError = validateStagesTree(stages);
    if (validationError) {
      toast({
        title: "Błąd walidacji",
        description: validationError,
        status: "error",
        duration: 3000,
      });
      return;
    }

    setSubmitting(true);
    try {
      const mappedStages = stages.map((stage) => mapStageToDto(stage, mode === 'edit'));
      const command = {
        name: scheduleName,
        ...(scheduleMode === 'linked' && mode === 'create' ? { costEstimateId: selectedCostEstimateId } : {}),
        stages: mappedStages,
      };

      if (mode === 'create') {
        const response = await projectApi.createWorkSchedule(tenantId, projectId, command);
        toast({
          title: "Sukces",
          description: "Harmonogram został utworzony",
          status: "success",
          duration: 3000,
        });
        onSuccess?.();
        onClose();
        const newId = response?.data?.id;
        if (newId) {
          navigate(`/projects/${projectId}/schedules/${newId}`);
        }
      } else {
        await projectApi.updateWorkSchedule(tenantId, projectId, schedule!.id, command);
        toast({
          title: "Sukces",
          description: "Harmonogram został zaktualizowany",
          status: "success",
          duration: 3000,
        });
        onSuccess?.();
        onClose();
      }
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

  const renderWork = (work: WorkFormData, stageTempId: string, workIndex: number) => (
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
        onDragStart={(e) => handleWorkDragStart(e, stageTempId, work.tempId)}
        onDragOver={handleStageDragOver}
        onDrop={(e) => handleWorkDrop(e, stageTempId, work.tempId)}
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
            onChange={(e) => updateWork(stageTempId, work.tempId, { name: e.target.value })}
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
              removeWork(stageTempId, work.tempId);
            }}
          />
        </HStack>
        <AccordionIcon ml={2} display={{ base: "none", md: "block" }} />
      </AccordionButton>
      <AccordionPanel pb={4} pt={2}>
        <VStack spacing={3} align="stretch" pl={{ base: 0, md: 4 }}>
          {/* Okresy pracy */}
          <FormControl>
            <HStack justify="space-between" mb={2} flexWrap="wrap" gap={2}>
              <FormLabel fontSize={{ base: "xs", md: "sm" }} mb={0}>Okresy pracy</FormLabel>
              <Button
                size={{ base: "xs", md: "sm" }}
                leftIcon={<Plus size={12} />}
                onClick={() => addPeriod(stageTempId, work.tempId)}
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
                      onChange={(e) => updatePeriod(stageTempId, work.tempId, period.tempId, { startDate: e.target.value })}
                      placeholder="Od"
                      flex={1}
                    />
                    <Input
                      type="date"
                      size={{ base: "sm", md: "md" }}
                      value={period.endDate}
                      onChange={(e) => updatePeriod(stageTempId, work.tempId, period.tempId, { endDate: e.target.value })}
                      placeholder="Do"
                      flex={1}
                    />
                    <IconButton
                      aria-label="Usuń okres"
                      icon={<Trash2 size={14} />}
                      size={{ base: "sm", md: "md" }}
                      colorScheme="red"
                      variant="ghost"
                      onClick={() => removePeriod(stageTempId, work.tempId, period.tempId)}
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
                      updateWork(stageTempId, work.tempId, {
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
          <FormControl>
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
                  onClick={() => updateWork(stageTempId, work.tempId, { colorRgb: color })}
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
                  onChange={(e) => updateWork(stageTempId, work.tempId, { colorRgb: e.target.value })}
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
          <FormControl>
            <Checkbox
              size={{ base: "sm", md: "md" }}
              colorScheme="green"
              isChecked={work.isClosed}
              onChange={(e) => {
                const newClosedState = e.target.checked;
                const updatedPeriods = work.periods.map(p => ({ ...p, isClosed: newClosedState }));
                updateWork(stageTempId, work.tempId, { isClosed: newClosedState, periods: updatedPeriods });
              }}
            >
              <Text fontSize={{ base: "xs", md: "sm" }}>Prace zakończone</Text>
            </Checkbox>
          </FormControl>

          {/* Przypisani członkowie */}
          <FormControl>
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
                  onClick={() => toggleAssignedUser(stageTempId, work.tempId, member.userId)}
                  _hover={{ transform: "scale(1.05)" }}
                  transition="all 0.2s"
                >
                  {member.firstName} {member.lastName}
                </Badge>
              ))}
            </Flex>
          </FormControl>

          {/* Komentarze */}
          <FormControl>
            <HStack justify="space-between" mb={2} flexWrap="wrap" gap={2}>
              <FormLabel fontSize={{ base: "xs", md: "sm" }} mb={0}>Komentarze</FormLabel>
              <Button
                size={{ base: "xs", md: "sm" }}
                leftIcon={<Plus size={12} />}
                onClick={() => addComment(stageTempId, work.tempId)}
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
                      onChange={(e) => updateComment(stageTempId, work.tempId, comment.tempId, e.target.value)}
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
                      onClick={() => removeComment(stageTempId, work.tempId, comment.tempId)}
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
  );

  const renderStage = (stage: StageFormData, stageIndex: number, depth: number = 0, pathLabel: string = ''): React.ReactElement => {
    const label = pathLabel ? `${pathLabel}.${stageIndex + 1}` : `${stageIndex + 1}`;
    return (
      <AccordionItem
        key={stage.tempId}
        borderWidth="2px"
        borderRadius="lg"
        borderColor={
          depth === 0
            ? (draggedStage === stage.tempId ? "blue.400" : borderColor)
            : childStageBorderColor
        }
        bg={bgColor}
        mb={depth === 0 ? 3 : 2}
        ml={depth > 0 ? 6 : 0}
      >
        <AccordionButton
          p={{ base: 2, md: 4 }}
          _hover={{ bg: hoverBg }}
          draggable={depth === 0}
          onDragStart={depth === 0 ? (e) => handleStageDragStart(e, stage.tempId) : undefined}
          onDragOver={depth === 0 ? handleStageDragOver : undefined}
          onDrop={depth === 0 ? (e) => handleStageDrop(e, stage.tempId) : undefined}
          flexDirection={{ base: "column", md: "row" }}
          alignItems={{ base: "flex-start", md: "center" }}
        >
          <HStack spacing={2} flex={1} width="100%" alignItems={{ base: "flex-start", md: "center" }}>
            {depth === 0 && (
              <Box cursor="grab" _active={{ cursor: "grabbing" }} display={{ base: "none", md: "block" }}>
                <GripVertical size={20} />
              </Box>
            )}
            <Badge colorScheme={depth === 0 ? "blue" : "purple"} fontSize={{ base: "10px", md: "xs" }} flexShrink={0}>
              Etap {label}
            </Badge>
            {stage.costEstimateGroupId && (
              <Badge colorScheme="orange" fontSize={{ base: "9px", md: "2xs" }} variant="subtle" flexShrink={0}>
                Kosztorys
              </Badge>
            )}
            <Input
              placeholder="Nazwa etapu (wymagane, max 200 znaków)"
              value={stage.name}
              onChange={(e) => updateStageName(stage.tempId, e.target.value)}
              onClick={(e) => e.stopPropagation()}
              flex={1}
              maxLength={200}
              isInvalid={!stage.costEstimateGroupId && !stage.name.trim()}
              isReadOnly={!!stage.costEstimateGroupId}
              title={stage.costEstimateGroupId ? "Nazwa etapu jest zarządzana przez kosztorys" : undefined}
              size={{ base: "sm", md: "md" }}
              width={{ base: "100%", md: "auto" }}
            />
            {!stage.costEstimateGroupId && (
              <Text fontSize={{ base: "9px", md: "xs" }} color="gray.500" minW={{ base: "50px", md: "60px" }} textAlign="right" flexShrink={0}>
                {stage.name.length}/200
              </Text>
            )}
            <IconButton
              aria-label="Usuń etap"
              icon={<Trash2 size={16} />}
              colorScheme="red"
              size={{ base: "sm", md: "md" }}
              variant="ghost"
              flexShrink={0}
              isDisabled={!!stage.costEstimateGroupId}
              title={stage.costEstimateGroupId ? "Nie można usunąć etapu powiązanego z kosztorysem" : undefined}
              onClick={(e) => {
                e.stopPropagation();
                removeStage(stage.tempId);
              }}
            />
          </HStack>
          <AccordionIcon ml={2} display={{ base: "none", md: "block" }} />
        </AccordionButton>
        <AccordionPanel pb={4} pt={2}>
          <VStack spacing={3} align="stretch" pl={{ base: 0, md: depth === 0 ? 8 : 4 }}>
            <Accordion allowMultiple>
              {stage.works.map((work, workIndex) => renderWork(work, stage.tempId, workIndex))}
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
            {stage.children.length > 0 && (
              <Box>
                <Text fontWeight="semibold" fontSize="sm" mb={2} color="purple.600">
                  Podetapy
                </Text>
                <Accordion allowMultiple>
                  {stage.children.map((child, childIndex) =>
                    renderStage(child, childIndex, depth + 1, label)
                  )}
                </Accordion>
              </Box>
            )}
            <Button
              leftIcon={<FolderPlus size={14} />}
              size={{ base: "sm", md: "md" }}
              variant="outline"
              colorScheme="purple"
              onClick={() => addChildStage(stage.tempId)}
            >
              Dodaj podetap
            </Button>
          </VStack>
        </AccordionPanel>
      </AccordionItem>
    );
  };

  const isFromCostEstimate = mode === 'create' && !!initialCostEstimateId;

  const modalTitle = isFromCostEstimate
    ? 'Nowy harmonogram z kosztorysu'
    : mode === 'create'
    ? `Utwórz harmonogram prac - ${projectName}`
    : `Edytuj harmonogram - ${schedule?.name || ''}`;

  const submitButtonText = mode === 'create' ? 'Utwórz harmonogram' : 'Zapisz zmiany';
  const submitLoadingText = mode === 'create' ? 'Tworzenie...' : 'Zapisywanie...';

  return (
    <Modal isOpen={isOpen} onClose={onClose} size={{ base: "full", md: isFromCostEstimate ? "md" : "6xl" }} scrollBehavior="inside">
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

            {mode === 'create' && !isFromCostEstimate && (
              <FormControl>
                <FormLabel fontSize={{ base: "xs", md: "sm" }}>Tryb harmonogramu</FormLabel>
                <RadioGroup value={scheduleMode} onChange={(v) => setScheduleMode(v as 'manual' | 'linked')}>
                  <Stack direction={{ base: "column", md: "row" }} spacing={4}>
                    <Radio value="manual">Ręczny (puste etapy)</Radio>
                    <Radio value="linked">Na podstawie kosztorysu</Radio>
                  </Stack>
                </RadioGroup>
              </FormControl>
            )}

            {scheduleMode === 'linked' && mode === 'create' && !isFromCostEstimate && (
              <FormControl isRequired>
                <FormLabel fontSize={{ base: "xs", md: "sm" }}>Wybierz kosztorys</FormLabel>
                {loadingCostEstimates ? (
                  <Spinner size="sm" />
                ) : (
                  <Select
                    placeholder="-- wybierz kosztorys --"
                    value={selectedCostEstimateId ?? ''}
                    onChange={(e) => setSelectedCostEstimateId(e.target.value || '')}
                    size={{ base: "sm", md: "md" }}
                  >
                    {costEstimates.map((ce) => (
                      <option key={ce.id} value={ce.id}>
                        {ce.name}
                      </option>
                    ))}
                  </Select>
                )}
              </FormControl>
            )}

            {isFromCostEstimate && (
              <Alert status="info" borderRadius="md">
                <AlertIcon />
                <AlertDescription fontSize="sm">
                  Etapy harmonogramu zostaną automatycznie utworzone na podstawie grup z kosztorysu
                  {initialCostEstimateName ? ` „${initialCostEstimateName}"` : ''}.
                </AlertDescription>
              </Alert>
            )}

            {isCostEstimateSynced && mode === 'edit' && (
              <Alert status="info" borderRadius="md">
                <AlertIcon />
                <AlertDescription fontSize="sm">
                  Ten harmonogram jest powiązany z kosztorysem. Etapy i zakresy robót odzwierciedlają strukturę kosztorysu.
                </AlertDescription>
              </Alert>
            )}

            {!isFromCostEstimate && (
            <>
            <Divider />

            <Box>
              <HStack justify="space-between" mb={4} flexWrap="wrap" gap={2}>
                <Text fontWeight="bold" fontSize={{ base: "md", md: "lg" }}>
                  Etapy i prace
                </Text>
                {scheduleMode === 'manual' && (
                  <Button
                    leftIcon={<Plus size={16} />}
                    colorScheme="blue"
                    size={{ base: "sm", md: "md" }}
                    onClick={addStage}
                  >
                    Dodaj etap
                  </Button>
                )}
              </HStack>
              <VStack spacing={4} align="stretch">
                <Accordion allowMultiple>
                  {stages.map((stage, stageIndex) => renderStage(stage, stageIndex))}
                </Accordion>
                {stages.length === 0 && scheduleMode === 'manual' && (
                  <Box textAlign="center" py={8} color="gray.500">
                    <Text fontSize={{ base: "xs", md: "sm" }}>Brak etapów. Kliknij "Dodaj etap" aby rozpocząć.</Text>
                  </Box>
                )}
                {stages.length === 0 && scheduleMode === 'linked' && (
                  <Box textAlign="center" py={8} color="gray.500">
                    <Text fontSize={{ base: "xs", md: "sm" }}>Wybierz kosztorys, aby zobaczyć etapy harmonogramu.</Text>
                  </Box>
                )}
              </VStack>
            </Box>
            </>
            )}
          </VStack>
        </ModalBody>

        <ModalFooter position={{ base: "fixed", md: "relative" }} bottom={{ base: 0, md: "auto" }} left={{ base: 0, md: "auto" }} right={{ base: 0, md: "auto" }} bg={bgColor} borderTopWidth={{ base: "1px", md: 0 }} borderColor={borderColor} p={3} gap={2}>
          {mode === 'edit' && schedule?.id && (
            <Button
              variant="outline"
              colorScheme="orange"
              onClick={() => { onClose(); navigate(`/projects/${projectId}/schedules/${schedule.id}`); }}
              isDisabled={submitting}
              size={{ base: "sm", md: "md" }}
              mr="auto"
            >
              Przejdź do harmonogramu
            </Button>
          )}
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
