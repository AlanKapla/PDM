using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Validation;

namespace Business.Implementation.Helpers.JsonConverters;

public sealed class FloorPlanDrawingJsonConverter : JsonConverter<FloorPlanDrawing>
{
    public override FloorPlanDrawing Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new FloorPlanDrawing();
        }

        JsonElement root;
        try
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return new FloorPlanDrawing();
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return new FloorPlanDrawing();
        }

        FloorPlanDrawing drawing = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "id", out JsonElement idElement))
        {
            drawing.Id = JsonParsingHelpers.ReadString(idElement, Guid.NewGuid().ToString());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "source", out JsonElement sourceElement)
            && sourceElement.ValueKind == JsonValueKind.Object)
        {
            drawing.Source = ParseSource(sourceElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "classification", out JsonElement classificationElement)
            && classificationElement.ValueKind == JsonValueKind.Object)
        {
            drawing.Classification = ParseClassification(classificationElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "rooms", out JsonElement roomsElement))
        {
            drawing.Rooms = JsonParsingHelpers.ReadList(roomsElement, ParseRoom);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "walls", out JsonElement wallsElement))
        {
            drawing.Walls = JsonParsingHelpers.ReadList(wallsElement, ParseWall);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "openings", out JsonElement openingsElement))
        {
            drawing.Openings = JsonParsingHelpers.ReadList(openingsElement, ParseOpening);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "totalAreaM2", out JsonElement totalAreaElement))
        {
            drawing.TotalAreaM2 = JsonParsingHelpers.ReadNullableDouble(totalAreaElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "areaNotes", out JsonElement areaNotesElement))
        {
            drawing.AreaNotes = JsonParsingHelpers.ReadString(areaNotesElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "externalDimensions", out JsonElement externalDimensionsElement)
            && externalDimensionsElement.ValueKind == JsonValueKind.Object)
        {
            drawing.ExternalDimensions = ReadSection<DrawingExternalDimensions>(externalDimensionsElement, options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "columns", out JsonElement columnsElement))
        {
            drawing.Columns = JsonParsingHelpers.ReadList(columnsElement, element => ParseStructuralColumn(element));
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "beams", out JsonElement beamsElement))
        {
            drawing.Beams = JsonParsingHelpers.ReadList(beamsElement, element =>
                ReadSection<StructuralBeam>(element, options));
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "lintels", out JsonElement lintelsElement))
        {
            drawing.Lintels = JsonParsingHelpers.ReadList(lintelsElement, element =>
                ReadSection<StructuralLintel>(element, options));
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "foundations", out JsonElement foundationsElement))
        {
            drawing.Foundations = ReadFoundationSection(foundationsElement, options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "floors", out JsonElement floorsElement))
        {
            drawing.Floors = ReadFloorSection(floorsElement, options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "roof", out JsonElement roofElement))
        {
            drawing.Roof = ReadRoofSection(roofElement, options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "site", out JsonElement siteElement)
            && siteElement.ValueKind == JsonValueKind.Object)
        {
            drawing.Site = ReadSection<SitePlanSection>(siteElement, options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "section", out JsonElement sectionElement)
            && sectionElement.ValueKind == JsonValueKind.Object)
        {
            drawing.Section = ReadSection<SectionDrawingData>(sectionElement, options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "elevation", out JsonElement elevationElement)
            && elevationElement.ValueKind == JsonValueKind.Object)
        {
            drawing.Elevation = ReadSection<ElevationDrawingData>(elevationElement, options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "details", out JsonElement detailsElement))
        {
            drawing.Details = JsonParsingHelpers.ReadList(detailsElement, ParseStructuralDetail);
        }

        ApplyRootAliases(root, drawing, options);

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "installations", out JsonElement installationsElement))
        {
            drawing.Installations = JsonSerializer.Deserialize<List<DrawingInstallation>>(
                installationsElement.GetRawText(),
                options) ?? new List<DrawingInstallation>();
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "interiorDoors", out JsonElement interiorDoorsElement))
        {
            drawing.InteriorDoors = JsonParsingHelpers.ReadList(interiorDoorsElement, element =>
                ReadSection<InteriorDoorEntry>(element, options));
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "textSources", out JsonElement textSourcesElement)
            && textSourcesElement.ValueKind == JsonValueKind.Object)
        {
            drawing.TextSources = ParseTextSources(textSourcesElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "crossReferences", out JsonElement crossReferencesElement))
        {
            drawing.CrossReferences = JsonParsingHelpers.ReadList(crossReferencesElement, ParseCrossReference);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "deferredDetails", out JsonElement deferredElement))
        {
            drawing.DeferredDetails = JsonParsingHelpers.ReadList(deferredElement, ParseDeferredDetail);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "validationReport", out JsonElement validationElement)
            && validationElement.ValueKind == JsonValueKind.Object)
        {
            drawing.ValidationReport = JsonSerializer.Deserialize<ValidationReport>(
                validationElement.GetRawText(),
                options);
        }

        return drawing;
    }

    public override void Write(Utf8JsonWriter writer, FloorPlanDrawing value, JsonSerializerOptions options)
    {
        JsonConverterWriteHelper.SerializeWithoutConverter<FloorPlanDrawing, FloorPlanDrawingJsonConverter>(writer, value, options);
    }

    private static TSection? ReadSection<TSection>(JsonElement element, JsonSerializerOptions options)
        where TSection : class
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return JsonSerializer.Deserialize<TSection>(element.GetRawText(), options);
    }

    private static FoundationSection? ReadFoundationSection(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            FoundationSection section = new();
            section.Footings = JsonParsingHelpers.ReadList(element, item => ReadSection<FootingDetail>(item, options));
            return section.Footings.Count > 0 ? section : null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        FoundationSection? foundations = ReadSection<FoundationSection>(element, options);
        if (foundations is null)
        {
            return null;
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "concrete", out JsonElement concreteElement)
            && concreteElement.ValueKind == JsonValueKind.String
            && string.IsNullOrWhiteSpace(foundations.ConcreteClass))
        {
            foundations.ConcreteClass = JsonParsingHelpers.ReadString(concreteElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "steel", out JsonElement steelElement)
            && steelElement.ValueKind == JsonValueKind.String
            && string.IsNullOrWhiteSpace(foundations.SteelSpecification))
        {
            foundations.SteelSpecification = JsonParsingHelpers.ReadString(steelElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "foundationWalls", out JsonElement wallElement)
            && wallElement.ValueKind == JsonValueKind.Object
            && foundations.FoundationWall is null)
        {
            foundations.FoundationWall = ReadSection<FoundationWallDetail>(wallElement, options);
        }

        return foundations;
    }

    private static FloorSection? ReadFloorSection(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        FloorSection? floors = ReadSection<FloorSection>(element, options);
        if (floors is null)
        {
            return null;
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "bars", out JsonElement barsElement)
            && floors.Bars.Count == 0)
        {
            floors.Bars = JsonParsingHelpers.ReadList(barsElement, item => ReadSection<RebarBar>(item, options));
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "totalMassKg", out JsonElement totalMassElement)
            && floors.TotalMassKg is null)
        {
            floors.TotalMassKg = JsonParsingHelpers.ReadNullableDouble(totalMassElement);
        }

        return floors;
    }

    private static RoofSection? ReadRoofSection(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        RoofSection? roof = ReadSection<RoofSection>(element, options);
        if (roof is null)
        {
            return null;
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "timberGroups", out JsonElement groupsElement)
            && roof.TimberGroups.Count == 0)
        {
            roof.TimberGroups = JsonParsingHelpers.ReadList(groupsElement, item => ReadSection<TimberGroup>(item, options));
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "totalVolumeM3", out JsonElement volumeElement)
            && roof.TotalVolumeM3 is null)
        {
            roof.TotalVolumeM3 = JsonParsingHelpers.ReadNullableDouble(volumeElement);
        }

        return roof;
    }

    private static void ApplyRootAliases(
        JsonElement root,
        FloorPlanDrawing drawing,
        JsonSerializerOptions options)
    {
        if (drawing.Site is null
            && JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "plotAreaM2", out _))
        {
            drawing.Site = ReadSection<SitePlanSection>(root, options);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "pads", out JsonElement padsElement))
        {
            drawing.Foundations ??= new FoundationSection();
            if (drawing.Foundations.Pads.Count == 0)
            {
                drawing.Foundations.Pads = JsonParsingHelpers.ReadList(padsElement, item =>
                    ReadSection<PadDetail>(item, options));
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "footings", out JsonElement footingsElement))
        {
            drawing.Foundations ??= new FoundationSection();
            if (drawing.Foundations.Footings.Count == 0)
            {
                drawing.Foundations.Footings = JsonParsingHelpers.ReadList(footingsElement, item =>
                    ReadSection<FootingDetail>(item, options));
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "timberGroups", out JsonElement timberGroupsElement))
        {
            drawing.Roof ??= new RoofSection();
            if (drawing.Roof.TimberGroups.Count == 0)
            {
                drawing.Roof.TimberGroups = JsonParsingHelpers.ReadList(timberGroupsElement, item =>
                    ReadSection<TimberGroup>(item, options));
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "totalVolumeM3", out JsonElement totalVolumeElement))
        {
            drawing.Roof ??= new RoofSection();
            if (drawing.Roof.TotalVolumeM3 is null)
            {
                drawing.Roof.TotalVolumeM3 = JsonParsingHelpers.ReadNullableDouble(totalVolumeElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "bars", out JsonElement barsElement))
        {
            drawing.Floors ??= new FloorSection();
            if (drawing.Floors.Bars.Count == 0)
            {
                drawing.Floors.Bars = JsonParsingHelpers.ReadList(barsElement, item =>
                    ReadSection<RebarBar>(item, options));
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "totalMassKg", out JsonElement totalMassElement))
        {
            drawing.Floors ??= new FloorSection();
            if (drawing.Floors.TotalMassKg is null)
            {
                drawing.Floors.TotalMassKg = JsonParsingHelpers.ReadNullableDouble(totalMassElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "concrete", out JsonElement concreteElement)
            && concreteElement.ValueKind == JsonValueKind.String)
        {
            drawing.Foundations ??= new FoundationSection();
            if (string.IsNullOrWhiteSpace(drawing.Foundations.ConcreteClass))
            {
                drawing.Foundations.ConcreteClass = JsonParsingHelpers.ReadString(concreteElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "steel", out JsonElement steelElement)
            && steelElement.ValueKind == JsonValueKind.String)
        {
            drawing.Foundations ??= new FoundationSection();
            if (string.IsNullOrWhiteSpace(drawing.Foundations.SteelSpecification))
            {
                drawing.Foundations.SteelSpecification = JsonParsingHelpers.ReadString(steelElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "coverageMm", out JsonElement coverageElement))
        {
            drawing.Foundations ??= new FoundationSection();
            if (drawing.Foundations.CoverageMm is null)
            {
                drawing.Foundations.CoverageMm = JsonParsingHelpers.ReadNullableInt(coverageElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "foundationLevelM", out JsonElement levelElement))
        {
            drawing.Foundations ??= new FoundationSection();
            if (drawing.Foundations.FoundationLevelM is null)
            {
                drawing.Foundations.FoundationLevelM = JsonParsingHelpers.ReadNullableDouble(levelElement);
            }
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(root, "foundationWalls", out JsonElement foundationWallElement)
            && foundationWallElement.ValueKind == JsonValueKind.Object)
        {
            drawing.Foundations ??= new FoundationSection();
            if (drawing.Foundations.FoundationWall is null)
            {
                drawing.Foundations.FoundationWall = ReadSection<FoundationWallDetail>(foundationWallElement, options);
            }
        }
    }

    private static DrawingSource ParseSource(JsonElement element)
    {
        DrawingSource source = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "fileName", out JsonElement fileNameElement))
        {
            source.FileName = JsonParsingHelpers.ReadString(fileNameElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "pageNumber", out JsonElement pageNumberElement))
        {
            source.PageNumber = JsonParsingHelpers.ReadInt(pageNumberElement);
        }

        return source;
    }

    private static DrawingClassification ParseClassification(JsonElement element)
    {
        DrawingClassification classification = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "drawingType", out JsonElement drawingTypeElement))
        {
            classification.DrawingType = JsonParsingHelpers.ReadString(drawingTypeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "scale", out JsonElement scaleElement))
        {
            classification.Scale = ScaleParsingHelper.ReadScale(scaleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "sheetNumber", out JsonElement sheetNumberElement))
        {
            classification.SheetNumber = JsonParsingHelpers.ReadString(sheetNumberElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "title", out JsonElement titleElement))
        {
            classification.Title = JsonParsingHelpers.ReadString(titleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "revision", out JsonElement revisionElement))
        {
            classification.Revision = JsonParsingHelpers.ReadString(revisionElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "descriptiveText", out JsonElement descriptiveElement))
        {
            classification.DescriptiveText = JsonParsingHelpers.ReadString(descriptiveElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "drawingTable", out JsonElement tableElement))
        {
            classification.DrawingTable = JsonParsingHelpers.ReadString(tableElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "relatedDrawings", out JsonElement relatedElement))
        {
            classification.RelatedDrawings = JsonParsingHelpers.ReadList(relatedElement, ParseRelatedDrawing);
        }

        return classification;
    }

    private static RelatedDrawingRef ParseRelatedDrawing(JsonElement element)
    {
        RelatedDrawingRef reference = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "referenceLabel", out JsonElement labelElement))
        {
            reference.ReferenceLabel = JsonParsingHelpers.ReadString(labelElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "targetSheetNumber", out JsonElement sheetElement))
        {
            reference.TargetSheetNumber = JsonParsingHelpers.ReadString(sheetElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "targetTitle", out JsonElement titleElement))
        {
            reference.TargetTitle = JsonParsingHelpers.ReadString(titleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "detailType", out JsonElement detailElement))
        {
            reference.DetailType = JsonParsingHelpers.ReadString(detailElement);
        }

        return reference;
    }

    private static StructuralColumn? ParseStructuralColumn(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        StructuralColumn column = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "id", out JsonElement idElement))
        {
            column.Id = JsonParsingHelpers.ReadString(idElement, Guid.NewGuid().ToString());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "symbol", out JsonElement symbolElement))
        {
            column.Symbol = JsonParsingHelpers.ReadString(symbolElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "bCm", out JsonElement bElement))
        {
            column.BCm = JsonParsingHelpers.ReadNullableDouble(bElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "hCm", out JsonElement hElement))
        {
            column.HCm = JsonParsingHelpers.ReadNullableDouble(hElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "heightM", out JsonElement heightElement))
        {
            column.HeightM = JsonParsingHelpers.ReadNullableDouble(heightElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "concreteClass", out JsonElement concreteElement))
        {
            column.ConcreteClass = JsonParsingHelpers.ReadString(concreteElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "longitudinalBars", out JsonElement barsElement))
        {
            column.LongitudinalBars = JsonParsingHelpers.ReadFlexibleString(barsElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "reinforcement", out JsonElement reinforcementElement))
        {
            column.LongitudinalBars ??= JsonParsingHelpers.ReadFlexibleString(reinforcementElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "stirrups", out JsonElement stirrupsElement))
        {
            column.Stirrups = JsonParsingHelpers.ReadFlexibleString(stirrupsElement);
        }

        return string.IsNullOrWhiteSpace(column.Symbol)
            && column.BCm is null
            && column.HCm is null
            && string.IsNullOrWhiteSpace(column.LongitudinalBars)
            ? null
            : column;
    }

    private static StructuralDetail? ParseStructuralDetail(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        StructuralDetail detail = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "title", out JsonElement titleElement))
        {
            detail.Title = JsonParsingHelpers.ReadString(titleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "reinforcement", out JsonElement reinforcementElement))
        {
            detail.Reinforcement = JsonParsingHelpers.ReadFlexibleString(reinforcementElement);
        }

        return string.IsNullOrWhiteSpace(detail.Title) && string.IsNullOrWhiteSpace(detail.Reinforcement)
            ? null
            : detail;
    }

    private static Room? ParseRoom(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        Room room = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "id", out JsonElement idElement))
        {
            room.Id = JsonParsingHelpers.ReadString(idElement, Guid.NewGuid().ToString());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "name", out JsonElement nameElement))
        {
            room.Name = JsonParsingHelpers.ReadString(nameElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "symbol", out JsonElement symbolElement))
        {
            room.Symbol = JsonParsingHelpers.ReadString(symbolElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "number", out JsonElement numberElement))
        {
            room.Number = JsonParsingHelpers.ReadString(numberElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "dimensions", out JsonElement dimensionsElement)
            && dimensionsElement.ValueKind == JsonValueKind.Object)
        {
            ApplyRoomMeasurements(room, dimensionsElement);
        }
        else
        {
            ApplyRoomMeasurements(room, element);
        }

        return room;
    }

    private static void ApplyRoomMeasurements(Room room, JsonElement element)
    {
        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "widthM", out JsonElement widthElement))
        {
            room.WidthM = JsonParsingHelpers.ReadDouble(widthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "lengthM", out JsonElement lengthElement))
        {
            room.LengthM = JsonParsingHelpers.ReadDouble(lengthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "heightM", out JsonElement heightElement))
        {
            room.HeightM = JsonParsingHelpers.ReadNullableDouble(heightElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "areaM2", out JsonElement areaElement))
        {
            room.AreaM2 = JsonParsingHelpers.ReadDouble(areaElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "category", out JsonElement categoryElement))
        {
            room.Category = JsonParsingHelpers.ReadString(categoryElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "notes", out JsonElement notesElement))
        {
            room.Notes = JsonParsingHelpers.ReadString(notesElement);
        }
    }

    private static Wall? ParseWall(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        Wall wall = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "id", out JsonElement idElement))
        {
            wall.Id = JsonParsingHelpers.ReadString(idElement, Guid.NewGuid().ToString());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "type", out JsonElement typeElement))
        {
            wall.Type = JsonParsingHelpers.ReadString(typeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "symbol", out JsonElement symbolElement))
        {
            wall.Symbol = JsonParsingHelpers.ReadString(symbolElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "lengthM", out JsonElement lengthElement))
        {
            wall.LengthM = JsonParsingHelpers.ReadDouble(lengthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "thicknessCm", out JsonElement thicknessElement))
        {
            wall.ThicknessCm = JsonParsingHelpers.ReadDouble(thicknessElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "grossAreaM2", out JsonElement grossAreaElement))
        {
            wall.GrossAreaM2 = JsonParsingHelpers.ReadNullableDouble(grossAreaElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "netAreaM2", out JsonElement netAreaElement))
        {
            wall.NetAreaM2 = JsonParsingHelpers.ReadNullableDouble(netAreaElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "layers", out JsonElement layersElement))
        {
            wall.Layers = JsonParsingHelpers.ReadList(layersElement, ParseWallLayer);
        }

        return wall;
    }

    private static WallLayer? ParseWallLayer(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        WallLayer layer = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "material", out JsonElement materialElement))
        {
            layer.Material = JsonParsingHelpers.ReadString(materialElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "thicknessCm", out JsonElement thicknessElement))
        {
            layer.ThicknessCm = JsonParsingHelpers.ReadDouble(thicknessElement);
        }

        return layer;
    }

    private static Opening? ParseOpening(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        Opening opening = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "id", out JsonElement idElement))
        {
            opening.Id = JsonParsingHelpers.ReadString(idElement, Guid.NewGuid().ToString());
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "type", out JsonElement typeElement))
        {
            opening.Type = JsonParsingHelpers.ReadString(typeElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "symbol", out JsonElement symbolElement))
        {
            opening.Symbol = JsonParsingHelpers.ReadString(symbolElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "widthCm", out JsonElement widthElement))
        {
            opening.WidthCm = JsonParsingHelpers.ReadDouble(widthElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "heightCm", out JsonElement heightElement))
        {
            opening.HeightCm = JsonParsingHelpers.ReadDouble(heightElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "count", out JsonElement countElement))
        {
            opening.Count = JsonParsingHelpers.ReadInt(countElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "wallId", out JsonElement wallIdElement))
        {
            opening.WallId = JsonParsingHelpers.ReadString(wallIdElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "material", out JsonElement materialElement))
        {
            opening.Material = JsonParsingHelpers.ReadString(materialElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "location", out JsonElement locationElement))
        {
            opening.Location = JsonParsingHelpers.ReadString(locationElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "isInterior", out JsonElement interiorElement))
        {
            opening.IsInterior = JsonParsingHelpers.ReadBool(interiorElement);
        }

        return opening;
    }

    private static DrawingTextSources ParseTextSources(JsonElement element)
    {
        DrawingTextSources sources = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "descriptiveText", out JsonElement descriptiveElement))
        {
            sources.DescriptiveText = JsonParsingHelpers.ReadString(descriptiveElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "drawingTable", out JsonElement tableElement))
        {
            sources.DrawingTable = JsonParsingHelpers.ReadString(tableElement);
        }

        return sources;
    }

    private static DrawingCrossReference ParseCrossReference(JsonElement element)
    {
        DrawingCrossReference reference = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "referenceLabel", out JsonElement labelElement))
        {
            reference.ReferenceLabel = JsonParsingHelpers.ReadString(labelElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "targetSheetNumber", out JsonElement sheetElement))
        {
            reference.TargetSheetNumber = JsonParsingHelpers.ReadString(sheetElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "targetTitle", out JsonElement titleElement))
        {
            reference.TargetTitle = JsonParsingHelpers.ReadString(titleElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "detailType", out JsonElement detailElement))
        {
            reference.DetailType = JsonParsingHelpers.ReadString(detailElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "notes", out JsonElement notesElement))
        {
            reference.Notes = JsonParsingHelpers.ReadString(notesElement);
        }

        return reference;
    }

    private static DeferredDetailNote ParseDeferredDetail(JsonElement element)
    {
        DeferredDetailNote note = new();

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "topic", out JsonElement topicElement))
        {
            note.Topic = JsonParsingHelpers.ReadString(topicElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "targetReference", out JsonElement targetElement))
        {
            note.TargetReference = JsonParsingHelpers.ReadString(targetElement);
        }

        if (JsonParsingHelpers.TryGetPropertyIgnoreCase(element, "notes", out JsonElement notesElement))
        {
            note.Notes = JsonParsingHelpers.ReadString(notesElement);
        }

        return note;
    }
}
