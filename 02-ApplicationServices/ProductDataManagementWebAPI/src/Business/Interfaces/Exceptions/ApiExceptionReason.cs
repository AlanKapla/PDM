namespace Business.Interfaces.Exceptions
{
    public enum ApiExceptionReason
    {
        ValidationError,
        NotFound,
        Unauthorized,
        Forbidden,
        Conflict,
        InvalidOperation
    }
}