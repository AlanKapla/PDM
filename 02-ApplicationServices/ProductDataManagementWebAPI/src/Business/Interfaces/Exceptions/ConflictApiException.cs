namespace Business.Interfaces.Exceptions
{
    public class ConflictApiException(string objectType, string objectId, string? message = null)
        : ApiException(ApiExceptionReason.Conflict, message ?? $"{objectType} with ID '{objectId}' already exists.", objectType, objectId)
    {
    }
}