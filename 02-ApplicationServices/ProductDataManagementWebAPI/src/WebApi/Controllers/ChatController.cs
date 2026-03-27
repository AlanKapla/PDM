using Chat.CQRS.Conversations.AddChatMember;
using Chat.CQRS.Conversations.CreateChat;
using Chat.CQRS.Conversations.DeleteChat;
using Chat.CQRS.Conversations.FindChatsByMembers;
using Chat.CQRS.Conversations.GetAvailableMembers;
using Chat.CQRS.Conversations.GetChatMembers;
using Chat.CQRS.Conversations.GetProjectMates;
using Chat.CQRS.Conversations.GetUserChats;
using Chat.CQRS.Conversations.LeaveChat;
using Chat.CQRS.Conversations.RemoveChatMember;
using Chat.CQRS.Conversations.RenameGroupChat;
using Chat.CQRS.Conversations.SearchChats;
using Chat.CQRS.Messages.DeleteMessage;
using Chat.CQRS.Messages.EditMessage;
using Chat.CQRS.Messages.GetChatMessages;
using Chat.CQRS.Messages.MarkAsRead;
using Chat.CQRS.Messages.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Authorize]
    public class ChatController(IMediator mediator) : BaseApiController(mediator)
    {
        /// <summary>Returns all chats for the current user.</summary>
        [HttpGet("api/chats")]
        public async Task<IActionResult> GetUserChats(CancellationToken cancellationToken = default)
        {
            var result = await Send(new GetUserChatsQuery());
            return Ok(result);
        }

        /// <summary>Returns all users grouped by project who share at least one project with the current user.</summary>
        [HttpGet("api/chats/contacts")]
        public async Task<IActionResult> GetProjectMates(CancellationToken cancellationToken = default)
        {
            var result = await Send(new GetProjectMatesQuery());
            return Ok(result);
        }

        /// <summary>Searches chats by phrase. Matches chat name, member full names, and message content. MatchingMessageIds contains IDs of messages that contained the phrase.</summary>
        [HttpGet("api/chats/search")]
        public async Task<IActionResult> SearchChats(
            [FromQuery] string q,
            CancellationToken cancellationToken = default)
        {
            var result = await Send(new SearchChatsQuery(q));
            return Ok(result);
        }

        /// <summary>
        /// Creates a chat. If memberUserIds contains 1 user, creates or returns an existing direct chat.
        /// If memberUserIds contains 2 or more users, creates a new group chat (projectId required).
        /// </summary>
        [HttpPost("api/chats")]
        public async Task<IActionResult> CreateChat(
            [FromBody] CreateChatRequest body,
            CancellationToken cancellationToken = default)
        {
            var result = await Send(new CreateChatCommand(body.ProjectId, body.MemberUserIds, body.Name));
            return CreatedAtAction(nameof(GetUserChats), new { id = result.Id }, result);
        }

        /// <summary>Finds all chats that contain the current user and every specified member. Useful for searching chats by participants.</summary>
        [HttpGet("api/chats/by-members")]
        public async Task<IActionResult> FindChatsByMembers(
            [FromQuery] List<Guid> memberIds,
            CancellationToken cancellationToken = default)
        {
            var result = await Send(new FindChatsByMembersQuery(memberIds));
            return Ok(result);
        }

        /// <summary>Renames a group chat. Admin only.</summary>
        [HttpPatch("api/chats/{chatId}")]
        public async Task<IActionResult> RenameChat(
            [FromRoute] Guid chatId,
            [FromBody] RenameChatRequest body,
            CancellationToken cancellationToken = default)
        {
            await Send(new RenameGroupChatCommand(chatId, body.NewName));
            return NoContent();
        }

        /// <summary>
        /// Deletes a chat and all of its messages and members.
        /// Group chat: admin only. Direct chat: any member.
        /// All members receive a ChatDeleted SignalR event.
        /// </summary>
        [HttpDelete("api/chats/{chatId}")]
        public async Task<IActionResult> DeleteChat(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            await Send(new DeleteChatCommand(chatId));
            return NoContent();
        }

        /// <summary>Returns all members of a chat. Requires membership.</summary>
        [HttpGet("api/chats/{chatId}/members")]
        public async Task<IActionResult> GetChatMembers(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            var result = await Send(new GetChatMembersQuery(chatId));
            return Ok(result);
        }

        /// <summary>Returns users who are members of the chat project but not yet in the chat. Group chats only.</summary>
        [HttpGet("api/chats/{chatId}/available-members")]
        public async Task<IActionResult> GetAvailableMembers(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            var result = await Send(new GetAvailableMembersQuery(chatId));
            return Ok(result);
        }

        /// <summary>Adds a member to a chat. For group chats: admin only. For direct chats: projectId required (converts to group).</summary>
        [HttpPost("api/chats/{chatId}/members")]
        public async Task<IActionResult> AddChatMember(
            [FromRoute] Guid chatId,
            [FromBody] AddChatMemberRequest body,
            CancellationToken cancellationToken = default)
        {
            await Send(new AddChatMemberCommand(chatId, body.UserId, body.ProjectId));
            return NoContent();
        }

        /// <summary>Removes a member from a group chat. Self-removal or admin removal of non-admin. If 2 members remain, redirects to direct chat.</summary>
        [HttpDelete("api/chats/{chatId}/members/{userId}")]
        public async Task<IActionResult> RemoveChatMember(
            [FromRoute] Guid chatId,
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            await Send(new RemoveChatMemberCommand(chatId, userId));
            return NoContent();
        }

        /// <summary>
        /// Current user leaves a chat.
        /// Non-admin: removed from the chat; if 2 members remain, a direct chat redirect is provided.
        /// Admin: the group is dissolved and all members receive a ChatDeleted event.
        /// </summary>
        [HttpPost("api/chats/{chatId}/leave")]
        public async Task<IActionResult> LeaveChat(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            await Send(new LeaveChatCommand(chatId));
            return NoContent();
        }

        /// <summary>Returns cursor-paginated messages for a chat. Pass before=messageId to load older pages.</summary>
        [HttpGet("api/chats/{chatId}/messages")]
        public async Task<IActionResult> GetChatMessages(
            [FromRoute] Guid chatId,
            [FromQuery] Guid? before = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var result = await Send(new GetChatMessagesQuery(chatId, before, pageSize));
            return Ok(result);
        }

        /// <summary>Sends a message to a chat. Requires membership.</summary>
        [HttpPost("api/chats/{chatId}/messages")]
        public async Task<IActionResult> SendMessage(
            [FromRoute] Guid chatId,
            [FromBody] SendMessageRequest body,
            CancellationToken cancellationToken = default)
        {
            var messageId = await Send(new SendMessageCommand(chatId, body.Content, body.ReplyToMessageId));
            return CreatedAtAction(nameof(GetChatMessages), new { chatId }, new { id = messageId });
        }

        /// <summary>Edits a message. Author only, within the configured time window.</summary>
        [HttpPatch("api/chats/{chatId}/messages/{messageId}")]
        public async Task<IActionResult> EditMessage(
            [FromRoute] Guid chatId,
            [FromRoute] Guid messageId,
            [FromBody] EditMessageRequest body,
            CancellationToken cancellationToken = default)
        {
            await Send(new EditMessageCommand(chatId, messageId, body.Content));
            return NoContent();
        }

        /// <summary>Soft-deletes a message. Author or chat admin only.</summary>
        [HttpDelete("api/chats/{chatId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(
            [FromRoute] Guid chatId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken = default)
        {
            await Send(new DeleteMessageCommand(chatId, messageId));
            return NoContent();
        }

        /// <summary>Marks all messages in a chat as read for the current user.</summary>
        [HttpPut("api/chats/{chatId}/read")]
        public async Task<IActionResult> MarkAsRead(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            await Send(new MarkAsReadCommand(chatId));
            return NoContent();
        }
    }

    public record CreateChatRequest(Guid? ProjectId, List<Guid> MemberUserIds, string? Name = null);
    public record RenameChatRequest(string NewName);
    public record AddChatMemberRequest(Guid UserId, Guid? ProjectId = null);
    public record SendMessageRequest(string Content, Guid? ReplyToMessageId = null);
    public record EditMessageRequest(string Content);
}
