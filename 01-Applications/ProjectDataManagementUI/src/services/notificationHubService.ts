import * as signalR from "@microsoft/signalr";
import type { NotificationPayloadDto, NotificationMarkAsReadDto } from "../types/notification.types";
import { msalInstance } from "../main";
import { silentRequest } from "../config/authConfig";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

class NotificationHubService {
  private connection: signalR.HubConnection | null = null;
  private payloadListeners: ((payload: NotificationPayloadDto) => void)[] = [];
  private syncListeners: ((dto: NotificationMarkAsReadDto) => void)[] = [];
  
  // Diagnostyka
  private lastNotificationTime: number | null = null;
  
  // Mutex dla startConnection - zapobiega równoległym startom
  private startPromise: Promise<void> | null = null;
  
  // Test metoda - sprawdza czy backend widzi nas jako użytkownika
  async testConnection(): Promise<string> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return "❌ Not connected";
    }
    try {
      const userId = await this.connection.invoke<string>("WhoAmI");
      return `✅ Backend sees you as: ${userId}`;
    } catch (error) {
      return `❌ Error: ${error}`;
    }
  }
  
  // Promise i resolver dla readiness (Connected state)
  private readyPromise: Promise<void> | null = null;
  private readyResolve: (() => void) | null = null;
  
  // Callback po reconnect dla resync
  private afterReconnect: (() => Promise<void>) | null = null;


  // 🔥 SINGLETON CONNECTION - tworzone tylko raz, nie przy każdym startConnection()
  private getOrCreateConnection(): signalR.HubConnection {
    if (this.connection) {
      return this.connection;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/api/hubs/notifications`, {
        // ✅ KLUCZOWE: accessTokenFactory wywoływane jest za KAŻDYM requestem
        // MSAL automatycznie odświeża token gdy trzeba
        accessTokenFactory: async () => {
          const accounts = msalInstance.getAllAccounts();
          
          if (accounts.length === 0) {
            throw new Error("No authenticated user");
          }

          const account = msalInstance.getActiveAccount() || accounts[0];
          
          try {
            const response = await msalInstance.acquireTokenSilent({
              ...silentRequest,
              account: account,
            });
            return response.accessToken;
          } catch (error) {
            throw error;
          }
        },
        withCredentials: false,
        skipNegotiation: false, // Negotiate potrzebny dla prawidłowej pracy przez nginx/proxy
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Natychmiastowy retry, potem exponential
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Agresywniejsze heartbeat/timeout dla szybszego wykrywania problemów
    this.connection.keepAliveIntervalInMilliseconds = 10_000; // Ping co 10s
    this.connection.serverTimeoutInMilliseconds = 20_000;     // Timeout po 20s ciszy

    // ✅ Rejestracja handlerów RAZ - PRZED start()
    this.registerHandlers();

    // Lifecycle events z diagnostyką
    this.connection.onreconnecting((error) => {
    });

    this.connection.onreconnected(async (connectionId) => {
      
      // Resolve readiness
      this.readyResolve?.();
      this.readyResolve = null;
      
      // Resync danych po reconnect
      if (this.afterReconnect) {
        try {
          await this.afterReconnect();
        } catch (error) {
        }
      }
    });

    this.connection.onclose((error) => {
      // Reset readiness dla kolejnego połączenia
      this.readyPromise = null;
      this.readyResolve = null;
    });

    return this.connection;
  }

  // ✅ Funkcja do rejestracji handlerów (wywoływana przed start i po reconnect)
  private registerHandlers(): void {
    if (!this.connection) return;

    // Usuń stare handlery (żeby uniknąć duplikatów)
    this.connection.off("ReceiveNotification");
    this.connection.off("ReceiveNotificationMarkAsRead");

    // Nasłuchuj na nowe powiadomienia
    this.connection.on("ReceiveNotification", (payload: NotificationPayloadDto) => {
      const receiveTimestamp = new Date().toISOString();
      const createdAt = payload.notification.createdAt;
      const latencyMs = Date.now() - new Date(createdAt).getTime();
      
      // Zapisz czas otrzymania
      this.lastNotificationTime = Date.now();
      
      // Powiadom listenery
      this.notifyPayloadListeners(payload);
    });

    // 🔄 Nasłuchuj na synchronizację między urządzeniami
    this.connection.on("ReceiveNotificationMarkAsRead", (dto: NotificationMarkAsReadDto) => {
      this.notifySyncListeners(dto);
    });
  }

  private ensureReadyPromise(): Promise<void> {
    if (!this.readyPromise) {
      this.readyPromise = new Promise<void>(resolve => (this.readyResolve = resolve));
    }
    return this.readyPromise;
  }

  async startConnection(): Promise<void> {
    const conn = this.getOrCreateConnection();

    // Jeśli już połączone, nie rób nic
    if (conn.state === signalR.HubConnectionState.Connected) {
      return;
    }

    // Jeśli trwa łączenie - CZEKAJ na ready zamiast zwracać
    if (conn.state === signalR.HubConnectionState.Connecting || 
        conn.state === signalR.HubConnectionState.Reconnecting) {
      await this.ensureReadyPromise();
      return;
    }

    // ✅ MUTEX: jeśli już trwa start, poczekaj na tamten
    if (this.startPromise) {
      return this.startPromise;
    }

    // Utwórz readiness promise
    this.ensureReadyPromise();

    // ✅ Utwórz promise dla tego start'u
    this.startPromise = (async () => {
      try {
        // Sprawdź ponownie stan (mogło się zmienić podczas await)
        if (conn.state === signalR.HubConnectionState.Connected) {
          this.readyResolve?.();
          this.readyResolve = null;
          return;
        }
        await conn.start();
        
        // Resolve readiness
        this.readyResolve?.();
        this.readyResolve = null;
        
        // 🔍 Sprawdź czy backend widzi UserIdentifier (oid)
        try {
          const userIdentifier = await conn.invoke("WhoAmI");
          
          if (userIdentifier === "NULL" || !userIdentifier) {
          } else {
          }
        } catch (invokeError) {
        }

        // NIE wywołuj afterReconnect przy pierwszym start - to jest zrobione osobno w AuthContext
      } catch (err) {
        // NIE rób ręcznego retry - automaticReconnect się tym zajmie
        throw err;
      } finally {
        this.startPromise = null;
      }
    })();

    return this.startPromise;
  }

  async stopConnection(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
      } catch (error) {
      }
      // NIE nulluj connection - zostaw jako singleton dla reconnect
    }
  }

  // Subskrybuj na nowe powiadomienia
  onNotificationReceived(callback: (payload: NotificationPayloadDto) => void): () => void {
    this.payloadListeners.push(callback);

    // Zwróć funkcję do unsubscribe
    return () => {
      this.payloadListeners = this.payloadListeners.filter(listener => listener !== callback);
    };
  }

  private notifyPayloadListeners(payload: NotificationPayloadDto): void {
    this.payloadListeners.forEach(listener => {
      try {
        listener(payload);
      } catch (error) {
      }
    });
  }

  private notifySyncListeners(dto: NotificationMarkAsReadDto): void {
    this.syncListeners.forEach(listener => {
      try {
        listener(dto);
      } catch (error) {
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

  // Diagnostyka - kiedy ostatnio otrzymano powiadomienie
  getLastNotificationTime(): number | null {
    return this.lastNotificationTime;
  }

  // Diagnostyka - ile sekund temu ostatnie powiadomienie
  getSecondsSinceLastNotification(): number | null {
    if (!this.lastNotificationTime) return null;
    return Math.floor((Date.now() - this.lastNotificationTime) / 1000);
  }

  // Ustaw callback wywoływany po reconnect (do resync z API)
  setAfterReconnect(callback: () => Promise<void>): void {
    this.afterReconnect = callback;
  }

  // Ping dla health check
  async ping(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error("SignalR not connected");
    }
    await this.connection.invoke("Ping");
  }

  // NIE POTRZEBNE - SignalR automatycznie obsługuje token refresh:
  // - accessTokenFactory pobiera świeży token przy każdym żądaniu (negotiate + reconnect)
  // - automaticReconnect odnawia połączenie gdy backend zwróci 401/403
  // Ręczny restart powoduje niestabilność - "czasem przychodzi, czasem nie"

  // Wymuszony restart połączenia (np. po ping failure lub długim disconnected)
  async forceRestart(): Promise<void> {
    
    const conn = this.connection;
    if (!conn) {
      return this.startConnection();
    }

    try {
      // Stop jeśli nie jest disconnected
      if (conn.state !== signalR.HubConnectionState.Disconnected) {
        await conn.stop();
      }
    } catch (stopError) {
    }

    // Krótka przerwa
    await new Promise(resolve => setTimeout(resolve, 200));

    // Start ponownie
    try {
      await conn.start();
      
      // Resync po restart
      if (this.afterReconnect) {
        await this.afterReconnect();
      }
    } catch (startError) {
      throw startError;
    }
  }

  // Wyczyść cache (np. przy logout)
  clearCache(): void {
  }
}

// Singleton instance
export const notificationHubService = new NotificationHubService();
