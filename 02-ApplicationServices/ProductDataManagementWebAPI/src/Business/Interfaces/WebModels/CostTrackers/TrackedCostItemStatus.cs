namespace Business.Interfaces.WebModels.CostTrackers
{
    public enum TrackedCostItemStatus
    {
        NoCosts = 0,
        NoBudget = 1,
        InProgress = 2,
        NearLimit = 3,
        OverBudget = 4
    }
}
