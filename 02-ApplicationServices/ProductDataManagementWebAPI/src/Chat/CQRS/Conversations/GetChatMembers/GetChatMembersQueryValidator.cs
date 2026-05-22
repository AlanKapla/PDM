using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.GetChatMembers;

public sealed class GetChatMembersQueryValidator : AbstractValidator<GetChatMembersQuery>
{
    public GetChatMembersQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ChatId).RequiredId();
    }
}
