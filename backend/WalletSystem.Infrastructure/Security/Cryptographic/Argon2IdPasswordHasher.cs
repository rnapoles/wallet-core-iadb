using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;
using WalletSystem.Application.Contracts.Services.Security;

namespace WalletSystem.Infrastructure.Security.Cryptographic;

/// <summary>
/// Argon2id password hasher based on the well-known <c>Isopoh.Cryptography.Argon2</c>
/// library (a pure managed port of the reference Argon2 implementation).
/// Add to the API container with:
/// <code>services.AddScoped&lt;IPasswordHasher, Argon2IdPasswordHasher&gt;();</code>
/// Hash format (PHC string): <c>$argon2id$v=19$m=&lt;KiB&gt;,t=&lt;iters&gt;,p=&lt;lanes&gt;$&lt;saltB64&gt;$&lt;tagB64&gt;</c>
/// </summary>
public class Argon2IdPasswordHasher : IPasswordHasher
{
    // OWASP-recommended parameters for Argon2id: 19 MiB memory, 2 iterations, 1 lane.
    // Adjust upward for your hardware budget; verification cost scales linearly with t and p.
    private const int DegreeOfParallelism = 1;
    private const int MemorySizeInKib = 19 * 1024; // 19 MiB
    private const int Iterations = 2;
    private const int SaltSizeInBytes = 16;
    private const int KeyLengthInBytes = 32;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        byte[] tag = ComputeTag(password, salt, MemorySizeInKib, Iterations, DegreeOfParallelism, KeyLengthInBytes);

        return string.Concat(
            "$argon2id$v=19",
            $"$m={MemorySizeInKib},t={Iterations},p={DegreeOfParallelism}",
            "$", Convert.ToBase64String(salt),
            "$", Convert.ToBase64String(tag));
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            // Expected shape: $argon2id$v=19$m=...,t=...,p=...$salt$tag
            var parts = hash.Split('$');
            // parts[0] = "" (leading $), [1] = argon2id, [2] = v=19, [3] = params, [4] = salt, [5] = tag
            if (parts.Length != 6 || parts[1] != "argon2id" || parts[2] != "v=19")
                return false;

            int m = 0, t = 0, p = 0;
            foreach (var kvp in parts[3].Split(','))
            {
                var pair = kvp.Split('=');
                if (pair.Length != 2 || !int.TryParse(pair[1], out var value))
                    return false;
                switch (pair[0])
                {
                    case "m": m = value; break;
                    case "t": t = value; break;
                    case "p": p = value; break;
                    default: return false;
                }
            }

            if (m <= 0 || t <= 0 || p <= 0)
                return false;

            byte[] salt = Convert.FromBase64String(parts[4]);
            byte[] expectedTag = Convert.FromBase64String(parts[5]);

            byte[] actualTag = ComputeTag(password, salt, m, t, p, expectedTag.Length);

            return CryptographicOperations.FixedTimeEquals(actualTag, expectedTag);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] ComputeTag(string password, byte[] salt, int memoryKib, int iterations, int lanes, int tagLength)
    {
        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing, // Argon2id
            Version = Argon2Version.Nineteen,   // v=19
            MemoryCost = memoryKib,
            TimeCost = iterations,
            Lanes = lanes,
            Threads = 1,
            Salt = salt,
            Password = Encoding.UTF8.GetBytes(password),
            HashLength = tagLength,
            ClearPassword = true, // the library zeroes the password buffer for us
        };

        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();
        return (byte[])hash.Buffer.Clone();
    }
}
