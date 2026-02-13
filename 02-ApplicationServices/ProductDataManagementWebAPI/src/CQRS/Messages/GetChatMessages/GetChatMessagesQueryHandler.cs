using Business.Interfaces.WebModels.Messages;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Messages.GetChatMessages
{
    public class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, List<MessageWeb>>
    {
        private readonly IReadRepository<MessageHistory> messageRepo;

        public GetChatMessagesQueryHandler(IReadRepository<MessageHistory> messageRepo)
        {
            this.messageRepo = messageRepo;
        }

        public async Task<List<MessageWeb>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
        {
            var allMessages = await messageRepo.GetBySearch(
                m => m.ChatId == request.ChatId,
                include => include
                    .Include(m => m.User)
                    .ThenInclude(tm => tm.User));

            var messages = allMessages
                .OrderByDescending(m => m.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            return messages.Select(m => new MessageWeb(
                Id: m.Id,
                ChatId: m.ChatId,
                UserId: m.UserId,
                UserFirstName: m.User.User.FirstName,
                UserLastName: m.User.User.LastName,
                Content: m.Content,
                CreatedAt: m.CreatedAt
            )).ToList();
        }
    }
}
