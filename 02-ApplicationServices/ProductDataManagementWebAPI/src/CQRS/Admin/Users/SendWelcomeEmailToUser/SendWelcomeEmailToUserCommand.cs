using Business.Interfaces.WebModels.Admin;
using CQRS;

namespace CQRS.Admin.Users.SendWelcomeEmailToUser
{
    public sealed record SendWelcomeEmailToUserCommand(Guid UserId) : IRequestCommand<AdminUserWeb>;
}
