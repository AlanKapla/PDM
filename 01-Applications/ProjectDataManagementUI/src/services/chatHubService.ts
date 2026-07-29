import * as signalR from "@microsoft/signalr";
import { msalInstance } from "../auth/msalInstance";
import { nativeSilentRequest } from "../config/authConfig";
import type {
  ChatWeb,
  MessageWeb,
  MessageEditedPayload,
  MessageDeletedPayload,
  ReadReceiptPayload,
  UserTypingPayload,
  MemberAddedPayload,
  RemovedFromChatPayload,
  ChatDeletedPayload,
} from "../types/chat.types";
import { isDemoOnlySession } from "../utils/demoSession";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

type Handler<T> = (payload: T) => void;

class ChatHubService {
  private connection: signalR.HubConnection | null = null;
  private startPromise: Promise<void> | null = null;

  private chatCreatedHandlers: Handler<ChatWeb>[] = [];
  private receiveMessageHandlers: Handler<MessageWeb>[] = [];
  private messageEditedHandlers: Handler<MessageEditedPayload>[] = [];
  private messageDeletedHandlers: Handler<MessageDeletedPayload>[] = [];
  private readReceiptHandlers: Handler<ReadReceiptPayload>[] = [];
  private userTypingHandlers: Handler<UserTypingPayload>[] = [];
  private memberAddedHandlers: Handler<MemberAddedPayload>[] = [];
  private removedFromChatHandlers: Handler<RemovedFromChatPayload>[] = [];
  private chatDeletedHandlers: Handler<ChatDeletedPayload>[] = [];

  private getOrCreateConnection(): signalR.HubConnection {
    if (this.connection) return this.connection;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/api/hubs/chat`, {
        accessTokenFactory: async () => {
          const accounts = msalInstance.getAllAccounts();
          if (accounts.length === 0) throw new Error("No authenticated user");
          const account = msalInstance.getActiveAccount() || accounts[0];
          const response = await msalInstance.acquireTokenSilent({
            ...nativeSilentRequest,
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
    if (!this.connection) return;

    const events = [
      "ChatCreated",
      "ReceiveMessage",
      "MessageEdited",
      "MessageDeleted",
      "ReadReceipt",
      "UserTyping",
      "MemberAdded",
      "RemovedFromChat",
      "ChatDeleted",
    ];
    events.forEach((e) => this.connection!.off(e));

    this.connection.on("ChatCreated", (chat: ChatWeb) =>
      this.chatCreatedHandlers.forEach((h) => h(chat))
    );
    this.connection.on("ReceiveMessage", (message: MessageWeb) =>
      this.receiveMessageHandlers.forEach((h) => h(message))
    );
    this.connection.on("MessageEdited", (payload: MessageEditedPayload) =>
      this.messageEditedHandlers.forEach((h) => h(payload))
    );
    this.connection.on("MessageDeleted", (payload: MessageDeletedPayload) =>
      this.messageDeletedHandlers.forEach((h) => h(payload))
    );
    this.connection.on("ReadReceipt", (payload: ReadReceiptPayload) =>
      this.readReceiptHandlers.forEach((h) => h(payload))
    );
    this.connection.on("UserTyping", (payload: UserTypingPayload) =>
      this.userTypingHandlers.forEach((h) => h(payload))
    );
    this.connection.on("MemberAdded", (payload: MemberAddedPayload) =>
      this.memberAddedHandlers.forEach((h) => h(payload))
    );
    this.connection.on("RemovedFromChat", (payload: RemovedFromChatPayload) =>
      this.removedFromChatHandlers.forEach((h) => h(payload))
    );
    this.connection.on("ChatDeleted", (chatId: string) =>
      this.chatDeletedHandlers.forEach((h) => h({ chatId }))
    );
  }

  async startConnection(): Promise<void> {
    if (isDemoOnlySession()) {
      return;
    }

    const conn = this.getOrCreateConnection();

    if (conn.state === signalR.HubConnectionState.Connected) return;

    if (
      conn.state === signalR.HubConnectionState.Connecting ||
      conn.state === signalR.HubConnectionState.Reconnecting
    ) {
      await this.startPromise;
      return;
    }

    if (this.startPromise) return this.startPromise;

    this.startPromise = conn.start().finally(() => {
      this.startPromise = null;
    });

    return this.startPromise;
  }

  async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state ?? null;
  }

  // ---- Hub method invocations ----

  async joinChat(chatId: string): Promise<void> {
    if (isDemoOnlySession()) {
      return;
    }
    await this.getOrCreateConnection().invoke("JoinChat", chatId);
  }

  async leaveChat(chatId: string): Promise<void> {
    if (isDemoOnlySession()) {
      return;
    }
    await this.getOrCreateConnection().invoke("LeaveChat", chatId);
  }

  async sendMessage(
    chatId: string,
    content: string,
    replyToMessageId?: string | null
  ): Promise<void> {
    if (isDemoOnlySession()) {
      return;
    }
    await this.getOrCreateConnection().invoke(
      "SendMessage",
      chatId,
      content,
      replyToMessageId ?? null
    );
  }

  async markAsRead(chatId: string): Promise<void> {
    if (isDemoOnlySession()) {
      return;
    }
    await this.getOrCreateConnection().invoke("MarkAsRead", chatId);
  }

  async startTyping(chatId: string): Promise<void> {
    if (isDemoOnlySession()) {
      return;
    }
    await this.getOrCreateConnection().invoke("StartTyping", chatId);
  }

  async stopTyping(chatId: string): Promise<void> {
    if (isDemoOnlySession()) {
      return;
    }
    await this.getOrCreateConnection().invoke("StopTyping", chatId);
  }

  // ---- Event subscriptions (return unsubscribe fn) ----

  onChatCreated(handler: Handler<ChatWeb>): () => void {
    this.chatCreatedHandlers.push(handler);
    return () => {
      this.chatCreatedHandlers = this.chatCreatedHandlers.filter((h) => h !== handler);
    };
  }

  onReceiveMessage(handler: Handler<MessageWeb>): () => void {
    this.receiveMessageHandlers.push(handler);
    return () => {
      this.receiveMessageHandlers = this.receiveMessageHandlers.filter((h) => h !== handler);
    };
  }

  onMessageEdited(handler: Handler<MessageEditedPayload>): () => void {
    this.messageEditedHandlers.push(handler);
    return () => {
      this.messageEditedHandlers = this.messageEditedHandlers.filter((h) => h !== handler);
    };
  }

  onMessageDeleted(handler: Handler<MessageDeletedPayload>): () => void {
    this.messageDeletedHandlers.push(handler);
    return () => {
      this.messageDeletedHandlers = this.messageDeletedHandlers.filter((h) => h !== handler);
    };
  }

  onReadReceipt(handler: Handler<ReadReceiptPayload>): () => void {
    this.readReceiptHandlers.push(handler);
    return () => {
      this.readReceiptHandlers = this.readReceiptHandlers.filter((h) => h !== handler);
    };
  }

  onUserTyping(handler: Handler<UserTypingPayload>): () => void {
    this.userTypingHandlers.push(handler);
    return () => {
      this.userTypingHandlers = this.userTypingHandlers.filter((h) => h !== handler);
    };
  }

  onMemberAdded(handler: Handler<MemberAddedPayload>): () => void {
    this.memberAddedHandlers.push(handler);
    return () => {
      this.memberAddedHandlers = this.memberAddedHandlers.filter((h) => h !== handler);
    };
  }

  onRemovedFromChat(handler: Handler<RemovedFromChatPayload>): () => void {
    this.removedFromChatHandlers.push(handler);
    return () => {
      this.removedFromChatHandlers = this.removedFromChatHandlers.filter((h) => h !== handler);
    };
  }

  onChatDeleted(handler: Handler<ChatDeletedPayload>): () => void {
    this.chatDeletedHandlers.push(handler);
    return () => {
      this.chatDeletedHandlers = this.chatDeletedHandlers.filter((h) => h !== handler);
    };
  }
}

export const chatHubService = new ChatHubService();
