using Business.Implementation.Services;
using Business.Interfaces.Exceptions;
using FluentAssertions;

namespace Business.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new PasswordHasher();

    [Fact]
    public void Hash_ValidPassword_ReturnsArgon2idEncodedString()
    {
        // Arrange
        string password = "SecurePass123!";

        // Act
        string hash = _sut.Hash(password);

        // Assert
        hash.Should().StartWith("$argon2id$");
    }

    [Fact]
    public void Hash_ValidPassword_ReturnsDifferentHashEachTime()
    {
        // Arrange
        string password = "SecurePass123!";

        // Act
        string hash1 = _sut.Hash(password);
        string hash2 = _sut.Hash(password);

        // Assert
        hash1.Should().NotBe(hash2, "each hash uses a different random salt");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Hash_EmptyOrNullPassword_ThrowsApiException(string? password)
    {
        // Act
        Action act = () => _sut.Hash(password!);

        // Assert
        act.Should().Throw<ApiException>()
            .Which.Reason.Should().Be(ApiExceptionReason.InvalidOperation);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        string password = "CorrectHorseBatteryStaple";
        string hash = _sut.Hash(password);

        // Act
        bool result = _sut.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        // Arrange
        string password = "CorrectPassword";
        string hash = _sut.Hash(password);

        // Act
        bool result = _sut.Verify("WrongPassword", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "$argon2id$v=19$m=65536,t=4,p=1$abc$def")]
    [InlineData(null, "$argon2id$v=19$m=65536,t=4,p=1$abc$def")]
    [InlineData("password", "")]
    [InlineData("password", null)]
    public void Verify_EmptyOrNullInputs_ReturnsFalse(string? password, string? hash)
    {
        // Act
        bool result = _sut.Verify(password!, hash!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse()
    {
        // Arrange
        string malformed = "not-a-valid-hash";

        // Act
        bool result = _sut.Verify("anypassword", malformed);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_HashWithWrongAlgorithmPrefix_ReturnsFalse()
    {
        // Arrange — valid format but wrong algorithm name
        string hash = "$bcrypt$v=19$m=65536,t=4,p=1$c2FsdA==$aGFzaA==";

        // Act
        bool result = _sut.Verify("password", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_TamperedHash_ReturnsFalse()
    {
        // Arrange
        string password = "OriginalPassword";
        string hash = _sut.Hash(password);

        // Tamper with the last character of the hash
        string tampered = hash[..^1] + (hash[^1] == 'A' ? 'B' : 'A');

        // Act
        bool result = _sut.Verify(password, tampered);

        // Assert
        result.Should().BeFalse();
    }
}
