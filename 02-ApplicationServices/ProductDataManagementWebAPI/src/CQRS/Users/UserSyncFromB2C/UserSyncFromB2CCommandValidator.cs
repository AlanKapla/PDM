using FluentValidation;

namespace CQRS.Users.UserSyncFromB2C
{
    public class UserSyncFromB2CCommandValidator : AbstractValidator<UserSyncFromB2CCommand>
    {
        public UserSyncFromB2CCommandValidator()
        {
            // No validation needed - data is extracted from authenticated user's token claims
            // Authentication is required at controller level via [Authorize] attribute
        }
    }
}
