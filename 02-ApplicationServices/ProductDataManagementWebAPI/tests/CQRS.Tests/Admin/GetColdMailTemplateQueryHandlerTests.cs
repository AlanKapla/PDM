using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.ColdMails.GetColdMailTemplate;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.Admin;

public sealed class GetColdMailTemplateQueryHandlerTests
{
    private readonly Mock<IColdMailHtmlBuilder> _htmlBuilderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetColdMailTemplateQueryHandler _handler;

    public GetColdMailTemplateQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);
        _htmlBuilderMock
            .Setup(b => b.GetTemplate())
            .Returns(new ColdMailTemplateWeb(
                HtmlTemplate: "<html>{subject}{bodyText}</html>",
                AppUrl: "https://app.brickly.pro",
                CtaLabel: "Poznaj Brickly"));

        _handler = new GetColdMailTemplateQueryHandler(
            _htmlBuilderMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotSuperAdmin_ThrowsForbiddenApiException()
    {
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);

        Func<Task> act = async () => await _handler.Handle(
            new GetColdMailTemplateQuery(),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenApiException>()
            .WithMessage("*SuperAdmin*");
    }

    [Fact]
    public async Task Handle_WhenSuperAdmin_ReturnsTemplate()
    {
        ColdMailTemplateWeb result = await _handler.Handle(
            new GetColdMailTemplateQuery(),
            CancellationToken.None);

        result.HtmlTemplate.Should().Contain("{subject}");
        result.AppUrl.Should().Be("https://app.brickly.pro");
        result.CtaLabel.Should().Be("Poznaj Brickly");
        _htmlBuilderMock.Verify(b => b.GetTemplate(), Times.Once);
    }
}
