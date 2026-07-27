namespace Business.Interfaces.WebModels.WorkSchedules
{
    public sealed record WorkScheduleAssignableAssigneesWeb(
        IReadOnlyList<WorkScheduleAssignableMemberWeb> Members,
        IReadOnlyList<WorkScheduleAssignableContractorWeb> Contractors);

    public sealed record WorkScheduleAssignableMemberWeb(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        string? CompanyName,
        IReadOnlyList<WorkScheduleAssigneeBusyPeriodWeb> Assignments);

    public sealed record WorkScheduleAssignableContractorWeb(
        Guid Id,
        string Name,
        IReadOnlyList<WorkScheduleAssigneeBusyPeriodWeb> Assignments);

    /// <summary>
    /// Okres, w którym osoba/kontrahent jest już przypisany do innej pracy (tenant-wide).
    /// </summary>
    public sealed record WorkScheduleAssigneeBusyPeriodWeb(
        Guid WorkId,
        string WorkName,
        Guid WorkScheduleId,
        string WorkScheduleName,
        Guid ProjectId,
        string ProjectName,
        DateTime StartDate,
        DateTime EndDate);
}
