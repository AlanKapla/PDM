using Business.Interfaces.WebModels.Users;
using CQRS;

namespace CQRS.Users.SendWelcomeEmailsToExistingUsers
{
    public sealed record SendWelcomeEmailsToExistingUsersCommand : IRequestCommand<SendWelcomeEmailsResultWeb>;
}
