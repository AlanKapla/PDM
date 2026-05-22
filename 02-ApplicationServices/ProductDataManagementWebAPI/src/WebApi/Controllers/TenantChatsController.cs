using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Chats;
using Business.Interfaces.WebModels.Chats.Requests;
using Chat.CQRS.Conversations.AddChatMember;
using Chat.CQRS.Conversations.CreateGroupChat;
using Chat.CQRS.Conversations.DeleteChat;
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
    /// <summary>
    /// Tenant-scoped chat operations. All endpoints route under
    /// <c>/api/tenants/{tenantId}/chats</c> and are authorized via
    /// <c>[Authorize(Policy = PermissionCodes.ChatXxx)]</c>, which extracts
    /// <c>tenantId</c> from the route. Cross-tenant direct chats are served by
    /// <see cref="DirectChatsController"/>.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/tenants/{tenantId}/chats")]
    public class TenantChatsController : BaseApiController
    {
        public TenantChatsController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>Returns chats for the current user within the given tenant.</summary>
        [HttpGet]
        [Authorize(Policy = PermissionCodes.ChatRead)]
        public async Task<IActionResult> GetTenantChats(
            [FromRoute] Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            GetUserChatsQuery query = new GetUserChatsQuery(TenantId: tenantId);
            List<ChatWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Searches chats within the tenant by phrase. Matches chat name, member full names, and message content.</summary>
        [HttpGet("search")]
        [Authorize(Policy = PermissionCodes.ChatRead)]
        public async Task<IActionResult> SearchChats(
            [FromRoute] Guid tenantId,
            [FromQuery] string q,
            CancellationToken cancellationToken = default)
        {
            SearchChatsQuery query = new SearchChatsQuery(q, tenantId);
            List<ChatSearchResultWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Creates a group chat (3+ members) bound to a project within the tenant.</summary>
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ChatWrite)]
        public async Task<IActionResult> CreateGroupChat(
            [FromRoute] Guid tenantId,
            [FromBody] CreateChatRequest body,
            CancellationToken cancellationToken = default)
        {
            CreateGroupChatCommand command = new CreateGroupChatCommand
            {
                TenantId = tenantId,
                ProjectId = body.ProjectId,
                MemberUserIds = body.MemberUserIds ?? new List<Guid>(),
                Name = body.Name
            };
            CreateChatResultWeb result = await Send(command);
            return CreatedAtAction(nameof(GetTenantChats), new { tenantId }, result);
        }

        /// <summary>Returns project contact groups limited to the current tenant.</summary>
        [HttpGet("contacts")]
        [Authorize(Policy = PermissionCodes.ChatRead)]
        public async Task<IActionResult> GetContacts(
            [FromRoute] Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            GetProjectMatesQuery query = new GetProjectMatesQuery(tenantId);
            List<ProjectContactsGroupWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Renames a group chat. Admin only.</summary>
        [HttpPatch("{chatId}")]
        [Authorize(Policy = PermissionCodes.ChatRename)]
        public async Task<IActionResult> RenameChat(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromBody] RenameChatRequest body,
            CancellationToken cancellationToken = default)
        {
            RenameGroupChatCommand command = new RenameGroupChatCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                NewName = body.NewName
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Deletes a group chat (admin only) along with its messages and members.</summary>
        [HttpDelete("{chatId}")]
        [Authorize(Policy = PermissionCodes.ChatDelete)]
        public async Task<IActionResult> DeleteChat(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            DeleteChatCommand command = new DeleteChatCommand
            {
                TenantId = tenantId,
                ChatId = chatId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Returns all members of a chat. Requires membership.</summary>
        [HttpGet("{chatId}/members")]
        [Authorize(Policy = PermissionCodes.ChatRead)]
        public async Task<IActionResult> GetChatMembers(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            GetChatMembersQuery query = new GetChatMembersQuery
            {
                TenantId = tenantId,
                ChatId = chatId
            };
            List<ChatMemberWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Returns project members not yet in this chat. Group chats only.</summary>
        [HttpGet("{chatId}/available-members")]
        [Authorize(Policy = PermissionCodes.ChatMembersManage)]
        public async Task<IActionResult> GetAvailableMembers(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            GetAvailableMembersQuery query = new GetAvailableMembersQuery
            {
                TenantId = tenantId,
                ChatId = chatId
            };
            List<AvailableMemberWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Adds a member to a group chat (admin only).</summary>
        [HttpPost("{chatId}/members")]
        [Authorize(Policy = PermissionCodes.ChatMembersManage)]
        public async Task<IActionResult> AddChatMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromBody] AddChatMemberRequest body,
            CancellationToken cancellationToken = default)
        {
            AddChatMemberCommand command = new AddChatMemberCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                UserId = body.UserId,
                ProjectId = body.ProjectId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Removes a member from a group chat. Self-removal or admin removal of non-admin.</summary>
        [HttpDelete("{chatId}/members/{userId}")]
        [Authorize(Policy = PermissionCodes.ChatMembersManage)]
        public async Task<IActionResult> RemoveChatMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            RemoveChatMemberCommand command = new RemoveChatMemberCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                UserId = userId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Current user leaves a group chat. Admin leaving dissolves the group.</summary>
        [HttpPost("{chatId}/leave")]
        public async Task<IActionResult> LeaveChat(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            LeaveChatCommand command = new LeaveChatCommand
            {
                TenantId = tenantId,
                ChatId = chatId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Returns cursor-paginated messages for a chat.</summary>
        [HttpGet("{chatId}/messages")]
        [Authorize(Policy = PermissionCodes.ChatRead)]
        public async Task<IActionResult> GetChatMessages(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromQuery] Guid? before = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            GetChatMessagesQuery query = new GetChatMessagesQuery
            {
                TenantId = tenantId,
                ChatId = chatId,
                Before = before,
                PageSize = pageSize
            };
            List<MessageWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Sends a message to a chat. Requires membership.</summary>
        [HttpPost("{chatId}/messages")]
        [Authorize(Policy = PermissionCodes.ChatWrite)]
        public async Task<IActionResult> SendMessage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromBody] SendMessageRequest body,
            CancellationToken cancellationToken = default)
        {
            SendMessageCommand command = new SendMessageCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                Content = body.Content,
                ReplyToMessageId = body.ReplyToMessageId
            };
            Guid messageId = await Send(command);
            return CreatedAtAction(nameof(GetChatMessages), new { tenantId, chatId }, new { id = messageId });
        }

        /// <summary>Edits a message. Author only, within the configured time window.</summary>
        [HttpPatch("{chatId}/messages/{messageId}")]
        [Authorize(Policy = PermissionCodes.ChatWrite)]
        public async Task<IActionResult> EditMessage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromRoute] Guid messageId,
            [FromBody] EditMessageRequest body,
            CancellationToken cancellationToken = default)
        {
            EditMessageCommand command = new EditMessageCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                MessageId = messageId,
                NewContent = body.Content
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Soft-deletes a message. Author or chat admin only.</summary>
        [HttpDelete("{chatId}/messages/{messageId}")]
        [Authorize(Policy = PermissionCodes.ChatWrite)]
        public async Task<IActionResult> DeleteMessage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken = default)
        {
            DeleteMessageCommand command = new DeleteMessageCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                MessageId = messageId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Marks all messages in a chat as read for the current user.</summary>
        [HttpPut("{chatId}/read")]
        [Authorize(Policy = PermissionCodes.ChatRead)]
        public async Task<IActionResult> MarkAsRead(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            MarkAsReadCommand command = new MarkAsReadCommand
            {
                TenantId = tenantId,
                ChatId = chatId
            };
            await Send(command);
            return NoContent();
        }
    }
}
