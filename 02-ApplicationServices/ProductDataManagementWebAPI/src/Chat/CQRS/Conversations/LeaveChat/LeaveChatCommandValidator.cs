using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.LeaveChat;

public sealed class LeaveChatCommandValidator : AbstractValidator<LeaveChatCommand>
{
    public LeaveChatCommandValidator()
    {
        RuleFor(x => x.ChatId).RequiredId();
    }
}
