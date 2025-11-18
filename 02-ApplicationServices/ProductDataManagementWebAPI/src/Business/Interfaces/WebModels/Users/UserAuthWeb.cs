public sealed record UserAuthWeb(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string? RefreshToken = null,
    DateTime? RefreshTokenExpiresAt = null
);
