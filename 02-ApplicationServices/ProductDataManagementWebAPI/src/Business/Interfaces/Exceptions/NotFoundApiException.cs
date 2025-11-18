namespace Business.Interfaces.Exceptions
{
    public class NotFoundApiException(string objectType, string objectId, string? message = null)
        : ApiException(ApiExceptionReason.NotFound, message ?? $"{objectType} with ID '{objectId}' was not found.", objectType, objectId)
    {
    }
}