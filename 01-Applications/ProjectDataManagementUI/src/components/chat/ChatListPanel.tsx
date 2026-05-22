import {
  Box,
  VStack,
  Text,
  Spinner,
  Center,
  Input,
  InputGroup,
  InputLeftElement,
  HStack,
  IconButton,
  useColorModeValue,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  Tooltip,
  Badge,
} from "@chakra-ui/react";
import { Search, PenLine, Users } from "lucide-react";
import { useState, useEffect, useRef, useContext } from "react";
import type { ChatSearchResultWeb, ChatWeb } from "../../types/chat.types";
import { chatApi } from "../../api/chatApi";
import ChatListItem from "./ChatListItem";
import { AuthContext } from "../../context/AuthContext";

const SEARCH_MIN_LENGTH = 2;
const SEARCH_DEBOUNCE_MS = 350;

interface ChatListPanelProps {
  chats: ChatWeb[];
  loading: boolean;
  activeChatId: string | null;
  currentUserId: string;
  onSelectChat: (chatId: string) => void;
  onNewDirectChat: () => void;
  onNewGroupChat: () => void;
}

export default function ChatListPanel({
  chats,
  loading,
  activeChatId,
  currentUserId,
  onSelectChat,
  onNewDirectChat,
  onNewGroupChat,
}: ChatListPanelProps) {
  const [query, setQuery] = useState("");
  const [searchResults, setSearchResults] = useState<ChatSearchResultWeb[] | null>(null);
  const [searching, setSearching] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const { user } = useContext(AuthContext);
  const activeTenantId = user?.activeTenantId ?? null;

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const headerBg = useColorModeValue("white", "gray.800");
  const mutedColor = useColorModeValue("gray.500", "gray.400");

  // Debounced search — odpytuje backend gdy fraza ≥ 2 znaki
  useEffect(() => {
    const trimmed = query.trim();

    if (trimmed.length < SEARCH_MIN_LENGTH) {
      setSearchResults(null);
      setSearching(false);
      return;
    }

    setSearching(true);

    const timer = setTimeout(async () => {
      // Anuluj poprzednie żądanie jeśli jeszcze trwa
      abortRef.current?.abort();
      abortRef.current = new AbortController();

      try {
        if (!activeTenantId) {
          setSearchResults([]);
          return;
        }
        const results = await chatApi.searchChats(activeTenantId, trimmed);
        setSearchResults(results);
      } catch {
        // Ignore aborted requests
        setSearchResults([]);
      } finally {
        setSearching(false);
      }
    }, SEARCH_DEBOUNCE_MS);

    return () => {
      clearTimeout(timer);
    };
  }, [query, activeTenantId]);

  // Wyniki wyszukiwania: dopasuj ChatSearchResultWeb do załadowanych ChatWeb
  const resolvedSearchItems = searchResults?.map((result) => ({
    result,
    chat: chats.find((c) => c.id === result.chatId) ?? null,
  })) ?? [];

  const isSearchActive = query.trim().length >= SEARCH_MIN_LENGTH;

  return (
    <Box
      w={{ base: "full", md: "280px" }}
      flexShrink={0}
      borderRight={{ base: "none", md: "1px solid" }}
      borderColor={borderColor}
      h="100%"
      display="flex"
      flexDirection="column"
    >
      {/* Nagłówek */}
      <HStack
        px={4}
        py={3}
        borderBottom="1px solid"
        borderColor={borderColor}
        bg={headerBg}
        justify="space-between"
      >
        <Text fontWeight="semibold" fontSize="md">
          Wiadomości
        </Text>
        <Menu>
          <Tooltip label="Nowa rozmowa">
            <MenuButton
              as={IconButton}
              icon={<PenLine size={16} />}
              size="sm"
              variant="ghost"
              aria-label="Nowa rozmowa"
            />
          </Tooltip>
          <MenuList>
            <MenuItem icon={<PenLine size={16} />} onClick={onNewDirectChat}>
              Wiadomość bezpośrednia
            </MenuItem>
            <MenuItem icon={<Users size={16} />} onClick={onNewGroupChat}>
              Nowa grupa
            </MenuItem>
          </MenuList>
        </Menu>
      </HStack>

      {/* Szukaj */}
      <Box px={3} py={2}>
        <InputGroup size="sm">
          <InputLeftElement pointerEvents="none">
            {searching ? <Spinner size="xs" /> : <Search size={14} />}
          </InputLeftElement>
          <Input
            placeholder="Szukaj rozmów i wiadomości..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            borderRadius="md"
          />
        </InputGroup>
      </Box>

      {/* Lista */}
      <Box flex={1} overflowY="auto" px={2} pb={2}>
        {loading && !isSearchActive ? (
          <Center py={8}>
            <Spinner size="md" />
          </Center>
        ) : isSearchActive ? (
          // Tryb wyszukiwania — wyniki z backendu
          searching ? (
            <Center py={8}>
              <Spinner size="md" />
            </Center>
          ) : resolvedSearchItems.length === 0 ? (
            <Center py={8}>
              <Text fontSize="sm" color={mutedColor}>
                Brak wyników dla „{query.trim()}"
              </Text>
            </Center>
          ) : (
            <VStack spacing={0.5} align="stretch">
              {resolvedSearchItems.map(({ result, chat }) =>
                chat ? (
                  <Box key={result.chatId} position="relative">
                    <ChatListItem
                      chat={chat}
                      isActive={chat.id === activeChatId}
                      currentUserId={currentUserId}
                      onClick={() => onSelectChat(chat.id)}
                    />
                    {result.matchingMessageIds.length > 0 && (
                      <Box px={3} pb={1} mt={-1}>
                        <Badge
                          fontSize="xs"
                          colorScheme="gray"
                          variant="subtle"
                          borderRadius="sm"
                        >
                          w wiadomości
                        </Badge>
                      </Box>
                    )}
                  </Box>
                ) : null
              )}
            </VStack>
          )
        ) : (
          // Tryb normalny — lokalna lista
          chats.length === 0 ? (
            <Center py={8}>
              <Text fontSize="sm" color={mutedColor}>
                Brak rozmów
              </Text>
            </Center>
          ) : (
            <VStack spacing={0.5} align="stretch">
              {chats.map((chat) => (
                <ChatListItem
                  key={chat.id}
                  chat={chat}
                  isActive={chat.id === activeChatId}
                  currentUserId={currentUserId}
                  onClick={() => onSelectChat(chat.id)}
                />
              ))}
            </VStack>
          )
        )}
      </Box>
    </Box>
  );
}

