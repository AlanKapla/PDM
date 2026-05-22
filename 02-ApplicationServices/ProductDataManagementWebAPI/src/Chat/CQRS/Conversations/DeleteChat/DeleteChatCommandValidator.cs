using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.DeleteChat;

public sealed class DeleteChatCommandValidator : AbstractValidator<DeleteChatCommand>
{
    public DeleteChatCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ChatId).RequiredId();
    }
}
