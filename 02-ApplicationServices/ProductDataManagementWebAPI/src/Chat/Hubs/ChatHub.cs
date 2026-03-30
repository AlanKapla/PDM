using Business.Interfaces.Model;
using Chat.CQRS.Messages.MarkAsRead;
using Chat.CQRS.Messages.SendMessage;
using Entities.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.Hubs;

/// <summary>
/// SignalR hub for real-time chat.
/// Group naming: chat:{chatId}, user:{userId}
/// </summary>
[Authorize]
public sealed class ChatHub : Hub<IChatClient>
{
    private readonly IMediator mediator;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<ChatHub> logger;

    public ChatHub(
        IMediator mediator,
        IReadRepository<ChatMember> chatMemberRepo,
        ICurrentUser currentUser,
        ILogger<ChatHub> logger)
    {
        this.mediator = mediator;
        this.chatMemberRepo = chatMemberRepo;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    /// <summary>
    /// On connect: register the user's personal group for cross-chat notifications.
    /// Individual chat groups are joined explicitly by the client via JoinChat.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        string? userId = Context.UserIdentifier;

        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("ChatHub: unauthenticated connection attempt {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ChatHubGroups.User(Guid.Parse(userId)));

        logger.LogDebug("ChatHub: user {UserId} connected ({ConnectionId})", userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = Context.UserIdentifier;

        logger.LogDebug(
            "ChatHub: user {UserId} disconnected ({ConnectionId})",
            userId,
            Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    // ── Client → Server invocations ─────────────────────────────────────────

    /// <summary>Join the SignalR group for the given chat to receive real-time events.</summary>
    public async Task JoinChat(Guid chatId)
    {
        bool isMember = await chatMemberRepo.AnyAsync(
            cm => cm.ChatId == chatId && cm.UserId == currentUser.Id);

        if (!isMember)
        {
            throw new HubException("You are not a member of this chat.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ChatHubGroups.Chat(chatId));

        logger.LogDebug(
            "ChatHub: user {UserId} joined chat group {ChatId}",
            Context.UserIdentifier,
            chatId);
    }

    /// <summary>Leave the SignalR group for the given chat.</summary>
    public async Task LeaveChat(Guid chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatHubGroups.Chat(chatId));

        logger.LogDebug(
            "ChatHub: user {UserId} left chat group {ChatId}",
            Context.UserIdentifier,
            chatId);
    }

    /// <summary>Send a message via the hub. Persists to DB and broadcasts to all members.</summary>
    public async Task SendMessage(Guid chatId, string content, Guid? replyToMessageId = null)
    {
        var command = new SendMessageCommand(chatId, content, replyToMessageId);
        await mediator.Send(command);
    }

    /// <summary>Mark a chat as read by the current user.</summary>
    public async Task MarkAsRead(Guid chatId)
    {
        var command = new MarkAsReadCommand(chatId);
        await mediator.Send(command);
    }

    /// <summary>Broadcast typing start indicator to other members of the chat.</summary>
    public async Task StartTyping(Guid chatId)
    {
        string? userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await Clients
            .OthersInGroup(ChatHubGroups.Chat(chatId))
            .UserTyping(new UserTypingPayload(chatId, Guid.Parse(userId), true));
    }

    /// <summary>Broadcast typing stop indicator to other members of the chat.</summary>
    public async Task StopTyping(Guid chatId)
    {
        string? userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await Clients
            .OthersInGroup(ChatHubGroups.Chat(chatId))
            .UserTyping(new UserTypingPayload(chatId, Guid.Parse(userId), false));
    }
}
