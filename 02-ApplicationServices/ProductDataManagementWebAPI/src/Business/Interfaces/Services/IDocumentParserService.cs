using Business.Interfaces.WebModels.AI;

namespace Business.Interfaces.Services
{
    public interface IDocumentParserService
    {
        /// <summary>
        /// Parsuje dokument (JPG/PNG/PDF jako bitmap) przez GPT-4o Vision.
        /// Zwraca wyciągnięte dane kosztu. NIE zapisuje do bazy danych.
        /// </summary>
        Task<ParsedCostDto> ParseAsync(
            byte[] fileBytes,
            string mediaType,
            CancellationToken cancellationToken);
    }
}
