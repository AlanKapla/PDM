import {
  Box,
  VStack,
  HStack,
  Text,
  Textarea,
  Input,
  IconButton,
  Spinner,
  Center,
  useColorModeValue,
  Button,
  Divider,
  Tooltip,
  Alert,
  AlertIcon,
  AlertDialog,
  AlertDialogOverlay,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogBody,
  AlertDialogFooter,
  useDisclosure,
} from "@chakra-ui/react";
import { Send, X, ArrowDown, Users, Pencil, Trash2, ChevronLeft } from "lucide-react";
import {
  useState,
  useRef,
  useEffect,
  useCallback,
  type KeyboardEvent,
} from "react";
import type { ChatWeb, MessageWeb } from "../../types/chat.types";
import { chatApi } from "../../api/chatApi";
import { chatHubService } from "../../services/chatHubService";
import { useChatMessages } from "../../hooks/useChat";
import ChatMessage from "./ChatMessage";
import ChatMembersDrawer from "./ChatMembersDrawer";
import { useToastNotification } from "../../hooks/useToastNotification";

interface ChatWindowProps {
  chat: ChatWeb;
  currentUserId: string;
  onBack?: () => void;
  onDeleted?: () => void;
  onDeleteFailed?: () => void;
  onOptimisticDelete?: (chatId: string) => void;
  onOptimisticRename?: (chatId: string, name: string) => void;
  onMembersChange?: (members: import("../../types/chat.types").ChatMemberWeb[]) => void;
}

const TYPING_STOP_DELAY_MS = 2000;

export default function ChatWindow({ chat, currentUserId, onBack, onDeleted, onDeleteFailed, onOptimisticDelete, onOptimisticRename, onMembersChange }: ChatWindowProps) {
  const { messages, loadingInitial, loadingMore, hasMore, error, typingUserIds, loadMore } =
    useChatMessages(chat.id);

  const [content, setContent] = useState("");
  const [sending, setSending] = useState(false);
  const [replyTo, setReplyTo] = useState<MessageWeb | null>(null);
  const [editingMessage, setEditingMessage] = useState<MessageWeb | null>(null);

  const messagesBottomRef = useRef<HTMLDivElement>(null);
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const typingTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isTypingRef = useRef(false);

  const {
    isOpen: isMembersOpen,
    onOpen: openMembers,
    onClose: closeMembers,
  } = useDisclosure();

  const {
    isOpen: isDeleteOpen,
    onOpen: openDelete,
    onClose: closeDelete,
  } = useDisclosure();
  const deleteCancelRef = useRef<HTMLButtonElement>(null);
  const [deleting, setDeleting] = useState(false);
  const { showError } = useToastNotification();

  const currentUserIsAdmin =
    chat.members.find((m) => m.userId === currentUserId)?.isAdmin ?? false;

  const handleDeleteChat = async () => {
    setDeleting(true);
    onOptimisticDelete?.(chat.id); // optimistic — natychmiast usuń z listy
    closeDelete();
    try {
      await chatApi.deleteChat(chat.id);
      onDeleted?.(); // nawigacja do /chat
    } catch {
      onDeleteFailed?.(); // reload — przywróć czat
      showError("Nie udało się usunąć rozmowy.");
    } finally {
      setDeleting(false);
    }
  };

  const [isRenamingChat, setIsRenamingChat] = useState(false);
  const [chatNameDraft, setChatNameDraft] = useState(chat.name);
  const [savingName, setSavingName] = useState(false);
  const nameInputRef = useRef<HTMLInputElement>(null);

  // Przy zmianie chatu zresetuj stan edycji nazwy
  useEffect(() => {
    setIsRenamingChat(false);
    setChatNameDraft(chat.name);
  }, [chat.id, chat.name]);

  const startRenaming = () => {
    setChatNameDraft(chat.name);
    setIsRenamingChat(true);
    // Focus po renderze
    setTimeout(() => nameInputRef.current?.focus(), 0);
  };

  const cancelRenaming = () => {
    setIsRenamingChat(false);
    setChatNameDraft(chat.name);
  };

  const saveRename = async () => {
    const trimmed = chatNameDraft.trim();
    if (!trimmed || trimmed === chat.name) {
      cancelRenaming();
      return;
    }
    setSavingName(true);
    onOptimisticRename?.(chat.id, trimmed); // optimistic — widoczne natychmiast
    try {
      await chatApi.renameGroupChat(chat.id, { newName: trimmed });
      setIsRenamingChat(false);
    } catch {
      onOptimisticRename?.(chat.id, chat.name); // cofnij zmianę
      cancelRenaming();
    } finally {
      setSavingName(false);
    }
  };

  const handleNameKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") { e.preventDefault(); saveRename(); }
    if (e.key === "Escape") { e.preventDefault(); cancelRenaming(); }
  };

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const headerBg = useColorModeValue("white", "gray.800");
  const inputBg = useColorModeValue("white", "gray.800");

  // Przewiń na dół po załadowaniu pierwszej strony
  useEffect(() => {
    if (!loadingInitial) {
      messagesBottomRef.current?.scrollIntoView({ behavior: "instant" });
    }
  }, [loadingInitial, chat.id]);

  const handleScroll = useCallback(() => {
    const el = scrollContainerRef.current;
    if (!el) return;
    // Gdy użytkownik dotrze do góry, załaduj starsze
    if (el.scrollTop < 80 && hasMore && !loadingMore) {
      const prevScrollHeight = el.scrollHeight;
      loadMore().then(() => {
        // Zachowaj pozycję scrolla po dołączeniu starszych wiadomości
        requestAnimationFrame(() => {
          if (!el) return;
          el.scrollTop = el.scrollHeight - prevScrollHeight;
        });
      });
    }
  }, [hasMore, loadingMore, loadMore]);

  const stopTypingSignal = useCallback(async () => {
    if (!isTypingRef.current) return;
    isTypingRef.current = false;
    try {
      await chatHubService.stopTyping(chat.id);
    } catch {}
  }, [chat.id]);

  const handleContentChange = useCallback(
    async (value: string) => {
      setContent(value);

      if (!isTypingRef.current) {
        isTypingRef.current = true;
        chatHubService.startTyping(chat.id).catch(() => {});
      }

      if (typingTimerRef.current) clearTimeout(typingTimerRef.current);
      typingTimerRef.current = setTimeout(stopTypingSignal, TYPING_STOP_DELAY_MS);
    },
    [chat.id, stopTypingSignal]
  );

  const sendMessage = useCallback(async () => {
    const trimmed = content.trim();
    if (!trimmed || sending) return;

    stopTypingSignal();
    setSending(true);
    setContent("");
    const replyId = replyTo?.id ?? null;
    const editId = editingMessage?.id ?? null;
    setReplyTo(null);
    setEditingMessage(null);

    try {
      if (editId) {
        await chatApi.editMessage(chat.id, editId, { content: trimmed });
      } else {
        await chatApi.sendMessage(chat.id, { content: trimmed, replyToMessageId: replyId });
      }
      messagesBottomRef.current?.scrollIntoView({ behavior: "smooth" });
    } catch {
      // Przywróć input gdy wysłanie się nie powiodło
      setContent(trimmed);
      if (replyId) setReplyTo(replyTo);
    } finally {
      setSending(false);
    }
  }, [content, sending, replyTo, editingMessage, chat.id, stopTypingSignal]);

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  const startEdit = (message: MessageWeb) => {
    setEditingMessage(message);
    setReplyTo(null);
    setContent(message.content);
  };

  const cancelEditOrReply = () => {
    setEditingMessage(null);
    setReplyTo(null);
    setContent("");
  };

  const handleDelete = async (message: MessageWeb) => {
    try {
      await chatApi.deleteMessage(chat.id, message.id);
    } catch {}
  };

  // Mapa wiadomości po id — do wyświetlania cytowanych
  const messageMap = new Map(messages.map((m) => [m.id, m]));

  // Wiadomości są posortowane newest→oldest — odwróć do wyświetlenia
  const sortedMessages = [...messages].reverse();

  const typingNames = typingUserIds
    .filter((id) => id !== currentUserId)
    .map((id) => {
      const member = chat.members.find((m) => m.userId === id);
      return member ? member.firstName : "Ktoś";
    });

  return (
    <Box flex={1} display="flex" flexDirection="column" h="100%" minW={0}>
      {/* Nagłówek rozmowy */}
      <HStack
        px={4}
        py={3}
        borderBottom="1px solid"
        borderColor={borderColor}
        bg={headerBg}
        flexShrink={0}
        justify="space-between"
        spacing={2}
      >
        {/* Przycisk wstecz — tylko mobile */}
        {onBack && (
          <IconButton
            display={{ base: "flex", md: "none" }}
            icon={<ChevronLeft size={20} />}
            size="sm"
            variant="ghost"
            aria-label="Wróć do listy"
            onClick={onBack}
            flexShrink={0}
          />
        )}
        <Box flex={1} minW={0}>
          {chat.isGroupChat && isRenamingChat ? (
            <HStack spacing={1}>
              <Input
                ref={nameInputRef}
                value={chatNameDraft}
                onChange={(e) => setChatNameDraft(e.target.value)}
                onKeyDown={handleNameKeyDown}
                onBlur={saveRename}
                size="sm"
                fontWeight="semibold"
                borderRadius="md"
                maxLength={100}
                isDisabled={savingName}
                autoComplete="off"
              />
              {savingName && <Spinner size="xs" flexShrink={0} />}
            </HStack>
          ) : (
            <HStack spacing={1} role="group">
              <Text fontWeight="semibold" fontSize="md" noOfLines={1}>
                {chat.name}
              </Text>
              {chat.isGroupChat && (
                <Tooltip label="Zmień nazwę grupy">
                  <IconButton
                    icon={<Pencil size={13} />}
                    size="xs"
                    variant="ghost"
                    aria-label="Zmień nazwę"
                    opacity={0}
                    _groupHover={{ opacity: 1 }}
                    onClick={startRenaming}
                    flexShrink={0}
                  />
                </Tooltip>
              )}
            </HStack>
          )}
          <Text fontSize="xs" color="gray.500">
            {chat.members.length} {chat.members.length === 1 ? "uczestnik" : "uczestników"}
          </Text>
        </Box>
        <HStack spacing={1} flexShrink={0}>
          {chat.isGroupChat && (
            <Tooltip label="Uczestnicy">
              <IconButton
                icon={<Users size={16} />}
                size="sm"
                variant="ghost"
                aria-label="Zarządzaj uczestnikami"
                onClick={openMembers}
              />
            </Tooltip>
          )}
          {(!chat.isGroupChat || currentUserIsAdmin) && (
            <Tooltip label={chat.isGroupChat ? "Usuń grupę" : "Usuń rozmowę"}>
              <IconButton
                icon={<Trash2 size={16} />}
                size="sm"
                variant="ghost"
                colorScheme="red"
                aria-label={chat.isGroupChat ? "Usuń grupę" : "Usuń rozmowę"}
                onClick={openDelete}
              />
            </Tooltip>
          )}
        </HStack>
      </HStack>

      {/* Potwierdzenie usunięcia grupy */}
      <AlertDialog
        isOpen={isDeleteOpen}
        leastDestructiveRef={deleteCancelRef}
        onClose={closeDelete}
        isCentered
      >
        <AlertDialogOverlay />
        <AlertDialogContent>
          <AlertDialogHeader>
            {chat.isGroupChat ? "Usuń grupę" : "Usuń rozmowę"}
          </AlertDialogHeader>
          <AlertDialogBody>
            Czy na pewno chcesz usunąć{" "}
            {chat.isGroupChat ? (
              <>grupę <strong>{chat.name}</strong></>
            ) : (
              "tę rozmowę"
            )}
            ? Wszystkie wiadomości zostaną trwale usunięte.
          </AlertDialogBody>
          <AlertDialogFooter>
            <Button ref={deleteCancelRef} onClick={closeDelete} isDisabled={deleting}>
              Anuluj
            </Button>
            <Button
              colorScheme="red"
              ml={3}
              isLoading={deleting}
              onClick={handleDeleteChat}
            >
              Usuń
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <ChatMembersDrawer
        chat={chat}
        currentUserId={currentUserId}
        isOpen={isMembersOpen}
        onClose={closeMembers}
        onMembersChange={onMembersChange}
      />

      {/* Lista wiadomości */}
      <Box
        ref={scrollContainerRef}
        flex={1}
        overflowY="auto"
        px={4}
        py={3}
        onScroll={handleScroll}
      >
        {loadingInitial ? (
          <Center h="100%">
            <Spinner />
          </Center>
        ) : error ? (
          <Alert status="error" borderRadius="md">
            <AlertIcon />
            {error}
          </Alert>
        ) : (
          <VStack spacing={2} align="stretch">
            {loadingMore && (
              <Center py={2}>
                <Spinner size="sm" />
              </Center>
            )}
            {!hasMore && sortedMessages.length > 0 && (
              <Text fontSize="xs" color="gray.400" textAlign="center" py={1}>
                Początek rozmowy
              </Text>
            )}
            {sortedMessages.length === 0 && (
              <Center py={12}>
                <Text color="gray.400" fontSize="sm">
                  Brak wiadomości. Napisz coś!
                </Text>
              </Center>
            )}
            {sortedMessages.map((msg) => (
              <ChatMessage
                key={msg.id}
                message={msg}
                isOwn={msg.senderId === currentUserId}
                replyTarget={msg.replyToMessageId ? (messageMap.get(msg.replyToMessageId) ?? null) : null}
                onReply={() => {
                  setReplyTo(msg);
                  setEditingMessage(null);
                }}
                onEdit={() => startEdit(msg)}
                onDelete={() => handleDelete(msg)}
              />
            ))}
            <div ref={messagesBottomRef} />
          </VStack>
        )}
      </Box>

      {/* Wskaźnik pisania */}
      {typingNames.length > 0 && (
        <Box px={4} pb={1}>
          <Text fontSize="xs" color="gray.500" fontStyle="italic">
            {typingNames.join(", ")} {typingNames.length === 1 ? "pisze..." : "piszą..."}
          </Text>
        </Box>
      )}

      {/* Pasek odpowiedzi / edycji */}
      {(replyTo || editingMessage) && (
        <Box
          mx={4}
          mb={1}
          px={3}
          py={2}
          borderLeft="3px solid"
            borderColor="primary.400"
            bg={useColorModeValue("primary.50", "primary.900")}
          borderRadius="md"
          position="relative"
        >
            <Text fontSize="xs" fontWeight="semibold" color="primary.500">
            {editingMessage ? "Edytujesz wiadomość" : `Odpowiadasz: ${replyTo!.senderFirstName}`}
          </Text>
          {replyTo && (
            <Text fontSize="xs" color="gray.500" noOfLines={1}>
              {replyTo.content}
            </Text>
          )}
          <IconButton
            icon={<X size={12} />}
            size="xs"
            variant="ghost"
            aria-label="Anuluj"
            position="absolute"
            top={1}
            right={1}
            onClick={cancelEditOrReply}
          />
        </Box>
      )}

      {/* Pole wpisywania */}
      <HStack
        px={4}
        py={3}
        borderTop="1px solid"
        borderColor={borderColor}
        bg={inputBg}
        flexShrink={0}
        align="flex-end"
        spacing={2}
        minW={0}
        w="100%"
      >
        <Textarea
          value={content}
          onChange={(e) => handleContentChange(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Napisz wiadomość..."
          resize="none"
          rows={1}
          maxH="120px"
          overflowY="auto"
          flex={1}
          minW={0}
          /* fontSize >= 16px zapobiega auto-zoom na iOS */
          fontSize={{ base: "16px", md: "sm" }}
          borderRadius="xl"
        />
        <Tooltip label={editingMessage ? "Zapisz" : "Wyślij (Enter)"}>
          <IconButton
            icon={<Send size={18} />}
            colorScheme="primary"
            borderRadius="full"
            aria-label="Wyślij"
            isLoading={sending}
            isDisabled={!content.trim()}
            onClick={sendMessage}
            flexShrink={0}
          />
        </Tooltip>
      </HStack>
    </Box>
  );
}
