using FluentValidation;

namespace Chat.CQRS.Conversations.GetChatMembers;

public sealed class GetChatMembersQueryValidator : AbstractValidator<GetChatMembersQuery>
{
    public GetChatMembersQueryValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");
    }
}
