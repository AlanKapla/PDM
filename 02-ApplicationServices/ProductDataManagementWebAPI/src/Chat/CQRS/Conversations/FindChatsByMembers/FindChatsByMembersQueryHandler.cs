using Business.Interfaces.Model;
using Chat.DTOs;
using Entities.Models;
using ChatModel = Entities.Models.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.FindChatsByMembers;

public sealed class FindChatsByMembersQueryHandler : IRequestHandler<FindChatsByMembersQuery, List<ChatWeb>>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly ICurrentUser currentUser;

    public FindChatsByMembersQueryHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatMember> chatMemberRepo,
        ICurrentUser currentUser)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.currentUser = currentUser;
    }

    public async Task<List<ChatWeb>> Handle(FindChatsByMembersQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<ChatMember> myMemberships = await chatMemberRepo.GetBySearch(
            cm => cm.UserId == currentUser.Id);

        List<Guid> candidateChatIds = myMemberships.Select(cm => cm.ChatId).ToList();

        if (candidateChatIds.Count == 0)
        {
            return new();
        }

        // Progressively narrow to chats where every requested member is also present
        foreach (Guid memberId in request.MemberUserIds.Distinct())
        {
            IEnumerable<ChatMember> theirMemberships = await chatMemberRepo.GetBySearch(
                cm => cm.UserId == memberId && candidateChatIds.Contains(cm.ChatId));

            candidateChatIds = theirMemberships.Select(cm => cm.ChatId).ToList();

            if (candidateChatIds.Count == 0)
            {
                return new();
            }
        }

        IEnumerable<ChatModel> chats = await chatRepo.GetBySearch(
            c => candidateChatIds.Contains(c.Id),
            include => include.Include(c => c.Members));

        return chats
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ChatWeb(
                Id: c.Id,
                Name: c.Name,
                IsGroupChat: c.IsGroupChat,
                ProjectId: c.ProjectId,
                TenantId: c.TenantId,
                CreatedAt: c.CreatedAt,
                CreatedByUserId: c.CreatedByUserId,
                UnreadCount: 0,
                LastMessage: null,
                Members: c.Members
                    .Select(m => new ChatMemberWeb(m.UserId, string.Empty, string.Empty, m.JoinedAt, m.IsAdmin, m.LastReadAt))
                    .ToList()))
            .ToList();
    }
}
