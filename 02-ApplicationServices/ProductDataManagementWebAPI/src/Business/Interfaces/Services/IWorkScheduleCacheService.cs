using Business.Interfaces.WebModels.WorkSchedules;

namespace Business.Interfaces.Services
{
    public interface IWorkScheduleCacheService
    {
        Task<WorkScheduleDetailsWeb?> GetOrBuildScheduleAsync(
            Guid workScheduleId,
            Func<Task<WorkScheduleDetailsWeb>> factory,
            CancellationToken ct = default);

        Task InvalidateScheduleAsync(Guid workScheduleId, CancellationToken ct = default);

        Task InvalidateWorkAsync(Guid workScheduleId, Guid workId, CancellationToken ct = default);
    }
}
