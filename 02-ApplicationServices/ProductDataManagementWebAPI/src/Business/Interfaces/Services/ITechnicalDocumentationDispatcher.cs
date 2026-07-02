using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace Business.Interfaces.Services;

public interface ITechnicalDocumentationDispatcher
{
    Task DispatchCompletedAsync(
        TechnicalDocumentationProcessingResultDto payload,
        CancellationToken cancellationToken);
}
