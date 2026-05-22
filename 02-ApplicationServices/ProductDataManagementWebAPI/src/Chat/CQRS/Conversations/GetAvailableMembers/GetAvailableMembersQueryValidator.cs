using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.GetAvailableMembers;

public sealed class GetAvailableMembersQueryValidator : AbstractValidator<GetAvailableMembersQuery>
{
    public GetAvailableMembersQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ChatId).RequiredId();
    }
}
