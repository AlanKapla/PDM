using Business.Implementation.Services;
using Business.Interfaces.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Business.Tests.Services;

public sealed class ColdMailHtmlBuilderTests
{
    private readonly ColdMailHtmlBuilder _sut = new(
        Options.Create(new FrontendSettings
        {
            BaseUrl = "https://app.brickly.pro",
            HomePath = "/"
        }));

    [Fact]
    public void Build_WhenBodyIsTipTapHtml_KeepsFormattingTagsUnescaped()
    {
        string html = _sut.Build("Temat", "<p><strong>Hej Alan</strong></p>");

        html.Should().Contain("<strong>Hej Alan</strong>");
        html.Should().NotContain("&lt;strong&gt;");
        html.Should().NotContain("&lt;p&gt;");
    }

    [Fact]
    public void Build_WhenBodyIsPlainText_EscapesAndConvertsNewlines()
    {
        string html = _sut.Build("Temat", "Linia1\nLinia2 <test>");

        html.Should().Contain("Linia1<br />Linia2 &lt;test&gt;");
    }

    [Fact]
    public void ToPlainText_WhenBodyIsTipTapHtml_StripsTags()
    {
        string plain = _sut.ToPlainText("<p><strong>Hej Alan</strong></p>");

        plain.Should().Be("Hej Alan");
    }

    [Fact]
    public void Build_WhenBodyContainsScript_RemovesDangerousTags()
    {
        string html = _sut.Build("Temat", "<p>ok</p><script>alert(1)</script>");

        html.Should().Contain("<p>ok</p>");
        html.Should().NotContain("<script>");
    }
}
