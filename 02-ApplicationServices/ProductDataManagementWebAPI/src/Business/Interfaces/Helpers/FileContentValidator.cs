using Microsoft.AspNetCore.Http;

namespace Business.Interfaces.Helpers
{
    public static class FileContentValidator
    {
        public const string AllowedFormatsMessage = "Dozwolone formaty: JPG, PNG, PDF.";

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".pdf"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "application/pdf"
        };

        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        public sealed record FileValidationResult
        {
            public bool IsSuccess { get; init; }
            public string? FailureReason { get; init; }

            public static FileValidationResult Success() =>
                new FileValidationResult { IsSuccess = true };

            public static FileValidationResult Failure(string reason) =>
                new FileValidationResult { IsSuccess = false, FailureReason = reason };
        }

        public static FileValidationResult Validate(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return FileValidationResult.Failure("Plik jest pusty.");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string contentType = file.ContentType.ToLowerInvariant();

            FileValidationResult metadataResult = ValidateMetadata(extension, contentType);
            if (!metadataResult.IsSuccess)
            {
                return metadataResult;
            }

            using Stream stream = file.OpenReadStream();
            byte[] header = ReadHeader(stream);

            if (!HasValidMagicBytes(header, extension))
            {
                return FileValidationResult.Failure(
                    "Zawartość pliku nie odpowiada dozwolonym formatom (JPG, PNG, PDF).");
            }

            return FileValidationResult.Success();
        }

        public static FileValidationResult ValidateBytes(
            byte[] content,
            string fileName,
            string contentType)
        {
            if (content.Length == 0)
            {
                return FileValidationResult.Failure("Plik jest pusty.");
            }

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            string normalizedContentType = contentType.ToLowerInvariant();

            FileValidationResult metadataResult = ValidateMetadata(extension, normalizedContentType);
            if (!metadataResult.IsSuccess)
            {
                return metadataResult;
            }

            int headerLength = Math.Min(content.Length, PngSignature.Length);
            byte[] header = content.AsSpan(0, headerLength).ToArray();

            if (!HasValidMagicBytes(header, extension))
            {
                return FileValidationResult.Failure(
                    "Zawartość pliku nie odpowiada dozwolonym formatom (JPG, PNG, PDF).");
            }

            return FileValidationResult.Success();
        }

        public static bool IsAllowedExtension(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        public static bool IsAllowedContentType(string contentType)
        {
            return AllowedContentTypes.Contains(contentType.ToLowerInvariant());
        }

        private static FileValidationResult ValidateMetadata(string extension, string contentType)
        {
            if (!AllowedExtensions.Contains(extension))
            {
                return FileValidationResult.Failure($"Niedozwolony format pliku. {AllowedFormatsMessage}");
            }

            if (!AllowedContentTypes.Contains(contentType))
            {
                return FileValidationResult.Failure("Nieprawidłowy typ pliku.");
            }

            return FileValidationResult.Success();
        }

        private static byte[] ReadHeader(Stream stream)
        {
            byte[] buffer = new byte[PngSignature.Length];
            int totalRead = 0;

            while (totalRead < buffer.Length)
            {
                int read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            if (totalRead == buffer.Length)
            {
                return buffer;
            }

            byte[] header = new byte[totalRead];
            Array.Copy(buffer, header, totalRead);
            return header;
        }

        private static bool HasValidMagicBytes(byte[] header, string extension)
        {
            if (header.Length < 3)
            {
                return false;
            }

            if (extension is ".jpg" or ".jpeg")
            {
                return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            }

            if (extension == ".png")
            {
                return header.Length >= PngSignature.Length
                    && header.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature);
            }

            if (extension == ".pdf")
            {
                return header.Length >= 4
                    && header[0] == 0x25
                    && header[1] == 0x50
                    && header[2] == 0x44
                    && header[3] == 0x46;
            }

            return false;
        }
    }
}
