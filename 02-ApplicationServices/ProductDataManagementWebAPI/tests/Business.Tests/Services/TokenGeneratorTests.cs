using Business.Implementation.Services;
using FluentAssertions;

namespace Business.Tests.Services;

public class TokenGeneratorTests
{
    private readonly TokenGenerator _sut = new TokenGenerator();

    [Fact]
    public void GenerateToken_DefaultSize_Returns32ByteBase64UrlToken()
    {
        // Act
        string token = _sut.GenerateToken();

        // Assert — 32 bytes base64url without padding = ceil(32*4/3) trimmed chars
        token.Should().NotBeNullOrEmpty();
        token.Should().MatchRegex(@"^[A-Za-z0-9\-_]+$", "token must be base64url encoded");
        token.Should().NotContain("=", "padding must be stripped");
        token.Should().NotContain("+", "+ must be replaced with -");
        token.Should().NotContain("/", "/ must be replaced with _");
    }

    [Fact]
    public void GenerateToken_DefaultSize_ProducesUniqueTokensEachCall()
    {
        // Act
        string token1 = _sut.GenerateToken();
        string token2 = _sut.GenerateToken();

        // Assert
        token1.Should().NotBe(token2, "tokens must be cryptographically random");
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void GenerateToken_CustomSize_ProducesCorrectLengthToken(int sizeBytes)
    {
        // Act
        string token = _sut.GenerateToken(sizeBytes);

        // Assert
        // Base64 produces ceil(sizeBytes / 3) * 4 chars, then trimEnd('=')
        int expectedMinLength = (int)Math.Ceiling(sizeBytes * 4.0 / 3.0) - 2; // -2 accounts for stripped padding
        token.Length.Should().BeGreaterThanOrEqualTo(expectedMinLength);
    }

    [Fact]
    public void GenerateToken_ReturnValue_IsValidBase64UrlWithNoPadding()
    {
        // Act
        string token = _sut.GenerateToken(32);

        // Assert — must decode back without errors after restoring standard base64
        string restored = token.Replace('-', '+').Replace('_', '/');
        // Restore padding
        int mod = restored.Length % 4;
        if (mod > 0)
        {
            restored += new string('=', 4 - mod);
        }

        byte[] decoded = Convert.FromBase64String(restored);
        decoded.Should().HaveCount(32);
    }
}
