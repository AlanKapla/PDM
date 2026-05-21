import { useState, useEffect, useCallback, useRef, useContext } from "react";
import { chatApi } from "../api/chatApi";
import { chatHubService } from "../services/chatHubService";
import { AuthContext } from "../context/AuthContext";
import type { ChatWeb, ChatMemberWeb, RemovedFromChatPayload, ChatDeletedPayload, MemberAddedPayload } from "../types/chat.types";

/**
 * Zarządza listą rozmów użytkownika i subskrybuje zdarzenia SignalR
 * dotyczące tworzenia nowych czatów i usuwania z czatu.
 */
export function useChatList() {
  const { user } = useContext(AuthContext);
  const currentUserIdRef = useRef<string | null>(null);
  useEffect(() => { currentUserIdRef.current = user?.id ?? null; }, [user?.id]);

  const [chats, setChats] = useState<ChatWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const activeTenantId = user?.activeTenantId ?? null;

  const loadChats = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await chatApi.getChats(activeTenantId);
      setChats(data);
    } catch {
      setError("Nie udało się załadować rozmów.");
    } finally {
      setLoading(false);
    }
  }, [activeTenantId]);

  useEffect(() => {
    loadChats();
  }, [loadChats]);

  // SignalR: nowy czat — dodaj do listy i subskrybuj grupę
  useEffect(() => {
    const unsubscribe = chatHubService.onChatCreated((chat) => {
      setChats((prev) => {
        if (prev.some((c) => c.id === chat.id)) return prev;
        return [chat, ...prev];
      });
      chatHubService.joinChat(chat.id).catch(() => {});
    });
    return unsubscribe;
  }, []);

  // SignalR: usunięto użytkownika z czatu
  useEffect(() => {
    const unsubscribe = chatHubService.onRemovedFromChat((payload: RemovedFromChatPayload) => {
      setChats((prev) => prev.filter((c) => c.id !== payload.chatId));
      chatHubService.leaveChat(payload.chatId).catch(() => {});
    });
    return unsubscribe;
  }, []);

  // SignalR: czat usunięty przez administratora
  useEffect(() => {
    const unsubscribe = chatHubService.onChatDeleted((payload: ChatDeletedPayload) => {
      setChats((prev) => prev.filter((c) => c.id !== payload.chatId));
      chatHubService.leaveChat(payload.chatId).catch(() => {});
    });
    return unsubscribe;
  }, []);

  // SignalR: nowy uczestnik dodany do czatu grupowego
  useEffect(() => {
    const unsubscribe = chatHubService.onMemberAdded((payload: MemberAddedPayload) => {
      setChats((prev) =>
        prev.map((c) => {
          if (c.id !== payload.chatId) return c;
          // Nie duplikuj jeśli już istnieje (idempotent)
          const exists = c.members.some((m) => m.userId === payload.member.userId);
          return exists
            ? c
            : { ...c, members: [...c.members, payload.member] };
        })
      );
    });
    return unsubscribe;
  }, []);

  // Aktualizuj ostatnią wiadomość i unreadCount na podstawie przychodzących wiadomości
  useEffect(() => {
    const unsubscribe = chatHubService.onReceiveMessage((message) => {
      const isOwnMessage = currentUserIdRef.current && message.senderId === currentUserIdRef.current;
      setChats((prev) =>
        prev.map((c) =>
          c.id === message.chatId
            ? { ...c, lastMessage: message, unreadCount: isOwnMessage ? c.unreadCount : c.unreadCount + 1 }
            : c
        )
      );
    });
    return unsubscribe;
  }, []);

  // Zeruję unreadCount gdy użytkownik oznacza czat jako przeczytany
  const markChatAsRead = useCallback((chatId: string) => {
    setChats((prev) =>
      prev.map((c) => (c.id === chatId ? { ...c, unreadCount: 0 } : c))
    );
  }, []);

  // Optimistic: usuń czat z listy (przed wywołaniem API)
  const removeChat = useCallback((chatId: string) => {
    setChats((prev) => prev.filter((c) => c.id !== chatId));
  }, []);

  // Optimistic: zmień nazwę czatu w liście
  const renameChat = useCallback((chatId: string, name: string) => {
    setChats((prev) => prev.map((c) => (c.id === chatId ? { ...c, name } : c)));
  }, []);

  // Optimistic: zaktualizuj listę członków czatu
  const updateChatMembers = useCallback((chatId: string, members: ChatMemberWeb[]) => {
    setChats((prev) => prev.map((c) => (c.id === chatId ? { ...c, members } : c)));
  }, []);

  return { chats, loading, error, reload: loadChats, markChatAsRead, removeChat, renameChat, updateChatMembers };
}

const PAGE_SIZE = 50;

/**
 * Zarządza wiadomościami dla konkretnej rozmowy: ładowanie stronami,
 * wysyłanie, edycja, usuwanie oraz obsługa zdarzeń SignalR.
 */
export function useChatMessages(tenantId: string | null, chatId: string | null) {
  const [messages, setMessages] = useState<import("../types/chat.types").MessageWeb[]>([]);
  const [loadingInitial, setLoadingInitial] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [typingUserIds, setTypingUserIds] = useState<string[]>([]);

  // Kursor do paginacji (id najstarszej załadowanej wiadomości)
  const cursorRef = useRef<string | undefined>(undefined);
  // Flaga czy jesteśmy już subskrybowani na czat w SignalR
  const joinedRef = useRef<string | null>(null);

  const reset = useCallback(() => {
    setMessages([]);
    setHasMore(true);
    setError(null);
    cursorRef.current = undefined;
  }, []);

  // Załaduj pierwszą stronę wiadomości
  const loadInitial = useCallback(async (id: string) => {
    try {
      setLoadingInitial(true);
      setError(null);
      const data = await chatApi.getMessages(tenantId, id, PAGE_SIZE);
      setMessages(data);
      setHasMore(data.length === PAGE_SIZE);
      cursorRef.current = data.length > 0 ? data[data.length - 1].id : undefined;
    } catch {
      setError("Nie udało się załadować wiadomości.");
    } finally {
      setLoadingInitial(false);
    }
  }, [tenantId]);

  // Załaduj starszą stronę (scroll w górę)
  const loadMore = useCallback(async () => {
    if (!chatId || loadingMore || !hasMore || !cursorRef.current) return;

    try {
      setLoadingMore(true);
      const data = await chatApi.getMessages(tenantId, chatId, PAGE_SIZE, cursorRef.current);
      if (data.length === 0) {
        setHasMore(false);
        return;
      }
      setMessages((prev) => [...prev, ...data]);
      setHasMore(data.length === PAGE_SIZE);
      cursorRef.current = data[data.length - 1].id;
    } catch {
      setError("Nie udało się załadować starszych wiadomości.");
    } finally {
      setLoadingMore(false);
    }
  }, [chatId, loadingMore, hasMore, tenantId]);

  // Przeładuj czat gdy zmieni się chatId
  useEffect(() => {
    if (!chatId) {
      reset();
      return;
    }

    reset();
    loadInitial(chatId);

    // Subskrybuj grupę SignalR
    if (joinedRef.current && joinedRef.current !== chatId) {
      chatHubService.leaveChat(joinedRef.current).catch(() => {});
    }
    chatHubService.joinChat(chatId).catch(() => {});
    joinedRef.current = chatId;

    // Oznacz jako przeczytane
    chatApi.markAsRead(tenantId, chatId).catch(() => {});

    return () => {
      if (joinedRef.current) {
        chatHubService.leaveChat(joinedRef.current).catch(() => {});
        joinedRef.current = null;
      }
    };
  }, [chatId, loadInitial, reset]);

  // SignalR: nowa wiadomość
  useEffect(() => {
    const unsubscribe = chatHubService.onReceiveMessage((message) => {
      if (message.chatId !== chatId) return;
      setMessages((prev) => {
        if (prev.some((m) => m.id === message.id)) return prev;
        return [message, ...prev];
      });
      chatApi.markAsRead(tenantId, chatId!).catch(() => {});
    });
    return unsubscribe;
  }, [chatId, tenantId]);

  // SignalR: edycja wiadomości
  useEffect(() => {
    const unsubscribe = chatHubService.onMessageEdited((payload) => {
      if (payload.chatId !== chatId) return;
      setMessages((prev) =>
        prev.map((m) =>
          m.id === payload.messageId
            ? { ...m, content: payload.newContent, isEdited: true, editedAt: payload.editedAt }
            : m
        )
      );
    });
    return unsubscribe;
  }, [chatId]);

  // SignalR: usunięcie wiadomości (soft-delete)
  useEffect(() => {
    const unsubscribe = chatHubService.onMessageDeleted((payload) => {
      if (payload.chatId !== chatId) return;
      setMessages((prev) =>
        prev.map((m) =>
          m.id === payload.messageId ? { ...m, isDeleted: true, content: "" } : m
        )
      );
    });
    return unsubscribe;
  }, [chatId]);

  // SignalR: wskaźnik pisania
  useEffect(() => {
    const unsubscribe = chatHubService.onUserTyping((payload) => {
      if (payload.chatId !== chatId) return;
      setTypingUserIds((prev) => {
        if (payload.isTyping) {
          return prev.includes(payload.userId) ? prev : [...prev, payload.userId];
        }
        return prev.filter((id) => id !== payload.userId);
      });
    });
    return unsubscribe;
  }, [chatId]);

  return {
    messages,
    loadingInitial,
    loadingMore,
    hasMore,
    error,
    typingUserIds,
    loadMore,
  };
}
