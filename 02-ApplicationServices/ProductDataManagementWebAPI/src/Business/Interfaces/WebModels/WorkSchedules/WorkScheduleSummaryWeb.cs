namespace Business.Interfaces.WebModels.WorkSchedules
{
    /// <summary>
    /// Summary view of a work schedule with basic information only
    /// </summary>
    public record WorkScheduleSummaryWeb(
        Guid Id,
        string Name,
        DateTime CreatedAt,
        Guid CreatedByUserId,
        string CreatedByUserName
    );
}
