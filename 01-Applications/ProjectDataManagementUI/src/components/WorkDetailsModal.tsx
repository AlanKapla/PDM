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
  useColorModeValue,
  useToast,
  IconButton,
  Badge,
  Checkbox,
  Textarea,
  FormControl,
  FormLabel,
  Divider,
} from "@chakra-ui/react";
import { Plus, Trash2 } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import type { WorkScheduleStageWorkWeb } from "../types/workSchedule.types";

interface WorkDetailsModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  workScheduleId: string;
  work: WorkScheduleStageWorkWeb | null;
  onWorkUpdated?: () => void;
}

interface CommentFormData {
  id?: string;
  tempId: string;
  content: string;
}

export default function WorkDetailsModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  workScheduleId,
  work,
  onWorkUpdated,
}: WorkDetailsModalProps) {
  const toast = useToast();
  const [periods, setPeriods] = useState<any[]>([]);
  const [comments, setComments] = useState<CommentFormData[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const borderColor = useColorModeValue("gray.200", "gray.700");

  useEffect(() => {
    if (isOpen && work) {
      loadWorkData();
    }
  }, [isOpen, work]);

  const loadWorkData = () => {
    if (!work) return;

    setPeriods(
      work.periods.map((p) => ({
        id: p.id,
        startDate: p.startDate,
        endDate: p.endDate,
        isClosed: p.isClosed,
      }))
    );

    setComments(
      work.comments.map((c) => ({
        id: c.id,
        tempId: `comment-${c.id}`,
        content: c.content,
      }))
    );
  };

  const togglePeriodClosed = (periodId: string) => {
    setPeriods(
      periods.map((p) => (p.id === periodId ? { ...p, isClosed: !p.isClosed } : p))
    );
  };

  const addComment = () => {
    const newComment: CommentFormData = {
      tempId: `comment-new-${Date.now()}`,
      content: "",
    };
    setComments([...comments, newComment]);
  };

  const updateComment = (tempId: string, content: string) => {
    setComments(comments.map((c) => (c.tempId === tempId ? { ...c, content } : c)));
  };

  const removeComment = (tempId: string) => {
    setComments(comments.filter((c) => c.tempId !== tempId));
  };

  const handleSubmit = async () => {
    if (!work) return;

    setSubmitting(true);
    try {
      // Tutaj wywołamy endpoint do aktualizacji pracy (tylko okresy i komentarze)
      // Najpierw musimy pobrać pełny harmonogram, zmienić tę pracę, i wysłać całość
      const scheduleResponse = await projectApi.getWorkSchedule(tenantId, projectId, workScheduleId);
      const schedule = scheduleResponse.data;

      // Znajdź stage i work
      let updatedStages = schedule.stages.map((stage: any) => ({
        id: stage.id,
        name: stage.name,
        order: stage.order,
        works: stage.works.map((w: any) => {
          if (w.id === work.id) {
            // Aktualizuj tę pracę
            return {
              id: w.id,
              name: w.name,
              order: w.order,
              colorRgb: w.colorRgb,
              isClosed: w.isClosed,
              periods: periods.map((p) => ({
                id: p.id,
                startDate: p.startDate,
                endDate: p.endDate,
                isClosed: p.isClosed,
              })),
              assignedUserIds: w.assignees.map((a: any) => a.userId),
              comments: comments
                .filter((c) => c.content.trim())
                .map((c) => ({
                  id: c.id,
                  content: c.content.trim(),
                })),
            };
          }
          // Pozostałe prace bez zmian
          return {
            id: w.id,
            name: w.name,
            order: w.order,
            colorRgb: w.colorRgb,
            isClosed: w.isClosed,
            periods: w.periods.map((p: any) => ({
              id: p.id,
              startDate: p.startDate,
              endDate: p.endDate,
              isClosed: p.isClosed,
            })),
            assignedUserIds: w.assignees.map((a: any) => a.userId),
            comments: w.comments.map((c: any) => ({
              id: c.id,
              content: c.content,
            })),
          };
        }),
      }));

      const command = {
        name: schedule.name,
        stages: updatedStages,
      };

      await projectApi.updateWorkSchedule(tenantId, projectId, workScheduleId, command);

      toast({
        title: "Sukces",
        description: "Praca została zaktualizowana",
        status: "success",
        duration: 3000,
      });
      onWorkUpdated?.();
      onClose();
    } catch (error) {
      console.error("Błąd aktualizacji pracy:", error);
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

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("pl-PL");
  };

  if (!work) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="xl">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>
          <VStack align="flex-start" spacing={2}>
            <Text>Szczegóły zakresu prac</Text>
            <Badge colorScheme="purple" fontSize="md">
              {work.name}
            </Badge>
          </VStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            <FormControl>
              <FormLabel fontSize="sm" fontWeight="bold">
                Okresy pracy
              </FormLabel>
              <VStack spacing={2} align="stretch">
                {periods.map((period, idx) => (
                  <HStack
                    key={period.id}
                    spacing={3}
                    p={3}
                    borderWidth="1px"
                    borderRadius="md"
                    borderColor={borderColor}
                  >
                    <Text fontSize="sm" fontWeight="medium" minW="20px">
                      {idx + 1}.
                    </Text>
                    <VStack align="flex-start" spacing={1} flex={1}>
                      <Text fontSize="sm">
                        {formatDate(period.startDate)} - {formatDate(period.endDate)}
                      </Text>
                      <Checkbox
                        size="sm"
                        isChecked={period.isClosed}
                        onChange={() => togglePeriodClosed(period.id)}
                        colorScheme="green"
                      >
                        <Text fontSize="xs">Okres wykonany</Text>
                      </Checkbox>
                    </VStack>
                  </HStack>
                ))}
              </VStack>
            </FormControl>

            <Divider />

            <FormControl>
              <HStack justify="space-between" mb={2}>
                <FormLabel fontSize="sm" fontWeight="bold" mb={0}>
                  Komentarze
                </FormLabel>
                <Button
                  size="sm"
                  leftIcon={<Plus size={14} />}
                  onClick={addComment}
                  colorScheme="purple"
                  variant="ghost"
                >
                  Dodaj
                </Button>
              </HStack>
              <VStack spacing={3} align="stretch">
                {comments.length === 0 && (
                  <Text fontSize="sm" color="gray.500">
                    Brak komentarzy
                  </Text>
                )}
                {comments.map((comment, idx) => (
                  <HStack key={comment.tempId} spacing={2} align="flex-start">
                    <Text fontSize="sm" minW="20px" mt={2}>
                      {idx + 1}.
                    </Text>
                    <Textarea
                      size="sm"
                      value={comment.content}
                      onChange={(e) => updateComment(comment.tempId, e.target.value)}
                      placeholder="Treść komentarza (max 2000 znaków)"
                      maxLength={2000}
                      resize="vertical"
                      minH="80px"
                    />
                    <IconButton
                      aria-label="Usuń komentarz"
                      icon={<Trash2 size={16} />}
                      size="sm"
                      colorScheme="red"
                      variant="ghost"
                      onClick={() => removeComment(comment.tempId)}
                      mt={1}
                    />
                  </HStack>
                ))}
              </VStack>
            </FormControl>
          </VStack>
        </ModalBody>

        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={onClose}>
            Anuluj
          </Button>
          <Button colorScheme="blue" onClick={handleSubmit} isLoading={submitting}>
            Zapisz zmiany
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
