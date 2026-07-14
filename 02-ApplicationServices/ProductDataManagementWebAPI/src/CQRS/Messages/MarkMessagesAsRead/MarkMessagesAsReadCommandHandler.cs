using Business.Interfaces.Model;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Messages.MarkMessagesAsRead
{
    public class MarkMessagesAsReadCommandHandler : IRequestHandler<MarkMessagesAsReadCommand, int>
    {
        private readonly IRepository<ChatMember> chatMemberRepo;
        private readonly ICurrentUser currentUser;

        public MarkMessagesAsReadCommandHandler(
            IRepository<ChatMember> chatMemberRepo,
            ICurrentUser currentUser)
        {
            this.chatMemberRepo = chatMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<int> Handle(MarkMessagesAsReadCommand request, CancellationToken cancellationToken)
        {
            var chatMember = await chatMemberRepo.GetFirstBySearch(
                cm => cm.ChatId == request.ChatId && cm.UserId == currentUser.Id);

            if (chatMember != null)
            {
                chatMember.MarkRead(DateTime.UtcNow);
                await chatMemberRepo.Update(chatMember);
                await chatMemberRepo.SaveChangesAsync(cancellationToken);
                return 1;
            }

            return 0;
        }
    }
}
