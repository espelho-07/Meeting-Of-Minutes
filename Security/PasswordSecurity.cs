using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace Meeting_Of_Minutes.Security
{
    public static class PasswordSecurity
    {
        private static readonly PasswordHasher<string> Hasher = new PasswordHasher<string>();

        public static string HashPassword(string password)
        {
            return Hasher.HashPassword(string.Empty, password);
        }

        public static bool VerifyPassword(string? passwordHash, string? legacyPassword, string providedPassword, out bool needsUpgrade)
        {
            needsUpgrade = false;

            if (!string.IsNullOrWhiteSpace(passwordHash))
            {
                PasswordVerificationResult result = Hasher.VerifyHashedPassword(string.Empty, passwordHash, providedPassword);
                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    needsUpgrade = true;
                    return true;
                }

                return result == PasswordVerificationResult.Success;
            }

            if (!string.IsNullOrEmpty(legacyPassword) && string.Equals(legacyPassword, providedPassword, StringComparison.Ordinal))
            {
                needsUpgrade = true;
                return true;
            }

            return false;
        }

        public static string GenerateTemporaryPassword(int length = 14)
        {
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowercase = "abcdefghijkmnopqrstuvwxyz";
            const string numbers = "23456789";
            const string symbols = "!@#$%^&*";
            string all = uppercase + lowercase + numbers + symbols;

            List<char> chars = new List<char>
            {
                GetRandomChar(uppercase),
                GetRandomChar(lowercase),
                GetRandomChar(numbers),
                GetRandomChar(symbols)
            };

            while (chars.Count < Math.Max(length, 8))
            {
                chars.Add(GetRandomChar(all));
            }

            return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
        }

        private static char GetRandomChar(string source)
        {
            return source[RandomNumberGenerator.GetInt32(source.Length)];
        }
    }
}
