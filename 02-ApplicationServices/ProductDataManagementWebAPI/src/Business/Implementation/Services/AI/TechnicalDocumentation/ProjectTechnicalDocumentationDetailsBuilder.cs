using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

[Obsolete("Legacy per-drawing summaries. Group pipeline writes TechnicalDocumentationDetailsJsonRoot (§8.1) without these fields.")]
internal static class ProjectTechnicalDocumentationDetailsBuilder
{
    public static void Apply(
        ProjectTechnicalDocumentationDetails details,
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        MaterialSchedule? computedSchedule,
        string buildingType,
        IReadOnlyList<TechnicalDocumentationImageInput>? sourceImages = null,
        IReadOnlyList<string>? failedPages = null)
    {
        details.ProjectModel = model;
        details.Project = BuildProjectInfo(model, buildingType);
        details.Rooms = BuildRooms(model, drawings);
        details.TotalAreaM2 = Math.Round(
            details.Rooms.Sum(floor => floor.TotalAreaM2 ?? floor.Items.Sum(item => item.AreaM2)),
            2);
        details.Roof = BuildRoof(model, drawings);
        details.TimberStructure = BuildTimberStructure(model, drawings);
        details.Walls = BuildWalls(model, drawings);
        details.Floors = BuildFloors(model, drawings);
        details.Foundations = BuildFoundations(model, drawings);
        details.ThermalInsulation = BuildThermalInsulation(drawings);
        details.Joinery = BuildJoinery(drawings);
        details.Installations = BuildInstallations(drawings, model);
        details.ValidatedDrawings = sourceImages is { Count: > 0 }
            ? ValidatedDrawingCatalogBuilder.Build(drawings, sourceImages, failedPages ?? [])
            : BuildValidatedDrawings(drawings);
        details.DrawingDependencies = BuildDrawingDependencies(dependencies);

        ApplyProjectMetadataFromDrawings(details, drawings);

        if (computedSchedule is not null)
        {
            details.MaterialSchedule = DetailsMaterialScheduleMapper.Map(computedSchedule, model, drawings);
        }
    }

    private static ProjectInfo BuildProjectInfo(ProjectModel model, string buildingType)
    {
        return new ProjectInfo
        {
            Name = model.Project.Name ?? string.Empty,
            Investor = model.Project.Investor,
            Address = model.Project.Address,
            Location = model.Project.Location,
            Designer = model.Project.Author,
            Collaborator = model.Project.Collaborator,
            Date = model.Project.Date,
            Phase = model.Project.Phase,
            BuildingType = string.IsNullOrWhiteSpace(buildingType) ? null : buildingType
        };
    }

    public static void ApplyProjectMetadataFromDrawings(
        ProjectTechnicalDocumentationDetails details,
        IReadOnlyList<FloorPlanDrawing> drawings)
    {
        if (!string.IsNullOrWhiteSpace(details.Project.Name) || drawings.Count == 0)
        {
            return;
        }

        DrawingClassification classification = drawings[0].Classification;
        details.Project.Name = classification.ProjectName ?? classification.Title ?? classification.DrawingType ?? string.Empty;
        details.Project.Investor ??= classification.Investor;
        details.Project.Address ??= classification.Address;
        details.Project.Location ??= classification.Location;
        details.Project.Designer ??= classification.Author;
        details.Project.Collaborator ??= classification.Collaborator;
        details.Project.Date ??= classification.Date;
        details.Project.Phase ??= classification.Phase;
        details.Project.BuildingType ??= classification.BuildingType;
    }

    private static List<RoomFloorGroup> BuildRooms(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        if (model.Floors.Count > 0)
        {
            return model.Floors
                .OrderBy(floor => floor.Order)
                .Select(floor => new RoomFloorGroup
                {
                    Floor = floor.Level,
                    FloorOrder = floor.Order,
                    TotalAreaM2 = floor.TotalAreaM2,
                    AreaNotes = floor.AreaNotes,
                    Items = floor.Rooms
                        .Where(room => !string.IsNullOrWhiteSpace(room.Name))
                        .Select(room => new RoomFloorItem
                        {
                            Number = ResolveProjectModelRoomNumber(room),
                            Name = room.Name,
                            AreaM2 = room.AreaM2 ?? 0,
                            Category = room.Category ?? RoomCategoryInferrer.Infer(room.Name),
                            Notes = room.Notes
                        })
                        .ToList()
                })
                .Where(group => group.Items.Count > 0)
                .ToList();
        }

        return drawings
            .Where(drawing => DrawingViewClassifier.Classify(drawing.Classification) == DrawingViewBucket.Plan)
            .GroupBy(drawing => DrawingViewClassifier.BuildFloorKey(drawing.Classification))
            .Select(group =>
            {
                List<RoomFloorItem> items = group
                    .SelectMany(drawing => drawing.Rooms)
                    .Select(room => new RoomFloorItem
                    {
                        Number = ResolveRoomNumber(room),
                        Name = room.Name,
                        AreaM2 = room.AreaM2,
                        Category = room.Category ?? RoomCategoryInferrer.Infer(room.Name),
                        Notes = room.Notes
                    })
                    .ToList();

                FloorPlanDrawing first = group.First();
                return new RoomFloorGroup
                {
                    Floor = DrawingViewClassifier.BuildFloorLabel(first.Classification),
                    FloorOrder = DrawingViewClassifier.BuildFloorOrder(first.Classification),
                    TotalAreaM2 = group.Sum(drawing => drawing.TotalAreaM2 ?? 0) > 0
                        ? group.Sum(drawing => drawing.TotalAreaM2 ?? 0)
                        : items.Sum(item => item.AreaM2),
                    AreaNotes = group.Select(drawing => drawing.AreaNotes).FirstOrDefault(note => !string.IsNullOrWhiteSpace(note)),
                    Items = items
                };
            })
            .Where(group => group.Items.Count > 0)
            .OrderBy(group => group.FloorOrder)
            .ToList();
    }

    private static RoofSummary? BuildRoof(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        RoofSection? roofDrawing = drawings
            .Select(drawing => drawing.Roof)
            .FirstOrDefault(roof => roof is not null);

        if (model.Roof.AreaM2 is null
            && string.IsNullOrWhiteSpace(model.Roof.CoveringType)
            && roofDrawing is null)
        {
            return null;
        }

        string? sourceDrawing = FindSheetNumber(drawings, "rzut_dachu", "rzut dachu");

        RoofSummary summary = new()
        {
            PitchDegrees = model.Roof.PitchDegrees ?? roofDrawing?.PitchDegrees,
            AreaM2 = model.Roof.AreaM2 ?? roofDrawing?.AreaM2,
            CoveringType = model.Roof.CoveringType ?? roofDrawing?.CoveringType,
            SourceDrawing = sourceDrawing
        };

        List<Opening> roofOpenings = drawings
            .SelectMany(drawing => drawing.Openings)
            .Where(opening => opening.Type.Contains("dach", StringComparison.OrdinalIgnoreCase)
                || opening.Type.Contains("połac", StringComparison.OrdinalIgnoreCase)
                || opening.Type.Contains("polac", StringComparison.OrdinalIgnoreCase))
            .ToList();

        summary.RoofWindows = roofOpenings
            .GroupBy(opening => $"{opening.Type}:{opening.WidthCm}:{opening.HeightCm}:{opening.Location}")
            .Select(group =>
            {
                Opening first = group.First();
                return new RoofWindowEntry
                {
                    Type = first.Type,
                    WidthCm = first.WidthCm > 0 ? first.WidthCm : null,
                    HeightCm = first.HeightCm > 0 ? first.HeightCm : null,
                    Count = group.Sum(item => item.Count),
                    Location = first.Location,
                    SourceDrawing = sourceDrawing
                };
            })
            .ToList();

        RoofSection? roofData = roofDrawing ?? drawings.Select(drawing => drawing.Roof).FirstOrDefault(roof => roof is not null);
        if (roofData?.Ventilation is not null)
        {
            summary.Ventilation = new RoofVentilationEntry
            {
                Type = roofData.Ventilation.Type,
                Count = roofData.Ventilation.Count,
                SourceDrawing = roofData.Ventilation.SourceDrawing ?? sourceDrawing
            };
        }

        if (roofData?.Drainage is not null)
        {
            summary.Drainage = new RoofDrainageEntry
            {
                DownpipeDiameterMm = roofData.Drainage.DownpipeDiameterMm,
                MinSlopePct = roofData.Drainage.MinSlopePct,
                Notes = roofData.Drainage.Notes
            };
        }

        SectionDrawingData? section = drawings
            .Select(drawing => drawing.Section)
            .FirstOrDefault(sectionData => sectionData?.RoofZones.Count > 0);

        if (section is not null)
        {
            summary.Layers = section.RoofZones
                .Select(zone => new RoofLayerZone
                {
                    Zone = zone.Zone ?? string.Empty,
                    Sequence = zone.Layers
                        .Select(layer => layer.Material)
                        .Where(material => !string.IsNullOrWhiteSpace(material))
                        .ToList()
                })
                .Where(layer => layer.Sequence.Count > 0)
                .ToList();
        }

        return summary;
    }

    private static TimberStructureSummary? BuildTimberStructure(
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<TimberGroup> timberGroups = TimberStructureCollector.CollectGroups(drawings);

        if (timberGroups.Count == 0 && model.Roof.TimberGroups.Count == 0)
        {
            return null;
        }

        if (timberGroups.Count == 0)
        {
            return new TimberStructureSummary
            {
                WoodClass = model.Roof.WoodClass,
                TotalVolumeM3 = model.Roof.TotalTimberVolumeM3,
                Groups = model.Roof.TimberGroups
                    .Select(group => new TimberGroupSummary
                    {
                        Name = group.Element,
                        Section = group.Section,
                        GroupVolumeM3 = group.VolumeM3
                    })
                    .ToList()
            };
        }

        RoofSection? roofSection = drawings
            .Select(drawing => drawing.Roof)
            .FirstOrDefault(roof => roof?.TimberGroups.Count > 0);

        return new TimberStructureSummary
        {
            WoodClass = roofSection?.WoodClass ?? model.Roof.WoodClass,
            SourceDrawing = FindSheetNumber(drawings, "rzut_wiezby", "więźba", "wiezba"),
            TotalVolumeM3 = roofSection?.TotalVolumeM3 ?? model.Roof.TotalTimberVolumeM3,
            Notes = roofSection?.Notes,
            Groups = timberGroups
                .Select(group => new TimberGroupSummary
                {
                    Name = group.Name,
                    Section = group.Section,
                    GroupSumMb = group.GroupSumMb,
                    GroupVolumeM3 = group.GroupVolumeM3,
                    Rows = group.Rows
                        .Select(row => new TimberStructureRow
                        {
                            Count = row.Count,
                            LengthM = row.LengthM,
                            RowSumMb = row.RowSumMb ?? row.Count * row.LengthM
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static WallsSummary? BuildWalls(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        bool hasWalls = model.Walls.External.ThicknessCm.HasValue
            || model.Walls.External.Layers.Count > 0
            || model.Columns.Count > 0
            || drawings.Any(drawing => drawing.Walls.Any(wall => wall.Layers.Count > 0) || drawing.Columns.Count > 0);

        if (!hasWalls)
        {
            return null;
        }

        List<string> sourceDrawings = CollectSheetNumbers(
            drawings,
            "rzut_parteru", "rzut_piętra", "rzut_poddasza", "elewacja", "przekroj");

        WallsSummary summary = new()
        {
            SourceDrawings = sourceDrawings,
            External = new WallExternalSummary
            {
                ThicknessCm = model.Walls.External.ThicknessCm,
                Layers = model.Walls.External.Layers.Count > 0
                    ? model.Walls.External.Layers
                        .Select(layer => new WallLayerSummary
                        {
                            Material = layer.Material,
                            ThicknessCm = layer.ThicknessCm
                        })
                        .ToList()
                    : drawings
                        .SelectMany(drawing => drawing.Walls)
                        .SelectMany(wall => wall.Layers)
                        .Select(layer => new WallLayerSummary
                        {
                            Material = layer.Material,
                            ThicknessCm = layer.ThicknessCm
                        })
                        .ToList()
            },
            Internal = BuildInternalWalls(model, drawings),
        };

        summary.External.Finishes = DeduplicateFinishes(drawings);

        summary.Columns = model.Columns
            .Concat(drawings.SelectMany(drawing => drawing.Columns)
                .Select(column => new ProjectModelColumn
                {
                    Symbol = column.Symbol,
                    BCm = column.BCm,
                    HCm = column.HCm
                }))
            .GroupBy(column => column.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(column => new WallColumnSummary
            {
                Symbol = column.Symbol,
                WidthCm = column.BCm,
                HeightCm = column.HCm,
                Reinforcement = column.LongitudinalBars,
                SourceDrawing = FindSheetNumber(drawings, "detale", "słup", "slup")
            })
            .ToList();

        SectionDrawingData? section = drawings
            .Select(drawing => drawing.Section)
            .FirstOrDefault(sectionData => sectionData?.RingBeam is not null
                || sectionData?.RingBeams.Count > 0
                || sectionData?.CollarWall is not null);

        if (section is not null)
        {
            if (section.CollarWall is not null)
            {
                summary.CollarWall = new CollarWallSummary
                {
                    ThicknessCm = section.CollarWall.ThicknessCm,
                    HeightCm = section.CollarWall.HeightCm,
                    Timber = section.CollarWall.Timber is null
                        ? null
                        : new CollarWallTimberSummary
                        {
                            Section = section.CollarWall.Timber.Section,
                            Material = section.CollarWall.Timber.Material
                        },
                    RingBeam = section.CollarWall.RingBeam is null
                        ? null
                        : new RingBeamSummary
                        {
                            Location = section.CollarWall.RingBeam.Location,
                            WidthCm = section.CollarWall.RingBeam.WidthCm,
                            HeightCm = section.CollarWall.RingBeam.HeightCm,
                            Reinforcement = section.CollarWall.RingBeam.Reinforcement
                        }
                };
            }

            if (section.RingBeams.Count > 0)
            {
                summary.RingBeams = section.RingBeams
                    .Select(beam => new RingBeamSummary
                    {
                        Location = beam.Location,
                        WidthCm = beam.WidthCm,
                        HeightCm = beam.HeightCm,
                        Reinforcement = beam.Reinforcement
                    })
                    .ToList();
            }
            else if (section.RingBeam is not null)
            {
                summary.RingBeams.Add(new RingBeamSummary
                {
                    Location = section.RingBeam.Location ?? "wg przekroju",
                    WidthCm = section.RingBeam.WidthCm,
                    HeightCm = section.RingBeam.HeightCm,
                    Reinforcement = section.RingBeam.Reinforcement
                });
            }
        }

        return summary;
    }

    private static WallInternalSummary BuildInternalWalls(
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings)
    {
        WallInternalGroupSummary? loadBearing = model.Walls.InternalLoadBearing.ThicknessCm.HasValue
            ? new WallInternalGroupSummary
            {
                ThicknessCm = model.Walls.InternalLoadBearing.ThicknessCm,
                Material = model.Walls.InternalLoadBearing.Layers.FirstOrDefault()?.Material
            }
            : null;

        WallInternalGroupSummary? partition = model.Walls.Partition.ThicknessCm.HasValue
            ? new WallInternalGroupSummary
            {
                ThicknessCm = model.Walls.Partition.ThicknessCm,
                Material = model.Walls.Partition.Layers.FirstOrDefault()?.Material
            }
            : null;

        foreach (Wall wall in drawings.SelectMany(drawing => drawing.Walls))
        {
            string wallType = wall.Type.ToLowerInvariant();
            if (loadBearing is null
                && (wallType.Contains("nośna", StringComparison.Ordinal)
                    || wallType.Contains("nosna", StringComparison.Ordinal)))
            {
                loadBearing = new WallInternalGroupSummary
                {
                    ThicknessCm = wall.ThicknessCm,
                    Material = wall.Layers.FirstOrDefault()?.Material ?? wall.Type
                };
            }

            if (partition is null
                && (wallType.Contains("działowa", StringComparison.Ordinal)
                    || wallType.Contains("dzialowa", StringComparison.Ordinal)
                    || wallType.Contains("partition", StringComparison.Ordinal)))
            {
                partition = new WallInternalGroupSummary
                {
                    ThicknessCm = wall.ThicknessCm,
                    Material = wall.Layers.FirstOrDefault()?.Material ?? wall.Type
                };
            }
        }

        return new WallInternalSummary
        {
            LoadBearing = loadBearing,
            Partition = partition
        };
    }

    private static FloorsSummary? BuildFloors(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        FloorReinforcementDrawingResolver.ReinforcementLayers reinforcementLayers =
            FloorReinforcementDrawingResolver.Resolve(drawings);

        FloorSection? bottomReinforcement = reinforcementLayers.Bottom;
        FloorSection? topReinforcement = reinforcementLayers.Top;

        ProjectModelCeiling? ceiling = model.Ceilings.FirstOrDefault();
        SectionDrawingData? section = drawings
            .Select(drawing => drawing.Section)
            .FirstOrDefault(sectionData => sectionData?.FloorZones.Count > 0);

        bool hasReinforcement = bottomReinforcement is not null || topReinforcement is not null;
        if (!hasReinforcement && ceiling is null && section is null)
        {
            return null;
        }

        FloorReinforcementSummary reinforcement = new()
        {
            Bottom = MapReinforcementLayer(bottomReinforcement, reinforcementLayers.BottomSheet ?? "K-02")
                ?? MapReinforcementFromModelCeiling(model.Ceilings, isTop: false, defaultSheet: "K-02"),
            Top = MapReinforcementLayer(topReinforcement, reinforcementLayers.TopSheet ?? "K-03")
                ?? MapReinforcementFromModelCeiling(model.Ceilings, isTop: true, defaultSheet: "K-03")
        };

        FloorsSummary summary = new()
        {
            SourceDrawings = CollectSheetNumbers(drawings, "przekroj", "zbrojenie_stropu"),
            SlabThicknessCm = ceiling?.ThicknessCm
                ?? bottomReinforcement?.Slabs.FirstOrDefault()?.ThicknessCm,
            ConcreteClass = ceiling?.Concrete ?? bottomReinforcement?.Slabs.FirstOrDefault()?.ConcreteClass,
            Reinforcement = reinforcement.Bottom is not null || reinforcement.Top is not null
                ? reinforcement
                : null,
            Zones = section?.FloorZones
                .Select(zone => new FloorZoneSummary
                {
                    Zone = zone.Zone ?? string.Empty,
                    SourceDrawing = zone.SourceDrawing,
                    Layers = zone.Layers
                        .Select(layer => new FloorLayerSummary
                        {
                            Material = layer.Material,
                            ThicknessCm = layer.ThicknessCm
                        })
                        .ToList()
                })
                .ToList() ?? []
        };

        return summary;
    }

    private static FloorReinforcementLayerSummary? MapReinforcementLayer(FloorSection? section, string defaultSheet)
    {
        if (section is null)
        {
            return null;
        }

        return new FloorReinforcementLayerSummary
        {
            SourceDrawing = defaultSheet,
            TotalMassKg = section.TotalMassKg
                ?? (section.Steel.Count > 0 ? section.Steel.Sum(item => item.Quantity) : null),
            BasicGrid = section.BasicGrid,
            Notes = section.Notes,
            Bars = section.Bars
                .Select(bar => new RebarBarSummary
                {
                    Pos = bar.Pos,
                    Count = bar.Count ?? 0,
                    DiameterMm = (int)(bar.DiameterMm ?? 0),
                    LengthM = bar.LengthM ?? 0,
                    TotalLengthM = bar.TotalLengthM ?? 0,
                    MassKg = bar.MassKg ?? 0
                })
                .ToList()
        };
    }

    private static FloorReinforcementLayerSummary? MapReinforcementFromModelCeiling(
        IReadOnlyList<ProjectModelCeiling> ceilings,
        bool isTop,
        string defaultSheet)
    {
        foreach (ProjectModelCeiling ceiling in ceilings)
        {
            double? massKg = isTop ? ceiling.SteelTopKg : ceiling.SteelBottomKg;
            if (massKg is > 0)
            {
                return new FloorReinforcementLayerSummary
                {
                    SourceDrawing = defaultSheet,
                    TotalMassKg = massKg,
                    Notes = ceiling.CoverageDescription
                };
            }
        }

        return null;
    }

    private static FoundationsSummary? BuildFoundations(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        FoundationSection? foundationDrawing = drawings
            .Select(drawing => drawing.Foundations)
            .FirstOrDefault(foundations => foundations is not null);

        if (model.Foundations.Footings.Count == 0
            && model.Foundations.Pads.Count == 0
            && foundationDrawing is null)
        {
            return null;
        }

        List<FoundationFootingSummary> footings = model.Foundations.Footings.Count > 0
            ? model.Foundations.Footings.Select(MapFooting).ToList()
            : foundationDrawing?.Footings.Select(footing => new FoundationFootingSummary
            {
                Symbol = footing.Symbol,
                WidthM = footing.WidthM,
                HeightM = footing.HeightM,
                Segments = footing.Segments
                    .Select(segment => new FoundationFootingSegmentSummary
                    {
                        Id = segment.Id,
                        LengthM = segment.LengthM
                    })
                    .ToList(),
                TotalLengthM = footing.Segments.Sum(segment => segment.LengthM) > 0
                    ? footing.Segments.Sum(segment => segment.LengthM)
                    : footing.LengthM > 0 ? footing.LengthM : null
            }).ToList() ?? [];

        double totalFootingLength = footings.Sum(footing => footing.TotalLengthM ?? 0);

        return new FoundationsSummary
        {
            SourceDrawings = CollectSheetNumbers(drawings, "rzut_fundamentow", "fundament", "detale"),
            ConcreteClass = foundationDrawing?.ConcreteClass ?? model.Foundations.Concrete,
            SteelSpecification = foundationDrawing?.SteelSpecification,
            CoverageMm = foundationDrawing?.CoverageMm,
            FoundationLevelM = foundationDrawing?.FoundationLevelM,
            FoundationBottomLevelM = drawings
                .Select(drawing => drawing.Section?.Levels?.FoundationBottomM)
                .FirstOrDefault(level => level.HasValue),
            Footings = footings,
            TotalFootingLengthM = totalFootingLength > 0 ? Math.Round(totalFootingLength, 2) : null,
            Pads = model.Foundations.Pads.Count > 0
                ? model.Foundations.Pads.Select(pad => new FoundationPadSummary
                {
                    Symbol = pad.Symbol,
                    BM = pad.BM,
                    LM = pad.LM,
                    HeightM = pad.HeightM,
                    Count = 1,
                    SourceDrawing = FindSheetNumber(drawings, "rzut_fundamentow", "fundament")
                }).ToList()
                : foundationDrawing?.Pads.Select(pad => new FoundationPadSummary
                {
                    Symbol = pad.Symbol,
                    BM = pad.BM,
                    LM = pad.LM,
                    HeightM = pad.HeightM,
                    Count = pad.Count > 0 ? pad.Count : 1,
                    SourceDrawing = FindSheetNumber(drawings, "rzut_fundamentow", "fundament")
                }).ToList() ?? [],
            FoundationWall = foundationDrawing?.FoundationWall is not null
                ? new FoundationWallSummary
                {
                    Material = foundationDrawing.FoundationWall.Material,
                    ThicknessCm = foundationDrawing.FoundationWall.ThicknessCm,
                    SourceDrawing = FindSheetNumber(drawings, "przekroj")
                }
                : string.IsNullOrWhiteSpace(model.Foundations.FoundationWall)
                    ? null
                    : new FoundationWallSummary { Material = model.Foundations.FoundationWall },
            ConnectionDetails = drawings
                .SelectMany(drawing => drawing.Details)
                .Select(detail => new FoundationConnectionDetailSummary
                {
                    Title = detail.Title ?? string.Empty,
                    Reinforcement = detail.Reinforcement,
                    SourceDrawing = FindSheetNumber(drawings, "detale")
                })
                .ToList()
        };
    }

    private static FoundationFootingSummary MapFooting(ProjectModelFooting footing)
    {
        double segmentSum = footing.Segments.Sum(segment => segment.LengthM ?? 0);
        return new FoundationFootingSummary
        {
            Symbol = footing.Symbol,
            WidthM = footing.WidthM,
            HeightM = footing.HeightM,
            Segments = footing.Segments
                .Select(segment => new FoundationFootingSegmentSummary
                {
                    Id = segment.Id,
                    LengthM = segment.LengthM ?? 0
                })
                .ToList(),
            TotalLengthM = segmentSum > 0 ? Math.Round(segmentSum, 2) : null
        };
    }

    private static ThermalInsulationSummary? BuildThermalInsulation(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<ThermalInsulationElement> elements = drawings
            .SelectMany(drawing => drawing.Section?.ThermalInsulation ?? [])
            .Select(element => new ThermalInsulationElement
            {
                Element = element.Element,
                Material = element.Material,
                ThicknessCm = element.ThicknessCm,
                System = element.System,
                Notes = element.Notes
            })
            .ToList();

        if (elements.Count == 0)
        {
            elements = drawings
                .SelectMany(drawing => drawing.Walls)
                .SelectMany(wall => wall.Layers)
                .Where(layer => layer.Material.Contains("styropian", StringComparison.OrdinalIgnoreCase)
                    || layer.Material.Contains("wełna", StringComparison.OrdinalIgnoreCase)
                    || layer.Material.Contains("welna", StringComparison.OrdinalIgnoreCase)
                    || layer.Material.Contains("eps", StringComparison.OrdinalIgnoreCase))
                .Select(layer => new ThermalInsulationElement
                {
                    Element = "Ściany zewnętrzne",
                    Material = layer.Material,
                    ThicknessCm = layer.ThicknessCm,
                    System = layer.Material.Contains("eps", StringComparison.OrdinalIgnoreCase) ? "ETICS" : null
                })
                .ToList();
        }

        if (elements.Count == 0)
        {
            return null;
        }

        return new ThermalInsulationSummary
        {
            SourceDrawings = CollectSheetNumbers(drawings, "przekroj", "rzut_parteru", "rzut_poddasza"),
            Elements = elements
                .GroupBy(element => $"{element.Element}:{element.Material}:{element.ThicknessCm}")
                .Select(group => group.First())
                .ToList()
        };
    }

    private static JoinerySummary BuildJoinery(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        Dictionary<string, JoineryWindowEntry> exteriorWindows = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, JoineryDoorEntry> exteriorDoors = new(StringComparer.OrdinalIgnoreCase);
        List<JoineryInteriorDoorEntry> interiorDoors = new();
        string? notes = null;

        foreach (FloorPlanDrawing drawing in drawings)
        {
            string sheet = drawing.Classification.SheetNumber ?? string.Empty;
            string? elevationLocation = drawing.Elevation?.Title;
            notes ??= drawing.Classification.Notes;

            foreach (InteriorDoorEntry interiorDoor in drawing.InteriorDoors)
            {
                interiorDoors.Add(new JoineryInteriorDoorEntry
                {
                    Type = interiorDoor.Type,
                    Floor = interiorDoor.Floor ?? drawing.Classification.FloorLevel,
                    CountEstimated = interiorDoor.CountEstimated
                });
            }

            foreach (Opening opening in drawing.Openings)
            {
                string type = opening.Type.Trim();
                string normalizedType = type.ToLowerInvariant();
                bool isDoor = normalizedType.Contains("drzwi") || normalizedType.Contains("brama");
                string? location = opening.Location ?? elevationLocation;

                if (isDoor && opening.IsInterior)
                {
                    interiorDoors.Add(new JoineryInteriorDoorEntry
                    {
                        Type = type,
                        Floor = drawing.Classification.FloorLevel,
                        CountEstimated = opening.Count > 0 ? opening.Count : 1
                    });
                    continue;
                }

                if (isDoor)
                {
                    string key = $"{type}:{location}:{opening.Symbol}";
                    if (!exteriorDoors.TryGetValue(key, out JoineryDoorEntry? door))
                    {
                        exteriorDoors[key] = new JoineryDoorEntry
                        {
                            Type = type,
                            Count = opening.Count,
                            Location = location,
                            SourceDrawing = sheet
                        };
                    }
                    else
                    {
                        door.Count = Math.Max(door.Count, opening.Count);
                    }
                }
                else
                {
                    string key = $"{type}:{opening.WidthCm}:{opening.HeightCm}:{location}:{opening.Symbol}";
                    if (!exteriorWindows.TryGetValue(key, out JoineryWindowEntry? window))
                    {
                        exteriorWindows[key] = new JoineryWindowEntry
                        {
                            Type = type,
                            Count = opening.Count,
                            Location = location,
                            WidthCm = opening.WidthCm > 0 ? opening.WidthCm : null,
                            HeightCm = opening.HeightCm > 0 ? opening.HeightCm : null,
                            SourceDrawing = sheet
                        };
                    }
                    else
                    {
                        window.Count = Math.Max(window.Count, opening.Count);
                    }
                }
            }
        }

        if (interiorDoors.Count == 0)
        {
            interiorDoors = BuildEstimatedInteriorDoors(drawings);
        }

        return new JoinerySummary
        {
            SourceDrawings = CollectSheetNumbers(drawings, "rzut_", "elewacja", "rzut_dachu"),
            Notes = notes,
            Exterior = new JoineryExteriorSummary
            {
                Windows = exteriorWindows.Values.ToList(),
                Doors = exteriorDoors.Values.ToList()
            },
            Interior = interiorDoors.Count > 0
                ? new JoineryInteriorSummary { Doors = ConsolidateInteriorDoors(interiorDoors) }
                : null
        };
    }

    private static List<JoineryInteriorDoorEntry> BuildEstimatedInteriorDoors(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        return drawings
            .Where(drawing => DrawingViewClassifier.Classify(drawing.Classification) == DrawingViewBucket.Plan)
            .Select(drawing => new JoineryInteriorDoorEntry
            {
                Type = "Drzwi wewnętrzne",
                Floor = DrawingViewClassifier.BuildFloorLabel(drawing.Classification),
                CountEstimated = drawing.Openings
                    .Where(opening => opening.IsInterior
                        || opening.Type.Contains("drzwi", StringComparison.OrdinalIgnoreCase))
                    .Sum(opening => opening.Count > 0 ? opening.Count : 1)
            })
            .Where(entry => entry.CountEstimated > 0)
            .ToList();
    }

    private static List<JoineryInteriorDoorEntry> ConsolidateInteriorDoors(List<JoineryInteriorDoorEntry> doors)
    {
        return doors
            .GroupBy(door => $"{door.Type}:{door.Floor}", StringComparer.OrdinalIgnoreCase)
            .Select(group => new JoineryInteriorDoorEntry
            {
                Type = group.First().Type,
                Floor = group.First().Floor,
                CountEstimated = group.Sum(item => item.CountEstimated)
            })
            .ToList();
    }

    private static InstallationsSummary BuildInstallations(
        IReadOnlyList<FloorPlanDrawing> drawings,
        ProjectModel model)
    {
        InstallationsSummary summary = new();

        foreach (FloorPlanDrawing drawing in drawings)
        {
            string sheet = drawing.Classification.SheetNumber ?? string.Empty;

            foreach (DrawingInstallation installation in drawing.Installations)
            {
                string type = installation.Type.ToLowerInvariant();
                if (type.Contains("wod") || type.Contains("kan") || type.Contains("plumbing"))
                {
                    summary.Plumbing ??= new InstallationPlumbingSummary();
                    if (installation.Floors.Count > 0)
                    {
                        summary.Plumbing.Floors = installation.Floors;
                    }

                    if (!string.IsNullOrWhiteSpace(installation.SewageType))
                    {
                        summary.Plumbing.Sewage = new InstallationSewageSummary
                        {
                            Type = installation.SewageType,
                            SourceDrawing = installation.SourceDrawing ?? sheet
                        };
                    }

                    summary.Plumbing.WaterSupply = new InstallationWaterSupplySummary
                    {
                        Type = installation.WaterSupplyType ?? installation.Type,
                        Notes = installation.Notes,
                        SourceDrawing = installation.SourceDrawing ?? sheet
                    };
                }
                else if (type.Contains("elektr"))
                {
                    summary.Electrical = new InstallationElectricalSummary
                    {
                        Type = installation.Type,
                        Notes = installation.Notes,
                        SourceDrawing = installation.SourceDrawing ?? sheet
                    };
                }
                else if (type.Contains("ogrzew") || type == "co")
                {
                    summary.Heating = new InstallationHeatingSummary
                    {
                        Type = installation.Type,
                        RoomNumber = installation.RoomNumber,
                        AreaM2 = installation.AreaM2,
                        Notes = installation.Notes
                    };
                }
                else if (type.Contains("wentyl") || type.Contains("rekuper"))
                {
                    summary.Ventilation = new InstallationVentilationSummary
                    {
                        Type = installation.Type,
                        Notes = installation.Notes,
                        SourceDrawings = installation.SourceDrawings.Count > 0
                            ? installation.SourceDrawings
                            : string.IsNullOrWhiteSpace(sheet) ? [] : [sheet]
                    };
                }
            }

            AppendInstallationsFromDescriptiveText(summary, drawing, sheet);
        }

        if (summary.Plumbing is null && model.Floors.Count > 0)
        {
            summary.Plumbing = new InstallationPlumbingSummary
            {
                Floors = model.Floors
                    .Select(floor => BuildPlumbingFloorDescription(floor))
                    .Where(description => !string.IsNullOrWhiteSpace(description))
                    .ToList()!
            };
        }

        return summary;
    }

    private static void AppendInstallationsFromDescriptiveText(
        InstallationsSummary summary,
        FloorPlanDrawing drawing,
        string sheet)
    {
        string? text = drawing.Classification.DescriptiveText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string normalized = text.ToLowerInvariant();
        if ((normalized.Contains("rekuper") || normalized.Contains("wentylacja mechaniczna"))
            && summary.Ventilation is null)
        {
            summary.Ventilation = new InstallationVentilationSummary
            {
                Type = "Wentylacja mechaniczna (rekuperacja)",
                Notes = text,
                SourceDrawings = string.IsNullOrWhiteSpace(sheet) ? [] : [sheet]
            };
        }
    }

    private static string? BuildPlumbingFloorDescription(ProjectModelFloor floor)
    {
        List<string> sanitaryRooms = floor.Rooms
            .Where(room => RoomCategoryInferrer.Infer(room.Name) is "sanitarne" or "usługowe")
            .Select(room => $"{room.Name} {room.Symbol}")
            .ToList();

        if (sanitaryRooms.Count == 0)
        {
            return string.IsNullOrWhiteSpace(floor.Level) ? null : floor.Level;
        }

        return $"{floor.Level} — {string.Join(", ", sanitaryRooms)}";
    }

    private static List<ValidatedDrawingEntry> BuildValidatedDrawings(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        return drawings
            .OrderBy(drawing => drawing.Classification.SheetNumber ?? drawing.Source.FileName)
            .Select(drawing => new ValidatedDrawingEntry
            {
                SheetNumber = drawing.Classification.SheetNumber,
                DrawingType = drawing.Classification.DrawingType ?? "nieznany",
                Title = drawing.Classification.Title,
                Scale = drawing.Classification.Scale,
                Validated = true,
                HasMaterialTable = drawing.Classification.HasMaterialTable
            })
            .ToList();
    }

    private static List<DrawingDependencyEntry> BuildDrawingDependencies(
        IReadOnlyList<DrawingDependencyLink> dependencies)
    {
        return dependencies
            .Select(link => new DrawingDependencyEntry
            {
                From = link.SourceSheetNumber ?? link.SourceFileName,
                To = link.TargetSheetNumber ?? link.TargetFileName ?? string.Empty,
                Relation = string.IsNullOrWhiteSpace(link.ReferenceLabel)
                    ? link.Notes ?? link.DetailType
                    : link.ReferenceLabel
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.From) && !string.IsNullOrWhiteSpace(entry.To))
            .ToList();
    }

    private static List<string> CollectSheetNumbers(
        IReadOnlyList<FloorPlanDrawing> drawings,
        params string[] keywords)
    {
        return drawings
            .Select(drawing => drawing.Classification.SheetNumber)
            .Where(sheet => !string.IsNullOrWhiteSpace(sheet))
            .Where(sheet => keywords.Any(keyword =>
                Normalize(sheet!).Contains(Normalize(keyword), StringComparison.Ordinal)
                || drawings.Any(drawing =>
                    drawing.Classification.SheetNumber == sheet
                    && (Normalize(drawing.Classification.DrawingType).Contains(Normalize(keyword), StringComparison.Ordinal)
                        || Normalize(drawing.Classification.Title).Contains(Normalize(keyword), StringComparison.Ordinal)))))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    private static string? FindSheetNumber(IReadOnlyList<FloorPlanDrawing> drawings, params string[] keywords)
    {
        FloorPlanDrawing? match = drawings.FirstOrDefault(drawing =>
            keywords.Any(keyword =>
                Normalize(drawing.Classification.DrawingType).Contains(Normalize(keyword), StringComparison.Ordinal)
                || Normalize(drawing.Classification.Title).Contains(Normalize(keyword), StringComparison.Ordinal)));

        return match?.Classification.SheetNumber;
    }

    private static string ResolveProjectModelRoomNumber(ProjectModelRoom room)
    {
        if (!string.IsNullOrWhiteSpace(room.Symbol))
        {
            return room.Symbol.Trim();
        }

        return string.Empty;
    }

    private static string ResolveRoomNumber(Room room)
    {
        if (!string.IsNullOrWhiteSpace(room.Number))
        {
            return room.Number.Trim();
        }

        if (!string.IsNullOrWhiteSpace(room.Symbol))
        {
            return room.Symbol.Trim();
        }

        return string.Empty;
    }

    private static List<WallFinishSummary> DeduplicateFinishes(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        Dictionary<string, WallFinishSummary> merged = new(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlanDrawing drawing in drawings)
        {
            if (drawing.Elevation is null || drawing.Elevation.Finishes.Count == 0)
            {
                continue;
            }

            string sourceDrawing = drawing.Classification.SheetNumber
                ?? DrawingSheetNumberInferrer.InferFromFileName(drawing.Source.FileName)
                ?? drawing.Elevation.Title
                ?? drawing.Source.FileName;

            foreach (ElevationFinish finish in drawing.Elevation.Finishes)
            {
                string zone = finish.Zone ?? string.Empty;
                string material = finish.Material ?? string.Empty;
                string color = finish.Color ?? string.Empty;
                string key = $"{zone}|{material}|{color}";

                if (!merged.TryGetValue(key, out WallFinishSummary? existing))
                {
                    merged[key] = new WallFinishSummary
                    {
                        Zone = zone,
                        Material = material,
                        Color = string.IsNullOrWhiteSpace(color) ? null : color,
                        SourceDrawings = [sourceDrawing]
                    };
                    continue;
                }

                if (!existing.SourceDrawings.Contains(sourceDrawing, StringComparer.OrdinalIgnoreCase))
                {
                    existing.SourceDrawings.Add(sourceDrawing);
                }
            }
        }

        return merged.Values
            .OrderBy(finish => finish.Zone, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finish => finish.Material, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant()
            .Replace('ł', 'l')
            .Replace('ó', 'o')
            .Replace('ą', 'a')
            .Replace('ę', 'e')
            .Replace('ś', 's')
            .Replace('ć', 'c')
            .Replace('ń', 'n')
            .Replace('ź', 'z')
            .Replace('ż', 'z');
    }
}
