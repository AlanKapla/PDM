using Business.Interfaces.Model;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    /// <summary>
    /// Background service konsolidujący udostępnienia plików
    /// Uruchamia się raz dziennie o 2:00 w nocy i konsoliduje:
    /// - Jeśli user ma >= 60% plików z paczki udostępnionych → konwertuj na paczkę + wykluczenia
    /// </summary>
    public class FileShareConsolidationService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<FileShareConsolidationService> logger;
        private readonly TimeSpan consolidationInterval = TimeSpan.FromHours(24);

        public FileShareConsolidationService(
            IServiceProvider serviceProvider,
            ILogger<FileShareConsolidationService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("FileShareConsolidationService started. Will run daily at 2:00 AM.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Oblicz czas do następnego uruchomienia (2:00 AM)
                    var now = DateTime.UtcNow;
                    var nextRun = now.Date.AddDays(1).AddHours(2); // Następna 2:00 AM
                    
                    if (now.Hour < 2)
                    {
                        // Jeśli jeszcze nie było 2:00 dziś, uruchom dziś
                        nextRun = now.Date.AddHours(2);
                    }

                    var delay = nextRun - now;
                    
                    logger.LogInformation(
                        "Next consolidation run scheduled for {NextRun} UTC (in {Hours}h {Minutes}m)",
                        nextRun, (int)delay.TotalHours, delay.Minutes);

                    await Task.Delay(delay, stoppingToken);

                    // Wykonaj konsolidację
                    await ConsolidateFileSharesAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in FileShareConsolidationService");
                    
                    // Poczekaj 1h przed ponowną próbą
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            logger.LogInformation("FileShareConsolidationService stopped.");
        }

        private async Task ConsolidateFileSharesAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting file shares consolidation...");

            using var scope = serviceProvider.CreateScope();
            var sharedFileRepo = scope.ServiceProvider.GetRequiredService<IRepository<SharedProjectFile>>();
            var projectFileRepo = scope.ServiceProvider.GetRequiredService<IReadRepository<ProjectFile>>();

            try
            {
                // Pobierz wszystkie udostępnienia (grupowane po PackageId + UserId)
                var allShares = await sharedFileRepo.GetBySearch(
                    spf => spf.ProjectFileId != null  // Tylko pojedyncze pliki (nie całe paczki)
                        && spf.Access == ProjectFileAccess.Allow);  // Tylko Allow

                // Grupuj po PackageId + UserId
                var sharesByPackageAndUser = allShares
                    .GroupBy(spf => new { spf.ProjectFilePackageId, spf.SharedWithUserId })
                    .ToList();

                logger.LogInformation(
                    "Found {GroupCount} package-user combinations to analyze",
                    sharesByPackageAndUser.Count);

                int consolidatedCount = 0;
                int savedRecords = 0;

                foreach (var group in sharesByPackageAndUser)
                {
                    var packageId = group.Key.ProjectFilePackageId;
                    var userId = group.Key.SharedWithUserId;
                    var shares = group.ToList();

                    // Sprawdź czy paczka już jest udostępniona
                    var existingPackageShare = await sharedFileRepo.GetFirstBySearch(
                        spf => spf.ProjectFilePackageId == packageId
                            && spf.ProjectFileId == null
                            && spf.SharedWithUserId == userId);

                    if (existingPackageShare != null)
                    {
                        // Paczka już jest udostępniona - pomiń
                        continue;
                    }

                    // Pobierz wszystkie pliki w paczce
                    var allFilesInPackage = await projectFileRepo.GetBySearch(
                        pf => pf.ProjectFilePackageId == packageId);

                    var totalFilesCount = allFilesInPackage.Count();
                    var sharedFilesCount = shares.Count;

                    // Próg konsolidacji: 60%
                    const double CONSOLIDATION_THRESHOLD = 0.6;

                    if (sharedFilesCount >= totalFilesCount * CONSOLIDATION_THRESHOLD)
                    {
                        // ✅ Konsoliduj: paczka + wykluczenia
                        var allFileIds = allFilesInPackage.Select(f => f.Id).ToHashSet();
                        var sharedFileIds = shares
                            .Where(s => s.ProjectFileId.HasValue)
                            .Select(s => s.ProjectFileId!.Value)
                            .ToHashSet();
                        var excludedFileIds = allFileIds.Except(sharedFileIds).ToList();

                        // Usuń wszystkie Allow dla pojedynczych plików
                        await sharedFileRepo.DeleteRange(shares);

                        // Dodaj udostępnienie paczki
                        var firstShare = shares.First();
                        var packageShare = new SharedProjectFile
                        {
                            TenantId = firstShare.TenantId,
                            ProjectId = firstShare.ProjectId,
                            ProjectFilePackageId = packageId,
                            ProjectFileId = null,
                            Access = ProjectFileAccess.Allow,
                            SharedByUserId = firstShare.SharedByUserId,
                            SharedWithUserId = userId,
                            SharedAt = DateTime.UtcNow
                        };

                        await sharedFileRepo.Insert(packageShare);

                        // Dodaj wykluczenia (Deny)
                        foreach (var excludedFileId in excludedFileIds)
                        {
                            var exclusion = new SharedProjectFile
                            {
                                TenantId = firstShare.TenantId,
                                ProjectId = firstShare.ProjectId,
                                ProjectFilePackageId = packageId,
                                ProjectFileId = excludedFileId,
                                Access = ProjectFileAccess.Deny,
                                SharedByUserId = firstShare.SharedByUserId,
                                SharedWithUserId = userId,
                                SharedAt = DateTime.UtcNow
                            };

                            await sharedFileRepo.Insert(exclusion);
                        }

                        consolidatedCount++;
                        var recordsSaved = sharedFilesCount - (1 + excludedFileIds.Count);
                        savedRecords += recordsSaved;

                        logger.LogInformation(
                            "Consolidated package {PackageId} for user {UserId}: " +
                            "{SharedCount}/{TotalCount} files → package + {ExcludedCount} exclusions " +
                            "(saved {SavedRecords} records)",
                            packageId, userId, sharedFilesCount, totalFilesCount,
                            excludedFileIds.Count, recordsSaved);
                    }
                }

                // Zapisz zmiany
                if (consolidatedCount > 0)
                {
                    await sharedFileRepo.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "Consolidation completed: {ConsolidatedCount} package-user combinations consolidated, " +
                        "{SavedRecords} records saved",
                        consolidatedCount, savedRecords);
                }
                else
                {
                    logger.LogInformation("Consolidation completed: No consolidation opportunities found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during file shares consolidation");
                throw;
            }
        }
    }
}
