using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;

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
                chatMember.LastReadAt = DateTime.UtcNow;
                await chatMemberRepo.Update(chatMember);
                await chatMemberRepo.SaveChangesAsync(cancellationToken);
                return 1;
            }

            return 0;
        }
    }
}
