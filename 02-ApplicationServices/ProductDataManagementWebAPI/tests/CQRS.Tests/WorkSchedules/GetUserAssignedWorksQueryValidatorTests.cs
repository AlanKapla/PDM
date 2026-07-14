using CQRS.WorkSchedules.GetUserAssignedWorks;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetUserAssignedWorksQueryValidatorTests
{
    private readonly GetUserAssignedWorksQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        GetUserAssignedWorksQuery query = new();

        TestValidationResult<GetUserAssignedWorksQuery> result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
