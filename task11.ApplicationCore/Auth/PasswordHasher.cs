using System.Security.Cryptography;

namespace task11.ApplicationCore.Auth;

/// <summary>
/// PBKDF2 (Rfc2898) password hasher using SHA-256 with a per-user salt.
/// Stored format: <c>{iterations}.{saltBase64}.{hashBase64}</c>.
/// </summary>
public sealed class PasswordHasher
{
    private const int _iterations = 100_000;
    private const int _saltSize = 16;   // 128-bit
    private const int _keySize = 32;    // 256-bit
    private const int _hashPartCount = 3;
    private static readonly HashAlgorithmName _algorithm = HashAlgorithmName.SHA256;

    /// <summary>Hashes a plaintext password into the stored format.</summary>
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = RandomNumberGenerator.GetBytes(_saltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, _algorithm, _keySize);

        return string.Join('.',
            _iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    /// <summary>Verifies a plaintext password against a stored hash. Constant-time comparison.</summary>
    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        string[] parts = storedHash.Split('.', _hashPartCount);
        if (parts.Length != _hashPartCount
            || !int.TryParse(parts[0], out int iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, _algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
