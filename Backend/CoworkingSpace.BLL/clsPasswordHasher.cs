using System;
using System.Security.Cryptography;
using System.Text;

namespace CoworkingSpace.BLL
{
    /// <summary>
    /// Password hashing with PBKDF2-HMAC-SHA256.
    /// New hashes are stored as "PBKDF2$SHA256${iterations}${base64 hash}" with the salt in PasswordSalt.
    /// Hashes created before this change (a single SHA-256 of password + salt, no prefix) still verify;
    /// callers should re-hash them after a successful login (see <see cref="NeedsRehash"/>).
    /// </summary>
    public class clsPasswordHasher
    {
        private const string Prefix = "PBKDF2$SHA256$";
        private const int Iterations = 600_000;   // OWASP recommendation for PBKDF2-HMAC-SHA256
        private const int HashSize = 32;          // bytes
        private const int SaltSize = 16;          // bytes

        public static string GenerateSalt()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
        }

        public static string ComputeHash(string password, string salt)
        {
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                Encoding.UTF8.GetBytes(salt),
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return $"{Prefix}{Iterations}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(storedHash) || storedSalt == null) return false;

            if (!storedHash.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return FixedTimeEquals(ComputeLegacyHash(enteredPassword, storedSalt), storedHash);
            }

            // Read the iteration count from the stored value so it can be raised later without breaking old hashes.
            string[] parts = storedHash.Substring(Prefix.Length).Split('$');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int iterations) || iterations <= 0) return false;

            byte[] expected;
            try { expected = Convert.FromBase64String(parts[1]); }
            catch (FormatException) { return false; }

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(enteredPassword),
                Encoding.UTF8.GetBytes(storedSalt),
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        /// <summary>True when the stored hash uses the old format or fewer iterations than today's setting.</summary>
        public static bool NeedsRehash(string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || !storedHash.StartsWith(Prefix, StringComparison.Ordinal)) return true;
            string[] parts = storedHash.Substring(Prefix.Length).Split('$');
            return parts.Length != 2 || !int.TryParse(parts[0], out int iterations) || iterations < Iterations;
        }

        // Old scheme: Base64(SHA256(password + salt)). Kept only to verify existing accounts.
        private static string ComputeLegacyHash(string password, string salt)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
            return Convert.ToBase64String(bytes);
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
        }
    }
}
