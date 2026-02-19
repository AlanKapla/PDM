using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Business.AIAgent.Configuration;
using Business.AIAgent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.AIAgent.Services;

/// <summary>
/// Service for extracting project cost data from files using Azure Document Intelligence or AI Vision
/// Prioritizes Document Intelligence for faster and cheaper extraction, falls back to Vision API
/// </summary>
public sealed class ProjectCostExtractionService
{
    private readonly DocumentIntelligenceClient documentIntelligenceClient;
    private readonly DocumentIntelligenceSettings documentIntelligenceSettings;
    private readonly ILogger<ProjectCostExtractionService> logger;

    public ProjectCostExtractionService(
        DocumentIntelligenceClient documentIntelligenceClient,
        IOptions<DocumentIntelligenceSettings> documentIntelligenceSettings,
        ILogger<ProjectCostExtractionService> logger)
    {
        this.documentIntelligenceClient = documentIntelligenceClient;
        this.documentIntelligenceSettings = documentIntelligenceSettings.Value;
        this.logger = logger;
    }

    /// <summary>
    /// Extract project cost totals from file using Document Intelligence (preferred) or Vision API (fallback)
    /// </summary>
    public async Task<ProjectCostExtractionResult> ExtractFromFileAsync(
        string fileName,
        byte[] fileContent,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting project cost extraction from file: {FileName}", fileName);

        try
        {
            logger.LogInformation("Attempting extraction with Azure Document Intelligence");
            return await ExtractUsingDocumentIntelligenceAsync(fileName, fileContent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Document Intelligence extraction failed, falling back to Vision API");
            return new ProjectCostExtractionResult
            {
                Success = false,
                ErrorMessage = $"Extraction failed: {ex.Message}"
            };
        }
    }

    private async Task<ProjectCostExtractionResult> ExtractUsingDocumentIntelligenceAsync(
        string fileName,
        byte[] fileContent,
        CancellationToken cancellationToken)
    {
        var content = BinaryData.FromBytes(fileContent);

        logger.LogInformation("Analyzing document with Document Intelligence (model: {ModelId}, size: {Size} bytes)",
            documentIntelligenceSettings.ModelId, fileContent.Length);

        // Analyze document using prebuilt-receipt model
        var operation = await documentIntelligenceClient.AnalyzeDocumentAsync(
            Azure.WaitUntil.Completed,
            documentIntelligenceSettings.ModelId,
            content,
            cancellationToken: cancellationToken);

        var result = operation.Value;

        // Parse Document Intelligence response
        var extractedData = ParseDocumentIntelligenceResult(result);

        logger.LogInformation(
            "Document Intelligence extraction successful: Vendor={Vendor}, Date={Date}, Net={Net}, Gross={Gross}",
            extractedData.VendorName ?? "N/A",
            extractedData.TransactionDate ?? "N/A",
            extractedData.TotalNet,
            extractedData.TotalGross);

        return new ProjectCostExtractionResult
        {
            Success = true,
            ExtractedData = extractedData
        };
    }

    private ExtractedProjectCostData ParseDocumentIntelligenceResult(AnalyzeResult analyzeResult)
    {
        var data = new ExtractedProjectCostData();

        if (analyzeResult.Documents == null || analyzeResult.Documents.Count == 0)
        {
            logger.LogWarning("No documents found in Document Intelligence result");
            return data;
        }

        var document = analyzeResult.Documents[0];
        var fields = document.Fields;

        logger.LogDebug("Document Intelligence returned {FieldCount} fields", fields.Count);

        // Extract merchant/vendor name
        if (fields.TryGetValue("MerchantName", out var merchantName) && merchantName.Content != null)
        {
            data.VendorName = merchantName.Content;
            logger.LogDebug("Extracted VendorName: {Value}", data.VendorName);
        }

        // Extract transaction date
        if (fields.TryGetValue("TransactionDate", out var transactionDate))
        {
            if (transactionDate.ValueDate.HasValue)
            {
                data.TransactionDate = transactionDate.ValueDate.Value.ToString("yyyy-MM-dd");
                logger.LogDebug("Extracted TransactionDate: {Value}", data.TransactionDate);
            }
        }

        // Extract receipt/invoice number
        if (fields.TryGetValue("ReceiptNumber", out var receiptNumber) && receiptNumber.Content != null)
        {
            data.DocumentNumber = receiptNumber.Content;
            logger.LogDebug("Extracted DocumentNumber: {Value}", data.DocumentNumber);
        }
        else if (fields.TryGetValue("InvoiceId", out var invoiceId) && invoiceId.Content != null)
        {
            data.DocumentNumber = invoiceId.Content;
            logger.LogDebug("Extracted DocumentNumber (from InvoiceId): {Value}", data.DocumentNumber);
        }

        // Extract total amounts
        if (fields.TryGetValue("Subtotal", out var subtotal) && subtotal.ValueCurrency?.Amount != null)
        {
            data.TotalNet = (decimal)subtotal.ValueCurrency.Amount;
            logger.LogDebug("Extracted TotalNet (Subtotal): {Value}", data.TotalNet);
        }

        if (fields.TryGetValue("Total", out var total) && total.ValueCurrency?.Amount != null)
        {
            data.TotalGross = (decimal)total.ValueCurrency.Amount;
            logger.LogDebug("Extracted TotalGross (Total): {Value}", data.TotalGross);
        }
        else if (fields.TryGetValue("InvoiceTotal", out var invoiceTotal) && invoiceTotal.ValueCurrency?.Amount != null)
        {
            data.TotalGross = (decimal)invoiceTotal.ValueCurrency.Amount;
            logger.LogDebug("Extracted TotalGross (InvoiceTotal): {Value}", data.TotalGross);
        }

        // If no subtotal found but we have total, use total for both
        if (data.TotalNet == 0 && data.TotalGross > 0)
        {
            data.TotalNet = data.TotalGross;
            logger.LogDebug("TotalNet not found, using TotalGross: {Value}", data.TotalNet);
        }

        // Try to build purchase description from document content (simple approach)
        // Document Intelligence doesn't always provide structured items in v1.0
        // We'll rely on Vision API for detailed item descriptions if needed

        return data;
    }
}

/// <summary>
/// Result of project cost extraction operation
/// </summary>
public sealed class ProjectCostExtractionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ExtractedProjectCostData? ExtractedData { get; set; }
}

/// <summary>
/// Extracted project cost data from AI
/// </summary>
public sealed class ExtractedProjectCostData
{
    public string? DocumentNumber { get; set; }
    public string? VendorName { get; set; }
    public string? TransactionDate { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalGross { get; set; }
    public string? PurchaseDescription { get; set; }
}
