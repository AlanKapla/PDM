using Business.Implementation.Validators;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentAssertions;

namespace Business.Tests.Validators;

public class CostEstimateGroupValidatorTests
{
    private readonly CostEstimateGroupValidator _sut = new();

    // ─── ValidateGroupHierarchy ───────────────────────────────────────────────

    [Fact]
    public void ValidateGroupHierarchy_NullTemplate_ReturnsInvalid()
    {
        // Arrange
        List<CostEstimateGroup> groups = [];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(null!, groups);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("Template not found"));
    }

    [Fact]
    public void ValidateGroupHierarchy_EmptyGroups_ReturnsValid()
    {
        // Arrange
        CostEstimateTemplate template = new() { CanBranchGroups = true };
        List<CostEstimateGroup> groups = [];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateGroupHierarchy_MaxGroupLevelExceeded_ReturnsInvalid()
    {
        // Arrange
        CostEstimateTemplate template = new() { MaxGroupLevel = 1, CanBranchGroups = true };
        List<CostEstimateGroup> groups =
        [
            new() { Id = Guid.NewGuid(), Level = 0 },
            new() { Id = Guid.NewGuid(), Level = 1 },
            new() { Id = Guid.NewGuid(), Level = 2 },  // exceeds MaxGroupLevel = 1
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("exceeds maximum allowed level"));
    }

    [Fact]
    public void ValidateGroupHierarchy_MaxGroupLevelNotExceeded_ReturnsValid()
    {
        // Arrange
        Guid parentId = Guid.NewGuid();
        CostEstimateTemplate template = new() { MaxGroupLevel = 2, CanBranchGroups = true };
        List<CostEstimateGroup> groups =
        [
            new() { Id = parentId, Level = 0 },
            new() { Id = Guid.NewGuid(), ParentGroupId = parentId, Level = 1 },
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateGroupHierarchy_BranchingNotAllowed_WithSubgroups_ReturnsInvalid()
    {
        // Arrange
        Guid parentId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        CostEstimateTemplate template = new() { CanBranchGroups = false };
        List<CostEstimateGroup> groups =
        [
            new() { Id = parentId, Level = 0 },
            new() { Id = childId, ParentGroupId = parentId, Level = 1 },
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("does not allow branching groups"));
    }

    [Fact]
    public void ValidateGroupHierarchy_BranchingNotAllowed_WithoutSubgroups_ReturnsValid()
    {
        // Arrange
        CostEstimateTemplate template = new() { CanBranchGroups = false };
        List<CostEstimateGroup> groups =
        [
            new() { Id = Guid.NewGuid(), Level = 0 },
            new() { Id = Guid.NewGuid(), Level = 0 },
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateGroupHierarchy_ParentNotFound_ReturnsInvalid()
    {
        // Arrange
        Guid missingParentId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        CostEstimateTemplate template = new() { CanBranchGroups = true };
        List<CostEstimateGroup> groups =
        [
            new() { Id = childId, ParentGroupId = missingParentId, Level = 1 },
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Parent group") && e.Contains("not found"));
    }

    [Fact]
    public void ValidateGroupHierarchy_WrongLevel_ReturnsInvalid()
    {
        // Arrange
        Guid parentId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        CostEstimateTemplate template = new() { CanBranchGroups = true };
        List<CostEstimateGroup> groups =
        [
            new() { Id = parentId, Level = 0 },
            new() { Id = childId, ParentGroupId = parentId, Level = 5 }, // should be 1
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid level"));
    }

    [Fact]
    public void ValidateGroupHierarchy_RootGroupWithNonZeroLevel_ReturnsInvalid()
    {
        // Arrange
        CostEstimateTemplate template = new() { CanBranchGroups = true };
        List<CostEstimateGroup> groups =
        [
            new() { Id = Guid.NewGuid(), Level = 3 }, // root must be level 0
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Root group must have level 0"));
    }

    [Fact]
    public void ValidateGroupHierarchy_ValidHierarchy_ReturnsValid()
    {
        // Arrange
        Guid parentId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        CostEstimateTemplate template = new() { CanBranchGroups = true, MaxGroupLevel = 5 };
        List<CostEstimateGroup> groups =
        [
            new() { Id = parentId, Level = 0 },
            new() { Id = childId, ParentGroupId = parentId, Level = 1 },
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupHierarchy(template, groups);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ─── ValidateGroupFieldValues ─────────────────────────────────────────────

    [Fact]
    public void ValidateGroupFieldValues_AllDefinitionsFound_ReturnsValid()
    {
        // Arrange
        Guid defId = Guid.NewGuid();
        Dictionary<Guid, CostEstimateTemplateGroupFieldDefinition> defs = new()
        {
            [defId] = new CostEstimateTemplateGroupFieldDefinition { Id = defId }
        };
        List<CostEstimateGroupFieldValue> values =
        [
            new() { FieldDefinitionId = defId }
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupFieldValues(defs, values);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateGroupFieldValues_MissingDefinition_ReturnsInvalid()
    {
        // Arrange
        Guid missingDefId = Guid.NewGuid();
        Dictionary<Guid, CostEstimateTemplateGroupFieldDefinition> defs = new();
        List<CostEstimateGroupFieldValue> values =
        [
            new() { FieldDefinitionId = missingDefId }
        ];

        // Act
        ValidationResult result = _sut.ValidateGroupFieldValues(defs, values);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains(missingDefId.ToString()) && e.Contains("not found"));
    }

    [Fact]
    public void ValidateGroupFieldValues_EmptyFieldValues_ReturnsValid()
    {
        // Arrange
        Dictionary<Guid, CostEstimateTemplateGroupFieldDefinition> defs = new();
        List<CostEstimateGroupFieldValue> values = [];

        // Act
        ValidationResult result = _sut.ValidateGroupFieldValues(defs, values);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
