using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Chats;
using Chat.Mappers;
using Entities.Models.Chats;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.FindChatsByMembers;

public sealed class FindChatsByMembersQueryHandler : IRequestHandler<FindChatsByMembersQuery, List<ChatWeb>>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly ICurrentUser currentUser;

    public FindChatsByMembersQueryHandler(
        IReadRepository<ChatModel> chatRepo,
        ICurrentUser currentUser)
    {
        this.chatRepo = chatRepo;
        this.currentUser = currentUser;
    }

    public async Task<List<ChatWeb>> Handle(FindChatsByMembersQuery request, CancellationToken cancellationToken)
    {
        // Result must include the caller and every requested member; nobody else.
        HashSet<Guid> required = request.MemberUserIds.ToHashSet();
        required.Add(currentUser.Id);
        List<Guid> requiredIds = required.ToList();
        int requiredCount = requiredIds.Count;

        if (requiredCount == 0)
        {
            return new();
        }

        IEnumerable<ChatModel> chats = await chatRepo.GetBySearch(
            c => c.Members.Count == requiredCount
                 && c.Members.Count(m => requiredIds.Contains(m.UserId)) == requiredCount,
            include => include.Include(c => c.Members));

        return chats
            .OrderByDescending(c => c.CreatedAt)
            .Select(c =>
            {
                List<ChatMemberWeb> memberWebs = c.Members
                    .Select(m => ChatMapper.MapMember(m, string.Empty, string.Empty))
                    .ToList();
                return ChatMapper.MapChat(c, memberWebs, lastMessage: null, unreadCount: 0);
            })
            .ToList();
    }
}
