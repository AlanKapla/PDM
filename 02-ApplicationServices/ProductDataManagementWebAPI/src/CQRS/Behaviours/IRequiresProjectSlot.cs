namespace CQRS.Behaviours
{
    /// <summary>
    /// Marker interface dla komend zużywających slot projektu w tenancie.
    /// SubscriptionLimitsBehavior sprawdzi CanAddProject przed wykonaniem handlera.
    /// </summary>
    public interface IRequiresProjectSlot
    {
        Guid TenantId { get; }
    }

    /// <summary>
    /// Marker interface dla komend zużywających slot użytkownika w tenancie.
    /// SubscriptionLimitsBehavior sprawdzi CanAddUser przed wykonaniem handlera.
    /// </summary>
    public interface IRequiresUserSlot
    {
        Guid TenantId { get; }
    }
}
