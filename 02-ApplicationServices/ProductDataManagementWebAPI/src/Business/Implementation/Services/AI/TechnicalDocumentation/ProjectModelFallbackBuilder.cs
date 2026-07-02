using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ProjectModelFallbackBuilder
{
    public static ProjectModel Build(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        ProjectModel model = new();
        if (drawings.Count == 0)
        {
            return model;
        }

        ApplyMetadata(model, drawings);
        ApplySite(model, drawings);
        model.Floors = BuildFloors(drawings);
        ApplyWalls(model, drawings);
        ApplyFoundations(model, drawings);
        ApplyCeilings(model, drawings);
        ApplyRoof(model, drawings);
        ApplyStructuralElements(model, drawings);
        return model;
    }

    private static void ApplyMetadata(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        DrawingClassification? classification = drawings
            .Where(drawing => drawing.Classification.DrawingType.Contains("zagospodarowanie", StringComparison.OrdinalIgnoreCase))
            .Select(drawing => drawing.Classification)
            .FirstOrDefault()
            ?? drawings
                .Where(drawing => drawing.Classification.DrawingType.Contains("rzut_parteru", StringComparison.OrdinalIgnoreCase))
                .Select(drawing => drawing.Classification)
                .FirstOrDefault()
            ?? drawings
                .Select(drawing => drawing.Classification)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Investor)
                    || !string.IsNullOrWhiteSpace(item.Location))
            ?? drawings
                .Where(drawing => DrawingViewClassifier.Classify(drawing.Classification) == DrawingViewBucket.Plan)
                .Select(drawing => drawing.Classification)
                .FirstOrDefault()
            ?? drawings
                .Select(drawing => drawing.Classification)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Title)
                    && !item.DrawingType.Contains("elewacja", StringComparison.OrdinalIgnoreCase));

        if (classification is null)
        {
            return;
        }

        model.Project.Name = ResolveProjectName(classification);
        model.Project.Address = classification.Address;
        model.Project.Location = classification.Location;
        model.Project.Investor = classification.Investor;
        model.Project.Author = classification.Author;
        model.Project.Collaborator = classification.Collaborator;
        model.Project.Date = classification.Date;
        model.Project.Phase = classification.Phase;
    }

    private static string? ResolveProjectName(DrawingClassification classification)
    {
        if (!string.IsNullOrWhiteSpace(classification.ProjectName))
        {
            return classification.ProjectName;
        }

        if (!string.IsNullOrWhiteSpace(classification.Title)
            && !classification.DrawingType.Contains("elewacja", StringComparison.OrdinalIgnoreCase)
            && !classification.DrawingType.Contains("rzut_", StringComparison.OrdinalIgnoreCase))
        {
            return classification.Title;
        }

        if (!string.IsNullOrWhiteSpace(classification.BuildingType))
        {
            return classification.BuildingType;
        }

        return classification.Title;
    }

    private static void ApplySite(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        SitePlanSection? site = drawings
            .Select(drawing => drawing.Site)
            .FirstOrDefault(section => section is not null);

        if (site is null)
        {
            return;
        }

        model.Site.PlotAreaM2 = site.PlotAreaM2;
        model.Site.BuildingFootprintM2 = site.BuildingFootprintM2;
        model.Site.BuildingVolumeM3 = site.BuildingVolumeM3;
    }

    private static List<ProjectModelFloor> BuildFloors(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        Dictionary<string, ProjectModelFloor> floors = new(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlanDrawing drawing in drawings)
        {
            if (DrawingViewClassifier.Classify(drawing.Classification) != DrawingViewBucket.Plan
                && drawing.Rooms.Count == 0)
            {
                continue;
            }

            string level = drawing.Classification.FloorLevel ?? InferFloorLevel(drawing.Classification);
            int order = drawing.Classification.FloorOrder ?? InferFloorOrder(level);
            string key = $"{order:D4}:{level}";

            if (!floors.TryGetValue(key, out ProjectModelFloor? floor))
            {
                floor = new ProjectModelFloor
                {
                    Level = level,
                    Order = order,
                    TotalAreaM2 = drawing.TotalAreaM2,
                    AreaNotes = drawing.AreaNotes
                };
                floors[key] = floor;
            }
            else
            {
                if (drawing.TotalAreaM2 is > 0 && floor.TotalAreaM2 is null)
                {
                    floor.TotalAreaM2 = drawing.TotalAreaM2;
                }

                if (string.IsNullOrWhiteSpace(floor.AreaNotes) && !string.IsNullOrWhiteSpace(drawing.AreaNotes))
                {
                    floor.AreaNotes = drawing.AreaNotes;
                }
            }

            foreach (Room room in drawing.Rooms)
            {
                double? area = ResolveRoomArea(room);
                floor.Rooms.Add(new ProjectModelRoom
                {
                    Name = room.Name,
                    Symbol = !string.IsNullOrWhiteSpace(room.Symbol) ? room.Symbol : room.Number,
                    WidthM = room.WidthM > 0 ? room.WidthM : null,
                    LengthM = room.LengthM > 0 ? room.LengthM : null,
                    HeightM = room.HeightM,
                    AreaM2 = area,
                    Category = room.Category ?? RoomCategoryInferrer.Infer(room.Name),
                    Notes = room.Notes
                });
            }
        }

        List<ProjectModelFloor> result = floors.Values
            .OrderBy(floor => floor.Order)
            .ThenBy(floor => floor.Level, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (ProjectModelFloor floor in result)
        {
            if (floor.TotalAreaM2 is null or <= 0)
            {
                floor.TotalAreaM2 = Math.Round(
                    floor.Rooms.Sum(room => room.AreaM2 ?? 0),
                    1);
            }
        }

        return result;
    }

    private static void ApplyWalls(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        Wall? externalWall = drawings
            .SelectMany(drawing => drawing.Walls)
            .FirstOrDefault(wall => wall.Type.Contains("zewn", StringComparison.OrdinalIgnoreCase));

        if (externalWall is not null)
        {
            model.Walls.External.ThicknessCm = externalWall.ThicknessCm > 0
                ? externalWall.ThicknessCm
                : null;
            model.Walls.External.Layers = externalWall.Layers
                .Select(layer => new ProjectModelWallLayer
                {
                    Material = layer.Material,
                    ThicknessCm = layer.ThicknessCm > 0 ? layer.ThicknessCm : null
                })
                .ToList();
        }

        SectionDrawingData? section = drawings
            .Select(drawing => drawing.Section)
            .FirstOrDefault(item => item is not null);

        if (section?.FloorZones is null || section.FloorZones.Count == 0)
        {
            ApplyThermalInsulationFromSection(model, section);
            return;
        }

        SectionZone? firstZone = section.FloorZones[0];
        if (model.Walls.External.Layers.Count == 0 && firstZone.Layers.Count > 0)
        {
            model.Walls.External.Layers = firstZone.Layers
                .Select(layer => new ProjectModelWallLayer
                {
                    Material = layer.Material,
                    ThicknessCm = layer.ThicknessCm > 0 ? layer.ThicknessCm : null
                })
                .ToList();
        }

        ApplyThermalInsulationFromSection(model, section);
    }

    private static void ApplyThermalInsulationFromSection(ProjectModel model, SectionDrawingData? section)
    {
        if (section?.ThermalInsulation is null || section.ThermalInsulation.Count == 0)
        {
            return;
        }

        ThermalInsulationElementDetail? externalInsulation = section.ThermalInsulation
            .FirstOrDefault(item =>
                item.Element.Contains("zewn", StringComparison.OrdinalIgnoreCase)
                || item.Element.Contains("scian", StringComparison.OrdinalIgnoreCase)
                || item.Element.Contains("ścian", StringComparison.OrdinalIgnoreCase));

        if (externalInsulation is null || externalInsulation.ThicknessCm is not > 0)
        {
            return;
        }

        ProjectModelWallLayer? insulationLayer = model.Walls.External.Layers
            .FirstOrDefault(layer => TechnicalDocumentationDomainRules.IsThermalInsulationMaterial(layer.Material));

        if (insulationLayer is not null)
        {
            insulationLayer.ThicknessCm = externalInsulation.ThicknessCm;
            if (!string.IsNullOrWhiteSpace(externalInsulation.Material))
            {
                insulationLayer.Material = externalInsulation.Material;
            }

            return;
        }

        model.Walls.External.Layers.Add(new ProjectModelWallLayer
        {
            Material = externalInsulation.Material ?? "Styropian EPS",
            ThicknessCm = externalInsulation.ThicknessCm
        });
    }

    private static void ApplyFoundations(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        FoundationSection? foundations = drawings
            .Select(drawing => drawing.Foundations)
            .FirstOrDefault(section => section is not null);

        if (foundations is null)
        {
            return;
        }

        model.Foundations.Concrete = foundations.ConcreteClass;
        model.Foundations.FoundationWall = foundations.FoundationWall is null
            ? null
            : $"{foundations.FoundationWall.Material} {foundations.FoundationWall.ThicknessCm}cm";

        model.Foundations.Footings = foundations.Footings
            .Select(footing => new ProjectModelFooting
            {
                Symbol = footing.Symbol,
                WidthM = footing.WidthM > 0 ? footing.WidthM : null,
                HeightM = footing.HeightM > 0 ? footing.HeightM : null,
                ConcreteClass = footing.ConcreteClass,
                Reinforcement = footing.Reinforcement,
                Segments = footing.Segments
                    .Where(segment => segment.LengthM > 0)
                    .Select(segment => new ProjectModelFootingSegment
                    {
                        Id = segment.Id,
                        LengthM = segment.LengthM
                    })
                    .ToList()
            })
            .ToList();

        model.Foundations.Pads = foundations.Pads
            .Select(pad => new ProjectModelPad
            {
                Symbol = pad.Symbol,
                BM = pad.BM > 0 ? pad.BM : null,
                LM = pad.LM > 0 ? pad.LM : null,
                HeightM = pad.HeightM > 0 ? pad.HeightM : null,
                ConcreteClass = pad.ConcreteClass,
                Reinforcement = pad.Reinforcement
            })
            .ToList();
    }

    private static void ApplyCeilings(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        foreach (FloorPlanDrawing drawing in drawings)
        {
            if (drawing.Floors is null)
            {
                continue;
            }

            FloorSection floors = drawing.Floors;
            string description = floors.CoverageDescription
                ?? drawing.Classification.Title
                ?? drawing.Classification.DrawingType;
            bool isTopReinforcement = drawing.Classification.DrawingType.Contains("gorne", StringComparison.OrdinalIgnoreCase)
                || drawing.Classification.DrawingType.Contains("górne", StringComparison.OrdinalIgnoreCase);
            double? steelKg = floors.TotalMassKg
                ?? (floors.Steel.Count > 0 ? floors.Steel.Sum(item => item.Quantity) : null);

            if (floors.Slabs.Count > 0)
            {
                foreach (SlabDetail slab in floors.Slabs)
                {
                    model.Ceilings.Add(new ProjectModelCeiling
                    {
                        CoverageDescription = description,
                        ThicknessCm = slab.ThicknessCm > 0 ? slab.ThicknessCm : null,
                        Concrete = slab.ConcreteClass,
                        SteelBottomKg = isTopReinforcement ? null : steelKg,
                        SteelTopKg = isTopReinforcement ? steelKg : null
                    });
                }
            }
            else if (floors.Bars.Count > 0 || steelKg is > 0)
            {
                SlabDetail? firstSlab = floors.Slabs.FirstOrDefault();
                model.Ceilings.Add(new ProjectModelCeiling
                {
                    CoverageDescription = description,
                    ThicknessCm = firstSlab is not null && firstSlab.ThicknessCm > 0
                        ? firstSlab.ThicknessCm
                        : 18,
                    Concrete = firstSlab?.ConcreteClass,
                    SteelBottomKg = isTopReinforcement ? null : steelKg,
                    SteelTopKg = isTopReinforcement ? steelKg : null
                });
            }
        }
    }

    private static void ApplyRoof(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        RoofSection? roofWithTimber = drawings
            .Select(drawing => drawing.Roof)
            .Where(section => section is not null)
            .OrderByDescending(section => section!.TimberGroups.Count + section!.Timber.Count)
            .FirstOrDefault();

        double? maxArea = drawings
            .Select(drawing => drawing.Roof?.AreaM2)
            .Where(area => area is > 0)
            .DefaultIfEmpty()
            .Max();

        if (roofWithTimber is null && maxArea is null)
        {
            return;
        }

        RoofSection? reference = roofWithTimber ?? drawings.Select(d => d.Roof).FirstOrDefault(r => r is not null);

        if (reference is null)
        {
            return;
        }

        model.Roof.PitchDegrees = reference.PitchDegrees > 0 ? reference.PitchDegrees : null;
        model.Roof.AreaM2 = maxArea is > 0 ? maxArea : (reference.AreaM2 > 0 ? reference.AreaM2 : null);
        model.Roof.CoveringType = reference.CoveringType;
        model.Roof.WoodClass = reference.WoodClass;
        model.Roof.TotalTimberVolumeM3 = reference.TotalVolumeM3
            ?? reference.Timber.Where(item => item.VolumeM3 is > 0).Sum(item => item.VolumeM3!.Value);

        if (reference.TimberGroups.Count > 0)
        {
            model.Roof.TimberGroups = reference.TimberGroups
                .Select(group => new ProjectModelTimberGroup
                {
                    Element = group.Name,
                    Section = group.Section,
                    Count = group.Rows.Sum(row => row.Count),
                    LengthM = group.GroupSumMb,
                    VolumeM3 = group.GroupVolumeM3
                })
                .ToList();
        }
        else
        {
            model.Roof.TimberGroups = reference.Timber
                .Select(item => new ProjectModelTimberGroup
                {
                    Element = item.Element,
                    Section = item.Section,
                    Count = item.Count > 0 ? item.Count : null,
                    LengthM = item.LengthM > 0 ? item.LengthM : null,
                    VolumeM3 = item.VolumeM3
                })
                .ToList();
        }
    }

    private static string InferFloorLevel(DrawingClassification classification)
    {
        string combined = $"{classification.DrawingType} {classification.Title}".ToLowerInvariant();

        if (combined.Contains("poddasze", StringComparison.Ordinal))
        {
            return "Poddasze";
        }

        if (combined.Contains("parter", StringComparison.Ordinal))
        {
            return "Parter";
        }

        if (combined.Contains("piętro", StringComparison.Ordinal) || combined.Contains("pietro", StringComparison.Ordinal))
        {
            return "Piętro";
        }

        return "Nieznana";
    }

    private static int InferFloorOrder(string level)
    {
        string normalized = level.ToLowerInvariant();

        if (normalized.Contains("poddasze", StringComparison.Ordinal))
        {
            return 1;
        }

        if (normalized.Contains("piętro", StringComparison.Ordinal) || normalized.Contains("pietro", StringComparison.Ordinal))
        {
            return 2;
        }

        return 0;
    }

    private static void ApplyStructuralElements(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        foreach (FloorPlanDrawing drawing in drawings)
        {
            foreach (StructuralColumn column in drawing.Columns)
            {
                model.Columns.Add(new ProjectModelColumn
                {
                    Symbol = column.Symbol,
                    BCm = column.BCm,
                    HCm = column.HCm,
                    HeightM = column.HeightM,
                    ConcreteClass = column.ConcreteClass,
                    LongitudinalBars = column.LongitudinalBars,
                    Stirrups = column.Stirrups
                });
            }

            foreach (StructuralBeam beam in drawing.Beams)
            {
                model.Beams.Add(new ProjectModelBeam
                {
                    Symbol = beam.Symbol,
                    SpanM = beam.SpanM,
                    BwCm = beam.BwCm ?? beam.BCm,
                    HCm = beam.HCm,
                    ConcreteClass = beam.ConcreteClass,
                    MainBars = beam.MainBars
                });
            }

            foreach (StructuralLintel lintel in drawing.Lintels)
            {
                model.Lintels.Add(new ProjectModelLintel
                {
                    Symbol = lintel.Symbol,
                    SpanM = lintel.SpanM,
                    BwCm = lintel.BwCm ?? lintel.BCm,
                    HCm = lintel.HCm,
                    ConcreteClass = lintel.ConcreteClass,
                    MainBars = lintel.MainBars,
                    Stirrups = lintel.Stirrups
                });
            }
        }
    }

    private static double? ResolveRoomArea(Room room)
    {
        if (room.AreaM2 > 0)
        {
            return Math.Round(room.AreaM2, 1);
        }

        if (room.WidthM > 0 && room.LengthM > 0)
        {
            return Math.Round(room.WidthM * room.LengthM, 1);
        }

        return null;
    }
}
