using System.Security.Cryptography;
using WalletSystem.Application.Contracts.Services.Security;

namespace WalletSystem.Infrastructure.Security.Cryptographic;

/// <summary>
/// PBKDF2 (RFC 8018 / PKCS#5 v2.1) password hasher built entirely on the .NET BCL
/// (<see cref="Rfc2898DeriveBytes"/> with HMAC-SHA256), so it adds no third-party dependency.
/// Add to the API container with:
/// <code>services.AddScoped&lt;IPasswordHasher, Pbkdf2PasswordHasher&gt;();</code>
/// Hash format (PHC-style string):
/// <c>$pbkdf2-sha256$i=&lt;iterations&gt;$&lt;saltB64&gt;$&lt;derivedKeyB64&gt;</c>
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    // OWASP recommends >= 600,000 iterations for PBKDF2-HMAC-SHA256 (2023 guidance).
    // Raise this as your latency budget allows; hash cost scales linearly with the iteration count.
    private const int Iterations = 600_000;
    private const int SaltSizeInBytes = 16;
    private const int KeyLengthInBytes = 32;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        byte[] key = DeriveKey(password, salt, Iterations, KeyLengthInBytes);

        return string.Concat(
            "$pbkdf2-sha256$",
            $"i={Iterations}",
            "$", Convert.ToBase64String(salt),
            "$", Convert.ToBase64String(key));
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            // Expected shape: $pbkdf2-sha256$i=...$salt$key
            var parts = hash.Split('$');
            // parts[0] = "" (leading $), [1] = pbkdf2-sha256, [2] = i=..., [3] = salt, [4] = key
            if (parts.Length != 5 || parts[1] != "pbkdf2-sha256")
                return false;

            var paramPair = parts[2].Split('=');
            if (paramPair.Length != 2 || paramPair[0] != "i" || !int.TryParse(paramPair[1], out var iterations) || iterations <= 0)
                return false;

            byte[] salt = Convert.FromBase64String(parts[3]);
            byte[] expectedKey = Convert.FromBase64String(parts[4]);

            byte[] actualKey = DeriveKey(password, salt, iterations, expectedKey.Length);

            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt, int iterations, int keyLength)
    {
        // Rfc2898DeriveBytes.Pbkdf2 (static overload) is the non-obsolete BCL API on .NET 10.
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keyLength);
    }
}
