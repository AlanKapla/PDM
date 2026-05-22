using Business.Interfaces.Model;
using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.CreateGroupChat;

public sealed class CreateGroupChatCommandValidator : AbstractValidator<CreateGroupChatCommand>
{
    private const int MaxMemberIds = 50;

    public CreateGroupChatCommandValidator(ICurrentUser currentUser)
    {
        RuleFor(x => x.TenantId).RequiredId();

        RuleFor(x => x.ProjectId)
            .Must(id => id is null || id != Guid.Empty)
            .WithMessage("ProjectId, when provided, must not be empty.");

        RuleFor(x => x.MemberUserIds)
            .NotNull().WithMessage("MemberUserIds is required.")
            .Must(ids => ids.Count >= 2)
                .WithMessage("A group chat must have at least 2 additional members.")
            .Must(ids => ids.Count <= MaxMemberIds)
                .WithMessage($"MemberUserIds cannot contain more than {MaxMemberIds} entries.")
            .NotCurrentUser(currentUser)
            .UniqueIds();

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Chat name must not exceed 200 characters.")
            .When(x => x.Name is not null);
    }
}
