using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Interfaces.Exceptions
{
    public class UnauthorizedApiExeption : ApiException
    {
        public UnauthorizedApiExeption() : base(ApiExceptionReason.Unauthorized, null)
        {
        }
    }
}
