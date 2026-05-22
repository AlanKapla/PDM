using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.RemoveChatMember;

public sealed class RemoveChatMemberCommandValidator : AbstractValidator<RemoveChatMemberCommand>
{
    public RemoveChatMemberCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ChatId).RequiredId();

        RuleFor(x => x.UserId).RequiredId();
    }
}
