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
  Input,
  Avatar,
  Wrap,
  WrapItem,
  Tooltip,
} from "@chakra-ui/react";
import { Plus, Trash2 } from "lucide-react";
import { projectApi } from "../api/projectApi";
import { handleApiError } from "../utils/handleApiError";
import type { EditableWork } from "../types/workSchedule.types";

interface Member {
  userId: string;
  userName: string;
}

interface WorkDetailsModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  workScheduleId: string;
  work: EditableWork | null;
  members?: Member[];
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
  members = [],
  onWorkUpdated,
}: WorkDetailsModalProps) {
  const toast = useToast();
  const [periods, setPeriods] = useState<any[]>([]);
  const [comments, setComments] = useState<CommentFormData[]>([]);
  const [assignedUserIds, setAssignedUserIds] = useState<string[]>([]);
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
        startDate: p.startDate.slice(0, 10),
        endDate: p.endDate.slice(0, 10),
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

    setAssignedUserIds(work.assignees.map((a: any) => a.userId));
  };

  const togglePeriodClosed = (periodId: string) => {
    setPeriods(
      periods.map((p) => (p.id === periodId ? { ...p, isClosed: !p.isClosed } : p))
    );
  };

  const updatePeriodDate = (periodId: string, field: 'startDate' | 'endDate', value: string) => {
    setPeriods(periods.map((p) => (p.id === periodId ? { ...p, [field]: value } : p)));
  };

  const addPeriod = () => {
    const today = new Date().toISOString().slice(0, 10);
    const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10);
    setPeriods([...periods, { id: `new-${Date.now()}`, startDate: today, endDate: tomorrow, isClosed: false }]);
  };

  const removePeriod = (periodId: string) => {
    setPeriods(periods.filter((p) => p.id !== periodId));
  };

  const toggleAssignee = (userId: string) => {
    setAssignedUserIds((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
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
      const scheduleResponse = await projectApi.getWorkSchedule(tenantId, projectId, workScheduleId);
      const schedule = scheduleResponse.data;

      const mapWork = (w: any) => {
        if (w.id === work.id) {
          return {
            id: w.id,
            name: w.name,
            order: w.order,
            colorRgb: w.colorRgb,
            isClosed: w.isClosed,
            periods: periods.map((p) => ({
              // Pomiń tymczasowe ID nowych okresów — backend nada własne
              ...(p.id && !String(p.id).startsWith('new-') ? { id: p.id } : {}),
              startDate: p.startDate,
              endDate: p.endDate,
              isClosed: p.isClosed,
            })),
            assignedUserIds: assignedUserIds,
            comments: comments
              .filter((c) => c.content.trim())
              .map((c) => ({ id: c.id, content: c.content.trim() })),
          };
        }
        return {
          id: w.id,
          name: w.name,
          order: w.order,
          colorRgb: w.colorRgb,
          isClosed: w.isClosed,
          periods: w.periods.map((p: any) => ({ id: p.id, startDate: p.startDate, endDate: p.endDate, isClosed: p.isClosed })),
          assignedUserIds: w.assignees.map((a: any) => a.userId),
          comments: w.comments.map((c: any) => ({ id: c.id, content: c.content })),
        };
      };

      const mapStage = (stage: any): any => ({
        id: stage.id,
        name: stage.name,
        order: stage.order,
        works: stage.works.map(mapWork),
        children: (stage.childStages ?? []).map(mapStage),
      });

      const command = {
        name: schedule.name,
        stages: schedule.stages.map(mapStage),
      };

      await projectApi.updateWorkSchedule(tenantId, projectId, workScheduleId, command);

      toast({ title: "Sukces", description: "Praca została zaktualizowana", status: "success", duration: 3000 });
      onWorkUpdated?.();
      onClose();
    } catch (error) {
      const { title, description } = handleApiError(error);
      toast({ title, description, status: "error", duration: 3000 });
    } finally {
      setSubmitting(false);
    }
  };

  if (!work) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} size={{ base: "full", md: "xl" }}>
      <ModalOverlay />
      <ModalContent mx={{ base: 0, md: "auto" }}>
        <ModalHeader fontSize={{ base: "lg", md: "xl" }}>
          <VStack align="flex-start" spacing={2}>
            <Text>Szczegóły zakresu prac</Text>
            <Badge colorScheme="purple" fontSize={{ base: "xs", md: "md" }}>
              {work.name}
            </Badge>
          </VStack>
        </ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">

            {/* Okresy pracy */}
            <FormControl>
              <HStack justify="space-between" mb={2}>
                <FormLabel fontSize="sm" fontWeight="bold" mb={0}>
                  Okresy wykonania
                </FormLabel>
                <Button size="sm" leftIcon={<Plus size={14} />} onClick={addPeriod} colorScheme="blue" variant="ghost">
                  Dodaj okres
                </Button>
              </HStack>
              {periods.length === 0 && (
                <Text fontSize="sm" color="gray.500">Brak okresów — dodaj przynajmniej jeden</Text>
              )}
              <VStack spacing={2} align="stretch">
                {periods.map((period, idx) => (
                  <HStack
                    key={period.id}
                    spacing={2}
                    p={3}
                    borderWidth="1px"
                    borderRadius="md"
                    borderColor={borderColor}
                    flexWrap="wrap"
                  >
                    <Text fontSize="xs" fontWeight="bold" minW="18px" color="gray.500">{idx + 1}.</Text>
                    <VStack align="flex-start" spacing={1} flex={1}>
                      <HStack spacing={2} flexWrap="wrap">
                        <VStack align="flex-start" spacing={0}>
                          <Text fontSize="2xs" color="gray.500">Od</Text>
                          <Input
                            type="date"
                            size="sm"
                            value={period.startDate}
                            onChange={(e) => updatePeriodDate(period.id, 'startDate', e.target.value)}
                            maxW="160px"
                          />
                        </VStack>
                        <VStack align="flex-start" spacing={0}>
                          <Text fontSize="2xs" color="gray.500">Do</Text>
                          <Input
                            type="date"
                            size="sm"
                            value={period.endDate}
                            onChange={(e) => updatePeriodDate(period.id, 'endDate', e.target.value)}
                            maxW="160px"
                          />
                        </VStack>
                      </HStack>
                      <Checkbox
                        size="sm"
                        isChecked={period.isClosed}
                        onChange={() => togglePeriodClosed(period.id)}
                        colorScheme="green"
                      >
                        <Text fontSize="xs">Okres wykonany</Text>
                      </Checkbox>
                    </VStack>
                    <Tooltip label="Usuń okres">
                      <IconButton
                        aria-label="Usuń okres"
                        icon={<Trash2 size={14} />}
                        size="xs"
                        colorScheme="red"
                        variant="ghost"
                        onClick={() => removePeriod(period.id)}
                      />
                    </Tooltip>
                  </HStack>
                ))}
              </VStack>
            </FormControl>

            <Divider />

            {/* Osoby przypisane */}
            {members.length > 0 && (
              <>
                <FormControl>
                  <FormLabel fontSize="sm" fontWeight="bold">Przypisane osoby</FormLabel>
                  <Wrap spacing={2}>
                    {members.map((member) => {
                      const isAssigned = assignedUserIds.includes(member.userId);
                      return (
                        <WrapItem key={member.userId}>
                          <Tooltip label={isAssigned ? `Odznacz ${member.userName}` : `Przypisz ${member.userName}`}>
                            <HStack
                              spacing={2}
                              px={3}
                              py={1.5}
                              borderRadius="full"
                              borderWidth="1px"
                              borderColor={isAssigned ? "blue.400" : borderColor}
                              bg={isAssigned ? "blue.50" : undefined}
                              cursor="pointer"
                              onClick={() => toggleAssignee(member.userId)}
                              _hover={{ bg: isAssigned ? "blue.100" : "gray.50" }}
                              transition="all 0.15s"
                            >
                              <Avatar size="2xs" name={member.userName} />
                              <Text fontSize="xs" fontWeight={isAssigned ? "semibold" : "normal"} color={isAssigned ? "blue.700" : undefined}>
                                {member.userName}
                              </Text>
                              {isAssigned && <Badge colorScheme="blue" fontSize="2xs" variant="solid">✓</Badge>}
                            </HStack>
                          </Tooltip>
                        </WrapItem>
                      );
                    })}
                  </Wrap>
                </FormControl>
                <Divider />
              </>
            )}

            {/* Komentarze */}
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
