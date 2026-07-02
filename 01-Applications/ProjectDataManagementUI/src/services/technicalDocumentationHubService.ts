import * as signalR from '@microsoft/signalr';
import { msalInstance } from '../main';
import { silentRequest } from '../config/authConfig';
import type { TechnicalDocumentationProcessingEvent } from '../types/technicalDocumentation.types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

type ProcessingCompletedHandler = (event: TechnicalDocumentationProcessingEvent) => void;

class TechnicalDocumentationHubService {
  private connection: signalR.HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private processingCompletedHandlers: ProcessingCompletedHandler[] = [];

  private getOrCreateConnection(): signalR.HubConnection {
    if (this.connection) {
      return this.connection;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/api/hubs/technical-documentation`, {
        accessTokenFactory: async () => {
          const accounts = msalInstance.getAllAccounts();
          if (accounts.length === 0) {
            throw new Error('No authenticated user');
          }
          const account = msalInstance.getActiveAccount() || accounts[0];
          const response = await msalInstance.acquireTokenSilent({
            ...silentRequest,
            account,
          });
          return response.accessToken;
        },
        withCredentials: false,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.keepAliveIntervalInMilliseconds = 10_000;
    this.connection.serverTimeoutInMilliseconds = 20_000;

    this.registerHandlers();

    this.connection.onclose(() => {
      this.startPromise = null;
    });

    return this.connection;
  }

  private registerHandlers(): void {
    if (!this.connection) {
      return;
    }

    this.connection.off('ProcessingCompleted');
    this.connection.on(
      'ProcessingCompleted',
      (event: TechnicalDocumentationProcessingEvent) => {
        this.processingCompletedHandlers.forEach((handler) => {
          try {
            handler(event);
          } catch {
            // ignore listener errors
          }
        });
      }
    );
  }

  async startConnection(): Promise<void> {
    const conn = this.getOrCreateConnection();

    if (conn.state === signalR.HubConnectionState.Connected) {
      return;
    }

    if (
      conn.state === signalR.HubConnectionState.Connecting ||
      conn.state === signalR.HubConnectionState.Reconnecting
    ) {
      await this.startPromise;
      return;
    }

    if (this.startPromise) {
      return this.startPromise;
    }

    this.startPromise = conn.start().finally(() => {
      this.startPromise = null;
    });

    return this.startPromise;
  }

  async stopConnection(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
      } catch {
        // ignore stop errors
      }
      this.connection = null;
      this.startPromise = null;
    }
  }

  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state ?? null;
  }

  onProcessingCompleted(handler: ProcessingCompletedHandler): () => void {
    this.processingCompletedHandlers.push(handler);
    return () => {
      this.processingCompletedHandlers = this.processingCompletedHandlers.filter(
        (h) => h !== handler
      );
    };
  }
}

export const technicalDocumentationHubService = new TechnicalDocumentationHubService();
