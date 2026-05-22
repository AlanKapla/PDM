import { Box, Text, Center, useColorModeValue, VStack, Icon } from "@chakra-ui/react";
import { MessageSquare } from "lucide-react";
import { useState, useEffect, useContext, useCallback, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDisclosure } from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import ChatListPanel from "../components/chat/ChatListPanel";
import ChatWindow from "../components/chat/ChatWindow";
import CreateDirectChatModal from "../components/chat/CreateDirectChatModal";
import CreateGroupChatModal from "../components/chat/CreateGroupChatModal";
import { useChatList } from "../hooks/useChat";
import { chatHubService } from "../services/chatHubService";
import { AuthContext } from "../context/AuthContext";
import { ChatUnreadContext } from "../context/ChatUnreadContext";

export default function ChatPage() {
  const { user } = useContext(AuthContext);
  const { chatId: chatIdParam } = useParams<{ chatId?: string }>();
  const navigate = useNavigate();

  const [activeChatId, setActiveChatId] = useState<string | null>(chatIdParam ?? null);
  // Ref zapewnia aktualną wartość w callbackach SignalR bez reinicjalizacji eventów
  const activeChatIdRef = useRef(activeChatId);
  useEffect(() => { activeChatIdRef.current = activeChatId; }, [activeChatId]);

  const { chats, loading, reload, markChatAsRead, removeChat, renameChat, updateChatMembers } = useChatList();
  const { markChatAsRead: markGlobalUnread } = useContext(ChatUnreadContext);

  const {
    isOpen: isDirectOpen,
    onOpen: openDirect,
    onClose: closeDirect,
  } = useDisclosure();
  const {
    isOpen: isGroupOpen,
    onOpen: openGroup,
    onClose: closeGroup,
  } = useDisclosure();

  const borderColor = useColorModeValue("gray.200", "gray.700");

  // Inicjalizuj połączenie SignalR i dołącz do wszystkich aktywnych czatów
  useEffect(() => {
    chatHubService.startConnection().then(() => {
      chats.forEach((c) => chatHubService.joinChat(c.id).catch(() => {}));
    }).catch(() => {});
  }, [chats.length]);

  // Synchronizuj URL z wybranym czatem
  useEffect(() => {
    if (chatIdParam && chatIdParam !== activeChatId) {
      setActiveChatId(chatIdParam);
    }
  }, [chatIdParam]);

  const selectChat = useCallback(
    (id: string) => {
      setActiveChatId(id);
      markChatAsRead(id);
      markGlobalUnread(id);
      navigate(`/chat/${id}`, { replace: true });
    },
    [navigate, markChatAsRead, markGlobalUnread]
  );

  const handleChatCreated = (newChatId: string) => {
    reload();
    selectChat(newChatId);
  };

  // Nawigacja gdy bieżący użytkownik zostaje usunięty z czatu
  useEffect(() => {
    return chatHubService.onRemovedFromChat((payload) => {
      if (activeChatIdRef.current !== payload.chatId) return;
      if (payload.redirectToChatId) {
        selectChat(payload.redirectToChatId);
      } else {
        setActiveChatId(null);
        navigate("/chat", { replace: true });
      }
    });
  // selectChat i navigate są stabilne — bez zbędnych reinicjalizacji
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Nawigacja gdy czat zostaje usunięty przez administratora
  useEffect(() => {
    return chatHubService.onChatDeleted((payload) => {
      if (activeChatIdRef.current === payload.chatId) {
        setActiveChatId(null);
        navigate("/chat", { replace: true });
      }
    });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const activeChat = chats.find((c) => c.id === activeChatId) ?? null;

  return (
    <MainLayout>
      <Box
        display="flex"
        h="calc(100vh - 60px)"
        overflow="hidden"
        borderTop="1px solid"
        borderColor={borderColor}
      >
        {/* Lewa kolumna — lista rozmów; na mobile ukryta gdy czat jest otwarty */}
        <Box
          display={{ base: activeChat ? "none" : "flex", md: "flex" }}
          w={{ base: "full", md: "auto" }}
          h="100%"
          flexDirection="column"
          flexShrink={0}
        >
          <ChatListPanel
            chats={chats}
            loading={loading}
            activeChatId={activeChatId}
            currentUserId={user?.id ?? ""}
            onSelectChat={selectChat}
            onNewDirectChat={openDirect}
            onNewGroupChat={openGroup}
          />
        </Box>

        {/* Prawa kolumna — okno rozmowy; na mobile ukryta gdy brak aktywnego czatu */}
        <Box
          display={{ base: activeChat ? "flex" : "none", md: "flex" }}
          flex={1}
          minW={0}
          h="100%"
          flexDirection="column"
        >
          {activeChat ? (
            <ChatWindow
              chat={activeChat}
              currentUserId={user?.id ?? ""}
              onBack={() => {
                setActiveChatId(null);
                navigate("/chat", { replace: true });
              }}
              onDeleted={() => {
                setActiveChatId(null);
                navigate("/chat", { replace: true });
              }}
              onDeleteFailed={reload}
              onOptimisticDelete={removeChat}
              onOptimisticRename={renameChat}
              onMembersChange={(members) => updateChatMembers(activeChat.id, members)}
            />
          ) : (
            <Center flex={1}>
              <VStack spacing={3} color="neutral.400">
                <Icon as={MessageSquare} boxSize={12} />
                <Text fontSize="md">Wybierz rozmowę lub rozpocznij nową</Text>
              </VStack>
            </Center>
          )}
        </Box>
      </Box>

      {/* Modale tworzenia rozmów */}
      {user && (
        <>
          <CreateDirectChatModal
            isOpen={isDirectOpen}
            onClose={closeDirect}
            onCreated={handleChatCreated}
          />
          <CreateGroupChatModal
            isOpen={isGroupOpen}
            onClose={closeGroup}
            onCreated={handleChatCreated}
          />
        </>
      )}
    </MainLayout>
  );
}
