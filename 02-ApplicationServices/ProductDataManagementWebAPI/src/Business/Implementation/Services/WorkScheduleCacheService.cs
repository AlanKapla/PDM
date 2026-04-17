using Business.Implementation.CacheKeys;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services
{
    public sealed class WorkScheduleCacheService : IWorkScheduleCacheService
    {
        private readonly ICacheService cache;
        private readonly ILogger<WorkScheduleCacheService> logger;

        public WorkScheduleCacheService(ICacheService cache, ILogger<WorkScheduleCacheService> logger)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public Task<WorkScheduleDetailsWeb?> GetOrBuildScheduleAsync(
            Guid workScheduleId,
            Func<Task<WorkScheduleDetailsWeb>> factory,
            CancellationToken ct = default)
        {
            string key = WorkScheduleCacheKeys.Schedule(workScheduleId);
            return cache.GetOrAddAsync(key, factory, WorkScheduleCacheKeys.Ttl, ct);
        }

        public async Task InvalidateScheduleAsync(Guid workScheduleId, CancellationToken ct = default)
        {
            string pattern = WorkScheduleCacheKeys.SchedulePattern(workScheduleId);
            logger.LogDebug("Invalidating all cache for schedule {WorkScheduleId}", workScheduleId);
            await cache.RemoveCacheContainsAsync(pattern, ct);
        }

        public async Task InvalidateWorkAsync(Guid workScheduleId, Guid workId, CancellationToken ct = default)
        {
            await Task.WhenAll(
                cache.RemoveCacheByKeyAsync(WorkScheduleCacheKeys.Schedule(workScheduleId), ct),
                cache.RemoveCacheContainsAsync(WorkScheduleCacheKeys.WorkPattern(workScheduleId, workId), ct));
        }
    }
}
