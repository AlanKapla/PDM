using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Users;
using Entities.Models.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.WelcomeEmails.SendWelcomeEmailsToExistingUsers
{
    public sealed class SendWelcomeEmailsToExistingUsersCommandHandler
        : IRequestHandler<SendWelcomeEmailsToExistingUsersCommand, SendWelcomeEmailsResultWeb>
    {
        private const int BatchSize = 50;

        private readonly IReadRepository<User> userReadRepo;
        private readonly IRepository<User> userRepo;
        private readonly ICurrentUser currentUser;
        private readonly IWelcomeEmailService welcomeEmailService;
        private readonly ILogger<SendWelcomeEmailsToExistingUsersCommandHandler> logger;

        public SendWelcomeEmailsToExistingUsersCommandHandler(
            IReadRepository<User> userReadRepo,
            IRepository<User> userRepo,
            ICurrentUser currentUser,
            IWelcomeEmailService welcomeEmailService,
            ILogger<SendWelcomeEmailsToExistingUsersCommandHandler> logger)
        {
            this.userReadRepo = userReadRepo;
            this.userRepo = userRepo;
            this.currentUser = currentUser;
            this.welcomeEmailService = welcomeEmailService;
            this.logger = logger;
        }

        public async Task<SendWelcomeEmailsResultWeb> Handle(
            SendWelcomeEmailsToExistingUsersCommand request,
            CancellationToken cancellationToken)
        {
            if (!currentUser.IsSuperAdmin)
            {
                throw new ForbiddenApiException("Only SuperAdmin can send bulk welcome emails.");
            }

            int sentCount = 0;
            int skippedCount = 0;
            int skip = 0;

            while (true)
            {
                List<User> batch = await userReadRepo.GetPagedBySearchAsync(
                    u => u.IsActive && u.WelcomeEmailSentAt == null && u.Email != string.Empty,
                    u => u.CreatedAt,
                    descending: false,
                    skip: skip,
                    take: BatchSize,
                    cancellationToken);

                if (batch.Count == 0)
                {
                    break;
                }

                (int batchSent, int batchSkipped) = await ProcessBatchAsync(batch, cancellationToken);
                sentCount += batchSent;
                skippedCount += batchSkipped;
                skip += batch.Count;
            }

            logger.LogInformation(
                "Bulk welcome email send completed. Sent={SentCount}, Skipped={SkippedCount}",
                sentCount,
                skippedCount);

            return new SendWelcomeEmailsResultWeb(sentCount, skippedCount);
        }

        private async Task<(int SentCount, int SkippedCount)> ProcessBatchAsync(
            List<User> users,
            CancellationToken cancellationToken)
        {
            int sentCount = 0;
            int skippedCount = 0;

            foreach (User user in users)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    skippedCount++;
                    continue;
                }

                await welcomeEmailService.SendWelcomeEmailAsync(user, cancellationToken);
                user.WelcomeEmailSentAt = DateTime.UtcNow;
                await userRepo.Update(user);
                sentCount++;
            }

            return (sentCount, skippedCount);
        }
    }
}
