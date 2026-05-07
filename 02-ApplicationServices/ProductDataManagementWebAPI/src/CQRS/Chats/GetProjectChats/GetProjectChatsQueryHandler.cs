using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Messages;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Chats.GetProjectChats
{
    public class GetProjectChatsQueryHandler : IRequestHandler<GetProjectChatsQuery, List<ChatWeb>>
    {
        private readonly IReadRepository<Chat> chatRepo;
        private readonly IRepository<ChatMember> chatMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetProjectChatsQueryHandler(
            IReadRepository<Chat> chatRepo,
            IRepository<ChatMember> chatMemberRepo,
            ICurrentUser currentUser)
        {
            this.chatRepo = chatRepo;
            this.chatMemberRepo = chatMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<List<ChatWeb>> Handle(GetProjectChatsQuery request, CancellationToken cancellationToken)
        {
            var userChatMemberships = await chatMemberRepo.GetBySearch(
                cm => cm.UserId == currentUser.Id,
                include => include.Include(cm => cm.Chat).ThenInclude(c => c.Messages));

            var chats = userChatMemberships
                .Select(cm => cm.Chat)
                .Distinct()
                .ToList();

            var result = new List<ChatWeb>();

            foreach (var chat in chats)
            {
                var members = await chatMemberRepo.GetBySearch(cm => cm.ChatId == chat.Id);
                var membersCount = members.Count();

                var lastMessage = chat.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                result.Add(new ChatWeb(
                    Id: chat.Id,
                    Name: chat.Name,
                    IsGroupChat: chat.IsGroupChat,
                    CreatedAt: chat.CreatedAt,
                    MembersCount: membersCount,
                    LastMessageAt: lastMessage?.CreatedAt
                ));
            }

            return result.OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt).ToList();
        }
    }
}
