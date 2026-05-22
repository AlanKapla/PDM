using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.AddChatMember;

public sealed class AddChatMemberCommandValidator : AbstractValidator<AddChatMemberCommand>
{
    public AddChatMemberCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ChatId).RequiredId();

        RuleFor(x => x.UserId).RequiredId();
    }
}
