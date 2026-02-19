# ✨ SEMANTIC KERNEL IMPLEMENTATION - Project Cost Extraction

## 🎯 Overview

Ten endpoint wykorzystuje **Microsoft Semantic Kernel** do orkiestracji Azure OpenAI w celu ekstrakcji kosztów projektu z plików.

## 🏗️ Architektura

```
File Upload (JPG/PDF, max 50MB)
    ↓
ProjectCostController
    ↓
ExtractProjectCostsFromFilesCommand (CQRS)
    ↓
ExtractProjectCostsFromFilesCommandHandler
    ↓
ProjectCostExtractionService ← Semantic Kernel Service
    ↓
KernelOrchestrator ← Semantic Kernel Core
    ↓
Azure OpenAI (GPT-4o with Vision)
    ↓
JSON Response with Cost Data
    ↓
ProjectCost Entity → Database
    +
Document → Blob Storage
```

## 🚀 Semantic Kernel Benefits

### 1. **Orchestration Layer**
- Centralized prompt management
- Automatic retry and error handling
- Token optimization
- Built-in logging and observability

### 2. **Plugin System** (Future-ready)
- Extensible architecture for custom functions
- Easy integration of domain-specific tools
- Reusable components across features

### 3. **Vision Support**
- Automatic handling of image inputs
- Base64 encoding for JPG/JPEG
- Seamless integration with GPT-4o vision model

### 4. **Structured Prompts**
- System prompt: Detailed schema with field descriptions
- User prompt: File-specific metadata
- Consistent formatting and validation

## 📝 Implementation Details

### Service: `ProjectCostExtractionService`

Lokalizacja: `src\Business.AIAgent\Services\ProjectCostExtractionService.cs`

**Odpowiedzialności:**
- Budowanie system i user promptów
- Wywołanie `KernelOrchestrator.ExecutePromptAsync()`
- Parsowanie JSON response
- Obsługa błędów ekstrakcji

**Kluczowe metody:**
```csharp
Task<ProjectCostExtractionResult> ExtractFromFileAsync(
    string fileName,
    byte[] fileContent,
    string fileExtension,
    CancellationToken cancellationToken)
```

### System Prompt - Szczegółowy Schema

Prompt zawiera:
1. **JSON Schema** - dokładna struktura odpowiedzi
2. **Field Descriptions** - opis każdego pola dla łatwego mapowania
3. **Extraction Rules** - zasady przetwarzania dokumentów
4. **Examples** - przykłady poprawnych odpowiedzi

**Pola w schema:**

| Pole | Typ | Opis dla AI | Mapowanie do DB |
|------|-----|-------------|-----------------|
| `vendorName` | string | Nazwa sklepu/dostawcy | → `Name` lub `Place` |
| `transactionDate` | string (ISO) | Data transakcji YYYY-MM-DD | → `Date` |
| `description` | string | Ogólny opis/notatki | → `Description` |
| **items[]** - Pozycje kosztów: |
| `name` | string (required) | Co zostało kupione | Suma → `Name` |
| `quantity` | decimal (required) | Ilość (domyślnie 1) | Do obliczeń |
| `unit` | string (required) | Jednostka miary | Do obliczeń |
| `unitPrice` | decimal (required) | Cena za jednostkę netto | Do obliczeń |
| `totalNet` | decimal (required) | Suma netto | Suma → `NetAmount` |
| `vatRate` | decimal (optional) | Stawka VAT (0.23 = 23%) | Średnia → `VatRate` |
| `totalVat` | decimal (optional) | Kwota VAT | Do obliczeń |
| `totalGross` | decimal (optional) | Suma brutto | Suma → `GrossAmount` |
| `category` | string (optional) | Kategoria wydatku | → `Place` |
| `notes` | string (optional) | Dodatkowe informacje | → `Description` |

### Handler: `ExtractProjectCostsFromFilesCommandHandler`

Lokalizacja: `src\CQRS\ProjectCosts\ExtractProjectCostsFromFiles\`

**Workflow:**
1. Walidacja tenant isolation
2. Dla każdego pliku:
   - Wczytanie do pamięci
   - Wywołanie `ProjectCostExtractionService`
   - Agregacja pozycji jeśli wiele
   - Upload do blob storage
   - Zapis do bazy jako `ProjectCost`
3. Zwrócenie response z sukcesami i błędami

**Agregacja kosztów:**
```csharp
// Jeśli paragon ma wiele pozycji, agregujemy:
totalNet = Sum(items.TotalNet)
totalGross = Sum(items.TotalGross ?? items.TotalNet)
totalVat = Sum(items.TotalVat ?? 0)
vatRate = (totalVat / totalNet) * 100 // Średnia stawka VAT

// Description zawiera listę pozycji (max 5)
```

## 🔧 Configuration

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-or-empty-if-managed-identity",
    "DeploymentName": "gpt-4o",
    "MaxTokens": 4000,
    "Temperature": 0.3,
    "TopP": null,
    "UseManagedIdentity": true
  },
  "BlobStorage": {
    "ContainerUrl": "https://yourstorageaccount.blob.core.windows.net/project-costs"
  }
}
```

### Dependency Injection

Rejestracja w `Business.AIAgent.Extensions.ServiceCollectionExtensions`:

```csharp
services.AddSingleton<Kernel>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
    var builder = Kernel.CreateBuilder();

    if (settings.UseManagedIdentity)
    {
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: settings.DeploymentName,
            endpoint: settings.Endpoint,
            credentials: new DefaultAzureCredential());
    }
    else
    {
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: settings.DeploymentName,
            endpoint: settings.Endpoint,
            apiKey: settings.ApiKey);
    }

    return builder.Build();
});

services.AddScoped<IKernelOrchestrator, KernelOrchestrator>();
services.AddScoped<ProjectCostExtractionService>();
```

## 📊 Example Flow

### Input: Receipt Photo (JPG)

```
COFFEE SHOP WARSZAWA
ul. Marszałkowska 1

Data: 2024-01-15 14:32

Cappuccino       1x   15.00 zł
Croissant        1x    8.50 zł
---------------------------------
Razem netto:           19.11 zł
VAT 23%:                4.39 zł
DO ZAPŁATY:            23.50 zł

Dziękujemy!
```

### AI Extraction (via Semantic Kernel)

```json
{
  "vendorName": "COFFEE SHOP WARSZAWA",
  "transactionDate": "2024-01-15",
  "description": "ul. Marszałkowska 1",
  "items": [
    {
      "name": "Cappuccino",
      "quantity": 1,
      "unit": "pcs",
      "unitPrice": 12.20,
      "totalNet": 12.20,
      "vatRate": 0.23,
      "totalVat": 2.81,
      "totalGross": 15.00,
      "category": "Food & Beverages"
    },
    {
      "name": "Croissant",
      "quantity": 1,
      "unit": "pcs",
      "unitPrice": 6.91,
      "totalNet": 6.91,
      "vatRate": 0.23,
      "totalVat": 1.59,
      "totalGross": 8.50,
      "category": "Food & Beverages"
    }
  ]
}
```

### Database Record: ProjectCost

```csharp
ProjectCost {
    Id = new Guid(),
    TenantId = request.TenantId,
    ProjectId = request.ProjectId,
    UserId = currentUser.Id,
    Name = "COFFEE SHOP WARSZAWA", // from vendorName
    Place = "COFFEE SHOP WARSZAWA", // from vendorName
    Date = new DateTime(2024, 1, 15), // parsed from transactionDate
    Description = "Extracted 2 cost items:\n- Cappuccino: 12.20\n- Croissant: 6.91",
    NetAmount = 19.11, // sum of totalNet
    VatRate = 23, // calculated average
    GrossAmount = 23.50, // sum of totalGross
    IsClosed = false,
    HasDocument = true,
    DocumentFileName = "receipt_20240115.jpg",
    DocumentBlobPath = "{tenant}/{project}/{user}/{costId}/receipt_20240115.jpg",
    DocumentContentType = "image/jpeg",
    DocumentSizeBytes = 245678,
    CreatedAt = DateTime.UtcNow
}
```

## 🎨 Prompt Engineering

### System Prompt Structure

1. **Role Definition**
   ```
   You are an AI assistant specialized in extracting cost information 
   from receipts, invoices, and bills.
   ```

2. **Schema Definition**
   - Complete JSON structure
   - Field types and requirements
   - Optional vs required fields

3. **Field Descriptions**
   - What to look for in the document
   - Examples of each field
   - How to map visual elements to JSON

4. **Extraction Rules**
   - Handle multiple items
   - Calculate missing values
   - Use sensible defaults
   - Return only JSON (no markdown)

5. **Examples**
   - Simple receipt example
   - Invoice with VAT example
   - Expected JSON output for each

### User Prompt

```
Please analyze this document and extract project cost data.
File name: receipt_20240115.jpg
File type: .jpg

This is a receipt, invoice, or bill document.
Extract cost information following the schema provided in the system message.

Focus on:
- Vendor/shop name
- Transaction date
- Individual line items with prices
- Total amounts (net, VAT, gross)
- VAT/tax rates if present

[IMAGE_BASE64]: /9j/4AAQSkZJRgABAQAAAQABAAD/2wBDA...

Return ONLY the JSON response with no additional text, markdown, or formatting.
```

## 🔍 Error Handling

### Extraction Errors

```csharp
public sealed class ProjectCostExtractionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ExtractedProjectCostData? ExtractedData { get; set; }
}
```

**Common errors:**
- Empty AI response → "AI returned empty response"
- No items extracted → "No cost data found in the document"
- JSON parsing error → "Failed to parse JSON: {details}"
- AI exception → "Extraction failed: {exception.Message}"

### Response Format

```json
{
  "createdProjectCostIds": ["guid1", "guid2"],
  "errors": [
    {
      "fileName": "unclear_receipt.jpg",
      "errorMessage": "No cost data found in the document",
      "errorType": "ExtractionFailed"
    }
  ],
  "totalFilesProcessed": 3,
  "successCount": 2,
  "errorCount": 1
}
```

## 📈 Performance Considerations

### Processing Time
- **Simple receipt (1-3 items)**: 3-5 seconds
- **Invoice (5-10 items)**: 5-8 seconds
- **Complex document**: 8-12 seconds

### Token Usage (GPT-4o)
- System prompt: ~1200 tokens
- User prompt: ~100 tokens
- Image (JPG): ~300-800 tokens (depending on size/resolution)
- Response: ~200-500 tokens
- **Total per file**: ~1800-2600 tokens

### Optimization Tips
1. **Resize images** before upload (max 2048px)
2. **Convert to JPG** (smaller than PNG)
3. **Batch processing**: Max 10 files at once
4. **Cache AI responses** for identical files (future enhancement)

## 🚀 Future Enhancements (via Semantic Kernel Plugins)

### Planned Features

1. **Smart Category Plugin**
   - Auto-categorize expenses based on vendor/content
   - Machine learning-based classification

2. **Duplicate Detection Plugin**
   - Check for similar receipts already uploaded
   - Prevent duplicate entries

3. **Budget Alert Plugin**
   - Real-time budget tracking
   - Alert when approaching limits

4. **Multi-Language Support**
   - Automatic language detection
   - Translation of cost descriptions

5. **OCR Enhancement Plugin**
   - Fallback to Azure Computer Vision for poor quality images
   - Improve accuracy for handwritten receipts

### Plugin Implementation Example

```csharp
[KernelFunction, Description("Categorizes expense based on vendor and items")]
public async Task<string> CategorizeExpense(
    [Description("Vendor name")] string vendorName,
    [Description("List of items")] List<string> items)
{
    // ML-based categorization logic
    // Returns: "Food & Beverages", "Materials", "Transport", etc.
}
```

## 📚 References

- [Microsoft Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/)
- [GPT-4o Vision Capabilities](https://platform.openai.com/docs/guides/vision)
- [Prompt Engineering Guide](https://platform.openai.com/docs/guides/prompt-engineering)

## 🎓 Best Practices

### For Developers

1. **Always use Semantic Kernel** for AI orchestration (not direct client)
2. **Structured prompts** with clear schema and examples
3. **Field descriptions** help AI understand the mapping
4. **Error handling** at every layer
5. **Logging** all AI interactions for debugging

### For Users

1. **Clear photos**: Good lighting, no blur
2. **Full receipt visible**: All text readable
3. **Supported formats**: JPG preferred over PDF
4. **File size**: Keep under 5MB per file
5. **Batch wisely**: Max 10 files at once

## ✅ Testing

### Unit Tests
```csharp
[Fact]
public async Task ExtractFromFile_ValidReceipt_ReturnsSuccess()
{
    // Arrange
    var service = new ProjectCostExtractionService(mockOrchestrator, mockLogger);
    var fileContent = GetTestReceiptBytes();
    
    // Act
    var result = await service.ExtractFromFileAsync("test.jpg", fileContent, ".jpg");
    
    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.ExtractedData);
    Assert.NotEmpty(result.ExtractedData.Items);
}
```

### Integration Tests
```csharp
[Fact]
public async Task EndToEnd_UploadReceipt_CreatesProjectCost()
{
    // Arrange
    var files = new List<IFormFile> { CreateTestFormFile() };
    var command = new ExtractProjectCostsFromFilesCommand { Files = files };
    
    // Act
    var response = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.Equal(1, response.SuccessCount);
    Assert.Single(response.CreatedProjectCostIds);
}
```

## 📞 Support

For issues or questions:
- Check logs in Application Insights
- Review AI responses in debugging mode
- Contact backend team for Semantic Kernel issues
