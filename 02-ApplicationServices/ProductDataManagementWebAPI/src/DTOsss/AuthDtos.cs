namespace DTOs;
public record AuthResponseDto(string Token, DateTime ExpiresAt, UserDto User);
public record UserDto(int Id, string? Username, string? Email);
