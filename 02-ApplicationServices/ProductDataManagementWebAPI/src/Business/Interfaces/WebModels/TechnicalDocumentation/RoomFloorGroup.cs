namespace Business.Interfaces.WebModels.TechnicalDocumentation;

public sealed class RoomFloorGroup
{
    public string Floor { get; set; } = string.Empty;
    public int FloorOrder { get; set; }
    public double? TotalAreaM2 { get; set; }
    public string? AreaNotes { get; set; }
    public List<RoomFloorItem> Items { get; set; } = new();
}

public sealed class RoomFloorItem
{
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double AreaM2 { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
}
