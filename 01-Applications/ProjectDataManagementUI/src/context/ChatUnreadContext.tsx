import {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
  useCallback,
  type ReactNode,
} from "react";
import { chatApi } from "../api/chatApi";
import { chatHubService } from "../services/chatHubService";
import { AuthContext } from "./AuthContext";
import { isDemoOnlySession } from "../utils/demoSession";

interface ChatUnreadContextType {
  totalUnread: number;
  markChatAsRead: (chatId: string) => void;
}

export const ChatUnreadContext = createContext<ChatUnreadContextType>({
  totalUnread: 0,
  markChatAsRead: () => {},
});

export function ChatUnreadProvider({ children }: { children: ReactNode }) {
  const { user } = useContext(AuthContext);
  const [unreadByChatId, setUnreadByChatId] = useState<Record<string, number>>({});
  // Przechowujemy userId gdy będzie dostępne (user.id jest typed jako opcjonalne)
  const userIdRef = useRef<string | null>(null);

  const totalUnread = Object.values(unreadByChatId).reduce((sum, n) => sum + n, 0);

  // Inicjalizacja: połącz z hubem, pobierz czaty, dołącz do grup SignalR
  useEffect(() => {
    if (!user) {
      return;
    }

    if (user.id) {
      userIdRef.current = user.id;
    }

    let cancelled = false;

    const init = async () => {
      try {
        const chats = await chatApi.getChats(user.activeTenantId ?? null);
        if (cancelled) {
          return;
        }

        const counts: Record<string, number> = {};
        for (const chat of chats) {
          counts[chat.id] = chat.unreadCount;
        }
        setUnreadByChatId(counts);

        if (isDemoOnlySession()) {
          return;
        }

        await chatHubService.startConnection();
        if (cancelled) {
          return;
        }

        for (const chat of chats) {
          chatHubService.joinChat(chat.id).catch(() => {});
        }
      } catch {
        // Nie blokuj aplikacji gdy czaty są niedostępne
      }
    };

    init();
    return () => {
      cancelled = true;
    };
  }, [user?.email, user?.activeTenantId]);

  // Nowa wiadomość → inkrementuj licznik tylko jeśli wiadomość nie pochodzi od nas
  useEffect(() => {
    return chatHubService.onReceiveMessage((msg) => {
      const currentUserId = userIdRef.current ?? user?.id;
      if (currentUserId && msg.senderId === currentUserId) return;
      setUnreadByChatId((prev) => ({
        ...prev,
        [msg.chatId]: (prev[msg.chatId] ?? 0) + 1,
      }));
    });
  }, [user?.id]);

  // Odczytanie czatu (ReadReceipt od bieżącego usera) → zeruj
  useEffect(() => {
    return chatHubService.onReadReceipt((payload) => {
      // Sprawdź czy to nasz ReadReceipt — użyj ref bo user.id może być undefined
      const currentUserId = userIdRef.current ?? user?.id;
      if (currentUserId && payload.userId !== currentUserId) return;
      setUnreadByChatId((prev) => ({ ...prev, [payload.chatId]: 0 }));
    });
  }, [user?.id]);

  // Nowy czat (zaproszenie) → dodaj do mapy + dołącz do grupy
  useEffect(() => {
    return chatHubService.onChatCreated((chat) => {
      setUnreadByChatId((prev) => ({ ...prev, [chat.id]: chat.unreadCount }));
      chatHubService.joinChat(chat.id).catch(() => {});
    });
  }, []);

  // Usunięto z czatu → usuń z mapy
  useEffect(() => {
    return chatHubService.onRemovedFromChat((payload) => {
      setUnreadByChatId((prev) => {
        const next = { ...prev };
        delete next[payload.chatId];
        return next;
      });
    });
  }, []);

  // Czat usunięty przez administratora → usuń z mapy
  useEffect(() => {
    return chatHubService.onChatDeleted((payload) => {
      setUnreadByChatId((prev) => {
        const next = { ...prev };
        delete next[payload.chatId];
        return next;
      });
    });
  }, []);

  // Lokalny reset (wywoływany z ChatPage zanim ReadReceipt dotrze)
  const markChatAsRead = useCallback((chatId: string) => {
    setUnreadByChatId((prev) => ({ ...prev, [chatId]: 0 }));
  }, []);

  return (
    <ChatUnreadContext.Provider value={{ totalUnread, markChatAsRead }}>
      {children}
    </ChatUnreadContext.Provider>
  );
}
