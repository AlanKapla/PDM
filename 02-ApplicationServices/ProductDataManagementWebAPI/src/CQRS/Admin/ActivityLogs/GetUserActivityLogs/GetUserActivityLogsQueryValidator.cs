using FluentValidation;

namespace CQRS.Admin.ActivityLogs.GetUserActivityLogs
{
    public sealed class GetUserActivityLogsQueryValidator
        : AbstractValidator<GetUserActivityLogsQuery>
    {
        public GetUserActivityLogsQueryValidator()
        {
            RuleFor(x => x.EventType)
                .IsInEnum()
                .When(x => x.EventType.HasValue);
        }
    }
}
