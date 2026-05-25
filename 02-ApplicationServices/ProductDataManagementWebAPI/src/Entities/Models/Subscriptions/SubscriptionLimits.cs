namespace Entities.Models.Subscriptions
{
    public sealed record SubscriptionLimits
    {
        public int MaxProjects { get; init; }
        public int MaxUsers    { get; init; }

        /// <summary>True gdy limity wynikają z flagi FullAccess (nie z planu).</summary>
        public bool IsFullAccess { get; init; }

        public bool HasUnlimitedProjects => MaxProjects == SubscriptionConstants.Unlimited;
        public bool HasUnlimitedUsers    => MaxUsers    == SubscriptionConstants.Unlimited;

        public bool CanAddProject(int current) => HasUnlimitedProjects || current < MaxProjects;
        public bool CanAddUser(int current)    => HasUnlimitedUsers    || current < MaxUsers;

        public static SubscriptionLimits FullAccess() => new()
        {
            MaxProjects  = SubscriptionConstants.Unlimited,
            MaxUsers     = SubscriptionConstants.Unlimited,
            IsFullAccess = true
        };

        public static SubscriptionLimits FromSubscription(TenantSubscription subscription) => new()
        {
            MaxProjects  = subscription.MaxProjects,
            MaxUsers     = subscription.MaxUsers,
            IsFullAccess = false
        };
    }
}
