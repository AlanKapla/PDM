using FluentValidation;

namespace CQRS.Admin.ColdMails.SendColdMails
{
    public sealed class SendColdMailsCommandValidator : AbstractValidator<SendColdMailsCommand>
    {
        public const int MaxEmailsPerRequest = 50;
        public const int MaxSubjectLength = 500;
        public const int MaxBodyLength = 100_000;
        public const int MaxEmailLength = 320;

        public SendColdMailsCommandValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty()
                .WithMessage("'Subject' is required.")
                .MaximumLength(MaxSubjectLength)
                .WithMessage($"'Subject' must not exceed {MaxSubjectLength} characters.");

            RuleFor(x => x.Body)
                .NotEmpty()
                .WithMessage("'Body' is required.")
                .MaximumLength(MaxBodyLength)
                .WithMessage($"'Body' must not exceed {MaxBodyLength} characters.");

            RuleFor(x => x.Emails)
                .NotEmpty()
                .WithMessage("'Emails' must contain at least one address.")
                .Must(emails => emails.Count <= MaxEmailsPerRequest)
                .WithMessage($"'Emails' must not contain more than {MaxEmailsPerRequest} addresses.");

            RuleForEach(x => x.Emails)
                .NotEmpty()
                .WithMessage("Email is required.")
                .MaximumLength(MaxEmailLength)
                .WithMessage($"Email cannot exceed {MaxEmailLength} characters.")
                .EmailAddress()
                .WithMessage("Invalid email format.");
        }
    }
}
