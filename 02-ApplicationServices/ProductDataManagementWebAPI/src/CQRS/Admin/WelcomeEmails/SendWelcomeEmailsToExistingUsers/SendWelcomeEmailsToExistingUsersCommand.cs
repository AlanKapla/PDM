using Business.Interfaces.WebModels.Users;
using CQRS;

namespace CQRS.Admin.WelcomeEmails.SendWelcomeEmailsToExistingUsers
{
    public sealed record SendWelcomeEmailsToExistingUsersCommand : IRequestCommand<SendWelcomeEmailsResultWeb>;
}
