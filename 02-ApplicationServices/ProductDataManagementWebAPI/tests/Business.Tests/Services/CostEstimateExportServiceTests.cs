using Business.Implementation.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Business.Tests.Services;

public sealed class CostEstimateExportServiceTests
{
    private readonly CostEstimateExportService _sut = new(NullLogger<CostEstimateExportService>.Instance);

    [Fact]
    public void Flatten_WhenGroupItemOptionComponent_ReturnsRowsInOrder()
    {
        // Arrange
        Guid fieldId = Guid.NewGuid();
        CostEstimateAdditionalFieldWeb field = new(
            fieldId, Guid.NewGuid(), "Uwagi", 0, 1, DateTime.UtcNow, null);

        (List<CostEstimateGroup> groups, List<CostEstimateItem> items) = BuildTree(fieldId, isSelected: true);

        // Act
        IReadOnlyList<CostEstimateExportRow> rows = _sut.Flatten(groups, items, [field]);

        // Assert
        rows.Should().HaveCount(4);
        rows.Select(r => r.RowType).Should().Equal(
            CostEstimateExportRowType.Group,
            CostEstimateExportRowType.Item,
            CostEstimateExportRowType.Option,
            CostEstimateExportRowType.Component);
        rows.Select(r => r.Name).Should().Equal("Grupa", "Pozycja", "Opcja", "Komponent");
        rows[1].AdditionalValues[fieldId.ToString()].Should().Be("wartość");
    }

    [Fact]
    public void Flatten_WhenItemNotSelected_StillIncludesRow()
    {
        // Arrange
        (List<CostEstimateGroup> groups, List<CostEstimateItem> items) = BuildTree(Guid.NewGuid(), isSelected: false);

        // Act
        IReadOnlyList<CostEstimateExportRow> rows = _sut.Flatten(groups, items, []);

        // Assert
        CostEstimateExportRow itemRow = rows.Should().ContainSingle(r => r.RowType == CostEstimateExportRowType.Item).Subject;
        itemRow.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void BuildFileName_WhenNameHasInvalidChars_SanitizesAndAppendsDate()
    {
        // Arrange
        DateTime utcNow = new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);

        // Act
        string fileName = CostEstimateExportService.BuildFileName("a/b*.xlsx", CostEstimateExportFormat.Xlsx, utcNow);

        // Assert
        fileName.Should().Be("a_b__20260721.xlsx");
        fileName.Should().NotContain("/");
        fileName.Should().NotContain("*");
    }

    [Fact]
    public void Export_WhenXlsx_ReturnsNonEmptyFileWithExtension()
    {
        // Arrange
        CostEstimate estimate = BuildEstimate();
        (List<CostEstimateGroup> groups, List<CostEstimateItem> items) = BuildTree(Guid.NewGuid(), isSelected: true);

        // Act
        CostEstimateExportFile file = _sut.Export(
            estimate, groups, items, [], "PLN", "zł", CostEstimateExportFormat.Xlsx,
            new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        file.Content.Length.Should().BeGreaterThan(0);
        file.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        file.FileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public void Export_WhenPdf_ReturnsNonEmptyPdf()
    {
        // Arrange
        CostEstimate estimate = BuildEstimate();
        (List<CostEstimateGroup> groups, List<CostEstimateItem> items) = BuildTree(Guid.NewGuid(), isSelected: true);

        // Act
        CostEstimateExportFile file = _sut.Export(
            estimate, groups, items, [], "PLN", "zł", CostEstimateExportFormat.Pdf,
            new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        file.Content.Length.Should().BeGreaterThan(100);
        file.ContentType.Should().Be("application/pdf");
        file.FileName.Should().EndWith(".pdf");
        System.Text.Encoding.ASCII.GetString(file.Content.AsSpan(0, 4)).Should().Be("%PDF");
    }

    [Fact]
    public void Export_WhenXlsxWithAdditionalField_IncludesFieldColumn()
    {
        // Arrange
        Guid fieldId = Guid.NewGuid();
        Guid costEstimateId = Guid.NewGuid();
        CostEstimateFieldSchemaWeb field = new(
            Id: fieldId,
            CostEstimateId: costEstimateId,
            FieldName: "Uwagi",
            FieldKey: "uwagi",
            FieldType: 0,
            IsBasicField: false,
            IsAdditionalField: true,
            Order: 1,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null);
        CostEstimate estimate = BuildEstimate();
        (List<CostEstimateGroup> groups, List<CostEstimateItem> items) = BuildTree(fieldId, isSelected: true);

        // Act
        CostEstimateExportFile file = _sut.Export(
            estimate, groups, items, [field], "PLN", "zł", CostEstimateExportFormat.Xlsx);

        // Assert
        file.Content.Length.Should().BeGreaterThan(0);
        using ClosedXML.Excel.XLWorkbook workbook = new(new MemoryStream(file.Content));
        ClosedXML.Excel.IXLWorksheet sheet = workbook.Worksheet("Kosztorys");
        sheet.Cell(1, 13).GetString().Should().Be("Uwagi");
        sheet.Cell(3, 13).GetString().Should().Be("wartość");
    }

    [Fact]
    public void Export_WhenSchemaHasRenamedBasicFields_UsesCustomHeaderLabels()
    {
        // Arrange
        CostEstimate estimate = BuildEstimate();
        (List<CostEstimateGroup> groups, List<CostEstimateItem> items) = BuildTree(Guid.NewGuid(), isSelected: true);
        List<CostEstimateFieldSchemaWeb> schemas =
        [
            BuildBasicSchema(estimate.Id, "name", "Nazwa haha", 100, 0),
            BuildBasicSchema(estimate.Id, "unit", "Jednostka haha", 102, 3),
            BuildBasicSchema(estimate.Id, "isSelected", "Sumuj custom", 109, 10),
        ];

        // Act
        CostEstimateExportFile file = _sut.Export(
            estimate, groups, items, schemas, "PLN", "zł", CostEstimateExportFormat.Xlsx);
        CostEstimateExportColumnLabels labels = CostEstimateExportService.ResolveColumnLabels(schemas);

        // Assert
        labels.Name.Should().Be("Nazwa haha");
        labels.Unit.Should().Be("Jednostka haha");
        labels.IsSelected.Should().Be("Sumuj custom");

        using ClosedXML.Excel.XLWorkbook workbook = new(new MemoryStream(file.Content));
        ClosedXML.Excel.IXLWorksheet sheet = workbook.Worksheet("Kosztorys");
        sheet.Cell(1, 3).GetString().Should().Be("Nazwa haha");
        sheet.Cell(1, 5).GetString().Should().Be("Jednostka haha");
        sheet.Cell(1, 12).GetString().Should().Be("Sumuj custom");
    }

    private static CostEstimateFieldSchemaWeb BuildBasicSchema(
        Guid costEstimateId,
        string fieldKey,
        string fieldName,
        int fieldType,
        int order)
    {
        return new CostEstimateFieldSchemaWeb(
            Id: Guid.NewGuid(),
            CostEstimateId: costEstimateId,
            FieldName: fieldName,
            FieldKey: fieldKey,
            FieldType: fieldType,
            IsBasicField: true,
            IsAdditionalField: false,
            Order: order,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null);
    }

    private static CostEstimate BuildEstimate() =>
        new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Test CE",
            TotalNet = 100m,
            TotalVat = 23m,
            TotalGross = 123m
        };

    private static (List<CostEstimateGroup> Groups, List<CostEstimateItem> Items) BuildTree(
        Guid fieldId,
        bool isSelected)
    {
        Guid groupId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        Guid optionId = Guid.NewGuid();
        Guid componentId = Guid.NewGuid();

        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = groupId,
            Name = "Grupa",
            Level = 0,
            Order = 0,
            TotalNet = 100m,
            TotalVat = 23m,
            TotalGross = 123m,
            AdditionalFieldValues = []
        };

        CostEstimateItem item = new CostEstimateItem
        {
            Id = itemId,
            GroupId = groupId,
            Name = "Pozycja",
            RelationType = ItemRelationType.None,
            Order = 0,
            Quantity = 1m,
            Unit = "szt",
            UnitPriceNet = 100m,
            VatRate = 0.23m,
            UnitPriceGross = 123m,
            NetValue = 100m,
            VatValue = 23m,
            GrossValue = 123m,
            IsSelected = isSelected,
            AdditionalFieldValues =
            [
                new CostEstimateAdditionalFieldValue
                {
                    FieldSchemaId = fieldId,
                    ItemId = itemId,
                    StringValue = "wartość"
                }
            ]
        };

        CostEstimateItem option = new CostEstimateItem
        {
            Id = optionId,
            GroupId = groupId,
            ParentItemId = itemId,
            Name = "Opcja",
            RelationType = ItemRelationType.Option,
            Order = 0,
            IsSelected = true,
            AdditionalFieldValues = []
        };

        CostEstimateItem component = new CostEstimateItem
        {
            Id = componentId,
            GroupId = groupId,
            ParentItemId = itemId,
            Name = "Komponent",
            RelationType = ItemRelationType.Component,
            Order = 1,
            IsSelected = true,
            AdditionalFieldValues = []
        };

        return ([group], [item, option, component]);
    }
}
