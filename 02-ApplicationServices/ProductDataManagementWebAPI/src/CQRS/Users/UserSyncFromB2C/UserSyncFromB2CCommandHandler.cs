using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserSyncFromB2C
{
    public class UserSyncFromB2CCommandHandler : IRequestHandler<UserSyncFromB2CCommand, Guid>
    {
        private readonly IReadRepository<User> userReadRepo;
        private readonly IRepository<User> userRepo;
        private readonly ICurrentUser currentUser;
        private readonly IMicrosoftGraphService graphService;
        private readonly ILogger<UserSyncFromB2CCommandHandler> logger;

        public UserSyncFromB2CCommandHandler(
            IReadRepository<User> userReadRepo,
            IRepository<User> userRepo,
            ICurrentUser currentUser,
            IMicrosoftGraphService graphService,
            ILogger<UserSyncFromB2CCommandHandler> logger)
        {
            this.userReadRepo = userReadRepo;
            this.userRepo = userRepo;
            this.currentUser = currentUser;
            this.graphService = graphService;
            this.logger = logger;
        }

        public async Task<Guid> Handle(UserSyncFromB2CCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated)
            {
                throw new UnauthorizedApiException();
            }

            string azureB2CObjectId = currentUser.AzureAdB2CObjectId;
            if (string.IsNullOrEmpty(azureB2CObjectId))
            {
                throw new ValidationApiException("Azure B2C Object ID not found in token");
            }

            string email = currentUser.Email;
            if (string.IsNullOrEmpty(email))
            {
                throw new ValidationApiException("Email not found in token");
            }

            // Check if user already exists by B2C Object ID
            User? existingUserByB2C = await userReadRepo.GetFirstBySearch(
                u => u.AzureAdB2CObjectId == azureB2CObjectId,
                cancellationToken);

            if (existingUserByB2C != null)
            {
                logger.LogInformation(
                    "User with Azure B2C Object ID {ObjectId} already exists. Returning existing user ID {UserId}",
                    azureB2CObjectId,
                    existingUserByB2C.Id);

                return existingUserByB2C.Id;
            }

            // Check if user already exists by email (migration scenario)
            User? existingUserByEmail = await userReadRepo.GetFirstBySearch(
                u => u.Email == email,
                cancellationToken);

            if (existingUserByEmail != null)
            {
                // Link existing user account with Azure B2C
                existingUserByEmail.AzureAdB2CObjectId = azureB2CObjectId;
                existingUserByEmail.IsActive = true;

                await userRepo.Update(existingUserByEmail);

                logger.LogInformation(
                    "Linked existing user {UserId} with email {Email} to Azure B2C Object ID {ObjectId}",
                    existingUserByEmail.Id,
                    email,
                    azureB2CObjectId);

                return existingUserByEmail.Id;
            }

            var graphData = await graphService.GetUserDataAsync(azureB2CObjectId, cancellationToken);

            // Create new user from B2C
            User newUser = new()
            {
                AzureAdB2CObjectId = azureB2CObjectId,
                Email = email,
                FirstName = graphData?.FirstName ?? string.Empty,
                LastName = graphData?.LastName ?? string.Empty,
                IsActive = true,
                SystemRole = SystemRole.User,
                CreatedAt = DateTime.UtcNow
            };

            await userRepo.Insert(newUser);

            logger.LogInformation(
                "Created new user {UserId} from Azure B2C with Object ID {ObjectId}",
                newUser.Id,
                azureB2CObjectId);

            return newUser.Id;
        }
    }
}
