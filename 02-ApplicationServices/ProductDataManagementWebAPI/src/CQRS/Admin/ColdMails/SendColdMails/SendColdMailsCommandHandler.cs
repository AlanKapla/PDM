using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Admin;
using CQRS.PostCommit;
using Entities.Enums;
using Entities.Models.ColdMails;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.ColdMails.SendColdMails
{
    public sealed class SendColdMailsCommandHandler
        : IRequestHandler<SendColdMailsCommand, SendColdMailsResultWeb>
    {
        private const int MaxErrorMessageLength = 2000;

        private readonly IRepository<ColdMailHistory> coldMailHistoryRepo;
        private readonly IEmailSender emailSender;
        private readonly ICurrentUser currentUser;
        private readonly IColdMailHtmlBuilder coldMailHtmlBuilder;
        private readonly IPostCommitDispatcher postCommitDispatcher;

        public SendColdMailsCommandHandler(
            IRepository<ColdMailHistory> coldMailHistoryRepo,
            IEmailSender emailSender,
            ICurrentUser currentUser,
            IColdMailHtmlBuilder coldMailHtmlBuilder,
            IPostCommitDispatcher postCommitDispatcher)
        {
            this.coldMailHistoryRepo = coldMailHistoryRepo;
            this.emailSender = emailSender;
            this.currentUser = currentUser;
            this.coldMailHtmlBuilder = coldMailHtmlBuilder;
            this.postCommitDispatcher = postCommitDispatcher;
        }

        public async Task<SendColdMailsResultWeb> Handle(
            SendColdMailsCommand request,
            CancellationToken cancellationToken)
        {
            EnsureSuperAdmin();

            List<string> recipients = NormalizeEmails(request.Emails);
            Guid batchId = Guid.NewGuid();
            DateTime sentAt = DateTime.UtcNow;
            string editorHtml = request.Body;
            string htmlBody = coldMailHtmlBuilder.Build(request.Subject, editorHtml);
            string plainBody = coldMailHtmlBuilder.ToPlainText(editorHtml);
            List<ColdMailSendItemWeb> items = new();

            foreach (string recipientEmail in recipients)
            {
                ColdMailSendItemWeb item = await RecordAndEnqueueAsync(
                    batchId,
                    recipientEmail,
                    request.Subject,
                    plainBody,
                    htmlBody,
                    sentAt,
                    cancellationToken);
                items.Add(item);
            }

            return BuildResult(batchId, items);
        }

        private void EnsureSuperAdmin()
        {
            if (!currentUser.IsSuperAdmin)
            {
                throw new ForbiddenApiException("Only SuperAdmin can send cold mails.");
            }
        }

        private static List<string> NormalizeEmails(IReadOnlyList<string> emails)
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            List<string> result = new();

            foreach (string email in emails)
            {
                string trimmed = email.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                if (seen.Add(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        private async Task<ColdMailSendItemWeb> RecordAndEnqueueAsync(
            Guid batchId,
            string recipientEmail,
            string subject,
            string plainBody,
            string htmlBody,
            DateTime sentAt,
            CancellationToken cancellationToken)
        {
            ColdMailHistory history = new()
            {
                BatchId = batchId,
                RecipientEmail = recipientEmail,
                Subject = subject,
                Body = plainBody,
                HtmlBody = htmlBody,
                Status = ColdMailStatus.Queued,
                ErrorMessage = null,
                SentByUserId = currentUser.Id,
                SentAt = sentAt
            };

            await coldMailHistoryRepo.Insert(history);
            await coldMailHistoryRepo.SaveChangesAsync(cancellationToken);

            EnqueueEmailAfterCommit(history, recipientEmail, subject, plainBody, htmlBody);

            return new ColdMailSendItemWeb(
                RecipientEmail: recipientEmail,
                Status: history.Status.ToString(),
                ErrorMessage: history.ErrorMessage);
        }

        private void EnqueueEmailAfterCommit(
            ColdMailHistory history,
            string recipientEmail,
            string subject,
            string plainBody,
            string htmlBody)
        {
            Guid historyId = history.Id;
            postCommitDispatcher.Enqueue(async ct =>
            {
                await SendEmailOrMarkFailedAsync(
                    history,
                    historyId,
                    recipientEmail,
                    subject,
                    plainBody,
                    htmlBody,
                    ct);
            });
        }

        private async Task SendEmailOrMarkFailedAsync(
            ColdMailHistory history,
            Guid historyId,
            string recipientEmail,
            string subject,
            string plainBody,
            string htmlBody,
            CancellationToken cancellationToken)
        {
            try
            {
                await emailSender.SendEmailAsync(
                    new EmailMessageDto
                    {
                        To = recipientEmail,
                        Subject = subject,
                        HtmlBody = htmlBody,
                        TextBody = plainBody,
                        ColdMailHistoryId = historyId
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                await MarkHistoryFailedAsync(history, ex.Message, cancellationToken);
            }
        }

        private async Task MarkHistoryFailedAsync(
            ColdMailHistory history,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            history.Status = ColdMailStatus.Failed;
            history.ErrorMessage = TruncateErrorMessage(errorMessage);
            await coldMailHistoryRepo.Update(history);
            await coldMailHistoryRepo.SaveChangesAsync(cancellationToken);
        }

        private static string TruncateErrorMessage(string message)
        {
            if (message.Length <= MaxErrorMessageLength)
            {
                return message;
            }

            return message[..MaxErrorMessageLength];
        }

        private static SendColdMailsResultWeb BuildResult(
            Guid batchId,
            IReadOnlyList<ColdMailSendItemWeb> items)
        {
            int queuedCount = items.Count(i => i.Status == ColdMailStatus.Queued.ToString());
            int failedCount = items.Count(i => i.Status == ColdMailStatus.Failed.ToString());

            return new SendColdMailsResultWeb(
                BatchId: batchId,
                QueuedCount: queuedCount,
                FailedCount: failedCount,
                Items: items);
        }
    }
}
