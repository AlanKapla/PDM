namespace Business.Implementation.CacheKeys
{
    public static class CostEstimateCacheKeys
    {
        public static string CostEstimate(Guid tenantId, Guid projectId, Guid id)
            => $"ce:{tenantId}:{projectId}:{id}";

        public static string Template(Guid id)
            => $"ce-template:{id}";

        public static string Groups(Guid tenantId, Guid projectId, Guid costEstimateId)
            => $"ce-groups:{tenantId}:{projectId}:{costEstimateId}";

        public static string Items(Guid tenantId, Guid projectId, Guid costEstimateId)
            => $"ce-items:{tenantId}:{projectId}:{costEstimateId}";

        public static string GroupFieldValues(Guid tenantId, Guid projectId, Guid costEstimateId)
            => $"ce-group-fv:{tenantId}:{projectId}:{costEstimateId}";

        public static string ItemFieldValues(Guid tenantId, Guid projectId, Guid costEstimateId)
            => $"ce-item-fv:{tenantId}:{projectId}:{costEstimateId}";

        public static TimeSpan Ttl => TimeSpan.FromMinutes(30);
    }
}
