using Business.Interfaces.Model;
using FluentValidation;

namespace Chat.CQRS.Conversations.CreateChat;

public sealed class CreateChatCommandValidator : AbstractValidator<CreateChatCommand>
{
    public CreateChatCommandValidator(ICurrentUser currentUser)
    {
        RuleFor(x => x.MemberUserIds)
            .NotNull().WithMessage("MemberUserIds is required.")
            .Must(ids => ids.Count >= 1).WithMessage("At least one member must be specified.")
            .Must(ids => !ids.Contains(currentUser.Id)).WithMessage("MemberUserIds must not contain the current user.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required for group chats.")
            .When(x => x.MemberUserIds != null && x.MemberUserIds.Count >= 2);

        RuleFor(x => x.Name)
            .Null().WithMessage("Name must not be set for direct chats.")
            .When(x => x.MemberUserIds != null && x.MemberUserIds.Count == 1);

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Chat name must not exceed 200 characters.")
            .When(x => x.Name != null && (x.MemberUserIds == null || x.MemberUserIds.Count != 1));
    }
}
