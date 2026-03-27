using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Messages;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Messages.GetChatMessages
{
    public class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, List<MessageWeb>>
    {
        private readonly IReadRepository<MessageHistory> messageRepo;
        private readonly IProjectMemberService projectMemberService;

        public GetChatMessagesQueryHandler(
            IReadRepository<MessageHistory> messageRepo,
            IProjectMemberService projectMemberService)
        {
            this.messageRepo = messageRepo;
            this.projectMemberService = projectMemberService;
        }

        public async Task<List<MessageWeb>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
        {
            var allMessages = await messageRepo.GetBySearch(
                m => m.ChatId == request.ChatId);

            var page = allMessages
                .OrderByDescending(m => m.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            HashSet<Guid> senderIds = page.Select(m => m.UserId).ToHashSet();

            Dictionary<Guid, (string FirstName, string LastName)> userNames =
                await projectMemberService.GetUserNamesByIdsAsync(senderIds, cancellationToken);

            return page.Select(m =>
            {
                userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) sender);
                return new MessageWeb(
                    Id: m.Id,
                    ChatId: m.ChatId,
                    UserId: m.UserId,
                    UserFirstName: sender.FirstName ?? string.Empty,
                    UserLastName: sender.LastName ?? string.Empty,
                    Content: m.Content,
                    CreatedAt: m.CreatedAt
                );
            }).ToList();
        }
    }
}
