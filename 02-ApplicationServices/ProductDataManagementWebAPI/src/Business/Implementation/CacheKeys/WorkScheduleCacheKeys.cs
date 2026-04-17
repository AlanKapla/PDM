namespace Business.Implementation.CacheKeys
{
    public static class WorkScheduleCacheKeys
    {
        private const string Prefix = "work-schedule";

        public static string Schedule(Guid id) => $"{Prefix}:{id}";
        public static string Work(Guid scheduleId, Guid workId) => $"{Prefix}:{scheduleId}:work:{workId}";
        public static string SchedulePattern(Guid scheduleId) => $"{Prefix}:{scheduleId}*";
        public static string WorkPattern(Guid scheduleId, Guid workId) => $"{Prefix}:{scheduleId}:work:{workId}*";

        public static TimeSpan Ttl => TimeSpan.FromMinutes(30);
    }
}
