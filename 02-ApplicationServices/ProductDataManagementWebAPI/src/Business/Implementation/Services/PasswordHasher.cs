using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Konscious.Security.Cryptography;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Business.Implementation.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 4;
        private const int MemorySizeKb = 65536;
        private static readonly int Parallelism = Math.Max(1, Environment.ProcessorCount);

        public string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, "Password must not be empty.", nameof(password));
            }

            byte[] salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);       

            byte[] hash = ComputeArgon2id(password, salt, MemorySizeKb, Iterations, Parallelism, HashSize);

            string saltB64 = Convert.ToBase64String(salt);
            string hashB64 = Convert.ToBase64String(hash);

            string encoded = $"$argon2id$v=19$m={MemorySizeKb},t={Iterations},p={Parallelism}${saltB64}${hashB64}";
            return encoded;
        }

        public bool Verify(string password, string encodedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encodedHash))
            {
                return false;
            }

            try
            {
                string[] parts = encodedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 5)
                {
                    return false;
                }

                if (!parts[0].Equals("argon2id", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string paramsPart = parts[2];

                int mem = MemorySizeKb;
                int iter = Iterations;
                int par = Parallelism;

                string[] paramPairs = paramsPart.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (string pair in paramPairs)
                {
                    string[] kv = pair.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length != 2)
                    {
                        continue;
                    }

                    string key = kv[0];
                    string val = kv[1];
                    if (key == "m" && int.TryParse(val, out int mm))
                    {
                        mem = mm;
                    }

                    if (key == "t" && int.TryParse(val, out int tt))
                    {
                        iter = tt;
                    }

                    if (key == "p" && int.TryParse(val, out int pp))
                    {
                        par = pp;
                    }
                }

                byte[] salt = Convert.FromBase64String(parts[3]);
                byte[] expectedHash = Convert.FromBase64String(parts[4]);

                byte[] computed = ComputeArgon2id(password, salt, mem, iter, par, expectedHash.Length);

                bool equals = FixedTimeEquals(computed, expectedHash);
                return equals;
            }
            catch (FormatException)
            {
                // The encodedHash format is invalid
                return false;
            }
            catch (ArgumentException)
            {
                // Argument was invalid (e.g., base64 decode failed)
                return false;
            }
            catch (OverflowException)
            {
                // Numeric conversion failed (e.g., parsing int from string)
                return false;
            }
        }

        private static byte[] ComputeArgon2id(string password, byte[] salt, int memoryKb, int iterations, int parallelism, int outputBytes)
        {
            using Argon2id argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = parallelism,
                Iterations = iterations,
                MemorySize = memoryKb
            };

            byte[] result = argon2.GetBytes(outputBytes);
            return result;
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}