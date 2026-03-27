using FluentValidation;

namespace Chat.CQRS.Conversations.GetProjectMates;

public sealed class GetProjectMatesQueryValidator : AbstractValidator<GetProjectMatesQuery>
{
    public GetProjectMatesQueryValidator()
    {
    }
}
