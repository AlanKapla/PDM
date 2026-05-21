using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Messages.GetChatMessages;

public sealed class GetChatMessagesQueryValidator : AbstractValidator<GetChatMessagesQuery>
{
    public GetChatMessagesQueryValidator()
    {
        RuleFor(x => x.ChatId).RequiredId();

        RuleFor(x => x.PageSize).PageSize(100);
    }
}
