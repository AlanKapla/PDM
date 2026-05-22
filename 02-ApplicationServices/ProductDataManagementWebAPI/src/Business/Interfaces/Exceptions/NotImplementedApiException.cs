using System.Net;

namespace Business.Interfaces.Exceptions
{
    public class NotImplementedApiException(string message)
        : ApiException(ApiExceptionReason.InvalidOperation, message)
    {
        public override HttpStatusCode GetStatusCode() => HttpStatusCode.NotImplemented;
    }
}
