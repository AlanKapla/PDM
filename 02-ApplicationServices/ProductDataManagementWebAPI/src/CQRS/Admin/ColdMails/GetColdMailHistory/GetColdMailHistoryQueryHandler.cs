using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.ColdMails;
using MediatR;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Admin.ColdMails.GetColdMailHistory
{
    public sealed class GetColdMailHistoryQueryHandler
        : IRequestHandler<GetColdMailHistoryQuery, IReadOnlyList<ColdMailHistoryWeb>>
    {
        private const int MaxResults = 500;

        private readonly IReadRepository<ColdMailHistory> coldMailHistoryRepo;
        private readonly ICurrentUser currentUser;

        public GetColdMailHistoryQueryHandler(
            IReadRepository<ColdMailHistory> coldMailHistoryRepo,
            ICurrentUser currentUser)
        {
            this.coldMailHistoryRepo = coldMailHistoryRepo;
            this.currentUser = currentUser;
        }

        public async Task<IReadOnlyList<ColdMailHistoryWeb>> Handle(
            GetColdMailHistoryQuery request,
            CancellationToken cancellationToken)
        {
            EnsureSuperAdmin();

            Expression<Func<ColdMailHistory, bool>> predicate = BuildPredicate(request.Email);

            List<ColdMailHistory> histories = await coldMailHistoryRepo.GetPagedBySearchAsync(
                predicate,
                h => h.SentAt,
                descending: true,
                skip: 0,
                take: MaxResults,
                cancellationToken);

            return histories.Select(MapToWeb).ToList();
        }

        private void EnsureSuperAdmin()
        {
            if (!currentUser.IsSuperAdmin)
            {
                throw new ForbiddenApiException("Only SuperAdmin can view cold mail history.");
            }
        }

        private static Expression<Func<ColdMailHistory, bool>> BuildPredicate(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return _ => true;
            }

            string filter = email.Trim().ToLowerInvariant();
            return h => h.RecipientEmail.ToLower().Contains(filter);
        }

        private static ColdMailHistoryWeb MapToWeb(ColdMailHistory history)
        {
            return new ColdMailHistoryWeb(
                Id: history.Id,
                BatchId: history.BatchId,
                RecipientEmail: history.RecipientEmail,
                Subject: history.Subject,
                Body: history.Body,
                HtmlBody: history.HtmlBody,
                Status: history.Status.ToString(),
                ErrorMessage: history.ErrorMessage,
                SentByUserId: history.SentByUserId,
                SentAt: history.SentAt);
        }
    }
}
