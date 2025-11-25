namespace Business.Interfaces.Exceptions
{
    public class ForbiddenApiException(string message) : ApiException(ApiExceptionReason.Forbidden, message)
    {
    }
}
