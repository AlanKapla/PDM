namespace Business.Interfaces.Model
{
    public sealed record TokenDto(string Token, DateTime ExpiredAt);
}