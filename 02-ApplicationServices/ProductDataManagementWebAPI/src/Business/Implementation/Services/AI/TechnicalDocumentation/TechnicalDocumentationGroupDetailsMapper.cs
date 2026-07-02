using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TechnicalDocumentationGroupDetailsMapper
{
    public static void Apply(
        ProjectTechnicalDocumentationDetails details,
        ProjectModel projectModel,
        MaterialSchedule? materialSchedule)
    {
        details.Project = new ProjectInfo
        {
            Name = projectModel.Project.Name ?? string.Empty,
            Address = projectModel.Project.Address,
            Location = projectModel.Project.Location,
            Investor = projectModel.Project.Investor,
            Designer = projectModel.Project.Author,
            Date = projectModel.Project.Date,
            Phase = projectModel.Project.Phase,
        };

        details.Rooms = projectModel.Floors
            .OrderBy(floor => floor.Order)
            .Select(floor => new RoomFloorGroup
            {
                Floor = floor.Level,
                FloorOrder = floor.Order,
                TotalAreaM2 = floor.TotalAreaM2,
                AreaNotes = floor.AreaNotes,
                Items = floor.Rooms
                    .Select(room => new RoomFloorItem
                    {
                        Number = room.Symbol ?? string.Empty,
                        Name = room.Name,
                        AreaM2 = room.AreaM2 ?? 0,
                        Category = room.Category,
                        Notes = room.Notes,
                    })
                    .ToList(),
            })
            .ToList();

        details.TotalAreaM2 = projectModel.Floors.Sum(floor => floor.TotalAreaM2 ?? 0);

        if (materialSchedule is not null)
        {
            details.MaterialSchedule = DetailsMaterialScheduleMapper.Map(materialSchedule, projectModel, []);
        }

        if (projectModel.Roof.PitchDegrees is not null || projectModel.Roof.AreaM2 is not null)
        {
            details.Roof = new RoofSummary
            {
                PitchDegrees = projectModel.Roof.PitchDegrees,
                AreaM2 = projectModel.Roof.AreaM2,
                CoveringType = projectModel.Roof.CoveringType,
            };
        }
    }
}
