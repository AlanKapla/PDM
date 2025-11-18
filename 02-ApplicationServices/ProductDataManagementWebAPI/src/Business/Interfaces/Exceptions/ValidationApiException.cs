namespace Business.Interfaces.Exceptions
{
    public class ValidationApiException(string message) : ApiException(ApiExceptionReason.ValidationError, message)
    {
    }
}