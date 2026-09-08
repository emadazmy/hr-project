using System.Security.Cryptography;

namespace HR_ERP.Data
{
    /// <summary>
    /// Password hashing for the Users table. Uses PBKDF2-HMAC-SHA256 with a random
    /// per-user salt — never stores or compares plain-text passwords. The default
    /// seeded "admin" user's hash in the database scripts was generated with these
    /// exact parameters, so don't change SaltSize/HashSize/Iterations without
    /// re-hashing that seed row too.
    /// </summary>
    public static class AuthHelper
    {
        private const int SaltSize = 16;   // bytes
        private const int HashSize = 32;   // bytes
        private const int Iterations = 100_000;

        public static (string HashBase64, string SaltBase64) HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public static bool VerifyPassword(string password, string hashBase64, string saltBase64)
        {
            var salt = Convert.FromBase64String(saltBase64);
            var expected = Convert.FromBase64String(hashBase64);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
