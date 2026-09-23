using Microsoft.Extensions.Configuration;

namespace WalletSystem.Shared.Settings.Extensions;

/// <summary>
/// Extension methods for adding .env file configuration to IConfigurationBuilder.
/// Configuration priority (highest to lowest):
/// 1. Environment Variables
/// 2. .env file
/// 3. appsettings.json
/// </summary>
public static class EnviromentExtensions
{
    /// <summary>
    /// Adds .env file configuration to the IConfigurationBuilder.
    /// This should be called AFTER AddJsonFile("appsettings.json") and BEFORE AddEnvironmentVariables()
    /// to ensure proper priority ordering.
    /// </summary>
    public static IConfigurationBuilder AddEnvironmentFile(this IConfigurationBuilder configurationBuilder)
    {
        return AddEnvironmentFile(configurationBuilder, Directory.GetCurrentDirectory());
    }

    /// <summary>
    /// Adds .env file configuration to the IConfigurationBuilder with a specified base directory.
    /// This should be called AFTER AddJsonFile("appsettings.json") and BEFORE AddEnvironmentVariables()
    /// to ensure proper priority ordering.
    /// </summary>
    public static IConfigurationBuilder AddEnvironmentFile(this IConfigurationBuilder configurationBuilder, string basePath)
    {
        // Find .env file in current directory or parent directory
        var envPath = Path.Combine(basePath, ".env");
        if (!File.Exists(envPath))
        {
            envPath = Path.Combine(basePath, "..", ".env");
        }

        if (File.Exists(envPath))
        {
            // Parse .env file and add to configuration
            var envConfig = new Dictionary<string, string?>();
            
            try
            {
                // Read and parse .env file manually to integrate with IConfiguration
                var lines = File.ReadAllLines(envPath);
                foreach (var line in lines)
                {
                    // Skip empty lines and comments
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    {
                        continue;
                    }

                    // Parse KEY=VALUE format
                    var separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        var key = trimmedLine.Substring(0, separatorIndex).Trim();
                        var value = trimmedLine.Substring(separatorIndex + 1).Trim();
                        
                        // Remove surrounding quotes if present
                        if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                            (value.StartsWith("'") && value.EndsWith("'")))
                        {
                            value = value[1..^1];
                        }

                        // Convert dotenv keys to IConfiguration compatible keys
                        // e.g., DATABASE__CONNECTION_STRING -> Database:ConnectionString
                        var configKey = key.Replace("__", ":");
                        
                        if (!string.IsNullOrEmpty(configKey) && !envConfig.ContainsKey(configKey))
                        {
                            envConfig[configKey] = value;
                        }
                    }
                }

                // Add .env configuration with lower priority than environment variables
                configurationBuilder.AddInMemoryCollection(envConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse .env file at {envPath}: {ex.Message}");
            }
        }

        return configurationBuilder;
    }
}