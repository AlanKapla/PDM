namespace Business.Interfaces.Services
{
    public interface IWorkScheduleAccessService
    {
        /// <summary>
        /// Ensures the current user is a tenant/project admin or the owner (creator) of the work schedule.
        /// Throws ForbiddenApiException otherwise.
        /// </summary>
        Task RequireAdminOrOwnerAsync(
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the current user is a tenant/project admin, the owner of the work schedule,
        /// or is assigned to the specified work item.
        /// Throws ForbiddenApiException otherwise.
        /// </summary>
        Task RequireAdminOwnerOrAssignedAsync(
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            Guid workScheduleStageWorkId,
            CancellationToken cancellationToken = default);
    }
}
