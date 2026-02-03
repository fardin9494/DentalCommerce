using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Common.Security;

internal static class Crypto
{
    public static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    public static string GenerateNumericCode(int length)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

        // Avoid modulo bias by generating enough entropy then mapping to digits.
        var bytes = RandomBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = (char)('0' + (bytes[i] % 10));
        return new string(chars);
    }

    public static byte[] Sha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return SHA256.HashData(bytes);
    }

    public static byte[] Sha256(byte[] input) => SHA256.HashData(input);

    public static byte[] ComputeSaltedCodeHash(string code, byte[] salt)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code required.", nameof(code));
        if (salt.Length == 0) throw new ArgumentException("Salt required.", nameof(salt));

        var codeBytes = Encoding.UTF8.GetBytes(code.Trim());
        var payload = new byte[salt.Length + codeBytes.Length];
        Buffer.BlockCopy(salt, 0, payload, 0, salt.Length);
        Buffer.BlockCopy(codeBytes, 0, payload, salt.Length, codeBytes.Length);
        return SHA256.HashData(payload);
    }

    public static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    public static string Base64UrlEncode(byte[] bytes)
    {
        var s = Convert.ToBase64String(bytes);
        s = s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return s;
    }

    public static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}

