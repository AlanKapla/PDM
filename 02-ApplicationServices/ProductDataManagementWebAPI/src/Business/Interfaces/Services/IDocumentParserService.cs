using Business.Interfaces.WebModels.AI;

namespace Business.Interfaces.Services
{
    public interface IDocumentParserService
    {
        /// <summary>
        /// Parses a cost document via Vision: JPG/PNG as a single image;
        /// PDF is converted in-memory to JPEG pages (max 20) and sent as multi-image.
        /// Returns extracted cost data. Does NOT persist to the database.
        /// PDF conversion failures throw <c>PdfConversionException</c> (not mapped to confidence=0).
        /// </summary>
        Task<ParsedCostDto> ParseAsync(
            byte[] fileBytes,
            string mediaType,
            CancellationToken cancellationToken);
    }
}
