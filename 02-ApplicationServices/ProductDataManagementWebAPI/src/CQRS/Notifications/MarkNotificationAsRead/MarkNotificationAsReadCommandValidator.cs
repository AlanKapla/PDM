using FluentValidation;

namespace CQRS.Notifications.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
    {
        public MarkNotificationAsReadCommandValidator()
        {
            RuleFor(x => x.NotificationId)
                .NotEmpty()
                .WithMessage("NotificationId is required");
        }
    }
}
