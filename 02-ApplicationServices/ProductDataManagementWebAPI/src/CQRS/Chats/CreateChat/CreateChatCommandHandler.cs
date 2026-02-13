using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Chats.CreateChat
{
    public class CreateChatCommandHandler : IRequestHandler<CreateChatCommand, Guid>
    {
        private readonly IRepository<Chat> chatRepo;
        private readonly IRepository<ChatMember> chatMemberRepo;
        private readonly ICurrentUser currentUser;

        public CreateChatCommandHandler(
            IRepository<Chat> chatRepo,
            IRepository<ChatMember> chatMemberRepo,
            ICurrentUser currentUser)
        {
            this.chatRepo = chatRepo;
            this.chatMemberRepo = chatMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateChatCommand request, CancellationToken cancellationToken)
        {
            Chat chat = new Chat
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                Name = request.Name,
                IsGroupChat = request.IsGroupChat,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id
            };

            await chatRepo.Insert(chat);

            ChatMember creatorMember = new ChatMember
            {
                ChatId = chat.Id,
                TenantId = request.TenantId,
                UserId = currentUser.Id,
                JoinedAt = DateTime.UtcNow
            };

            await chatMemberRepo.Insert(creatorMember);

            foreach (var userId in request.MemberUserIds.Where(id => id != currentUser.Id))
            {
                ChatMember member = new ChatMember
                {
                    ChatId = chat.Id,
                    TenantId = request.TenantId,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                };

                await chatMemberRepo.Insert(member);
            }

            return chat.Id;
        }
    }
}
