namespace Business.Interfaces.Services
{
    public interface ITokenGenerator
    {
        /// <summary>
        /// Generuje kryptograficznie losowy token w formacie Base64Url bez paddingu.
        /// </summary>
        /// <param name="sizeBytes">Liczba bajtów entropii (domyœlnie 32 = 256 bitów).</param>
        string GenerateToken(int sizeBytes = 32);
    }
}
