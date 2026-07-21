using CQRS.Activity.RecordDemoActivity;
using CQRS.Activity.RecordLoginActivity;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Activity;

public sealed class RecordLoginActivityCommandValidatorTests
{
    private readonly RecordLoginActivityCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenRouteTooLong_HasValidationError()
    {
        RecordLoginActivityCommand command = ValidCommand() with
        {
            Route = new string('a', RecordLoginActivityCommandValidator.MaxRouteLength + 1)
        };

        TestValidationResult<RecordLoginActivityCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Route);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        TestValidationResult<RecordLoginActivityCommand> result =
            _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static RecordLoginActivityCommand ValidCommand() => new()
    {
        IpAddress = "1.2.3.4",
        Route = "/home"
    };
}

public sealed class RecordDemoActivityCommandValidatorTests
{
    private readonly RecordDemoActivityCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenRouteTooLong_HasValidationError()
    {
        RecordDemoActivityCommand command = ValidCommand() with
        {
            Route = new string('b', RecordDemoActivityCommandValidator.MaxRouteLength + 1)
        };

        TestValidationResult<RecordDemoActivityCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Route);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        TestValidationResult<RecordDemoActivityCommand> result =
            _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static RecordDemoActivityCommand ValidCommand() => new()
    {
        IpAddress = "5.6.7.8",
        Route = "/demo"
    };
}
