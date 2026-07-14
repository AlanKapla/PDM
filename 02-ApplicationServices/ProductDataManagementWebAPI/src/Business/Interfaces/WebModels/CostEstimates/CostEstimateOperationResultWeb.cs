namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// DTO for reordering groups within a cost estimate
    /// Supports moving groups between parents (subgroups)
    /// </summary>
    public sealed record ReorderGroupDto(
        Guid GroupId,
        Guid? ParentGroupId,
        int Order
    );

    /// <summary>
    /// DTO for reordering items within a group
    /// </summary>
    public sealed record ReorderItemDto(
        Guid ItemId,
        int Order
    );

    /// <summary>
    /// DTO for reordering child items (options or components) within a parent item
    /// </summary>
    public sealed record ReorderItemChildDto(
        Guid ItemId,
        int Order
    );
}
