using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Validation;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class FloorPlanDrawingMerger
{
  public static FloorPlanDrawing Merge(FloorPlanDrawing agentA, FloorPlanDrawing agentB)
  {
    FloorPlanDrawing merged = new()
    {
      Rooms = MergeRooms(agentA.Rooms, agentB.Rooms),
      Walls = MergeWalls(agentA.Walls, agentB.Walls),
      Openings = MergeOpenings(agentA.Openings, agentB.Openings),
      Foundations = MergeFoundations(agentA.Foundations, agentB.Foundations),
      Floors = MergeFloors(agentA.Floors, agentB.Floors),
      Roof = MergeRoof(agentA.Roof, agentB.Roof),
      Installations = MergeInstallations(agentA.Installations, agentB.Installations),
      TextSources = MergeTextSources(agentA.TextSources, agentB.TextSources),
      CrossReferences = MergeCrossReferences(agentA.CrossReferences, agentB.CrossReferences),
      DeferredDetails = MergeDeferredDetails(agentA.DeferredDetails, agentB.DeferredDetails),
      AreaNotes = string.IsNullOrWhiteSpace(agentA.AreaNotes) ? agentB.AreaNotes : agentA.AreaNotes,
      InteriorDoors = MergeInteriorDoors(agentA.InteriorDoors, agentB.InteriorDoors),
      Section = MergeSection(agentA.Section, agentB.Section),
      Elevation = agentA.Elevation ?? agentB.Elevation,
      Site = agentA.Site ?? agentB.Site,
      Columns = agentA.Columns.Count >= agentB.Columns.Count ? agentA.Columns : agentB.Columns,
      Details = agentA.Details.Count >= agentB.Details.Count ? agentA.Details : agentB.Details,
      ValidationReport = TechnicalDocumentationValidationReportBuilder.Build(agentA, agentB)
    };

    return merged;
  }

  private static List<Room> MergeRooms(List<Room> roomsA, List<Room> roomsB)
  {
    Dictionary<string, Room> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (Room room in roomsA)
    {
      string key = BuildRoomKey(room);
      if (!string.IsNullOrEmpty(key))
      {
        merged[key] = room;
      }
    }

    foreach (Room room in roomsB)
    {
      string key = BuildRoomKey(room);
      if (string.IsNullOrEmpty(key))
      {
        continue;
      }

      if (!merged.TryGetValue(key, out Room? existing))
      {
        merged[key] = room;
        continue;
      }

      if (GetRoomArea(room) > GetRoomArea(existing))
      {
        merged[key] = room;
      }
    }

    return merged.Values.ToList();
  }

  private static List<Wall> MergeWalls(List<Wall> wallsA, List<Wall> wallsB)
  {
    Dictionary<string, Wall> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (Wall wall in wallsA.Concat(wallsB))
    {
      string key = BuildWallKey(wall);
      if (string.IsNullOrEmpty(key))
      {
        continue;
      }

      if (!merged.TryGetValue(key, out Wall? existing))
      {
        merged[key] = wall;
        continue;
      }

      if (ResolveWallAreaM2(wall) > ResolveWallAreaM2(existing) || wall.LengthM > existing.LengthM)
      {
        merged[key] = wall;
      }
    }

    return merged.Values.ToList();
  }

  private static List<Opening> MergeOpenings(List<Opening> openingsA, List<Opening> openingsB)
  {
    Dictionary<string, Opening> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (Opening opening in openingsA.Concat(openingsB))
    {
      string key = BuildOpeningKey(opening);
      if (string.IsNullOrEmpty(key))
      {
        continue;
      }

      if (!merged.TryGetValue(key, out Opening? existing))
      {
        merged[key] = opening;
        continue;
      }

      existing.Count = Math.Max(existing.Count, opening.Count);
    }

    return merged.Values.ToList();
  }

  private static FoundationSection? MergeFoundations(FoundationSection? a, FoundationSection? b)
  {
    if (a is null && b is null)
    {
      return null;
    }

    FoundationSection merged = new()
    {
      Footings = MergeFootings(a?.Footings ?? [], b?.Footings ?? []),
      Blocks = MergeMaterialQuantities(a?.Blocks ?? [], b?.Blocks ?? []),
      Concrete = MergeMaterialQuantities(a?.Concrete ?? [], b?.Concrete ?? []),
      Steel = MergeMaterialQuantities(a?.Steel ?? [], b?.Steel ?? []),
      Insulation = MergeMaterialQuantities(a?.Insulation ?? [], b?.Insulation ?? [])
    };

    if (merged.Footings.Count == 0
        && merged.Blocks.Count == 0
        && merged.Concrete.Count == 0
        && merged.Steel.Count == 0
        && merged.Insulation.Count == 0)
    {
      return null;
    }

    return merged;
  }

  private static FloorSection? MergeFloors(FloorSection? a, FloorSection? b)
  {
    if (a is null && b is null)
    {
      return null;
    }

    FloorSection merged = new()
    {
      Slabs = MergeSlabs(a?.Slabs ?? [], b?.Slabs ?? []),
      Concrete = MergeMaterialQuantities(a?.Concrete ?? [], b?.Concrete ?? []),
      Steel = MergeMaterialQuantities(a?.Steel ?? [], b?.Steel ?? [])
    };

    if (merged.Slabs.Count == 0 && merged.Concrete.Count == 0 && merged.Steel.Count == 0)
    {
      return null;
    }

    return merged;
  }

  private static RoofSection? MergeRoof(RoofSection? a, RoofSection? b)
  {
    if (a is null)
    {
      return b;
    }

    if (b is null)
    {
      return a;
    }

    RoofSection primary = a.AreaM2 >= b.AreaM2 ? a : b;
    RoofSection secondary = ReferenceEquals(primary, a) ? b : a;

    return new RoofSection
    {
      AreaM2 = Math.Max(a.AreaM2, b.AreaM2),
      PitchDegrees = primary.PitchDegrees > 0 ? primary.PitchDegrees : secondary.PitchDegrees,
      CoveringType = !string.IsNullOrWhiteSpace(primary.CoveringType)
          ? primary.CoveringType
          : secondary.CoveringType,
      Timber = MergeTimberElements(a.Timber, b.Timber)
    };
  }

  private static List<TimberElement> MergeTimberElements(List<TimberElement> timberA, List<TimberElement> timberB)
  {
    Dictionary<string, TimberElement> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (TimberElement timber in timberA.Concat(timberB))
    {
      string key = $"{timber.Element}:{timber.Section}:{timber.WoodType}".ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(timber.Element))
      {
        continue;
      }

      if (!merged.TryGetValue(key, out TimberElement? existing))
      {
        merged[key] = timber;
        continue;
      }

      if (timber.Count > existing.Count)
      {
        existing.Count = timber.Count;
      }

      if (timber.LengthM > existing.LengthM)
      {
        existing.LengthM = timber.LengthM;
      }
    }

    return merged.Values.ToList();
  }

  private static List<DrawingInstallation> MergeInstallations(
    List<DrawingInstallation> installationsA,
    List<DrawingInstallation> installationsB)
  {
    Dictionary<string, DrawingInstallation> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (DrawingInstallation installation in installationsA.Concat(installationsB))
    {
      string key = installation.Type.Trim().ToLowerInvariant();
      if (string.IsNullOrEmpty(key))
      {
        continue;
      }

      if (!merged.TryGetValue(key, out DrawingInstallation? existing))
      {
        merged[key] = installation;
        continue;
      }

      existing.IsPresent = existing.IsPresent || installation.IsPresent;
      if (string.IsNullOrWhiteSpace(existing.Notes) && !string.IsNullOrWhiteSpace(installation.Notes))
      {
        existing.Notes = installation.Notes;
      }
    }

    return merged.Values.ToList();
  }

  private static string BuildRoomKey(Room room)
  {
    if (!string.IsNullOrWhiteSpace(room.Name))
    {
      return TechnicalDocumentationDomainRules.NormalizeRoomName(room.Name);
    }

    if (!string.IsNullOrWhiteSpace(room.Symbol))
    {
      return room.Symbol.Trim().ToLowerInvariant();
    }

    return string.Empty;
  }

  private static double GetRoomArea(Room room)
  {
    if (room.AreaM2 > 0)
    {
      return room.AreaM2;
    }

    if (room.WidthM > 0 && room.LengthM > 0)
    {
      return Math.Round(room.WidthM * room.LengthM, 1);
    }

    return 0;
  }

  private static double ResolveWallAreaM2(Wall wall)
  {
    if (wall.NetAreaM2.HasValue && wall.NetAreaM2.Value > 0)
    {
      return wall.NetAreaM2.Value;
    }

    if (wall.GrossAreaM2.HasValue && wall.GrossAreaM2.Value > 0)
    {
      return wall.GrossAreaM2.Value;
    }

    return 0;
  }

  private static DrawingTextSources? MergeTextSources(DrawingTextSources? a, DrawingTextSources? b)
  {
    if (a is null && b is null)
    {
      return null;
    }

    DrawingTextSources merged = new();

    if (!string.IsNullOrWhiteSpace(a?.DescriptiveText))
    {
      merged.DescriptiveText = a.DescriptiveText;
    }
    else if (!string.IsNullOrWhiteSpace(b?.DescriptiveText))
    {
      merged.DescriptiveText = b.DescriptiveText;
    }

    if (!string.IsNullOrWhiteSpace(a?.DrawingTable))
    {
      merged.DrawingTable = a.DrawingTable;
    }
    else if (!string.IsNullOrWhiteSpace(b?.DrawingTable))
    {
      merged.DrawingTable = b.DrawingTable;
    }

    if (string.IsNullOrWhiteSpace(merged.DescriptiveText) && string.IsNullOrWhiteSpace(merged.DrawingTable))
    {
      return null;
    }

    return merged;
  }

  private static List<DrawingCrossReference> MergeCrossReferences(
    List<DrawingCrossReference> referencesA,
    List<DrawingCrossReference> referencesB)
  {
    Dictionary<string, DrawingCrossReference> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (DrawingCrossReference reference in referencesA.Concat(referencesB))
    {
      string key = $"{reference.ReferenceLabel}:{reference.DetailType}".ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(reference.ReferenceLabel))
      {
        continue;
      }

      if (!merged.TryGetValue(key, out DrawingCrossReference? existing))
      {
        merged[key] = reference;
        continue;
      }

      if (string.IsNullOrWhiteSpace(existing.TargetSheetNumber) && !string.IsNullOrWhiteSpace(reference.TargetSheetNumber))
      {
        existing.TargetSheetNumber = reference.TargetSheetNumber;
      }

      if (string.IsNullOrWhiteSpace(existing.TargetTitle) && !string.IsNullOrWhiteSpace(reference.TargetTitle))
      {
        existing.TargetTitle = reference.TargetTitle;
      }

      if (string.IsNullOrWhiteSpace(existing.Notes) && !string.IsNullOrWhiteSpace(reference.Notes))
      {
        existing.Notes = reference.Notes;
      }
    }

    return merged.Values.ToList();
  }

  private static List<DeferredDetailNote> MergeDeferredDetails(
    List<DeferredDetailNote> detailsA,
    List<DeferredDetailNote> detailsB)
  {
    Dictionary<string, DeferredDetailNote> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (DeferredDetailNote detail in detailsA.Concat(detailsB))
    {
      string key = $"{detail.Topic}:{detail.TargetReference}".ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(detail.Topic))
      {
        continue;
      }

      if (!merged.ContainsKey(key))
      {
        merged[key] = detail;
      }
    }

    return merged.Values.ToList();
  }

  private static string BuildWallKey(Wall wall)
  {
    if (!string.IsNullOrWhiteSpace(wall.Symbol))
    {
      return wall.Symbol.Trim().ToLowerInvariant();
    }

    return $"{wall.Type.Trim().ToLowerInvariant()}:{wall.LengthM:0.##}";
  }

  private static string BuildOpeningKey(Opening opening)
  {
    if (!string.IsNullOrWhiteSpace(opening.Symbol))
    {
      return $"{opening.Type}:{opening.Symbol}".ToLowerInvariant();
    }

    return $"{opening.Type}:{opening.WidthCm:0.#}:{opening.HeightCm:0.#}".ToLowerInvariant();
  }

  private static List<FootingDetail> MergeFootings(List<FootingDetail> footingsA, List<FootingDetail> footingsB)
  {
    Dictionary<string, FootingDetail> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (FootingDetail footing in footingsA.Concat(footingsB))
    {
      string key = !string.IsNullOrWhiteSpace(footing.Symbol)
          ? footing.Symbol.Trim()
          : $"{footing.LengthM:0.##}:{footing.WidthM:0.##}:{footing.HeightM:0.##}";

      if (!merged.TryGetValue(key, out FootingDetail? existing))
      {
        merged[key] = footing;
        continue;
      }

      if (footing.Segments.Count > existing.Segments.Count)
      {
        existing.Segments = footing.Segments;
        existing.LengthM = footing.LengthM;
      }

      if (footing.LengthM > existing.LengthM)
      {
        existing.LengthM = footing.LengthM;
      }

      if (footing.WidthM > existing.WidthM)
      {
        existing.WidthM = footing.WidthM;
      }

      if (footing.HeightM > existing.HeightM)
      {
        existing.HeightM = footing.HeightM;
      }
    }

    return merged.Values.ToList();
  }

  private static List<SlabDetail> MergeSlabs(List<SlabDetail> slabsA, List<SlabDetail> slabsB)
  {
    Dictionary<string, SlabDetail> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (SlabDetail slab in slabsA.Concat(slabsB))
    {
      string key = $"{slab.AreaM2:0.##}:{slab.ThicknessCm:0.#}:{slab.ConcreteClass}";
      if (!merged.ContainsKey(key) || slab.AreaM2 > merged[key].AreaM2)
      {
        merged[key] = slab;
      }
    }

    return merged.Values.ToList();
  }

  private static List<MaterialQuantity> MergeMaterialQuantities(
    List<MaterialQuantity> materialsA,
    List<MaterialQuantity> materialsB)
  {
    Dictionary<string, MaterialQuantity> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (MaterialQuantity material in materialsA.Concat(materialsB))
    {
      string key = $"{material.MaterialType}:{material.Unit}".ToLowerInvariant();
      if (!merged.TryGetValue(key, out MaterialQuantity? existing))
      {
        merged[key] = new MaterialQuantity
        {
          MaterialType = material.MaterialType,
          Quantity = material.Quantity,
          Unit = material.Unit
        };

        continue;
      }

      existing.Quantity = Math.Max(existing.Quantity, material.Quantity);
    }

    return merged.Values.ToList();
  }

  private static List<InteriorDoorEntry> MergeInteriorDoors(
    List<InteriorDoorEntry> doorsA,
    List<InteriorDoorEntry> doorsB)
  {
    Dictionary<string, InteriorDoorEntry> merged = new(StringComparer.OrdinalIgnoreCase);

    foreach (InteriorDoorEntry door in doorsA.Concat(doorsB))
    {
      string key = $"{door.Type}:{door.Floor}".ToLowerInvariant();
      if (!merged.TryGetValue(key, out InteriorDoorEntry? existing))
      {
        merged[key] = door;
        continue;
      }

      existing.CountEstimated = Math.Max(existing.CountEstimated, door.CountEstimated);
    }

    return merged.Values.ToList();
  }

  private static SectionDrawingData? MergeSection(SectionDrawingData? sectionA, SectionDrawingData? sectionB)
  {
    if (sectionA is null)
    {
      return sectionB;
    }

    if (sectionB is null)
    {
      return sectionA;
    }

    return sectionA.FloorZones.Count + sectionA.RoofZones.Count + sectionA.RingBeams.Count
        >= sectionB.FloorZones.Count + sectionB.RoofZones.Count + sectionB.RingBeams.Count
      ? sectionA
      : sectionB;
  }
}
