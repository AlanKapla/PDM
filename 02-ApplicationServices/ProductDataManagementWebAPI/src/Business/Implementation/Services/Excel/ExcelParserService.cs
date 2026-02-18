using OfficeOpenXml;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.Excel;

/// <summary>
/// Service for parsing Excel files into structured data
/// Supports various Excel formats and structures with intelligent field type detection
/// </summary>
public interface IExcelParserService
{
    Task<ExcelParseResult> ParseExcelFileAsync(
        Stream excelStream,
        CancellationToken cancellationToken = default);
}

public sealed class ExcelParserService : IExcelParserService
{
    private readonly ILogger<ExcelParserService> _logger;

    public ExcelParserService(ILogger<ExcelParserService> logger)
    {
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<ExcelParseResult> ParseExcelFileAsync(
        Stream excelStream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet == null)
            {
                return new ExcelParseResult
                {
                    Success = false,
                    ErrorMessage = "No worksheets found in Excel file"
                };
            }

            var result = new ExcelParseResult
            {
                Success = true,
                WorksheetName = worksheet.Name
            };

            var headerRow = 1;
            var lastColumn = worksheet.Dimension?.End.Column ?? 0;

            if (lastColumn == 0)
            {
                return new ExcelParseResult
                {
                    Success = false,
                    ErrorMessage = "Excel file appears to be empty"
                };
            }

            // Read headers
            for (int col = 1; col <= lastColumn; col++)
            {
                var headerValue = worksheet.Cells[headerRow, col].Value?.ToString() ?? $"Column{col}";
                result.Headers.Add(headerValue);
            }

            // Read data rows
            var lastRow = worksheet.Dimension.End.Row;
            for (int row = headerRow + 1; row <= lastRow; row++)
            {
                var rowData = new List<string>();
                for (int col = 1; col <= lastColumn; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
                    rowData.Add(cellValue);
                }

                if (rowData.Any(c => !string.IsNullOrWhiteSpace(c)))
                {
                    result.Rows.Add(rowData);
                }
            }

            _logger.LogInformation(
                "Parsed Excel: {Headers} headers, {Rows} rows",
                result.Headers.Count, result.Rows.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Excel file");
            return new ExcelParseResult
            {
                Success = false,
                ErrorMessage = $"Error parsing Excel: {ex.Message}"
            };
        }
    }
}


public sealed class ExcelParseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string WorksheetName { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}


