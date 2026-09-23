using System.Net.Http.Json;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Shared.Commands;

/// <summary>
/// Command to login a user and get JWT token
/// </summary>
public class LoginCommand : ICommand<LoginResult>
{
    private readonly HttpClient _httpClient;
    private readonly string _email;
    private readonly string _password;

    public LoginCommand(HttpClient httpClient, string email, string password)
    {
        _httpClient = httpClient;
        _email = email;
        _password = password;
    }

    public async Task<LoginResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            Email = _email,
            Password = _password
        };

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.PostAsJsonAsync("api/auth/login", requestBody, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Login failed for {_email} with status {response.StatusCode}: {errorContent}",
                    null,
                    response.StatusCode
                );
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Failed to deserialize login response");

            return new LoginResult(result.Token, result.RefreshToken,
                new UserInfo(result.UserId, result.Email, string.Empty, []));
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Login command failed for {_email}: {ex.Message}", ex);
        }
    }
}


