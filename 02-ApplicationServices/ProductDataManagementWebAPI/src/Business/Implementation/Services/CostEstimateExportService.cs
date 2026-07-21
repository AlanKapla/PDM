using System.Globalization;
using System.Text.RegularExpressions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using ClosedXML.Excel;
using Entities.Models.CostEstimates;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Business.Implementation.Services
{
    public sealed class CostEstimateExportService : ICostEstimateExportService
    {
        private const int SoftWarnRowThreshold = 5000;
        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string PdfContentType = "application/pdf";
        private const string NumberFormat = "#,##0.00";

        private static readonly CultureInfo PlCulture = CultureInfo.GetCultureInfo("pl-PL");
        // Cross-platform (Windows-safe) set — Path.GetInvalidFileNameChars() differs on Linux CI.
        private static readonly Regex InvalidFileNameChars = new(
            "[<>:\"/\\\\|?*\\x00-\\x1F]",
            RegexOptions.Compiled);

        private readonly ILogger<CostEstimateExportService> logger;

        static CostEstimateExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public CostEstimateExportService(ILogger<CostEstimateExportService> logger)
        {
            this.logger = logger;
        }

        public CostEstimateExportFile Export(
            CostEstimate costEstimate,
            IReadOnlyList<CostEstimateGroup> allGroups,
            IReadOnlyList<CostEstimateItem> allItems,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields,
            string? currencyCode,
            string? currencySymbol,
            CostEstimateExportFormat format,
            DateTime? exportedAtUtc = null)
        {
            DateTime exportedAt = exportedAtUtc ?? DateTime.UtcNow;
            IReadOnlyList<CostEstimateAdditionalFieldWeb> orderedFields = additionalFields
                .OrderBy(f => f.Order)
                .ToList();

            IReadOnlyList<CostEstimateExportRow> rows = Flatten(allGroups, allItems, orderedFields);
            WarnIfLargeExport(costEstimate.Id, rows.Count);

            CostEstimateExportMeta meta = BuildMeta(costEstimate, currencyCode, currencySymbol, exportedAt);
            string fileName = BuildFileName(costEstimate.Name, format, exportedAt);

            return format switch
            {
                CostEstimateExportFormat.Xlsx => BuildXlsx(meta, rows, orderedFields, fileName),
                CostEstimateExportFormat.Pdf => BuildPdf(meta, rows, orderedFields, fileName),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format.")
            };
        }

        internal IReadOnlyList<CostEstimateExportRow> Flatten(
            IReadOnlyList<CostEstimateGroup> allGroups,
            IReadOnlyList<CostEstimateItem> allItems,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId = BuildChildGroupsLookup(allGroups);
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId = BuildMainItemsLookup(allItems);
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId = BuildChildItemsLookup(allItems);

            List<CostEstimateExportRow> rows = new List<CostEstimateExportRow>();
            List<CostEstimateGroup> rootGroups = allGroups
                .Where(g => g.ParentGroupId is null)
                .OrderBy(g => g.Order)
                .ToList();

            foreach (CostEstimateGroup root in rootGroups)
            {
                AppendGroupRows(root, 0, childGroupsByParentId, mainItemsByGroupId, childItemsByParentId, additionalFields, rows);
            }

            return rows;
        }

        internal static string BuildFileName(string estimateName, CostEstimateExportFormat format, DateTime utcNow)
        {
            string sanitized = SanitizeFileName(estimateName);
            string extension = format == CostEstimateExportFormat.Pdf ? "pdf" : "xlsx";
            return $"{sanitized}_{utcNow:yyyyMMdd}.{extension}";
        }

        private void WarnIfLargeExport(Guid costEstimateId, int rowCount)
        {
            if (rowCount <= SoftWarnRowThreshold)
            {
                return;
            }

            logger.LogWarning(
                "Cost estimate export has {RowCount} rows (threshold {Threshold}). CostEstimateId={CostEstimateId}",
                rowCount,
                SoftWarnRowThreshold,
                costEstimateId);
        }

        private static CostEstimateExportMeta BuildMeta(
            CostEstimate costEstimate,
            string? currencyCode,
            string? currencySymbol,
            DateTime exportedAtUtc)
        {
            return new CostEstimateExportMeta(
                Name: costEstimate.Name,
                CurrencyCode: currencyCode,
                CurrencySymbol: currencySymbol,
                TotalNet: costEstimate.TotalNet,
                TotalGross: costEstimate.TotalGross,
                TotalVat: costEstimate.TotalVat,
                ExportedAtUtc: exportedAtUtc);
        }

        private static string SanitizeFileName(string estimateName)
        {
            string trimmed = string.IsNullOrWhiteSpace(estimateName) ? "Kosztorys" : estimateName.Trim();
            string withoutExtension = StripKnownExportExtension(trimmed);
            string sanitized = InvalidFileNameChars.Replace(withoutExtension, "_");
            sanitized = sanitized.Replace(' ', '_');
            return string.IsNullOrWhiteSpace(sanitized) ? "Kosztorys" : sanitized;
        }

        private static string StripKnownExportExtension(string name)
        {
            string[] knownExtensions = [".xlsx", ".xls", ".pdf"];
            foreach (string extension in knownExtensions)
            {
                if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return name[..^extension.Length];
                }
            }

            return name;
        }

        private static Dictionary<Guid, List<CostEstimateGroup>> BuildChildGroupsLookup(
            IReadOnlyList<CostEstimateGroup> allGroups)
        {
            return allGroups
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Order).ToList());
        }

        private static Dictionary<Guid, List<CostEstimateItem>> BuildMainItemsLookup(
            IReadOnlyList<CostEstimateItem> allItems)
        {
            return allItems
                .Where(i => i.RelationType == ItemRelationType.None)
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());
        }

        private static Dictionary<Guid, List<CostEstimateItem>> BuildChildItemsLookup(
            IReadOnlyList<CostEstimateItem> allItems)
        {
            return allItems
                .Where(i => i.ParentItemId.HasValue)
                .GroupBy(i => i.ParentItemId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());
        }

        private static void AppendGroupRows(
            CostEstimateGroup group,
            int level,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields,
            List<CostEstimateExportRow> rows)
        {
            rows.Add(MapGroupRow(group, level, additionalFields));

            if (childGroupsByParentId.TryGetValue(group.Id, out List<CostEstimateGroup>? children))
            {
                foreach (CostEstimateGroup child in children)
                {
                    AppendGroupRows(
                        child,
                        level + 1,
                        childGroupsByParentId,
                        mainItemsByGroupId,
                        childItemsByParentId,
                        additionalFields,
                        rows);
                }
            }

            AppendGroupItems(group.Id, level + 1, mainItemsByGroupId, childItemsByParentId, additionalFields, rows);
        }

        private static void AppendGroupItems(
            Guid groupId,
            int itemLevel,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields,
            List<CostEstimateExportRow> rows)
        {
            if (!mainItemsByGroupId.TryGetValue(groupId, out List<CostEstimateItem>? mainItems))
            {
                return;
            }

            foreach (CostEstimateItem item in mainItems)
            {
                rows.Add(MapItemRow(item, CostEstimateExportRowType.Item, itemLevel, additionalFields));
                AppendChildItems(item.Id, itemLevel + 1, ItemRelationType.Option, CostEstimateExportRowType.Option, childItemsByParentId, additionalFields, rows);
                AppendChildItems(item.Id, itemLevel + 1, ItemRelationType.Component, CostEstimateExportRowType.Component, childItemsByParentId, additionalFields, rows);
            }
        }

        private static void AppendChildItems(
            Guid parentItemId,
            int level,
            ItemRelationType relationType,
            CostEstimateExportRowType rowType,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields,
            List<CostEstimateExportRow> rows)
        {
            if (!childItemsByParentId.TryGetValue(parentItemId, out List<CostEstimateItem>? children))
            {
                return;
            }

            foreach (CostEstimateItem child in children.Where(c => c.RelationType == relationType))
            {
                rows.Add(MapItemRow(child, rowType, level, additionalFields));
            }
        }

        private static CostEstimateExportRow MapGroupRow(
            CostEstimateGroup group,
            int level,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            return new CostEstimateExportRow(
                RowType: CostEstimateExportRowType.Group,
                Level: level,
                Name: group.Name,
                Quantity: null,
                Unit: null,
                UnitPriceNet: null,
                VatRate: null,
                UnitPriceGross: null,
                NetValue: group.TotalNet,
                VatValue: group.TotalVat,
                GrossValue: group.TotalGross,
                IsSelected: null,
                AdditionalValues: MapAdditionalValues(group.AdditionalFieldValues, additionalFields));
        }

        private static CostEstimateExportRow MapItemRow(
            CostEstimateItem item,
            CostEstimateExportRowType rowType,
            int level,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            return new CostEstimateExportRow(
                RowType: rowType,
                Level: level,
                Name: item.Name,
                Quantity: item.Quantity,
                Unit: item.Unit,
                UnitPriceNet: item.UnitPriceNet,
                VatRate: item.VatRate,
                UnitPriceGross: item.UnitPriceGross,
                NetValue: item.NetValue,
                VatValue: item.VatValue,
                GrossValue: item.GrossValue,
                IsSelected: item.IsSelected,
                AdditionalValues: MapAdditionalValues(item.AdditionalFieldValues, additionalFields));
        }

        private static IReadOnlyDictionary<string, string?> MapAdditionalValues(
            ICollection<CostEstimateAdditionalFieldValue> values,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            Dictionary<Guid, CostEstimateAdditionalFieldValue> byFieldId = values
                .GroupBy(v => v.FieldSchemaId)
                .ToDictionary(g => g.Key, g => g.First());

            Dictionary<string, string?> result = new Dictionary<string, string?>();
            foreach (CostEstimateAdditionalFieldWeb field in additionalFields)
            {
                string key = field.Id.ToString();
                if (!byFieldId.TryGetValue(field.Id, out CostEstimateAdditionalFieldValue? value))
                {
                    result[key] = null;
                    continue;
                }

                result[key] = FormatAdditionalValue(value);
            }

            return result;
        }

        private static string? FormatAdditionalValue(CostEstimateAdditionalFieldValue value)
        {
            if (value.StringValue is not null)
            {
                return value.StringValue;
            }

            if (value.DecimalValue.HasValue)
            {
                return value.DecimalValue.Value.ToString("N2", PlCulture);
            }

            if (value.BoolValue.HasValue)
            {
                return value.BoolValue.Value ? "Tak" : "Nie";
            }

            if (value.DateTimeValue.HasValue)
            {
                return value.DateTimeValue.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return null;
        }

        private static CostEstimateExportFile BuildXlsx(
            CostEstimateExportMeta meta,
            IReadOnlyList<CostEstimateExportRow> rows,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields,
            string fileName)
        {
            using XLWorkbook workbook = new XLWorkbook();
            WriteSummarySheet(workbook, meta);
            WriteKosztorysSheet(workbook, rows, additionalFields);

            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new CostEstimateExportFile(stream.ToArray(), XlsxContentType, fileName);
        }

        private static void WriteSummarySheet(XLWorkbook workbook, CostEstimateExportMeta meta)
        {
            IXLWorksheet sheet = workbook.Worksheets.Add("Podsumowanie");
            sheet.Cell(1, 1).Value = "Nazwa";
            sheet.Cell(1, 2).Value = meta.Name;
            sheet.Cell(2, 1).Value = "Waluta";
            sheet.Cell(2, 2).Value = FormatCurrencyLabel(meta);
            sheet.Cell(3, 1).Value = "Suma netto";
            SetDecimalCell(sheet.Cell(3, 2), meta.TotalNet);
            sheet.Cell(4, 1).Value = "Suma VAT";
            SetDecimalCell(sheet.Cell(4, 2), meta.TotalVat);
            sheet.Cell(5, 1).Value = "Suma brutto";
            SetDecimalCell(sheet.Cell(5, 2), meta.TotalGross);
            sheet.Cell(6, 1).Value = "Data eksportu (UTC)";
            sheet.Cell(6, 2).Value = meta.ExportedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            sheet.Columns().AdjustToContents();
        }

        private static void WriteKosztorysSheet(
            XLWorkbook workbook,
            IReadOnlyList<CostEstimateExportRow> rows,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            IXLWorksheet sheet = workbook.Worksheets.Add("Kosztorys");
            WriteKosztorysHeaders(sheet, additionalFields);

            int rowIndex = 2;
            foreach (CostEstimateExportRow row in rows)
            {
                WriteKosztorysDataRow(sheet, rowIndex, row, additionalFields);
                rowIndex++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();
        }

        private static void WriteKosztorysHeaders(
            IXLWorksheet sheet,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            string[] headers =
            [
                "Poziom", "Typ", "Nazwa", "Ilość", "Jm", "Cena netto", "VAT %", "Cena brutto",
                "Wartość netto", "Wartość VAT", "Wartość brutto", "Zaznaczono"
            ];

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
            }

            for (int i = 0; i < additionalFields.Count; i++)
            {
                sheet.Cell(1, headers.Length + i + 1).Value = additionalFields[i].Name;
            }

            sheet.Row(1).Style.Font.Bold = true;
        }

        private static void WriteKosztorysDataRow(
            IXLWorksheet sheet,
            int rowIndex,
            CostEstimateExportRow row,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            sheet.Cell(rowIndex, 1).Value = row.Level;
            sheet.Cell(rowIndex, 2).Value = row.RowType.ToString();
            sheet.Cell(rowIndex, 3).Value = new string(' ', row.Level * 2) + row.Name;
            SetDecimalCell(sheet.Cell(rowIndex, 4), row.Quantity);
            sheet.Cell(rowIndex, 5).Value = row.Unit;
            SetDecimalCell(sheet.Cell(rowIndex, 6), row.UnitPriceNet);
            SetDecimalCell(sheet.Cell(rowIndex, 7), row.VatRate.HasValue ? row.VatRate.Value * 100m : null);
            SetDecimalCell(sheet.Cell(rowIndex, 8), row.UnitPriceGross);
            SetDecimalCell(sheet.Cell(rowIndex, 9), row.NetValue);
            SetDecimalCell(sheet.Cell(rowIndex, 10), row.VatValue);
            SetDecimalCell(sheet.Cell(rowIndex, 11), row.GrossValue);
            sheet.Cell(rowIndex, 12).Value = FormatIsSelected(row.IsSelected);

            if (row.RowType == CostEstimateExportRowType.Group)
            {
                sheet.Row(rowIndex).Style.Font.Bold = true;
            }

            for (int i = 0; i < additionalFields.Count; i++)
            {
                string key = additionalFields[i].Id.ToString();
                row.AdditionalValues.TryGetValue(key, out string? value);
                sheet.Cell(rowIndex, 13 + i).Value = value;
            }
        }

        private static void SetDecimalCell(IXLCell cell, decimal? value)
        {
            if (!value.HasValue)
            {
                cell.Clear();
                return;
            }

            cell.Value = value.Value;
            cell.Style.NumberFormat.Format = NumberFormat;
        }

        private static CostEstimateExportFile BuildPdf(
            CostEstimateExportMeta meta,
            IReadOnlyList<CostEstimateExportRow> rows,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields,
            string fileName)
        {
            bool landscape = additionalFields.Count > 2;
            byte[] content = Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePdfPage(page, landscape);
                    page.Header().Element(h => ComposePdfHeader(h, meta));
                    page.Content().Element(c => ComposePdfTable(c, rows, additionalFields));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Strona ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();

            return new CostEstimateExportFile(content, PdfContentType, fileName);
        }

        private static void ConfigurePdfPage(PageDescriptor page, bool landscape)
        {
            page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(8));
        }

        private static void ComposePdfHeader(IContainer container, CostEstimateExportMeta meta)
        {
            container.Column(column =>
            {
                column.Item().Text(meta.Name).SemiBold().FontSize(14);
                column.Item().Text($"Waluta: {FormatCurrencyLabel(meta)}");
                column.Item().Text(
                    $"Eksport: {meta.ExportedAtUtc:yyyy-MM-dd HH:mm} UTC | " +
                    $"Netto: {FormatDecimal(meta.TotalNet)} | " +
                    $"VAT: {FormatDecimal(meta.TotalVat)} | " +
                    $"Brutto: {FormatDecimal(meta.TotalGross)}");
                column.Item().PaddingBottom(8);
            });
        }

        private static void ComposePdfTable(
            IContainer container,
            IReadOnlyList<CostEstimateExportRow> rows,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            container.Table(table =>
            {
                DefinePdfColumns(table, additionalFields.Count);
                table.Header(header => WritePdfHeaderCells(header, additionalFields));

                foreach (CostEstimateExportRow row in rows)
                {
                    WritePdfDataCells(table, row, additionalFields);
                }
            });
        }

        private static void DefinePdfColumns(TableDescriptor table, int additionalFieldCount)
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn(3);
                columns.ConstantColumn(40);
                columns.ConstantColumn(28);
                columns.ConstantColumn(48);
                columns.ConstantColumn(40);
                columns.ConstantColumn(48);
                columns.ConstantColumn(48);
                columns.ConstantColumn(40);
                for (int i = 0; i < additionalFieldCount; i++)
                {
                    columns.RelativeColumn(2);
                }
            });
        }

        private static void WritePdfHeaderCells(
            TableCellDescriptor header,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            header.Cell().Element(PdfHeaderCell).Text("Typ");
            header.Cell().Element(PdfHeaderCell).Text("Nazwa");
            header.Cell().Element(PdfHeaderCell).Text("Ilość");
            header.Cell().Element(PdfHeaderCell).Text("Jm");
            header.Cell().Element(PdfHeaderCell).Text("Cena n.");
            header.Cell().Element(PdfHeaderCell).Text("VAT");
            header.Cell().Element(PdfHeaderCell).Text("Netto");
            header.Cell().Element(PdfHeaderCell).Text("Brutto");
            header.Cell().Element(PdfHeaderCell).Text("Zazn.");

            foreach (CostEstimateAdditionalFieldWeb field in additionalFields)
            {
                string title = field.Name.Length > 18 ? field.Name[..18] + "…" : field.Name;
                header.Cell().Element(PdfHeaderCell).Text(title);
            }
        }

        private static void WritePdfDataCells(
            TableDescriptor table,
            CostEstimateExportRow row,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields)
        {
            string indentedName = new string(' ', row.Level * 2) + row.Name;
            bool bold = row.RowType == CostEstimateExportRowType.Group;

            table.Cell().Element(PdfBodyCell).Text(ShortRowType(row.RowType));
            table.Cell().Element(PdfBodyCell).Text(text =>
            {
                if (bold)
                {
                    text.Span(indentedName).SemiBold();
                }
                else
                {
                    text.Span(indentedName);
                }
            });
            table.Cell().Element(PdfBodyCell).AlignRight().Text(FormatDecimal(row.Quantity));
            table.Cell().Element(PdfBodyCell).Text(row.Unit ?? string.Empty);
            table.Cell().Element(PdfBodyCell).AlignRight().Text(FormatDecimal(row.UnitPriceNet));
            table.Cell().Element(PdfBodyCell).AlignRight().Text(FormatVatPercent(row.VatRate));
            table.Cell().Element(PdfBodyCell).AlignRight().Text(FormatDecimal(row.NetValue));
            table.Cell().Element(PdfBodyCell).AlignRight().Text(FormatDecimal(row.GrossValue));
            table.Cell().Element(PdfBodyCell).Text(FormatIsSelected(row.IsSelected));

            foreach (CostEstimateAdditionalFieldWeb field in additionalFields)
            {
                row.AdditionalValues.TryGetValue(field.Id.ToString(), out string? value);
                table.Cell().Element(PdfBodyCell).Text(value ?? string.Empty);
            }
        }

        private static IContainer PdfHeaderCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Darken2)
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .DefaultTextStyle(x => x.SemiBold().FontSize(7));
        }

        private static IContainer PdfBodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(2)
                .PaddingHorizontal(2);
        }

        private static string ShortRowType(CostEstimateExportRowType rowType)
        {
            return rowType switch
            {
                CostEstimateExportRowType.Group => "G",
                CostEstimateExportRowType.Item => "P",
                CostEstimateExportRowType.Option => "O",
                CostEstimateExportRowType.Component => "K",
                _ => "?"
            };
        }

        private static string FormatCurrencyLabel(CostEstimateExportMeta meta)
        {
            if (!string.IsNullOrWhiteSpace(meta.CurrencySymbol) && !string.IsNullOrWhiteSpace(meta.CurrencyCode))
            {
                return $"{meta.CurrencyCode} ({meta.CurrencySymbol})";
            }

            return meta.CurrencyCode ?? meta.CurrencySymbol ?? "-";
        }

        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("N2", PlCulture) : string.Empty;
        }

        private static string FormatVatPercent(decimal? vatRate)
        {
            return vatRate.HasValue ? (vatRate.Value * 100m).ToString("N0", PlCulture) + "%" : string.Empty;
        }

        private static string FormatIsSelected(bool? isSelected)
        {
            if (!isSelected.HasValue)
            {
                return string.Empty;
            }

            return isSelected.Value ? "Tak" : "Nie";
        }
    }
}
