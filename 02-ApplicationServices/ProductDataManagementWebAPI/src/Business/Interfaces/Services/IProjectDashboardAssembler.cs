using Business.Interfaces.WebModels.ProjectDashboard;
using Entities.Models.Projects;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Składa ProjectDashboardWeb z surowych danych pobranych przez IDashboardDataLoader.
    /// </summary>
    public interface IProjectDashboardAssembler
    {
        Task<ProjectDashboardWeb> AssembleAsync(
            Project project,
            DashboardData data,
            CancellationToken cancellationToken);
    }
}
