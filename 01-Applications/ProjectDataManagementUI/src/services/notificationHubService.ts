import * as signalR from "@microsoft/signalr";
import type { NotificationWeb, NotificationMarkAsReadDto } from "../types/notification.types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

class NotificationHubService {
  private connection: signalR.HubConnection | null = null;
  private listeners: ((notification: NotificationWeb) => void)[] = [];
  private syncListeners: ((dto: NotificationMarkAsReadDto) => void)[] = []; // Listenery dla sync events
  private isConnecting: boolean = false;
  
  // Cache powiadomień w pamięci
  private notificationsCache: NotificationWeb[] = [];
  private unreadCountCache: number = 0;
  private cacheInitialized: boolean = false;

  async startConnection(): Promise<void> {
    // Zapobiegnij wielokrotnym próbom połączenia
    if (this.isConnecting) {
      console.log("SignalR connection already in progress");
      return;
    }

    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log("SignalR już połączony");
      return;
    }

    this.isConnecting = true;

    // Pobierz access token z cookie dla WebSocket
    const getAccessToken = (): string | null => {
      const name = "access_token=";
      const decodedCookie = decodeURIComponent(document.cookie);
      const ca = decodedCookie.split(';');
      for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) === ' ') {
          c = c.substring(1);
        }
        if (c.indexOf(name) === 0) {
          return c.substring(name.length, c.length);
        }
      }
      return null;
    };

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/api/hubs/notifications`, {
        accessTokenFactory: () => getAccessToken() || "", // Token w query string dla WebSocket
        withCredentials: true,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 2s, 4s, 8s, 16s, max 30s
          return Math.min(2000 * Math.pow(2, retryContext.previousRetryCount), 30000);
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Nasłuchuj na nowe powiadomienia
    this.connection.on("ReceiveNotification", (notification: NotificationWeb) => {
      console.log("Nowe powiadomienie otrzymane:", notification);
      
      // ✅ Dodaj do cache TUTAJ (przed notyfikowaniem listenerów)
      this.addNotificationToCache(notification);
      
      // Powiadom listenery (oni już NIE dodają do cache, tylko odświeżają UI)
      this.notifyListeners(notification);
    });

    // 🔄 Nasłuchuj na synchronizację między urządzeniami (gdy inne urządzenie oznaczyło jako przeczytane)
    // UWAGA: SignalR automatycznie konwertuje nazwę z backendu na camelCase
    this.connection.on("ReceiveNotificationMarkAsRead", (dto: NotificationMarkAsReadDto) => {
      console.log("🔄 Synchronizacja: Powiadomienie oznaczone jako przeczytane na innym urządzeniu:", dto);
      this.markAsReadInCache(dto.notificationId);
      this.notifySyncListeners(dto);
    });

    // Obsługa reconnect
    this.connection.onreconnecting((error) => {
      console.warn("SignalR reconnecting...", error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log("SignalR reconnected. Connection ID:", connectionId);
    });

    this.connection.onclose((error) => {
      console.error("SignalR connection closed", error);
    });

    try {
      await this.connection.start();
      console.log("SignalR Connected. Connection ID:", this.connection.connectionId);
    } catch (err) {
      console.error("SignalR Connection Error:", err);
      // Spróbuj ponownie za 5 sekund
      setTimeout(() => {
        this.isConnecting = false;
        this.startConnection();
      }, 5000);
    } finally {
      this.isConnecting = false;
    }
  }

  async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      console.log("SignalR Disconnected");
    }
  }

  // Subskrybuj na nowe powiadomienia
  onNotificationReceived(callback: (notification: NotificationWeb) => void): () => void {
    this.listeners.push(callback);

    // Zwróć funkcję do unsubscribe
    return () => {
      this.listeners = this.listeners.filter(listener => listener !== callback);
    };
  }

  private notifyListeners(notification: NotificationWeb): void {
    this.listeners.forEach(listener => {
      try {
        listener(notification);
      } catch (error) {
        console.error("Error in notification listener:", error);
      }
    });
  }

  private notifySyncListeners(dto: NotificationMarkAsReadDto): void {
    this.syncListeners.forEach(listener => {
      try {
        listener(dto);
      } catch (error) {
        console.error("Error in sync listener:", error);
      }
    });
  }

  // Subskrybuj na eventy synchronizacji (oznaczenie jako przeczytane na innym urządzeniu)
  onNotificationSynced(callback: (dto: NotificationMarkAsReadDto) => void): () => void {
    this.syncListeners.push(callback);
    return () => {
      this.syncListeners = this.syncListeners.filter(listener => listener !== callback);
    };
  }

  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state || null;
  }

  // Inicjalizuj cache z API (wywołaj raz przy starcie aplikacji)
  async initializeCache(notifications: NotificationWeb[]): Promise<void> {
    this.notificationsCache = notifications;
    this.unreadCountCache = notifications.filter(n => !n.readed).length;
    this.cacheInitialized = true;
    console.log("🔵 Notification cache initialized:", this.notificationsCache.length, "notifications,", this.unreadCountCache, "unread");
  }

  // Dodaj nowe powiadomienie do cache (wywołane przez SignalR)
  addNotificationToCache(notification: NotificationWeb): void {
    // Dodaj na początek listy
    this.notificationsCache = [notification, ...this.notificationsCache];
    
    // Jeśli nieprzeczytane, zwiększ licznik
    if (!notification.readed) {
      this.unreadCountCache++;
    }
    
    console.log("🔵 Notification added to cache:", notification.title, "| Total:", this.notificationsCache.length, "| Unread:", this.unreadCountCache);
  }

  // Oznacz jako przeczytane w cache
  markAsReadInCache(notificationId: string): void {
    const notification = this.notificationsCache.find(n => n.id === notificationId);
    if (notification && !notification.readed) {
      notification.readed = true;
      this.unreadCountCache = Math.max(0, this.unreadCountCache - 1);
      console.log("🔵 Notification marked as read in cache:", notificationId, "| Unread count:", this.unreadCountCache);
    }
  }

  // Pobierz nieprzeczytane powiadomienia z cache
  getUnreadNotificationsFromCache(): NotificationWeb[] {
    return this.notificationsCache.filter(n => !n.readed);
  }

  // Pobierz wszystkie powiadomienia z cache
  getAllNotificationsFromCache(): NotificationWeb[] {
    return [...this.notificationsCache];
  }

  // Pobierz licznik nieprzeczytanych z cache
  getUnreadCountFromCache(): number {
    return this.unreadCountCache;
  }

  // Sprawdź czy cache jest zainicjalizowany
  isCacheInitialized(): boolean {
    return this.cacheInitialized;
  }

  // Wyczyść cache (np. przy logout)
  clearCache(): void {
    this.notificationsCache = [];
    this.unreadCountCache = 0;
    this.cacheInitialized = false;
    console.log("🔵 Notification cache cleared");
  }
}

// Singleton instance
export const notificationHubService = new NotificationHubService();
