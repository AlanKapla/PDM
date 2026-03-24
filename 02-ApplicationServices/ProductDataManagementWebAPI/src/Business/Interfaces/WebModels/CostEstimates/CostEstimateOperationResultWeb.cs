namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// DTO for reordering groups within a cost estimate
    /// Supports moving groups between parents (subgroups)
    /// </summary>
    public record ReorderGroupDto(
        Guid GroupId,
        Guid? ParentGroupId,
        int Order
    );

    /// <summary>
    /// DTO for reordering items within a group
    /// </summary>
    public record ReorderItemDto(
        Guid ItemId,
        int Order
    );
}
