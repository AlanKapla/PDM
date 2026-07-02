using Business.Interfaces.WebModels.TechnicalDocumentation;

namespace WebApi.Hubs;

public interface ITechnicalDocumentationClient
{
    Task ProcessingCompleted(TechnicalDocumentationProcessingResultDto result);
}
