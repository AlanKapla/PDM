using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TechnicalDocumentationDeterministicAuditor
{
    private const double AreaTolerancePercent = 0.05;
    private const double QuantityTolerance = 0.05;

    public static AuditResult Audit(ProjectModel projectModel, MaterialSchedule? materialSchedule)
    {
        AuditResult result = new();

        AuditFloorAreas(projectModel, result);
        if (materialSchedule is not null)
        {
            AuditMaterialQuantities(materialSchedule, result);
            AuditMaterialCalculations(materialSchedule, result);
        }

        return result;
    }

    private static void AuditFloorAreas(ProjectModel projectModel, AuditResult result)
    {
        foreach (ProjectModelFloor floor in projectModel.Floors)
        {
            if (!floor.TotalAreaM2.HasValue || floor.TotalAreaM2.Value <= 0)
            {
                continue;
            }

            double roomsSum = floor.Rooms.Sum(room => room.AreaM2 ?? 0);
            if (roomsSum <= 0)
            {
                continue;
            }

            double totalArea = floor.TotalAreaM2.Value;
            double deviation = Math.Abs(roomsSum - totalArea) / totalArea;
            if (deviation > AreaTolerancePercent)
            {
                result.Warnings.Add(
                    $"Floor '{floor.Level}': sum of room areas ({roomsSum:0.##} m2) differs from totalAreaM2 "
                    + $"({totalArea:0.##} m2) by more than {AreaTolerancePercent * 100:0}%.");
            }
        }
    }

    private static void AuditMaterialQuantities(MaterialSchedule schedule, AuditResult result)
    {
        foreach (MaterialItem item in EnumerateMaterialItems(schedule))
        {
            if (item.WastePercent <= 0 || item.NetQuantity <= 0)
            {
                continue;
            }

            double expectedGross = item.NetQuantity * (1 + item.WastePercent / 100);
            if (Math.Abs(item.GrossQuantity - expectedGross) > QuantityTolerance)
            {
                result.Warnings.Add(
                    $"Material '{item.Element}': gross ({item.GrossQuantity:0.##}) does not match "
                    + $"net × (1 + waste/100) = {expectedGross:0.##}.");
            }
        }
    }

    private static void AuditMaterialCalculations(MaterialSchedule schedule, AuditResult result)
    {
        foreach (MaterialItem item in EnumerateMaterialItems(schedule))
        {
            bool isCalculated = item.NetQuantity > 0 || item.GrossQuantity > 0;
            if (!isCalculated)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Calculation))
            {
                result.Warnings.Add(
                    $"Material '{item.Element}' has quantities but empty calculation field.");
            }
        }
    }

    private static IEnumerable<MaterialItem> EnumerateMaterialItems(MaterialSchedule schedule)
    {
        foreach (MaterialItem item in schedule.Masonry) { yield return item; }
        foreach (MaterialItem item in schedule.Insulation) { yield return item; }
        foreach (MaterialItem item in schedule.Concrete) { yield return item; }
        foreach (MaterialItem item in schedule.Steel) { yield return item; }
        foreach (MaterialItem item in schedule.Timber) { yield return item; }
        foreach (MaterialItem item in schedule.Roofing) { yield return item; }
        foreach (MaterialItem item in schedule.Finishes) { yield return item; }
        foreach (MaterialItem item in schedule.Foundations.Concrete) { yield return item; }
        foreach (MaterialItem item in schedule.Foundations.Steel) { yield return item; }
        foreach (MaterialItem item in schedule.Foundations.Blocks) { yield return item; }
        foreach (MaterialItem item in schedule.Walls.Masonry) { yield return item; }
        foreach (MaterialItem item in schedule.Walls.Mortar) { yield return item; }
        foreach (MaterialItem item in schedule.Walls.Insulation) { yield return item; }
        foreach (MaterialItem item in schedule.Ceilings.Concrete) { yield return item; }
        foreach (MaterialItem item in schedule.Ceilings.Steel) { yield return item; }
        foreach (MaterialItem item in schedule.Columns.Concrete) { yield return item; }
        foreach (MaterialItem item in schedule.Columns.Steel) { yield return item; }
        foreach (MaterialItem item in schedule.Roof.Timber) { yield return item; }
        foreach (MaterialItem item in schedule.Roof.Covering) { yield return item; }
    }
}
