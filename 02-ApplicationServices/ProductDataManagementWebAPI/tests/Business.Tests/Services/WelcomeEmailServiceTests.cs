using Business.Implementation.Services;
using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Business.Tests.Services;

public sealed class WelcomeEmailServiceTests
{
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly IOptions<FrontendSettings> _frontendSettings;
    private readonly WelcomeEmailService _sut;

    public WelcomeEmailServiceTests()
    {
        _frontendSettings = Options.Create(new FrontendSettings
        {
            BaseUrl = "https://app.test",
            HomePath = "/home"
        });

        _emailSenderMock
            .Setup(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new WelcomeEmailService(
            _emailSenderMock.Object,
            _frontendSettings,
            NullLogger<WelcomeEmailService>.Instance);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WhenUserIsValid_EnqueuesEmail()
    {
        // Arrange
        User user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            FirstName = "Jan",
            LastName = "Kowalski",
            AzureAdB2CObjectId = "oid-123"
        };

        // Act
        await _sut.SendWelcomeEmailAsync(user, CancellationToken.None);

        // Assert
        _emailSenderMock.Verify(
            s => s.SendEmailAsync(
                It.Is<EmailMessageDto>(m =>
                    m.To == "user@test.com"
                    && m.Subject == "Witaj w Brickly!"
                    && m.HtmlBody!.Contains("Jan")
                    && m.TextBody!.Contains("https://app.test/home")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
