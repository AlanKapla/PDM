using Business.Implementation.Utilities;
using FluentAssertions;

namespace Business.Tests.Utilities;

public sealed class UtcDateTimeHelperTests
{
    [Fact]
    public void ToUtcOffset_WhenUnspecifiedKind_TreatsValueAsUtc()
    {
        // Arrange
        DateTime unspecified = new DateTime(2026, 7, 14, 9, 47, 0, DateTimeKind.Unspecified);

        // Act
        DateTimeOffset result = UtcDateTimeHelper.ToUtcOffset(unspecified);

        // Assert
        result.Offset.Should().Be(TimeSpan.Zero);
        result.UtcDateTime.Should().Be(unspecified);
    }

    [Fact]
    public void ToUtcOffset_WhenUtcKind_PreservesInstant()
    {
        // Arrange
        DateTime utc = new DateTime(2026, 7, 14, 9, 47, 0, DateTimeKind.Utc);

        // Act
        DateTimeOffset result = UtcDateTimeHelper.ToUtcOffset(utc);

        // Assert
        result.Offset.Should().Be(TimeSpan.Zero);
        result.UtcDateTime.Should().Be(utc);
    }
}
