namespace WalletSystem.Demo.Client;

/// <summary>
/// Shared configuration for the WalletSystem Demo Client
/// </summary>
public static class ClientConfiguration
{
    /// <summary>
    /// The base URL for the API server
    /// Can be set via command-line arguments or environment variable
    /// </summary>
    public static string ApiBaseUrl { get; set; } = "http://localhost:5000/";

    /// <summary>
    /// Initialize the configuration from command-line args or environment
    /// </summary>
    public static void Initialize(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            ApiBaseUrl = args[0];
        }
        else
        {
            var envUrl = Environment.GetEnvironmentVariable("WALLET_API_URL");
            if (!string.IsNullOrWhiteSpace(envUrl))
            {
                ApiBaseUrl = envUrl;
            }
        }

        // Ensure trailing slash
        if (!ApiBaseUrl.EndsWith("/"))
        {
            ApiBaseUrl += "/";
        }
    }
}
