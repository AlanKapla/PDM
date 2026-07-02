using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Interfaces.Services;

public sealed record TechnicalDocumentationImageInput(
    byte[] ImageBytes,
    string FileName,
    int PageNumber,
    string MediaType = "image/jpeg");

public sealed record TechnicalDocumentationPartialResult(
    string FileName,
    int PageNumber,
    FloorPlanDrawing Drawing,
    bool CrossValidationUsed = false);

public interface ITechnicalDocumentationOrchestratorService
{
    Task<ProjectTechnicalDocumentationDetails> ProcessImagesAsync(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        CancellationToken cancellationToken);
}
