using System.Net;

namespace Business.Interfaces.Exceptions
{
    public class ApiException(ApiExceptionReason reason, string? message, string? objectType = null, string? objectId = null) : Exception(message)
    {
        public ApiExceptionReason Reason { get; } = reason;
        public string? ObjectType { get; } = objectType;
        public string? ObjectId { get; } = objectId;

        public virtual HttpStatusCode GetStatusCode() =>
            Reason switch
            {
                ApiExceptionReason.ValidationError => HttpStatusCode.BadRequest,
                ApiExceptionReason.NotFound => HttpStatusCode.NotFound,
                ApiExceptionReason.Unauthorized => HttpStatusCode.Unauthorized,
                ApiExceptionReason.Forbidden => HttpStatusCode.Forbidden,
                ApiExceptionReason.Conflict => HttpStatusCode.Conflict,
                ApiExceptionReason.SubscriptionSuspended => HttpStatusCode.PaymentRequired,
                _ => HttpStatusCode.InternalServerError
            };
    }
}