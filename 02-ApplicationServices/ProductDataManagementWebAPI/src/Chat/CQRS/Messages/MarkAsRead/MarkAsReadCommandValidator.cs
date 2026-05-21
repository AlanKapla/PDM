using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Messages.MarkAsRead;

public sealed class MarkAsReadCommandValidator : AbstractValidator<MarkAsReadCommand>
{
    public MarkAsReadCommandValidator()
    {
        RuleFor(x => x.ChatId).RequiredId();
    }
}
