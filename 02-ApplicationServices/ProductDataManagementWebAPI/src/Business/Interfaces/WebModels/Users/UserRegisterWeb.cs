using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Interfaces.WebModels.Users
{
    public sealed record UserRegisterWeb(Guid Id, string Email)
    {
    }
}
