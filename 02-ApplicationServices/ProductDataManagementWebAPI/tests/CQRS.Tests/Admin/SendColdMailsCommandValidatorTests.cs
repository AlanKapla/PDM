using CQRS.Admin.ColdMails.SendColdMails;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Admin;

public sealed class SendColdMailsCommandValidatorTests
{
    private readonly SendColdMailsCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenSubjectIsEmpty_HasValidationError()
    {
        SendColdMailsCommand command = ValidCommand() with { Subject = string.Empty };

        TestValidationResult<SendColdMailsCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Subject);
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_HasValidationError()
    {
        SendColdMailsCommand command = ValidCommand() with
        {
            Emails = new[] { "not-an-email" }
        };

        TestValidationResult<SendColdMailsCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Emails[0]");
    }

    [Fact]
    public void Validate_WhenMoreThan50Emails_HasValidationError()
    {
        List<string> emails = Enumerable.Range(1, 51)
            .Select(i => $"user{i}@example.com")
            .ToList();

        SendColdMailsCommand command = ValidCommand() with { Emails = emails };

        TestValidationResult<SendColdMailsCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Emails);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        SendColdMailsCommand command = ValidCommand();

        TestValidationResult<SendColdMailsCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static SendColdMailsCommand ValidCommand() => new()
    {
        Emails = new[] { "prospect@example.com" },
        Subject = "Hello",
        Body = "Cold mail body"
    };
}
