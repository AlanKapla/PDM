import * as signalR from "@microsoft/signalr";
import type { NotificationWeb } from "../types/notification.types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

class NotificationHubService {
  private connection: signalR.HubConnection | null = null;
  private listeners: ((notification: NotificationWeb) => void)[] = [];
  private isConnecting: boolean = false;

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
      this.notifyListeners(notification);
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

  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state || null;
  }
}

// Singleton instance
export const notificationHubService = new NotificationHubService();
