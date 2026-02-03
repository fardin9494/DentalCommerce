using System.Text;

namespace Identity.Domain.Users;

public static class PhoneNumber
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Phone number is required.", nameof(input));

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsDigit(ch))
                sb.Append(ch);
        }

        var digits = sb.ToString();
        if (digits.Length == 0)
            throw new ArgumentException("Phone number is invalid.", nameof(input));

        // Iran common formats:
        //  - 09xxxxxxxxx
        //  - 9xxxxxxxxx
        //  - 98xxxxxxxxxx
        // We normalize to 98xxxxxxxxxx (no plus) to keep it simple and consistent.
        if (digits.StartsWith("00"))
            digits = digits[2..];

        if (digits.StartsWith("0"))
            digits = digits[1..];

        if (!digits.StartsWith("98"))
            digits = "98" + digits;

        return digits;
    }
}

