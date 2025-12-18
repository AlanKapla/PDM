using System.Security.Cryptography;
using System.Text;
using Business.Interfaces.Services;

namespace Business.Implementation.Services
{
    public class TokenGenerator : ITokenGenerator
    {
        public string GenerateToken(int sizeBytes = 32)
        {
            byte[] bytes = new byte[sizeBytes];
            RandomNumberGenerator.Fill(bytes);
            // Base64Url bez paddingu
            string token = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
            return token;
        }
    }
}
