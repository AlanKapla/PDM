import { useState, useRef } from "react";
import {
  Box,
  VStack,
  HStack,
  Text,
  Input,
  Button,
  IconButton,
  Avatar,
  Divider,
  Textarea,
  Spinner,
  useColorModeValue,
} from "@chakra-ui/react";
import { Plus, Trash2, Edit2, Check, X } from "lucide-react";
import { useGantt } from "./GanttContext";
import type { WorkScheduleStageWorkWeb } from "../../types/workSchedule.types";
import { AuthContext } from "../../context/AuthContext";
import { useContext } from "react";

interface GanttCommentPopoverProps {
  work: WorkScheduleStageWorkWeb;
  stageId: string;
  /** Jeśli true — ukrywa formularz dodawania nowego komentarza (tryb Podgląd) */
  isReadOnly?: boolean;
}

export default function GanttCommentPopover({ work, stageId, isReadOnly = false }: GanttCommentPopoverProps) {
  const { addComment, updateComment, deleteComment, isMutating } = useGantt();
  const { user } = useContext(AuthContext);
  const [newContent, setNewContent] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editContent, setEditContent] = useState("");

  const isAdding = isMutating.has(`addComment-${work.id}`);
  const borderColor = useColorModeValue("gray.200", "gray.600");

  const fmtDate = (d: string) =>
    new Date(d).toLocaleDateString("pl-PL", { day: "numeric", month: "short", year: "numeric" });

  const handleAdd = async () => {
    const trimmed = newContent.trim();
    if (!trimmed) return;
    await addComment(stageId, work.id, trimmed);
    setNewContent("");
  };

  const handleStartEdit = (commentId: string, content: string) => {
    setEditingId(commentId);
    setEditContent(content);
  };

  const handleEditSave = async (commentId: string) => {
    const trimmed = editContent.trim();
    if (!trimmed) return;
    await updateComment(stageId, work.id, commentId, trimmed);
    setEditingId(null);
  };

  const handleDelete = async (commentId: string) => {
    await deleteComment(stageId, work.id, commentId);
  };

  return (
    <VStack align="stretch" spacing={2} w="100%">
      <Text fontSize="xs" fontWeight="semibold">Komentarze ({work.comments?.length ?? 0})</Text>

      {(work.comments ?? []).map(comment => {
        const isOwn = comment.createdByUserId === user?.id;
        const isEditingThis = isMutating.has(`updateComment-${comment.id}`);
        const isDeletingThis = isMutating.has(`deleteComment-${comment.id}`);

        return (
          <Box
            key={comment.id}
            p={2}
            borderWidth="1px"
            borderColor={borderColor}
            borderRadius="md"
            fontSize="xs"
          >
            <HStack justify="space-between" mb={1}>
              <HStack spacing={1}>
                <Avatar size="2xs" name={comment.createdByUserName} />
                <Text fontWeight="semibold" fontSize="10px">{comment.createdByUserName}</Text>
              </HStack>
              <Text color="gray.500" fontSize="10px">{fmtDate(comment.createdAt)}</Text>
            </HStack>

            {editingId === comment.id ? (
              <VStack spacing={1}>
                <Textarea
                  value={editContent}
                  onChange={e => setEditContent(e.target.value)}
                  size="xs"
                  rows={2}
                />
                <HStack>
                  <Button size="xs" colorScheme="green" onClick={() => handleEditSave(comment.id)} isLoading={isEditingThis}>
                    Zapisz
                  </Button>
                  <Button size="xs" variant="ghost" onClick={() => setEditingId(null)}>Anuluj</Button>
                </HStack>
              </VStack>
            ) : (
              <HStack justify="space-between">
                <Text flexBasis="full">{comment.content}</Text>
                {isOwn && (
                  <HStack spacing={0}>
                    <IconButton
                      aria-label="Edytuj"
                      icon={<Edit2 size={10} />}
                      size="xs"
                      variant="ghost"
                      onClick={() => handleStartEdit(comment.id, comment.content)}
                    />
                    <IconButton
                      aria-label="Usuń"
                      icon={isDeletingThis ? <Spinner size="xs" /> : <Trash2 size={10} />}
                      size="xs"
                      variant="ghost"
                      colorScheme="red"
                      isDisabled={isDeletingThis}
                      onClick={() => handleDelete(comment.id)}
                    />
                  </HStack>
                )}
              </HStack>
            )}
          </Box>
        );
      })}

      <HStack spacing={1}>
        {!isReadOnly && (
          <>
            <Input
              value={newContent}
              onChange={e => setNewContent(e.target.value)}
              onKeyDown={e => e.key === "Enter" && !e.shiftKey && handleAdd()}
              placeholder="Dodaj komentarz..."
              size="xs"
              flex={1}
              isDisabled={isAdding}
            />
            <Button size="xs" colorScheme="primary" isLoading={isAdding} onClick={handleAdd}>
              Dodaj
            </Button>
          </>
        )}
      </HStack>
    </VStack>
  );
}
