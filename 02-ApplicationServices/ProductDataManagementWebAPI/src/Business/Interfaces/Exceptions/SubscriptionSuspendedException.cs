namespace Business.Interfaces.Exceptions
{
    public class SubscriptionSuspendedException(Guid tenantId)
        : ApiException(
            ApiExceptionReason.SubscriptionSuspended,
            "Tenant subscription is suspended. Only an admin can access this tenant to renew the subscription.",
            nameof(tenantId),
            tenantId.ToString())
    {
    }
}
