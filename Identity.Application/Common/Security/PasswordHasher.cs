using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Common.Security;

internal static class PasswordHasher
{
    public const string Algorithm = "PBKDF2-SHA256";

    public static (byte[] Hash, byte[] Salt, int Iterations, string Algorithm) HashPassword(string password, int iterations)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password required.", nameof(password));
        if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations));

        var salt = Crypto.RandomBytes(16);
        var hash = Pbkdf2(password, salt, iterations);
        return (hash, salt, iterations, Algorithm);
    }

    public static bool Verify(string password, byte[] salt, byte[] expectedHash, int iterations)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (salt.Length == 0 || expectedHash.Length == 0 || iterations <= 0) return false;

        var computed = Pbkdf2(password, salt, iterations);
        return Crypto.FixedTimeEquals(computed, expectedHash);
    }

    private static byte[] Pbkdf2(string password, byte[] salt, int iterations)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        return Rfc2898DeriveBytes.Pbkdf2(bytes, salt, iterations, HashAlgorithmName.SHA256, 32);
    }
}

