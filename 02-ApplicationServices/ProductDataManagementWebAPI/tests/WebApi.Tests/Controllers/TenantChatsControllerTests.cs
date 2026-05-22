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
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class TenantChatsControllerTests : ControllerTestBase
    {
        private readonly TenantChatsController sut;

        public TenantChatsControllerTests()
        {
            sut = new TenantChatsController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetTenantChats_PassesTenantId_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();

            IActionResult result = await sut.GetTenantChats(tenantId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetUserChatsQuery>(q => q.TenantId == tenantId);
        }

        [Fact]
        public async Task SearchChats_PassesTenantIdAndQuery_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();

            IActionResult result = await sut.SearchChats(tenantId, "test");

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<SearchChatsQuery>(q => q.TenantId == tenantId && q.Phrase == "test");
        }

        [Fact]
        public async Task CreateGroupChat_BuildsCommand_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            CreateChatRequest body = new CreateChatRequest(null, new List<Guid>(), "Chat");

            IActionResult result = await sut.CreateGroupChat(tenantId, body);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<CreateGroupChatCommand>(c => c.TenantId == tenantId && c.Name == "Chat");
        }

        [Fact]
        public async Task GetContacts_PassesTenantId_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();

            IActionResult result = await sut.GetContacts(tenantId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetProjectMatesQuery>(q => q.TenantId == tenantId);
        }

        [Fact]
        public async Task RenameChat_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();
            RenameChatRequest body = new RenameChatRequest("NewName");

            IActionResult result = await sut.RenameChat(tenantId, chatId, body);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<RenameGroupChatCommand>(c => c.TenantId == tenantId && c.ChatId == chatId && c.NewName == "NewName");
        }

        [Fact]
        public async Task DeleteChat_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.DeleteChat(tenantId, chatId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteChatCommand>(c => c.TenantId == tenantId && c.ChatId == chatId);
        }

        [Fact]
        public async Task GetChatMembers_BuildsQuery_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.GetChatMembers(tenantId, chatId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetChatMembersQuery>(q => q.TenantId == tenantId && q.ChatId == chatId);
        }

        [Fact]
        public async Task GetAvailableMembers_BuildsQuery_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.GetAvailableMembers(tenantId, chatId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetAvailableMembersQuery>(q => q.TenantId == tenantId && q.ChatId == chatId);
        }

        [Fact]
        public async Task AddChatMember_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            AddChatMemberRequest body = new AddChatMemberRequest(userId);

            IActionResult result = await sut.AddChatMember(tenantId, chatId, body);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<AddChatMemberCommand>(c => c.TenantId == tenantId && c.ChatId == chatId && c.UserId == userId);
        }

        [Fact]
        public async Task RemoveChatMember_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();

            IActionResult result = await sut.RemoveChatMember(tenantId, chatId, userId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<RemoveChatMemberCommand>(c => c.TenantId == tenantId && c.ChatId == chatId && c.UserId == userId);
        }

        [Fact]
        public async Task LeaveChat_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.LeaveChat(tenantId, chatId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<LeaveChatCommand>(c => c.TenantId == tenantId && c.ChatId == chatId);
        }

        [Fact]
        public async Task GetChatMessages_BuildsQuery_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.GetChatMessages(tenantId, chatId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetChatMessagesQuery>(q => q.TenantId == tenantId && q.ChatId == chatId);
        }

        [Fact]
        public async Task SendMessage_BuildsCommand_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();
            SendMessageRequest body = new SendMessageRequest("Hi");

            IActionResult result = await sut.SendMessage(tenantId, chatId, body);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<SendMessageCommand>(c => c.TenantId == tenantId && c.ChatId == chatId && c.Content == "Hi");
        }

        [Fact]
        public async Task EditMessage_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();
            Guid messageId = Guid.NewGuid();
            EditMessageRequest body = new EditMessageRequest("edited");

            IActionResult result = await sut.EditMessage(tenantId, chatId, messageId, body);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<EditMessageCommand>(c => c.TenantId == tenantId && c.ChatId == chatId && c.MessageId == messageId);
        }

        [Fact]
        public async Task DeleteMessage_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();
            Guid messageId = Guid.NewGuid();

            IActionResult result = await sut.DeleteMessage(tenantId, chatId, messageId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteMessageCommand>(c => c.TenantId == tenantId && c.ChatId == chatId && c.MessageId == messageId);
        }

        [Fact]
        public async Task MarkAsRead_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.MarkAsRead(tenantId, chatId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<MarkAsReadCommand>(c => c.TenantId == tenantId && c.ChatId == chatId);
        }
    }
}
