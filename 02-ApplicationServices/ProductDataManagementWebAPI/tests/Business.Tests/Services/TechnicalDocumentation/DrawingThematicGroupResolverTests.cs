using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class DrawingThematicGroupResolverTests
{
    [Fact]
    public void Resolve_detaleKonstrukcyjne_assignsReinforcementAndFoundations()
    {
        DrawingThematicGroupResolver resolver = CreateResolver();
        ClassifiedTechnicalDocumentationImage k06 = CreateClassifiedImage(
            "K-06.pdf",
            TechnicalDocumentationOptions.DrawingTypes.DetaleKonstrukcyjne,
            "K-06");

        IReadOnlyList<ThematicDrawingGroup> groups = resolver.Resolve([k06]);

        groups.Should().Contain(group => group.GroupName == TechnicalDocumentationOptions.ThematicGroups.Reinforcement);
        groups.Should().Contain(group => group.GroupName == TechnicalDocumentationOptions.ThematicGroups.Foundations);

        ThematicDrawingGroup reinforcement = groups.Single(group =>
            group.GroupName == TechnicalDocumentationOptions.ThematicGroups.Reinforcement);
        ThematicDrawingGroup foundations = groups.Single(group =>
            group.GroupName == TechnicalDocumentationOptions.ThematicGroups.Foundations);

        reinforcement.Images.Should().ContainSingle();
        foundations.Images.Should().ContainSingle();
        reinforcement.Images[0].Image.FileName.Should().Be("K-06.pdf");
    }

    [Fact]
    public void Resolve_allConfiguredDrawingTypes_mapToAtLeastOneGroup()
    {
        DrawingThematicGroupResolver resolver = CreateResolver();
        IReadOnlyDictionary<string, string[]> mapping =
            TechnicalDocumentationOptions.CreateDefaultDrawingTypeToThematicGroups();

        foreach (KeyValuePair<string, string[]> entry in mapping)
        {
            ClassifiedTechnicalDocumentationImage image = CreateClassifiedImage(
                $"{entry.Key}.pdf",
                entry.Key,
                entry.Key);

            IReadOnlyList<ThematicDrawingGroup> groups = resolver.Resolve([image]);
            groups.Should().NotBeEmpty($"drawingType {entry.Key} should map to a group");
        }
    }

    private static DrawingThematicGroupResolver CreateResolver()
    {
        IOptions<TechnicalDocumentationOptions> options = Options.Create(new TechnicalDocumentationOptions());
        return new DrawingThematicGroupResolver(options);
    }

    private static ClassifiedTechnicalDocumentationImage CreateClassifiedImage(
        string fileName,
        string drawingType,
        string sheetNumber)
    {
        return new ClassifiedTechnicalDocumentationImage
        {
            Image = new TechnicalDocumentationImageInput([], fileName, 1),
            Classification = new DrawingClassification
            {
                DrawingType = drawingType,
                SheetNumber = sheetNumber,
            },
        };
    }
}
