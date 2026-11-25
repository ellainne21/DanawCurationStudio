using System.Security.Cryptography;
using Danaw.Core.Abstractions;

namespace Danaw.Core.Services;

// Stores passwords as "PBKDF2-SHA256$iterations$salt$hash". Every password gets its own
// random salt, so two identical passwords never produce the same stored value.
public sealed class PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2-SHA256";
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        try
        {
            var parts = (storedHash ?? string.Empty).Split('$');
            if (parts.Length != 4 || parts[0] != Prefix) return false;
            if (!int.TryParse(parts[1], out var iterations) || iterations < 1 || iterations > 10_000_000) return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            // Constant-time comparison, so response time does not reveal how much matched.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false; // a damaged stored value is treated as "does not match"
        }
    }
}