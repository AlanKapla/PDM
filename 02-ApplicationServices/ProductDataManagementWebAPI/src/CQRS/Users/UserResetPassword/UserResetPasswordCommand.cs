using Business.Interfaces.WebModels.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Users.UserResetPassword
{
    public sealed record UserResetPasswordCommand(string Email, string Password) : IRequestCommand<UserResetPasswordWeb>
    {
    }
}
