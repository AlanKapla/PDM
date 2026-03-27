using FluentValidation;

namespace Chat.CQRS.Conversations.GetUserChats;

public sealed class GetUserChatsQueryValidator : AbstractValidator<GetUserChatsQuery>
{
    public GetUserChatsQueryValidator()
    {
    }
}
