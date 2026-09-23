using Microsoft.Extensions.Logging;
using System.Text.Json;
using WalletSystem.Application.Contracts.Services.Cache;

namespace WalletSystem.Infrastructure.Cache.Providers;

/// <summary>
/// <see cref="ICacheProvider"/> that persists cache entries as JSON files on disk.
/// Each key maps to a single <c>.json</c> file inside <see cref="BaseDirectory"/>.
/// </summary>
public sealed class FileCacheProvider : ICacheProvider
{
    private readonly string _baseDirectory;
    private readonly ILogger<FileCacheProvider>? _logger;

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Creates a new <see cref="FileCacheProvider"/>.
    /// </summary>
    /// <param name="baseDirectory">
    /// Root directory where cache files are stored.
    /// Defaults to <c>{TempPath}/wallet_cache</c> when null or empty.
    /// </param>
    public FileCacheProvider(string? baseDirectory = null, ILogger<FileCacheProvider>? logger = null)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.Combine(Path.GetTempPath(), "wallet_cache")
            : baseDirectory;

        _logger = logger;

        Directory.CreateDirectory(_baseDirectory);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var wrapper = await ReadWrapperAsync<T>(filePath, ct);
            if (wrapper is null || wrapper.IsExpired())
            {
                File.Delete(filePath);
                return null;
            }

            return wrapper.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read cache file for key '{Key}'.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var expiry = DateTimeOffset.UtcNow.Add(ttl ?? DefaultTtl);
        var wrapper = new CacheEntry<T>(value, expiry);

        var filePath = GetFilePath(key);
        var json = JsonSerializer.Serialize(wrapper);

        try
        {
            await File.WriteAllTextAsync(filePath, json, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to write cache file for key '{Key}'.", key);
        }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var filePath = GetFilePath(key);
        TryDeleteFile(filePath);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        // File names are base64url-encoded keys; enumerate all and match decoded names.
        foreach (var file in Directory.EnumerateFiles(_baseDirectory, "*.json"))
        {
            var decodedKey = DecodeKey(Path.GetFileNameWithoutExtension(file));
            if (decodedKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                TryDeleteFile(file);
            }
        }

        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return false;
        }

        // Check expiry without full deserialization
        try
        {
            var wrapper = await ReadWrapperAsync<object>(filePath, ct);
            if (wrapper is null || wrapper.IsExpired())
            {
                TryDeleteFile(filePath);
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        foreach (var file in Directory.EnumerateFiles(_baseDirectory, "*.json"))
        {
            TryDeleteFile(file);
        }

        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private string GetFilePath(string key)
    {
        // Encode the key so it is a valid filename on all OSes.
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key))
                             .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return Path.Combine(_baseDirectory, $"{encoded}.json");
    }

    private static string DecodeKey(string encodedFileName)
    {
        try
        {
            var base64 = encodedFileName.Replace('-', '+').Replace('_', '/');
            // Re-add padding
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static async Task<CacheEntry<T>?> ReadWrapperAsync<T>(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<CacheEntry<T>>(stream, cancellationToken: ct);
    }

    private void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not delete cache file '{File}'.", filePath);
        }
    }
}
