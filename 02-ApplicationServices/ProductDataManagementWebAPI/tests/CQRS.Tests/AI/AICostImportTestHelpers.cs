using Business.Interfaces.WebModels.AI;
using CQRS.AI.ParseCostDocument;
using Entities.Enums;
using Entities.Models.AI;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text.Json;
using EntityCostDocumentType = Entities.Enums.CostDocumentType;

namespace CQRS.Tests.AI;

internal static class AICostImportTestHelpers
{
    internal static readonly Guid TenantId = Guid.NewGuid();
    internal static readonly Guid ProjectId = Guid.NewGuid();
    internal static readonly Guid UserId = Guid.NewGuid();
    internal static readonly Guid BatchId = Guid.NewGuid();
    internal static readonly Guid ItemId = Guid.NewGuid();

    internal static readonly byte[] JpegMagicBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    internal static readonly byte[] PngMagicBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    internal static readonly byte[] PdfMagicBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];

    internal static Mock<IFormFile> BuildFormFileMock(string fileName = "invoice.jpg", long length = 1024)
    {
        byte[] content = BuildFileContent(fileName, length);
        Mock<IFormFile> mock = new();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(GetContentType(fileName));
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        return mock;
    }

    internal static byte[] BuildFileContent(string fileName, long length)
    {
        byte[] magic = GetMagicBytes(fileName);
        if (length <= magic.Length)
        {
            return magic.Take((int)length).ToArray();
        }

        byte[] content = new byte[length];
        Array.Copy(magic, content, magic.Length);
        return content;
    }

    internal static string GetContentType(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    internal static byte[] GetMagicBytes(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => JpegMagicBytes,
            ".png" => PngMagicBytes,
            ".pdf" => PdfMagicBytes,
            _ => [0x00, 0x01, 0x02, 0x03]
        };
    }

    internal static AICostImportBatch BuildBatch(
        EntityCostDocumentType costDocumentType = EntityCostDocumentType.ProjectCost,
        AICostImportBatchStatus status = AICostImportBatchStatus.Completed)
    {
        return new AICostImportBatch
        {
            Id = BatchId,
            TenantId = TenantId,
            ProjectId = ProjectId,
            CreatedByUserId = UserId,
            CostDocumentType = costDocumentType,
            Status = status,
            TotalFiles = 2,
            ProcessedFiles = 2,
            PendingCount = 1,
            ErrorCount = 0,
            DuplicateCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    internal static AICostImportItem BuildItem(
        AICostImportItemStatus status = AICostImportItemStatus.Pending,
        string? parsedDataJson = null)
    {
        return new AICostImportItem
        {
            Id = ItemId,
            BatchId = BatchId,
            TenantId = TenantId,
            ProjectId = ProjectId,
            Status = status,
            OriginalFileName = "invoice.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024,
            FileHashSha256 = "abc123",
            BlobPath = "pending/path.jpg",
            ParsedDataJson = parsedDataJson ?? SerializeParsedData(ValidParsedCost()),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    internal static ParsedCostDto ValidParsedCost() =>
        new ParsedCostDto
        {
            Name = "Materiały budowlane",
            Net = 1000m,
            Gross = 1230m,
            Number = "FV/1/2026",
            Date = new DateTime(2026, 1, 15),
            ContractorId = Guid.NewGuid(),
            ContractorFound = true,
            Confidence = 0.9
        };

    internal static string SerializeParsedData(ParsedCostDto dto) =>
        JsonSerializer.Serialize(dto);
}
