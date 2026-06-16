import { useContext, useState } from "react";
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
  IconButton,
  Textarea,
  Button,
  Box,
  useColorModeValue,
} from "@chakra-ui/react";
import { Pencil, Trash2, Check, X } from "lucide-react";
import { AuthContext } from "../../../context/AuthContext";
import { useGantt } from "../GanttContext";
import type { WorkScheduleStageWorkWeb, WorkScheduleStageWorkCommentWeb } from "../../../types/workSchedule.types";
import { formatDateTimeCompact, parseApiDateTime } from "../../../utils/formatters";

interface CommentsModalProps {
  isOpen: boolean;
  onClose: () => void;
  stageId: string;
  work: WorkScheduleStageWorkWeb;
}

function fmtDateTime(iso: string): string {
  return formatDateTimeCompact(iso);
}

export default function CommentsModal({ isOpen, onClose, stageId, work }: CommentsModalProps) {
  const { user } = useContext(AuthContext);
  const { addComment, updateComment, deleteComment, isMutating } = useGantt();

  const [newText, setNewText] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingText, setEditingText] = useState("");

  const ownBg = useColorModeValue("neutral.50", "neutral.800");
  const otherBg = useColorModeValue("neutral.25", "neutral.700");
  const borderColor = useColorModeValue("neutral.100", "neutral.600");

  const comments = [...(work.comments ?? [])].sort(
    (a, b) => (parseApiDateTime(a.createdAt)?.getTime() ?? 0) - (parseApiDateTime(b.createdAt)?.getTime() ?? 0)
  );

  const handleAdd = async () => {
    if (!newText.trim()) return;
    await addComment(stageId, work.id, newText.trim());
    setNewText("");
  };

  const startEdit = (comment: WorkScheduleStageWorkCommentWeb) => {
    setEditingId(comment.id);
    setEditingText(comment.content);
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditingText("");
  };

  const handleUpdate = async () => {
    if (!editingId || !editingText.trim()) return;
    await updateComment(stageId, work.id, editingId, editingText.trim());
    setEditingId(null);
    setEditingText("");
  };

  const handleDelete = async (commentId: string) => {
    await deleteComment(stageId, work.id, commentId);
  };

  return (
    <Drawer isOpen={isOpen} onClose={onClose} placement="bottom" size="md">
      <DrawerOverlay />
      <DrawerContent borderTopRadius="lg" maxH="80vh">
        <DrawerCloseButton />
        <DrawerHeader borderBottomWidth="1px">Komentarze — {work.name}</DrawerHeader>
        <DrawerBody py={4} overflowY="auto">
          <VStack spacing={3} align="stretch">
            {comments.length === 0 && (
              <Text fontSize="sm" color="neutral.400" textAlign="center" py={4}>
                Brak komentarzy
              </Text>
            )}
            {comments.map(comment => {
              const isOwn = comment.createdByUserId === user?.id;
              const isUpdating = isMutating.has(`updateComment-${comment.id}`);
              const isDeleting = isMutating.has(`deleteComment-${comment.id}`);

              return (
                <Box
                  key={comment.id}
                  bg={isOwn ? ownBg : otherBg}
                  borderRadius="md"
                  p={3}
                  borderWidth="1px"
                  borderColor={borderColor}
                >
                  <HStack justify="space-between" mb={1}>
                    <Text fontSize="xs" color="neutral.500" fontWeight="medium">
                      {comment.createdByUserName}
                    </Text>
                    <HStack spacing={1}>
                      <Text fontSize="xs" color="neutral.400">{fmtDateTime(comment.createdAt)}</Text>
                      {isOwn && editingId !== comment.id && (
                        <>
                          <IconButton
                            aria-label="Edytuj komentarz"
                            icon={<Pencil size={12} />}
                            size="xs"
                            variant="ghost"
                            onClick={() => startEdit(comment)}
                          />
                          <IconButton
                            aria-label="Usuń komentarz"
                            icon={<Trash2 size={12} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="red"
                            isLoading={isDeleting}
                            onClick={() => handleDelete(comment.id)}
                          />
                        </>
                      )}
                      {editingId === comment.id && (
                        <>
                          <IconButton
                            aria-label="Zatwierdź"
                            icon={<Check size={12} />}
                            size="xs"
                            colorScheme="primary"
                            variant="ghost"
                            isLoading={isUpdating}
                            onClick={handleUpdate}
                          />
                          <IconButton
                            aria-label="Anuluj"
                            icon={<X size={12} />}
                            size="xs"
                            variant="ghost"
                            onClick={cancelEdit}
                          />
                        </>
                      )}
                    </HStack>
                  </HStack>

                  {editingId === comment.id ? (
                    <Textarea
                      value={editingText}
                      onChange={e => setEditingText(e.target.value)}
                      size="sm"
                      rows={2}
                      autoFocus
                    />
                  ) : (
                    <Text fontSize="sm" whiteSpace="pre-wrap">{comment.content}</Text>
                  )}
                </Box>
              );
            })}
          </VStack>
        </DrawerBody>
        <DrawerFooter borderTopWidth="1px">
          <VStack w="100%" spacing={2} align="stretch">
            <Textarea
              placeholder="Nowy komentarz..."
              value={newText}
              onChange={e => setNewText(e.target.value)}
              size="sm"
              rows={2}
            />
            <HStack justify="flex-end">
              <Button
                size="sm"
                colorScheme="primary"
                onClick={handleAdd}
                isDisabled={!newText.trim()}
                isLoading={isMutating.has(`addComment-${work.id}`)}
              >
                Dodaj komentarz
              </Button>
            </HStack>
          </VStack>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
