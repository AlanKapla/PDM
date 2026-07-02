using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationDeterministicAuditorTests
{
    [Fact]
    public void Audit_whenRoomAreasExceedTolerance_addsWarning()
    {
        // Arrange
        ProjectModel projectModel = new()
        {
            Floors =
            [
                new ProjectModelFloor
                {
                    Level = "Parter",
                    TotalAreaM2 = 100,
                    Rooms =
                    [
                        new ProjectModelRoom { Name = "Salon", AreaM2 = 50 },
                        new ProjectModelRoom { Name = "Kuchnia", AreaM2 = 30 }
                    ]
                }
            ]
        };

        // Act
        AuditResult result = TechnicalDocumentationDeterministicAuditor.Audit(projectModel, materialSchedule: null);

        // Assert
        result.Warnings.Should().ContainSingle(w => w.Contains("Parter"));
    }

    [Fact]
    public void Audit_whenGrossMatchesNetAndWaste_hasNoQuantityWarning()
    {
        // Arrange
        MaterialSchedule schedule = new()
        {
            Masonry =
            [
                new MaterialItem
                {
                    Element = "bloczek",
                    NetQuantity = 100,
                    WastePercent = 10,
                    GrossQuantity = 110,
                    Calculation = "38mb × 2.8m",
                    Unit = "m2"
                }
            ]
        };

        // Act
        AuditResult result = TechnicalDocumentationDeterministicAuditor.Audit(new ProjectModel(), schedule);

        // Assert
        result.Warnings.Should().NotContain(w => w.Contains("gross"));
    }

    [Fact]
    public void Audit_whenCalculatedItemMissingCalculation_addsWarning()
    {
        // Arrange
        MaterialSchedule schedule = new()
        {
            Concrete =
            [
                new MaterialItem
                {
                    Element = "lawa",
                    NetQuantity = 12,
                    GrossQuantity = 12,
                    Unit = "m3",
                    Calculation = string.Empty
                }
            ]
        };

        // Act
        AuditResult result = TechnicalDocumentationDeterministicAuditor.Audit(new ProjectModel(), schedule);

        // Assert
        result.Warnings.Should().ContainSingle(w => w.Contains("empty calculation"));
    }
}
