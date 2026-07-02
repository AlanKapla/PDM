using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class AuditAgentService : IAuditAgent
{
    private readonly ILogger<AuditAgentService> logger;

    public AuditAgentService(ILogger<AuditAgentService> logger)
    {
        this.logger = logger;
    }

    public Task<AuditResult> AuditAsync(
        ProjectModel projectModel,
        MaterialSchedule? materialSchedule,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Using deterministic audit (no LLM)");
        AuditResult result = TechnicalDocumentationDeterministicAuditor.Audit(projectModel, materialSchedule);
        return Task.FromResult(result);
    }
}
