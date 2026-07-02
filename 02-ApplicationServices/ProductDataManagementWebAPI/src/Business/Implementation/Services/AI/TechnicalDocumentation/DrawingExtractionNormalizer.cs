using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class DrawingExtractionNormalizer
{
    public static void Normalize(FloorPlanDrawing drawing, DrawingClassification classification)
    {
        NormalizeRooms(drawing, classification);
        NormalizeStructuralElements(drawing);
        NormalizeFoundations(drawing);
        NormalizeFloors(drawing, classification);
        NormalizeRoof(drawing, classification);
        NormalizeSite(drawing);
    }

    private static void NormalizeRooms(FloorPlanDrawing drawing, DrawingClassification classification)
    {
        CorrectRoomNumbersFromTable(drawing, classification);

        foreach (Room room in drawing.Rooms)
        {
            if (string.IsNullOrWhiteSpace(room.Number) && !string.IsNullOrWhiteSpace(room.Symbol))
            {
                room.Number = room.Symbol.Trim();
            }

            if (string.IsNullOrWhiteSpace(room.Symbol) && !string.IsNullOrWhiteSpace(room.Number))
            {
                room.Symbol = room.Number.Trim();
            }

            if (string.IsNullOrWhiteSpace(room.Category))
            {
                room.Category = RoomCategoryInferrer.Infer(room.Name);
            }
        }

        if (string.IsNullOrWhiteSpace(drawing.AreaNotes))
        {
            drawing.AreaNotes = DrawingFloorNotesParser.TryParseAreaNotes(classification.DrawingTable)
                ?? DrawingFloorNotesParser.TryParseAreaNotes(classification.TableContent);
        }

        SupplementRoomsFromDrawingTable(drawing, classification);
    }

    private static void CorrectRoomNumbersFromTable(
        FloorPlanDrawing drawing,
        DrawingClassification classification)
    {
        string? table = !string.IsNullOrWhiteSpace(classification.DrawingTable)
            ? classification.DrawingTable
            : classification.TableContent;

        if (string.IsNullOrWhiteSpace(table))
        {
            return;
        }

        List<Room> tableRooms = DrawingRoomTableParser.Parse(table);
        if (tableRooms.Count == 0)
        {
            return;
        }

        foreach (Room room in drawing.Rooms)
        {
            Room? tableMatch = tableRooms.FirstOrDefault(candidate =>
                string.Equals(candidate.Name.Trim(), room.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (tableMatch is null)
            {
                continue;
            }

            room.Number = tableMatch.Number;
            room.Symbol = tableMatch.Symbol;
        }
    }

    private static void NormalizeStructuralElements(FloorPlanDrawing drawing)
    {
        foreach (StructuralBeam beam in drawing.Beams)
        {
            if (beam.BCm is null && beam.BwCm is not null)
            {
                beam.BCm = beam.BwCm;
            }
        }

        foreach (StructuralLintel lintel in drawing.Lintels)
        {
            if (lintel.BCm is null && lintel.BwCm is not null)
            {
                lintel.BCm = lintel.BwCm;
            }
        }

        foreach (StructuralColumn column in drawing.Columns)
        {
            if (string.IsNullOrWhiteSpace(column.LongitudinalBars)
                && !string.IsNullOrWhiteSpace(column.Stirrups))
            {
                column.LongitudinalBars = column.Stirrups;
            }
        }
    }

    private static void NormalizeFoundations(FloorPlanDrawing drawing)
    {
        if (drawing.Foundations is null)
        {
            return;
        }

        FoundationSection foundations = drawing.Foundations;

        foreach (FootingDetail footing in foundations.Footings)
        {
            if (footing.Segments.Count > 0)
            {
                double segmentSum = footing.Segments.Sum(segment => segment.LengthM);
                if (segmentSum > 0)
                {
                    footing.LengthM = Math.Round(segmentSum, 2);
                }
            }

            if (footing.LengthM <= 0 && footing.WidthM > 0)
            {
                footing.LengthM = footing.WidthM;
            }
        }
    }

    private static void NormalizeFloors(FloorPlanDrawing drawing, DrawingClassification classification)
    {
        if (drawing.Floors is null)
        {
            return;
        }

        FloorSection floors = drawing.Floors;

        if (floors.TotalMassKg is null && floors.Bars.Count > 0)
        {
            double sum = floors.Bars.Sum(bar => bar.MassKg ?? 0);
            if (sum > 0)
            {
                floors.TotalMassKg = Math.Round(sum, 2);
            }
        }

        if (floors.TotalMassKg is null && floors.Steel.Count > 0)
        {
            double sum = floors.Steel.Sum(item => item.Quantity);
            if (sum > 0)
            {
                floors.TotalMassKg = Math.Round(sum, 2);
            }
        }

        if (floors.TotalMassKg is null)
        {
            floors.TotalMassKg = DrawingTableMassParser.TryParseTotalMassKg(classification.DrawingTable)
                ?? DrawingTableMassParser.TryParseTotalMassKg(classification.TableContent)
                ?? DrawingTableMassParser.TryParseTotalMassKg(classification.DescriptiveText);
        }

        if (floors.Steel.Count == 0 && floors.TotalMassKg is > 0)
        {
            floors.Steel.Add(new MaterialQuantity
            {
                MaterialType = "stal zbrojeniowa",
                Quantity = floors.TotalMassKg.Value,
                Unit = "kg"
            });
        }

        if (floors.Concrete.Count == 0)
        {
            foreach (SlabDetail slab in floors.Slabs.Where(item => !string.IsNullOrWhiteSpace(item.ConcreteClass)))
            {
                floors.Concrete.Add(new MaterialQuantity
                {
                    MaterialType = slab.ConcreteClass!,
                    Quantity = slab.ThicknessCm > 0 ? slab.ThicknessCm : 18,
                    Unit = "cm"
                });
            }
        }

        foreach (RebarBar bar in floors.Bars)
        {
            if (bar.TotalLengthM is null && bar.LengthM is not null && bar.Count is not null)
            {
                bar.TotalLengthM = Math.Round(bar.LengthM.Value * bar.Count.Value, 2);
            }
        }
    }

    private static void SupplementRoomsFromDrawingTable(
        FloorPlanDrawing drawing,
        DrawingClassification classification)
    {
        string? table = !string.IsNullOrWhiteSpace(classification.DrawingTable)
            ? classification.DrawingTable
            : classification.TableContent;

        if (string.IsNullOrWhiteSpace(table))
        {
            return;
        }

        List<Room> parsedRooms = DrawingRoomTableParser.Parse(table);
        if (parsedRooms.Count == 0)
        {
            return;
        }

        HashSet<string> existingKeys = drawing.Rooms
            .Select(room => BuildRoomKey(room))
            .Where(key => key.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (Room parsedRoom in parsedRooms)
        {
            string key = BuildRoomKey(parsedRoom);
            if (key.Length == 0 || existingKeys.Contains(key))
            {
                continue;
            }

            drawing.Rooms.Add(parsedRoom);
            existingKeys.Add(key);
        }
    }

    private static string BuildRoomKey(Room room)
    {
        if (!string.IsNullOrWhiteSpace(room.Symbol))
        {
            return room.Symbol.Trim();
        }

        if (!string.IsNullOrWhiteSpace(room.Number))
        {
            return room.Number.Trim();
        }

        return room.Name.Trim();
    }

    private static void NormalizeRoof(FloorPlanDrawing drawing, DrawingClassification classification)
    {
        if (drawing.Roof is null)
        {
            return;
        }

        RoofSection roof = drawing.Roof;

        if (roof.TimberGroups.Count > 0 && roof.Timber.Count == 0)
        {
            FlattenTimberGroups(roof);
        }

        if (roof.TotalVolumeM3 is null && roof.TimberGroups.Count > 0)
        {
            double sum = roof.TimberGroups.Sum(group => group.GroupVolumeM3 ?? 0);
            if (sum > 0)
            {
                roof.TotalVolumeM3 = Math.Round(sum, 3);
            }
        }

        if (roof.TotalVolumeM3 is null)
        {
            roof.TotalVolumeM3 = DrawingTableMassParser.TryParseTimberTotalVolumeM3(classification.DrawingTable)
                ?? DrawingTableMassParser.TryParseTimberTotalVolumeM3(classification.TableContent);
        }

        foreach (TimberElement timber in roof.Timber)
        {
            if (timber.VolumeM3 is null or <= 0)
            {
                double volume = MaterialUnitNormalizer.ResolveTimberVolumeM3(timber);
                if (volume > 0)
                {
                    timber.VolumeM3 = volume;
                }
            }
        }
    }

    private static void FlattenTimberGroups(RoofSection roof)
    {
        foreach (TimberGroup group in roof.TimberGroups)
        {
            if (group.Rows.Count == 0)
            {
                roof.Timber.Add(new TimberElement
                {
                    Element = group.Name,
                    Section = group.Section,
                    Count = 1,
                    LengthM = group.GroupSumMb ?? 0,
                    RowSumMb = group.GroupSumMb,
                    VolumeM3 = group.GroupVolumeM3,
                    WoodType = roof.WoodClass
                });
                continue;
            }

            foreach (TimberGroupRow row in group.Rows)
            {
                roof.Timber.Add(new TimberElement
                {
                    Element = group.Name,
                    Section = group.Section,
                    Count = row.Count,
                    LengthM = row.LengthM,
                    RowSumMb = row.RowSumMb,
                    WoodType = roof.WoodClass
                });
            }
        }
    }

    private static void NormalizeSite(FloorPlanDrawing drawing)
    {
        if (drawing.Site is null)
        {
            return;
        }

        if (drawing.TotalAreaM2 is null && drawing.Site.BuildingFootprintM2 is > 0)
        {
            drawing.TotalAreaM2 = drawing.Site.BuildingFootprintM2;
        }
    }
}
