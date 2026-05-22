using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Notifications.MarkNotificationAsRead
{
    public sealed class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
    {
        public MarkNotificationAsReadCommandValidator()
        {
            RuleFor(x => x.NotificationId).RequiredId();
        }
    }
}
